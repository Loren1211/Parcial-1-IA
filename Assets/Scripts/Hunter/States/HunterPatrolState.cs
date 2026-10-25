using System.Collections;
using UnityEngine;

public class HunterPatrolState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;

    private int _currentIndex;
    private bool _reverse;
    private Coroutine _spawnRoutine;

    public HunterPatrolState(HunterAgent hunter) => _hunter = hunter;

    public override void Enter()
    {
        Debug.Log("Cazador: entra a Patrol");
        _spawnRoutine = _hunter.StartCoroutine(SpawnInterestRoutine());
    }

    public override void Update()
    {
        Patrol();

        if (_hunter.Sensor.HasInactiveBoids)
        {
            _fsm.ChangeState(HunterAgent.States.Gather);
            return;
        }

        if (_hunter.CanAttack() && _hunter.Sensor.HasActiveBoids)
            _fsm.ChangeState(HunterAgent.States.Attack);
    }

    public override void Exit()
    {
        Debug.Log("Cazador: sale de Patrol");

        if (_spawnRoutine != null)
        {
            _hunter.StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private void Patrol()
    {
        PatrolRoute route = _hunter.PatrolRoute;
        if (route == null || route.Count == 0) return;

        if (_currentIndex >= route.Count) _currentIndex = 0;
        if (_currentIndex < 0) _currentIndex = 0;

        Transform target = route.Waypoints[_currentIndex];
        if (target == null) return;

        if (Vector3.Distance(target.position, _hunter.transform.position) <= route.WaypointCheckDistance)
        {
            AdvanceIndex(route.Count);
            target = route.Waypoints[_currentIndex];
            if (target == null) return;
        }

        Vector3 direction = target.position - _hunter.transform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;

        _hunter.transform.position += _hunter.Speed * Time.deltaTime * direction.normalized;
        _hunter.transform.forward = direction;
    }

    private void AdvanceIndex(int count)
    {
        if (count <= 1)
        {
            _currentIndex = 0;
            return;
        }

        if (_hunter.PatrolRoute != null && _hunter.PatrolRoute.Mode == PatrolRoute.RouteMode.Loop)
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

    private IEnumerator SpawnInterestRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_hunter.InterestSpawnInterval);

            if (_hunter.InterestObjectPrefab == null) continue;

            int activeCount = InterestObjectTracker.Instance != null
                ? InterestObjectTracker.Instance.ActiveCount
                : UnityEngine.Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None).Length;

            if (activeCount >= _hunter.MaxInterestObjects) continue;

            Vector3 position = WorldBounds.Instance != null
                ? WorldBounds.Instance.RandomPosition()
                : Vector3.zero;

            UnityEngine.Object.Instantiate(_hunter.InterestObjectPrefab, position, Quaternion.identity);
        }
    }
}
