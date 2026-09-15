using UnityEngine;

public class HunterGatherState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;
    private Agent _target;
    private readonly float _gatherTime = 1.5f;
    private readonly float _stopRadius = 1f;
    private float _timer;

    public HunterGatherState(HunterAgent hunter) => _hunter = hunter;

    public override void Enter()
    {
        Debug.Log("Cazador: entra a Gather");
        _target = GetClosestInactiveBoid();
        _hunter.CurrentTarget = _target;
        _timer = 0f;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsInactive)
        {
            _fsm.ChangeState(HunterAgent.States.Patrol);
            return;
        }

        float dist = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (dist > _stopRadius)
        {
            Vector3 dir = (_target.transform.position - _hunter.transform.position).normalized;
            _hunter.transform.position += dir * _hunter.Speed * Time.deltaTime;
            _hunter.transform.forward = dir;
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

    private Agent GetClosestInactiveBoid()
    {
        var list = _hunter.GetInactiveBoidsInRange();
        Agent closest = null;
        float minDist = float.MaxValue;
        foreach (var a in list)
        {
            float d = Vector3.Distance(_hunter.transform.position, a.transform.position);
            if (d < minDist) { minDist = d; closest = a; }
        }
        return closest;
    }
}