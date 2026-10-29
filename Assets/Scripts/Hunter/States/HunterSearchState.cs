using UnityEngine;

/// <summary>
/// Busqueda del Parcial 2: recorre la ruta de patrulla con A*, sin generar
/// cebos. Las transiciones de prioridad (Flee, Exit, Gather, Attack) se
/// evaluan antes de moverse.
/// </summary>
public class HunterSearchState : HunterStateBase
{
    private int _index;
    private bool _hasDestination;
    private bool _wolfVisible;

    public HunterSearchState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Hunter.LastAction = "Buscando";
        _index = SelectWaypointIndex();
        _hasDestination = false;
        _wolfVisible = HasVisibleWolf;
    }

    public override void Update()
    {
        if (CheckPriorityTransitions()) return;

        PatrolRoute route = Hunter.PatrolRoute;

        if (route == null || route.Count == 0)
        {
            Stop();
            return;
        }

        // Al detectar un lobo durante el compromiso de salida se elige el
        // waypoint mas lejano para romper el contacto en vez de acercarse.
        bool wolfVisible = HasVisibleWolf;

        if (wolfVisible && !_wolfVisible)
        {
            _index = SelectWaypointIndex();
            _hasDestination = false;
        }

        _wolfVisible = wolfVisible;

        if (_index < 0 || _index >= route.Count) _index = 0;

        Vector3 waypoint = route.Waypoints[_index].position;

        if (!_hasDestination)
        {
            _hasDestination = Hunter.Nav != null && Hunter.Nav.SetDestination(waypoint, true);

            if (!_hasDestination)
                MoveTowards(waypoint);
        }
        else
        {
            NavigateTowards(waypoint);
        }

        if (Vector3.Distance(Hunter.transform.position, waypoint) <= route.WaypointCheckDistance)
        {
            _index = (_index + 1) % route.Count;
            _hasDestination = false;
        }
    }

    public override void Exit() { }

    private bool HasVisibleWolf => Hunter.WolfSensor != null && Hunter.WolfSensor.HasVisibleWolf;

    private int SelectWaypointIndex()
    {
        PatrolRoute route = Hunter.PatrolRoute;
        if (route == null || route.Count == 0) return 0;

        WolfAgent wolf = Hunter.WolfSensor != null ? Hunter.WolfSensor.NearestWolf : null;

        if (wolf == null)
            return NearestWaypointIndex();

        int best = 0;
        float bestScore = float.MinValue;

        for (int i = 0; i < route.Count; i++)
        {
            Vector3 waypoint = route.Waypoints[i].position;
            float fromWolf = (waypoint - wolf.transform.position).sqrMagnitude;
            float fromHunter = (waypoint - Hunter.transform.position).sqrMagnitude;

            // Prioriza alejarse del lobo; el segundo termino desempata por cercania.
            float score = fromWolf - fromHunter * 0.15f;

            if (score <= bestScore) continue;

            bestScore = score;
            best = i;
        }

        return best;
    }

    private int NearestWaypointIndex()
    {
        PatrolRoute route = Hunter.PatrolRoute;
        if (route == null || route.Count == 0) return 0;

        int best = 0;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < route.Count; i++)
        {
            float sqr = (route.Waypoints[i].position - Hunter.transform.position).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            best = i;
        }

        return best;
    }
}
