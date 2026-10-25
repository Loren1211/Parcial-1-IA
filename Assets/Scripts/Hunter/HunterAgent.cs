using UnityEngine;

/// <summary>
/// Configuración y dueño de la FSM del cazador.
/// La detección vive en HunterSensor y las transiciones en cada estado.
/// </summary>
public class HunterAgent : MonoBehaviour
{
    public enum States { Patrol, Attack, Gather }

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;

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
    public Vector3 Velocity { get; private set; }

    public BoidAgent CurrentTarget { get; set; }
    public string LastAction { get; set; } = "Ninguna";
    public int DetectedBoidsCount => Sensor != null ? Sensor.DetectedBoidsCount : 0;

    private readonly FiniteStateMachine<States> _fsm = new();
    public FiniteStateMachine<States> FSM => _fsm;

    private float _tbaTimer;
    private Vector3 _lastPosition;
    private States _lastLoggedState;
    private string _lastChangeLog = "Inicio en Patrol";

    public bool CanAttack() => _tbaTimer <= 0f;
    public void ResetTBA() => _tbaTimer = _tba;

    private void Awake()
    {
        if (_patrolRoute == null)
            _patrolRoute = FindFirstObjectByType<PatrolRoute>();

        Sensor = new HunterSensor(transform, _boidLayer, _perceptionRadius, _detectionInterval);
    }

    private void Start()
    {
        _lastPosition = transform.position;

        _fsm.AddState(States.Patrol, new HunterPatrolState(this));
        _fsm.AddState(States.Attack, new HunterAttackState(this));
        _fsm.AddState(States.Gather, new HunterGatherState(this));
        _fsm.ChangeState(States.Patrol);
    }

    private void Update()
    {
        if (_tbaTimer > 0f) _tbaTimer -= Time.deltaTime;

        Sensor.Tick();
        _fsm.Update();

        Vector3 delta = transform.position - _lastPosition;
        Velocity = delta / Mathf.Max(0.0001f, Time.deltaTime);
        _lastPosition = transform.position;

        if (_fsm.CurrentStateID != _lastLoggedState)
        {
            _lastChangeLog = $"{_lastLoggedState} -> {_fsm.CurrentStateID}";
            _lastLoggedState = _fsm.CurrentStateID;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 420, 20), $"Estado actual: {_fsm.CurrentStateID}");
        GUI.Label(new Rect(10, 30, 420, 20), $"Objetivo actual: {(CurrentTarget != null ? CurrentTarget.name : "Ninguno")}");
        GUI.Label(new Rect(10, 50, 420, 20), $"Boids detectados: {DetectedBoidsCount}");
        GUI.Label(new Rect(10, 70, 420, 20), $"Última acción: {LastAction}");
        GUI.Label(new Rect(10, 90, 420, 20), $"Último cambio: {_lastChangeLog}");
        GUI.Label(new Rect(10, 110, 420, 20), $"TBA listo: {CanAttack()}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
    }
}
