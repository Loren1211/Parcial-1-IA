using UnityEngine;

public class IdleState : State<FSMAgent.States>
{
    readonly float _timeToChangeToPatrol = 3f;
    float _timer = 0f;

    public override void Enter()
    {
        Debug.LogError("Entre a Idle");
        _timer = 0f;
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if(_timer >= _timeToChangeToPatrol)
        {
            _fsm.ChangeState(FSMAgent.States.Patrol);
        }
    }

    public override void Exit()
    {
        Debug.LogError("Sali de idle");
    }

}
