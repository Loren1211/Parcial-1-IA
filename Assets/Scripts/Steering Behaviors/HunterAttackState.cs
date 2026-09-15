using UnityEngine;

public class HunterAttackState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;
    private Agent _target;

    public HunterAttackState(HunterAgent hunter) => _hunter = hunter;

    public override void Enter()
    {
        Debug.Log("Cazador: entra a Attack");
        _target = GetClosestBoid();
        _hunter.CurrentTarget = _target;
    }

    public override void Update()
    {
        if (_target == null || _target.IsInactive || !StillInPerception())
        {
            _fsm.ChangeState(HunterAgent.States.Patrol);
            return;
        }

        float dist = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (dist <= _hunter.MeleeAttackRadius) PerformMeleeAttack();
        else if (dist <= _hunter.RangeAttackRadius) PerformRangeAttack();
        else MoveTowards(_target.transform.position);
    }

    public override void Exit()
    {
        Debug.Log("Cazador: sale de Attack");
        _hunter.CurrentTarget = null;
    }

    private Agent GetClosestBoid()
    {
        var boids = _hunter.GetBoidsInPerception();
        Agent closest = null;
        float minDist = float.MaxValue;
        foreach (var b in boids)
        {
            float d = Vector3.Distance(_hunter.transform.position, b.transform.position);
            if (d < minDist) { minDist = d; closest = b; }
        }
        return closest;
    }

    private bool StillInPerception() =>
        Vector3.Distance(_hunter.transform.position, _target.transform.position) <= _hunter.PerceptionRadius;

    private void MoveTowards(Vector3 pos)
    {
        Vector3 dir = (pos - _hunter.transform.position).normalized;
        _hunter.transform.position += dir * _hunter.Speed * Time.deltaTime;
        _hunter.transform.forward = dir;
    }

    private void PerformMeleeAttack()
    {
        if (!_hunter.CanAttack()) return;
        Debug.Log("Cazador ataca cuerpo a cuerpo a " + _target.name);
        _hunter.LastAction = "Ataque melee a " + _target.name;
        _target.BecomeInactive();
        _hunter.ResetTBA();
        _fsm.ChangeState(HunterAgent.States.Patrol);
    }

    private void PerformRangeAttack()
    {
        if (!_hunter.CanAttack()) return;
        Debug.Log("Cazador ataca a distancia a " + _target.name);
        _hunter.LastAction = "Ataque a distancia a " + _target.name;
        _target.BecomeInactive();
        _hunter.ResetTBA();
        _fsm.ChangeState(HunterAgent.States.Patrol);
    }
}