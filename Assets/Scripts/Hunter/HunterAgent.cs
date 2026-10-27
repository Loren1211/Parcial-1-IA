using UnityEngine;

/// <summary>
/// Configuración y dueño de la FSM del cazador.
/// El movimiento vive en HunterMotor, el combate en HunterCombat, la
/// percepción en HunterSensor y los cebos en HunterInterestSpawner.
/// </summary>
public class HunterAgent : MonoBehaviour
{
    public enum States { Patrol, Attack, Gather }

    [Header("Movement")]
    [SerializeField] private float _speed = 6.5f;

    [Header("Combat")]
    [SerializeField] private float _tba = 2f;
    [SerializeField] private float _rangeAttackRadius = 6f;
    [SerializeField] private float _meleeAttackRadius = 1.5f;
    [SerializeField] private float _perceptionRadius = 9f;
    [SerializeField] private float _meleeDamage = 100f;
    [SerializeField] private float _rangeDamage = 50f;
    [SerializeField] private LayerMask _boidLayer;

    [Header("Perception")]
    [SerializeField] private float _detectionInterval = 0.15f;

    [Header("Patrol")]
    [SerializeField] private PatrolRoute _patrolRoute;

    [Header("Interest Object")]
    [SerializeField] private GameObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 4f;
    [SerializeField] private int _maxInterestObjects = 5;

    public float Speed => _speed;
    public float TBA => _tba;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float PerceptionRadius => _perceptionRadius;
    public float MeleeDamage => _meleeDamage;
    public float RangeDamage => _rangeDamage;
    public PatrolRoute PatrolRoute => _patrolRoute;
    public GameObject InterestObjectPrefab => _interestObjectPrefab;
    public float InterestSpawnInterval => _interestSpawnInterval;
    public int MaxInterestObjects => _maxInterestObjects;

    public HunterSensor Sensor { get; private set; }
    public HunterMotor Motor { get; private set; }
    public HunterCombat Combat { get; private set; }
    public HunterInterestSpawner InterestSpawner { get; private set; }
    public HunterFeedback Feedback { get; private set; }

    public Vector3 Velocity => Motor != null ? Motor.Velocity : Vector3.zero;
    public float TbaRemaining => Combat != null ? Combat.TbaRemaining : 0f;
    public BoidAgent CurrentTarget { get; set; }
    public string LastAction { get; set; } = "Ninguna";
    public float GatherProgress { get; set; }
    public int DetectedBoidsCount => Sensor != null ? Sensor.DetectedBoidsCount : 0;

    private FiniteStateMachine<States> _fsm;
    public FiniteStateMachine<States> FSM => _fsm;

    public bool CanAttack() => Combat != null && Combat.CanAttack;

    private void Awake()
    {
        _fsm = new FiniteStateMachine<States>();
        LastAction = "Ninguna";

        if (_patrolRoute == null)
            _patrolRoute = FindFirstObjectByType<PatrolRoute>();

        Sensor = new HunterSensor(transform, _boidLayer, _perceptionRadius, _detectionInterval);
        Motor = new HunterMotor(transform, _speed);
        Combat = new HunterCombat(_tba, _meleeDamage, _rangeDamage);
        InterestSpawner = new HunterInterestSpawner(this);
        Feedback = new HunterFeedback(GetComponent<Renderer>());

        Combat.Attacked += OnAttacked;
        Combat.Killed += OnKilled;
    }

    private void Start()
    {
        _fsm.AddState(States.Patrol, new HunterPatrolState(this));
        _fsm.AddState(States.Attack, new HunterAttackState(this));
        _fsm.AddState(States.Gather, new HunterGatherState(this));
        _fsm.OnStateChanged += OnStateChanged;
        _fsm.ChangeState(States.Patrol);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        Combat.Tick(deltaTime);
        Sensor.Tick();
        _fsm.Update();
        InterestSpawner.Tick(deltaTime);
        Motor.Tick(deltaTime);
    }

    private void OnDestroy()
    {
        if (Combat != null)
        {
            Combat.Attacked -= OnAttacked;
            Combat.Killed -= OnKilled;
        }

        _fsm.OnStateChanged -= OnStateChanged;
    }

    private void OnAttacked(BoidAgent target, bool isMelee)
    {
        LastAction = (isMelee ? "Ataque melee a " : "Ataque a distancia a ") + target.name;
        FeedbackManager.Instance?.NotifyAttack(target, isMelee);
    }

    private void OnKilled(BoidAgent target)
    {
        FeedbackManager.Instance?.NotifyKill(target);
    }

    private void OnStateChanged(States previous, States current)
    {
        Feedback?.SetState(current);

        if (previous == current) return;

        LastAction = $"Estado: {previous} -> {current}";
        FeedbackManager.Instance?.NotifyHunterStateChanged(previous, current);
    }

    private void OnDrawGizmosSelected()
    {
        if (!FeedbackManager.ShowGizmos) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
    }
}
