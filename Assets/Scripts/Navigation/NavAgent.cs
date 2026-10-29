using UnityEngine;

/// <summary>
/// Sigue un camino de A* sobre el grafo. Devuelve el punto hacia el que debe
/// moverse el agente: normalmente el siguiente nodo del camino y, en el tramo
/// final con linea de vision, el objetivo directo (steering directo).
/// </summary>
public class NavAgent
{
    private readonly Transform _transform;
    private readonly NavGraph _graph;
    private readonly bool _avoidSafeZones;
    private readonly float _arriveRadius;
    private readonly float _repathDistance;
    private readonly float _directRange;

    private readonly NavPath _path = new();

    private Vector3 _destination;
    private bool _hasDestination;
    private bool _reached;
    private int _index;

    public NavAgent(
        Transform transform,
        NavGraph graph,
        bool avoidSafeZones,
        float arriveRadius = 0.4f,
        float repathDistance = 1.5f,
        float directRange = 4f)
    {
        _transform = transform;
        _graph = graph;
        _avoidSafeZones = avoidSafeZones;
        _arriveRadius = Mathf.Max(0.05f, arriveRadius);
        _repathDistance = Mathf.Max(0.1f, repathDistance);
        _directRange = Mathf.Max(0.1f, directRange);
    }

    public NavPath Path => _path;
    public bool HasDestination => _hasDestination;
    public bool HasPath => _path.Found && _path.Points.Count > 0;
    public bool ReachedDestination => _reached;
    public Vector3 Destination => _destination;
    public int CurrentWaypointIndex => _index;

    public bool SetDestination(Vector3 destination, bool force = false)
    {
        bool closeEnough = _hasDestination
            && FlatDistance(destination, _destination) < _repathDistance;

        _destination = destination;
        _hasDestination = true;
        _reached = false;

        if (!force && closeEnough && HasPath) return true;

        _index = 0;

        if (_graph == null)
        {
            _path.Clear();
            return false;
        }

        if (!_graph.GetPath(_transform.position, destination, _avoidSafeZones, _path))
        {
            _path.Clear();
            return false;
        }

        return true;
    }

    public void ClearDestination()
    {
        _hasDestination = false;
        _reached = false;
        _index = 0;
        _path.Clear();
    }

    public Vector3 GetMoveTarget()
    {
        Vector3 position = _transform.position;

        if (!_hasDestination) return position;

        float flatToDestination = FlatDistance(position, _destination);
        _reached = flatToDestination <= _arriveRadius;

        if (_reached) return _destination;

        bool canSteerDirect = flatToDestination <= _directRange
            && (_graph == null || _graph.HasLineOfSight(position, _destination))
            && (!_avoidSafeZones || _graph == null || !_graph.SegmentCrossesSafeZone(position, _destination));

        if (canSteerDirect) return _destination;

        while (_index < _path.Points.Count
            && FlatDistance(position, _path.Points[_index]) <= _arriveRadius)
        {
            _index++;
        }

        if (_index >= _path.Points.Count) return _destination;

        return _path.Points[_index];
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
