using UnityEngine;

/// <summary>
/// Ataque del cazador. Mantiene el objetivo hasta matarlo: si se aleja lo
/// persigue con A* en vez de volver a Search, que provocaba un ciclo
/// Attack/Search por frame y dejaba al cazador sin recolecciones.
/// </summary>
public class HunterAttackState : HunterStateBase
{
    private const float LostTargetTime = 2f;

    private BoidAgent _target;
    private float _lostTimer;

    public HunterAttackState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        _target = Hunter.Sensor.NearestActiveBoid;
        Hunter.CurrentTarget = _target;
        _lostTimer = 0f;

        if (_target == null)
            _fsm.ChangeState(Hunter.IdleState);
    }

    public override void Update()
    {
        if (CheckPriorityTransitions()) return;

        if (_target == null || _target.IsCollected)
        {
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        if (!_target.IsActive)
        {
            if (_target.IsDead && !_target.IsCollected)
            {
                _fsm.ChangeState(HunterAgent.States.Gather);
                return;
            }

            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        _lostTimer = IsTracked() ? 0f : _lostTimer + Time.deltaTime;

        if (_lostTimer >= LostTargetTime)
        {
            Hunter.LastAction = "Objetivo perdido";
            _fsm.ChangeState(Hunter.IdleState);
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
            NavigateTowards(_target.transform.position);
        }
    }

    public override void Exit()
    {
        Hunter.CurrentTarget = null;
    }

    private bool IsTracked()
    {
        var tracked = Hunter.Sensor.ActiveBoids;

        for (int i = 0; i < tracked.Count; i++)
        {
            if (tracked[i] == _target) return true;
        }

        return false;
    }
}
