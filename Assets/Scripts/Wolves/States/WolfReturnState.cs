using UnityEngine;

/// <summary>
/// Vuelve al waypoint de patrulla mas cercano. Si vuelve a ver al Cazador o
/// escucha otra alerta, retoma la persecucion o la busqueda.
/// </summary>
public class WolfReturnState : WolfStateBase
{
    private float _startTime;

    public WolfReturnState(WolfAgent wolf) : base(wolf) { }

    public override void Enter()
    {
        Wolf.LastAction = "Regreso";
        _startTime = Time.time;

        PatrolRoute route = Wolf.PatrolRoute;

        if (route == null || route.Count == 0) return;

        int index = Wolf.NearestWaypointIndex(Wolf.transform.position);
        Wolf.GoTo(route.Waypoints[index].position, true);
    }

    public override void Update()
    {
        if (Wolf.SeesHunter)
        {
            _fsm.ChangeState(WolfAgent.States.Chase);
            return;
        }

        if (Wolf.HasPackAlert)
        {
            _fsm.ChangeState(WolfAgent.States.Alert);
            return;
        }

        Wolf.MoveAlongPath(Wolf.PatrolSpeed);

        if (Wolf.Nav.ReachedDestination || Time.time - _startTime > 8f)
            _fsm.ChangeState(WolfAgent.States.Patrol);
    }

    public override void Exit() { }
}
