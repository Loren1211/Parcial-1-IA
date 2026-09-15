using System.Collections;
using UnityEngine;

public class HunterPatrolState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;
    private int _currentIndex = 0;
    private bool _reverse = false;
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

        if (_hunter.GetInactiveBoidsInRange().Count > 0)
        {
            _fsm.ChangeState(HunterAgent.States.Gather);
            return;
        }

        if (_hunter.CanAttack() && _hunter.GetBoidsInPerception().Count > 0)
        {
            _fsm.ChangeState(HunterAgent.States.Attack);
        }
    }

    public override void Exit()
    {
        Debug.Log("Cazador: sale de Patrol");
        if (_spawnRoutine != null) _hunter.StopCoroutine(_spawnRoutine);
    }

    private void Patrol()
    {
        var data = _hunter.PatrolData;
        if (data.waypoints == null || data.waypoints.Count == 0) return;

        Transform target = data.waypoints[_currentIndex];
        if (Vector3.Distance(target.position, data.transform.position) <= data.waypointCheckDistance)
        {
            AdvanceIndex(data.waypoints.Count);
            target = data.waypoints[_currentIndex];
        }

        Vector3 dir = target.position - data.transform.position;
        data.transform.position += _hunter.Speed * Time.deltaTime * dir.normalized;
        data.transform.forward = dir;
    }

    private void AdvanceIndex(int count)
    {
        if (!_reverse)
        {
            _currentIndex++;
            if (_currentIndex >= count) { _currentIndex = count - 1; _reverse = true; }
        }
        else
        {
            _currentIndex--;
            if (_currentIndex < 0) { _currentIndex = 0; _reverse = false; }
        }
    }

    private IEnumerator SpawnInterestRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_hunter.InterestSpawnInterval);
            if (InterestObject.ActiveCount < _hunter.MaxInterestObjects)
            {
                Vector3 random = new(Random.Range(-30f, 30f), 0f, Random.Range(-17f, 17f));
                Vector3 pos = Bounds.Instance.CalculateBoundPosition(random);
                Object.Instantiate(_hunter.InterestObjectPrefab, pos, Quaternion.identity);
            }
        }
    }
}