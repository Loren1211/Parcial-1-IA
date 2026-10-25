using UnityEngine;

public class HunterAttackState : State<HunterAgent.States>
{
    private readonly HunterAgent _hunter;

    private BoidAgent _target;

    public HunterAttackState(HunterAgent hunter) => _hunter = hunter;

    public override void Enter()
    {
        Debug.Log("Cazador: entra a Attack");

        _target = _hunter.Sensor.NearestActiveBoid;
        _hunter.CurrentTarget = _target;

        if (_target == null)
            _fsm.ChangeState(HunterAgent.States.Patrol);
    }

    public override void Update()
    {
        if (_target == null || !_target.IsActive || !StillInPerception())
        {
            _fsm.ChangeState(HunterAgent.States.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance <= _hunter.MeleeAttackRadius)
            PerformMeleeAttack();
        else if (distance <= _hunter.RangeAttackRadius)
            PerformRangeAttack();
        else
            MoveTowards(_target.transform.position);
    }

    public override void Exit()
    {
        Debug.Log("Cazador: sale de Attack");
        _hunter.CurrentTarget = null;
    }

    private bool StillInPerception()
    {
        if (_target == null) return false;

        return Vector3.Distance(_hunter.transform.position, _target.transform.position)
               <= _hunter.PerceptionRadius;
    }

    private void MoveTowards(Vector3 position)
    {
        Vector3 direction = (position - _hunter.transform.position).normalized;

        _hunter.transform.position += direction * _hunter.Speed * Time.deltaTime;
        _hunter.transform.forward = direction;
    }

    private void PerformMeleeAttack()
    {
        if (!_hunter.CanAttack()) return;

        Debug.Log("Cazador ataca cuerpo a cuerpo a " + _target.name);
        _hunter.LastAction = "Ataque melee a " + _target.name;
        _target.TakeDamage(_hunter.MeleeDamage);

        _hunter.ResetTBA();
        _fsm.ChangeState(HunterAgent.States.Patrol);
    }

    private void PerformRangeAttack()
    {
        if (!_hunter.CanAttack()) return;

        Debug.Log("Cazador ataca a distancia a " + _target.name);
        _hunter.LastAction = "Ataque a distancia a " + _target.name;
        _target.TakeDamage(_hunter.RangeDamage);

        _hunter.ResetTBA();
        _fsm.ChangeState(HunterAgent.States.Patrol);
    }
}
