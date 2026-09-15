using System.Collections.Generic;
using UnityEngine;

public class FSMAgent : MonoBehaviour
{
    [SerializeField]
    private float _speed = 3f;
    public float Speed => _speed;

    [SerializeField]
    private PatrolData _patrolData;

    private readonly FiniteStateMachine<States> _fsm = new();
    public enum States { Idle, Patrol, Death }

    public int HealthPoints;

    //private Animator _animator;
    //private readonly int _walkAnimBool = Animator.StringToHash("Walk");

    private void Start()
    {
        var idle = new IdleState();
        var patrol = new PatrolState<States>(_patrolData, this, States.Idle);
        _fsm.AddState(States.Idle, idle);
        _fsm.AddState(States.Patrol, patrol);
        _fsm.ChangeState(States.Idle);
    }
    private void Update()
    {
        _fsm.Update();
    }

    //private void ChangeToWalkAnimation() => _animator.SetBool(_walkAnimBool, true);

    public void TakeDamage(int damage)
    {
        HealthPoints -= damage;
        if (HealthPoints <= 0) _fsm.ChangeState(States.Death);
    }

}

/*public enum States
{
    Idle, 
    Patrol,
    Attack,
    Death,
    Revive,
    Stun,

}*/
/*
public enum GameStates
{
    Menu,
    Game,
    Pause
}*/
