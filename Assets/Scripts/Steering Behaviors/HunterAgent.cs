using System.Collections.Generic;
using UnityEngine;

public class HunterAgent : MonoBehaviour
{
    public enum States { Patrol, Attack, Gather }

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;
    public float Speed => _speed;

    [Header("Combat")]
    [SerializeField] private float _tba = 2f;
    [SerializeField] private float _rangeAttackRadius = 6f;
    [SerializeField] private float _meleeAttackRadius = 1.5f;
    [SerializeField] private float _perceptionRadius = 9f;
    [SerializeField] private LayerMask _boidLayer;

    public float TBA => _tba;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float PerceptionRadius => _perceptionRadius;

    public Agent CurrentTarget { get; set; }
    public string LastAction { get; set; } = "Ninguna";
    public int DetectedBoidsCount { get; private set; }

    private States _lastLoggedState;
    private string _lastChangeLog = "Inicio en Patrol";

    [Header("Patrol")]
    [SerializeField] private PatrolData _patrolData;
    public PatrolData PatrolData => _patrolData;

    [Header("Interest Object")]
    [SerializeField] private GameObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 4f;
    [SerializeField] private int _maxInterestObjects = 5;
    public GameObject InterestObjectPrefab => _interestObjectPrefab;
    public float InterestSpawnInterval => _interestSpawnInterval;
    public int MaxInterestObjects => _maxInterestObjects;

    private readonly FiniteStateMachine<States> _fsm = new();
    public FiniteStateMachine<States> FSM => _fsm;

    private float _tbaTimer;
    public bool CanAttack() => _tbaTimer <= 0f;
    public void ResetTBA() => _tbaTimer = _tba;

    private void Start()
    {
        _fsm.AddState(States.Patrol, new HunterPatrolState(this));
        _fsm.AddState(States.Attack, new HunterAttackState(this));
        _fsm.AddState(States.Gather, new HunterGatherState(this));
        _fsm.ChangeState(States.Patrol);
    }

    private void Update()
    {
        if (_tbaTimer > 0f) _tbaTimer -= Time.deltaTime;
        _fsm.Update();

        DetectedBoidsCount = GetBoidsInPerception().Count;

        if (_fsm.CurrentStateID != _lastLoggedState)
        {
            _lastChangeLog = $"{_lastLoggedState} -> {_fsm.CurrentStateID}";
            _lastLoggedState = _fsm.CurrentStateID;
        }
    }

    public List<Agent> GetBoidsInPerception()
    {
        List<Agent> result = new();
        Collider[] hits = Physics.OverlapSphere(transform.position, _perceptionRadius, _boidLayer);
        foreach (var hit in hits)
            if (hit.TryGetComponent<Agent>(out var a) && !a.IsInactive) result.Add(a);
        return result;
    }

    public List<Agent> GetInactiveBoidsInRange() =>
        Agent.GetInactiveAgentsInRange(transform.position, _perceptionRadius);

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20), $"Estado actual: {_fsm.CurrentStateID}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Objetivo actual: {(CurrentTarget != null ? CurrentTarget.name : "Ninguno")}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Boids detectados: {DetectedBoidsCount}");
        GUI.Label(new Rect(10, 70, 400, 20), $"Última acción: {LastAction}");
        GUI.Label(new Rect(10, 90, 400, 20), $"Último cambio: {_lastChangeLog}");
        GUI.Label(new Rect(10, 110, 400, 20), $"TBA listo: {CanAttack()}");
    }
}