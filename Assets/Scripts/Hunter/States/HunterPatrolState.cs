using UnityEngine;

public class HunterPatrolState : HunterStateBase
{
    private int _currentIndex;
    private bool _reverse;

    public HunterPatrolState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Hunter.InterestSpawner.BeginPatrolSpawning();
    }

    public override void Update()
    {
        Patrol();

        if (Hunter.Sensor.HasInactiveBoids)
        {
            _fsm.ChangeState(HunterAgent.States.Gather);
            return;
        }

        if (Hunter.CanAttack() && Hunter.Sensor.HasActiveBoids)
            _fsm.ChangeState(HunterAgent.States.Attack);
    }

    public override void Exit()
    {
        Hunter.InterestSpawner.StopSpawning();
    }

    private void Patrol()
    {
        PatrolRoute route = Hunter.PatrolRoute;
        if (route == null || route.Count == 0) return;

        if (_currentIndex >= route.Count) _currentIndex = 0;
        if (_currentIndex < 0) _currentIndex = 0;

        Transform target = route.Waypoints[_currentIndex];
        if (target == null) return;

        if (Vector3.Distance(target.position, Hunter.transform.position) <= route.WaypointCheckDistance)
        {
            AdvanceIndex(route.Count);
            target = route.Waypoints[_currentIndex];
            if (target == null) return;
        }

        MoveTowards(target.position);
    }

    private void AdvanceIndex(int count)
    {
        if (count <= 1)
        {
            _currentIndex = 0;
            return;
        }

        if (Hunter.PatrolRoute != null && Hunter.PatrolRoute.Mode == PatrolRoute.RouteMode.Loop)
        {
            _currentIndex++;
            if (_currentIndex >= count) _currentIndex = 0;
            return;
        }

        if (!_reverse)
        {
            _currentIndex++;
            if (_currentIndex >= count)
            {
                _currentIndex = count - 1;
                _reverse = true;
            }
        }
        else
        {
            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = 0;
                _reverse = false;
            }
        }
    }
}
