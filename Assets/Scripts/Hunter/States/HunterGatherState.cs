using UnityEngine;

public class HunterGatherState : HunterStateBase
{
    private const float GatherTime = 1.5f;
    private const float StopRadius = 1f;

    private BoidAgent _target;
    private float _timer;

    public HunterGatherState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        _target = Hunter.Sensor.NearestInactiveBoid;
        Hunter.CurrentTarget = _target;
        _timer = 0f;

        if (_target == null)
        {
            _fsm.ChangeState(Hunter.IdleState);
        }
    }

    public override void Update()
    {
        if (CheckPriorityTransitions()) return;

        if (_target == null || !_target.IsDead || _target.IsCollected)
        {
            FeedbackManager.Instance?.AddLog("GATHER CANCELADO: objetivo no disponible");
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        float distance = Vector3.Distance(Hunter.transform.position, _target.transform.position);

        if (distance > StopRadius)
        {
            NavigateTowards(_target.transform.position);
            return;
        }

        _timer += Time.deltaTime;
        Hunter.GatherProgress = Mathf.Clamp01(_timer / GatherTime);

        if (_timer >= GatherTime)
        {
            Hunter.LastAction = "Recolectó a " + _target.name;
            FeedbackManager.Instance?.NotifyGather(_target);
            Hunter.AddGather();
            _target.Collect();
            _fsm.ChangeState(Hunter.IdleState);
        }
    }

    public override void Exit()
    {
        Hunter.GatherProgress = 0f;
        Hunter.CurrentTarget = null;
    }
}
