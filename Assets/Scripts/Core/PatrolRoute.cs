using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenedor de waypoints. Toma automáticamente los hijos del objeto
/// para evitar listas manuales en el cazador.
/// </summary>
public class PatrolRoute : MonoBehaviour
{
    public enum RouteMode
    {
        PingPong,
        Loop
    }

    [SerializeField] private RouteMode _mode = RouteMode.PingPong;
    [SerializeField] private float _waypointCheckDistance = 0.5f;
    [SerializeField] private List<Transform> _waypoints = new();

    public RouteMode Mode => _mode;
    public float WaypointCheckDistance => _waypointCheckDistance;
    public IReadOnlyList<Transform> Waypoints => _waypoints;
    public int Count => _waypoints.Count;

    private void Awake()
    {
        _waypoints ??= new List<Transform>();

        if (_waypoints.Count == 0) RefreshWaypoints();
    }

    [ContextMenu("Refresh Waypoints")]
    public void RefreshWaypoints()
    {
        _waypoints ??= new List<Transform>();
        _waypoints.Clear();

        foreach (Transform child in transform)
            _waypoints.Add(child);
    }

    private void OnValidate()
    {
        if (_waypoints == null) _waypoints = new List<Transform>();
        if (_waypoints.Count == 0) RefreshWaypoints();
    }

    private void OnDrawGizmos()
    {
        if (!FeedbackManager.ShowGizmos) return;
        if (_waypoints == null || _waypoints.Count == 0) return;

        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);

        for (int i = 0; i < _waypoints.Count; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null) continue;

            Gizmos.DrawWireSphere(waypoint.position, 0.35f);

            if (i + 1 < _waypoints.Count && _waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoint.position, _waypoints[i + 1].position);
        }

        if (_mode == RouteMode.Loop && _waypoints.Count > 1 && _waypoints[0] != null && _waypoints[_waypoints.Count - 1] != null)
            Gizmos.DrawLine(_waypoints[_waypoints.Count - 1].position, _waypoints[0].position);
    }
}
