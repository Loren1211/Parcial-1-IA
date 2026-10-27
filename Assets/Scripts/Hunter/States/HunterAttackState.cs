using UnityEngine;

public class HunterAttackState : HunterStateBase
{
    private BoidAgent _target;

    public HunterAttackState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        _target = Hunter.Sensor.NearestActiveBoid;
        Hunter.CurrentTarget = _target;

        if (_target == null)
            _fsm.ChangeState(HunterAgent.States.Patrol);
    }

    public override void Update()
    {
        if (_target == null || !StillInPerception())
        {
            _fsm.ChangeState(HunterAgent.States.Patrol);
            return;
        }

        if (!_target.IsActive)
        {
            _fsm.ChangeState(HunterAgent.States.Gather);
            return;
        }

        float distance = Vector3.Distance(Hunter.transform.position, _target.transform.position);

        if (distance <= Hunter.MeleeAttackRadius)
        {
            if (Hunter.Combat.TryMeleeAttack(_target) && !_target.IsActive)
                _fsm.ChangeState(HunterAgent.States.Gather);
        }
        else if (distance <= Hunter.RangeAttackRadius)
        {
            if (Hunter.Combat.TryRangeAttack(_target) && !_target.IsActive)
                _fsm.ChangeState(HunterAgent.States.Gather);
        }
        else
        {
            MoveTowards(_target.transform.position);
        }
    }

    public override void Exit()
    {
        Hunter.CurrentTarget = null;
    }

    private bool StillInPerception()
    {
        if (_target == null) return false;

        return Vector3.Distance(Hunter.transform.position, _target.transform.position)
               <= Hunter.PerceptionRadius;
    }
}
