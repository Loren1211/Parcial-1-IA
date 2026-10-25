using UnityEngine;

public class HunterGatherState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;
    private readonly float _gatherTime = 1.5f;
    private readonly float _stopRadius = 1f;

    private BoidAgent _target;
    private float _timer;

    public HunterGatherState(HunterAgent hunter) => _hunter = hunter;

    public override void Enter()
    {
        Debug.Log("Cazador: entra a Gather");

        _target = _hunter.Sensor.NearestInactiveBoid;
        _hunter.CurrentTarget = _target;
        _timer = 0f;

        if (_target == null)
            _fsm.ChangeState(HunterAgent.States.Patrol);
    }

    public override void Update()
    {
        if (_target == null || !_target.IsDead || _target.IsCollected)
        {
            _fsm.ChangeState(HunterAgent.States.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance > _stopRadius)
        {
            Vector3 direction = (_target.transform.position - _hunter.transform.position).normalized;
            _hunter.transform.position += direction * _hunter.Speed * Time.deltaTime;
            _hunter.transform.forward = direction;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= _gatherTime)
        {
            _hunter.LastAction = "Recolectó a " + _target.name;
            _target.Collect();
            _fsm.ChangeState(HunterAgent.States.Patrol);
        }
    }

    public override void Exit()
    {
        Debug.Log("Cazador: sale de Gather");
        _hunter.CurrentTarget = null;
    }
}
