/// <summary>
/// Patrulla por waypoints usando A*. Si ve al Cazador pasa a Chase y si otro
/// lobo aulla pasa a Alert.
/// </summary>
public class WolfPatrolState : WolfStateBase
{
    private int _index;
    private bool _hasDestination;

    public WolfPatrolState(WolfAgent wolf) : base(wolf) { }

    public override void Enter()
    {
        Wolf.LastAction = "Patrulla";
        _index = Wolf.NearestWaypointIndex(Wolf.transform.position);
        _hasDestination = false;
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

        PatrolRoute route = Wolf.PatrolRoute;

        if (route == null || route.Count == 0)
        {
            Wolf.Stop();
            return;
        }

        if (_index < 0 || _index >= route.Count) _index = 0;

        UnityEngine.Vector3 waypoint = route.Waypoints[_index].position;

        if (!_hasDestination)
        {
            _hasDestination = Wolf.GoTo(waypoint, true);

            if (!_hasDestination)
                Wolf.MoveTowards(waypoint, Wolf.PatrolSpeed);
        }
        else
        {
            Wolf.MoveAlongPath(Wolf.PatrolSpeed);
        }

        if (Wolf.DistanceTo(waypoint) <= route.WaypointCheckDistance)
        {
            _index = (_index + 1) % route.Count;
            _hasDestination = false;
        }
    }

    public override void Exit() { }
}
