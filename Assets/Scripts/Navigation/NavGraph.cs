using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grafo de navegacion unico y compartido. Se hornea al iniciar con una grilla
/// y raycasts contra Obstacle, marca los nodos de zonas seguras y expone la
/// consulta de caminos A* y de linea de vision.
/// </summary>
[DisallowMultipleComponent]
public class NavGraph : MonoBehaviour
{
    public static NavGraph Instance { get; private set; }

    [Header("Area")]
    [SerializeField] private Vector3 _areaCenter = Vector3.zero;
    [SerializeField] private Vector2 _areaSize = new(44f, 32f);
    [SerializeField] private Vector3 _bakeSeed = new(-18.5f, 1f, 0f);

    [Header("Bake")]
    [SerializeField] private float _spacing = 1f;
    [SerializeField] private float _agentRadius = 0.45f;
    [SerializeField] private float _sampleHeight = 1f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private bool _bakeOnAwake = true;

    [Header("Gizmos")]
    [SerializeField] private bool _drawNodes = true;

    private readonly List<int> _nodeBuffer = new();

    public NavGraphData Data { get; private set; }
    public bool IsReady => Data != null && Data.IsValid;
    public int NodeCount => IsReady ? Data.Nodes.Length : 0;
    public int SafeNodeCount { get; private set; }
    public int WolfBlockedNodeCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (_bakeOnAwake) Bake();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Rehace el grafo. Los muros y zonas ya deben existir en la escena.
    /// </summary>
    public void Bake()
    {
        if (_obstacleMask.value == 0)
            _obstacleMask = GameLayers.Mask(GameLayers.Obstacle);

        Physics.SyncTransforms();

        Data = NavGraphBaker.Bake(
            _areaCenter,
            _areaSize,
            _spacing,
            _agentRadius,
            _sampleHeight,
            _obstacleMask,
            _bakeSeed);

        MarkSafeZones();
    }

    public bool GetPath(Vector3 start, Vector3 goal, bool avoidSafeZones, NavPath path)
    {
        path.Clear();

        if (!IsReady) return false;

        int startNode = FindNearestNode(start, avoidSafeZones);
        int goalNode = FindNearestNode(goal, avoidSafeZones);

        if (startNode < 0 || goalNode < 0) return false;
        if (!AStarPathfinder.FindPath(Data, startNode, goalNode, avoidSafeZones, _nodeBuffer))
            return false;

        path.Build(_nodeBuffer, Data, start);
        Smooth(path, avoidSafeZones);
        return true;
    }

    public int FindNearestNode(Vector3 position, bool avoidSafeZones)
    {
        if (!IsReady) return -1;

        NavNode[] nodes = Data.Nodes;
        int best = -1;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < nodes.Length; i++)
        {
            NavNode node = nodes[i];

            if (!node.Walkable) continue;
            if (avoidSafeZones && node.WolfBlocked) continue;

            float sqr = (node.Position - position).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            best = i;
        }

        return best;
    }

    public bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 a = new(from.x, _sampleHeight, from.z);
        Vector3 b = new(to.x, _sampleHeight, to.z);

        return !Physics.Linecast(a, b, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool SegmentCrossesSafeZone(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);
        int steps = Mathf.CeilToInt(distance / 0.5f);

        for (int i = 1; i < steps; i++)
        {
            Vector3 point = Vector3.Lerp(from, to, i / (float)steps);
            if (ZoneVolume.IsInsideWolfExcluded(point)) return true;
        }

        return false;
    }

    public void Configure(
        Vector3 areaCenter,
        Vector2 areaSize,
        float spacing,
        float agentRadius,
        float sampleHeight,
        LayerMask obstacleMask,
        Vector3 bakeSeed,
        bool bakeOnAwake = true)
    {
        _areaCenter = areaCenter;
        _areaSize = areaSize;
        _spacing = Mathf.Max(0.25f, spacing);
        _agentRadius = Mathf.Max(0.05f, agentRadius);
        _sampleHeight = sampleHeight;
        _obstacleMask = obstacleMask;
        _bakeSeed = bakeSeed;
        _bakeOnAwake = bakeOnAwake;
    }

    private void MarkSafeZones()
    {
        SafeNodeCount = 0;
        WolfBlockedNodeCount = 0;

        if (!IsReady) return;

        ZoneVolume[] zones = FindObjectsByType<ZoneVolume>(FindObjectsSortMode.None);
        NavNode[] nodes = Data.Nodes;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].Walkable) continue;

            bool inSafe = false;
            bool wolfBlocked = false;

            for (int z = 0; z < zones.Length; z++)
            {
                ZoneVolume zone = zones[z];
                if (zone == null) continue;
                if (!zone.Contains(nodes[i].Position)) continue;

                if (zone.Kind == ZoneKind.Safe)
                {
                    inSafe = true;
                    wolfBlocked = true;
                }
                else if (zone.Kind == ZoneKind.Initial)
                {
                    wolfBlocked = true;
                }
            }

            nodes[i].InSafeZone = inSafe;
            nodes[i].WolfBlocked = wolfBlocked;

            if (inSafe) SafeNodeCount++;
            if (wolfBlocked) WolfBlockedNodeCount++;
        }
    }

    /// <summary>
    /// Suaviza el camino saltando nodos intermedios cuando hay linea de
    /// vision. Si el agente debe evitar zonas seguras, tampoco permite que el
    /// atajo las cruce.
    /// </summary>
    private void Smooth(NavPath path, bool avoidSafeZones)
    {
        List<Vector3> points = path.MutablePoints;
        if (points.Count < 3) return;

        List<Vector3> smoothed = new() { points[0] };
        int current = 0;

        while (current < points.Count - 1)
        {
            int furthest = current + 1;

            for (int next = points.Count - 1; next > current + 1; next--)
            {
                if (!HasLineOfSight(points[current], points[next])) continue;
                if (avoidSafeZones && SegmentCrossesSafeZone(points[current], points[next])) continue;

                furthest = next;
                break;
            }

            smoothed.Add(points[furthest]);
            current = furthest;
        }

        points.Clear();
        points.AddRange(smoothed);
        path.RecalculateLength();
    }

    private void OnDrawGizmos()
    {
        if (!_drawNodes || !FeedbackManager.ShowGizmos) return;
        if (Data == null || Data.Nodes == null) return;

        NavNode[] nodes = Data.Nodes;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].Walkable) continue;

            int[] neighbors = nodes[i].Neighbors;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.22f);

            for (int j = 0; j < neighbors.Length; j++)
            {
                int next = neighbors[j];
                if (next <= i) continue;

                Gizmos.DrawLine(nodes[i].Position, nodes[next].Position);
            }
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].Walkable) continue;

            Gizmos.color = nodes[i].InSafeZone
                ? new Color(0.8f, 0.3f, 1f, 0.9f)
                : new Color(0.3f, 0.9f, 1f, 0.55f);

            Gizmos.DrawSphere(nodes[i].Position, 0.09f);
        }
    }
}
