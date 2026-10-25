using System;
using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine<T>
{
    private State<T> _currentState;
    private readonly Dictionary<T, State<T>> _allStates = new();

    public T CurrentStateID { get; private set; }
    public State<T> CurrentState => _currentState;

    public event Action<T, T> OnStateChanged;

    public void AddState(T id, State<T> state)
    {
        state.SetFSM(this);

        if (!_allStates.ContainsKey(id))
            _allStates.Add(id, state);
        else
            _allStates[id] = state;
    }

    public void ChangeState(T id)
    {
        if (!_allStates.ContainsKey(id))
        {
            Debug.LogError($"Missing State: {id}");
            return;
        }

        T previous = CurrentStateID;

        _currentState?.Exit();
        _currentState = _allStates[id];
        CurrentStateID = id;
        _currentState.Enter();

        OnStateChanged?.Invoke(previous, id);
    }

    public void Update()
    {
        if (_currentState != null) _currentState.Update();
    }
}
