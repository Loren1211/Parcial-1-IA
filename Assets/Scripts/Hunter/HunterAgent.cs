using UnityEngine;

/// <summary>
/// Configuración y dueño de la FSM del cazador.
/// El movimiento vive en HunterMotor, el combate en HunterCombat, la
/// percepción en HunterSensor y los cebos en HunterInterestSpawner.
/// En el Parcial 2 la navegación usa NavGraph/A* y se suman los estados
/// Search, Flee, Exit y Dead.
/// </summary>
public class HunterAgent : MonoBehaviour
{
    public enum States { Patrol, Attack, Gather, Search, Flee, Exit, Dead }

    public enum Mode { Parcial1, Parcial2 }

    [Header("Mode")]
    [SerializeField] private Mode _mode = Mode.Parcial1;

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
    [SerializeField] private LayerMask _wolfLayer;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _wolfDetectionRadius = 9f;

    [Header("Parcial 2")]
    [SerializeField] private int _requiredGathers = 3;
    [SerializeField] private float _respawnTime = 5f;
    [SerializeField] private float _safeZoneWaitTime = 3f;
    [SerializeField] private float _spawnProtectionTime = 2f;
    [SerializeField] private float _fleeCommitTime = 3f;
    [SerializeField] private float _fleeSpeedMultiplier = 1.3f;
    [SerializeField] private float _panicRadius = 1.2f;

    [Header("Patrol")]
    [SerializeField] private PatrolRoute _patrolRoute;

    [Header("Navigation")]
    [SerializeField] private NavGraph _navGraph;

    [Header("Interest Object")]
    [SerializeField] private GameObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 4f;
    [SerializeField] private int _maxInterestObjects = 5;

    public Mode CurrentMode => _mode;
    public bool IsParcial2 => _mode == Mode.Parcial2;
    public States IdleState => IsParcial2 ? States.Search : States.Patrol;

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
    public int RequiredGathers => _requiredGathers;
    public float RespawnTime => _respawnTime;
    public float SafeZoneWaitTime => _safeZoneWaitTime;
    public float SpawnProtectionTime => _spawnProtectionTime;
    public float FleeCommitTime => _fleeCommitTime;
    public float FleeSpeedMultiplier => _fleeSpeedMultiplier;
    public float PanicRadius => _panicRadius;

    public HunterSensor Sensor { get; private set; }
    public HunterWolfSensor WolfSensor { get; private set; }
    public HunterMotor Motor { get; private set; }
    public HunterCombat Combat { get; private set; }
    public HunterInterestSpawner InterestSpawner { get; private set; }
    public HunterFeedback Feedback { get; private set; }
    public NavAgent Nav { get; private set; }

    public Vector3 Velocity => Motor != null ? Motor.Velocity : Vector3.zero;
    public float TbaRemaining => Combat != null ? Combat.TbaRemaining : 0f;
    public BoidAgent CurrentTarget { get; set; }
    public string LastAction { get; set; } = "Ninguna";
    public float GatherProgress { get; set; }

    public int Gathers { get; private set; }
    public bool IsCaptured { get; private set; }
    public bool HasEscaped { get; private set; }
    public bool IsImmune => ZoneVolume.IsInsideSafeZone(transform.position);
    public bool IsSpawnProtected => Time.time < _spawnProtectionUntil;
    public bool IsFleeCommitted => Time.time < _fleeCommitUntil;
    public ZoneVolume ChosenSafeZone { get; set; }
    public WolfAgent LastCaptor { get; private set; }

    public bool ShouldFlee => IsParcial2 && !IsSpawnProtected
        && WolfSensor != null && WolfSensor.HasVisibleWolf && !IsImmune
        && (!IsFleeCommitted || NearestWolfDistance <= _panicRadius);
    public bool ShouldExit => IsParcial2 && Gathers >= _requiredGathers && !HasEscaped;

    public float NearestWolfDistance
    {
        get
        {
            if (WolfSensor == null || WolfSensor.NearestWolf == null) return float.MaxValue;
            return Vector3.Distance(transform.position, WolfSensor.NearestWolf.transform.position);
        }
    }

    public int DetectedBoidsCount => Sensor != null ? Sensor.DetectedBoidsCount : 0;

    private FiniteStateMachine<States> _fsm;
    private float _spawnProtectionUntil;
    private float _fleeCommitUntil;
    public FiniteStateMachine<States> FSM => _fsm;

    public bool CanAttack() => Combat != null && Combat.CanAttack;

    private void Awake()
    {
        _fsm = new FiniteStateMachine<States>();
        LastAction = "Ninguna";
        _spawnProtectionUntil = Time.time + _spawnProtectionTime;

        if (_patrolRoute == null)
            _patrolRoute = FindFirstObjectByType<PatrolRoute>();

        if (_navGraph == null)
            _navGraph = FindFirstObjectByType<NavGraph>();

        if (_wolfLayer.value == 0)
            _wolfLayer = GameLayers.Mask(GameLayers.Wolf);

        if (_obstacleMask.value == 0)
            _obstacleMask = GameLayers.Mask(GameLayers.Obstacle);

        Sensor = new HunterSensor(
            transform,
            _boidLayer,
            _perceptionRadius,
            _detectionInterval,
            _obstacleMask,
            IsParcial2);
        WolfSensor = new HunterWolfSensor(
            transform,
            _wolfLayer,
            _obstacleMask,
            _wolfDetectionRadius,
            _detectionInterval);
        Motor = new HunterMotor(transform, _speed);
        Combat = new HunterCombat(_tba, _meleeDamage, _rangeDamage);
        InterestSpawner = new HunterInterestSpawner(this);
        Feedback = new HunterFeedback(GetComponent<Renderer>());
        Nav = new NavAgent(transform, _navGraph, false, 0.4f, 1.2f, 3.5f);

        Combat.Attacked += OnAttacked;
        Combat.Killed += OnKilled;
    }

    private void Start()
    {
        _fsm.AddState(States.Patrol, new HunterPatrolState(this));
        _fsm.AddState(States.Attack, new HunterAttackState(this));
        _fsm.AddState(States.Gather, new HunterGatherState(this));
        _fsm.AddState(States.Search, new HunterSearchState(this));
        _fsm.AddState(States.Flee, new HunterFleeState(this));
        _fsm.AddState(States.Exit, new HunterExitState(this));
        _fsm.AddState(States.Dead, new HunterDeadState(this));
        _fsm.OnStateChanged += OnStateChanged;
        _fsm.ChangeState(IdleState);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        Combat.Tick(deltaTime);
        Sensor.Tick();

        if (IsParcial2)
            WolfSensor.Tick();

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

        if (_fsm != null)
            _fsm.OnStateChanged -= OnStateChanged;
    }

    public void AddGather()
    {
        Gathers++;
        FeedbackManager.Instance?.AddLog($"RECOLECCIONES: {Gathers}/{_requiredGathers}");
    }

    public void OnCapturedByWolf(WolfAgent wolf)
    {
        if (!IsParcial2 || IsCaptured || HasEscaped) return;

        IsCaptured = true;
        LastCaptor = wolf;
        _fsm.ChangeState(States.Dead);
    }

    public void NotifyRespawned()
    {
        IsCaptured = false;
        Gathers = 0;
        HasEscaped = false;
        ChosenSafeZone = null;
        _spawnProtectionUntil = Time.time + _spawnProtectionTime;
        _fleeCommitUntil = 0f;
        Motor.SpeedMultiplier = 1f;
    }

    public void NotifyEscaped()
    {
        HasEscaped = true;
    }

    /// <summary>
    /// Al salir de un refugio el Cazador se compromete a alejarse unos
    /// segundos. Sin esto vuelve a huir apenas ve al lobo que espera afuera
    /// y queda oscilando entre Search y Flee sin progresar.
    /// </summary>
    public void CommitAfterRefuge()
    {
        _fleeCommitUntil = Time.time + _fleeCommitTime;
    }

    public Vector3 GetRespawnPosition()
    {
        ZoneVolume initial = ZoneVolume.FindNearest(ZoneKind.Initial, transform.position);

        if (initial == null)
            return transform.position;

        if (initial.EntryPoint != null)
            return initial.EntryPoint.position + Vector3.up;

        return initial.Center + Vector3.up;
    }

    public void SetRendererEnabled(bool enabled)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = enabled;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = enabled;
    }

    private void OnAttacked(BoidAgent target, bool isMelee)
    {
        LastAction = (isMelee ? "Ataque melee a " : "Ataque a distancia a ") + target.name;
        FeedbackManager.Instance?.NotifyAttack(target, isMelee, transform.position + Vector3.up * 0.8f);
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
        FeedbackManager.Instance?.SpawnFloatingText(
            transform.position + Vector3.up * 1.9f,
            $"{previous} -> {current}",
            Color.white);
    }

    private void OnDrawGizmos()
    {
        if (!FeedbackManager.ShowGizmos) return;
        if (Nav == null || !Nav.HasPath) return;

        // Camino A* activo del Cazador: se ve en la vista de Escena durante
        // el Play y complementa el conteo de nodos del HUD.
        Gizmos.color = new Color(0.2f, 0.95f, 0.35f, 0.9f);

        Vector3 previous = transform.position;
        var points = Nav.Path.Points;

        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawLine(previous, points[i]);
            Gizmos.DrawWireSphere(points[i], 0.12f);
            previous = points[i];
        }
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

        if (!IsParcial2) return;

        Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, _wolfDetectionRadius);

        if (ChosenSafeZone != null)
        {
            Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position, ChosenSafeZone.Center);
        }

        if (Nav != null && Nav.Path.Found && Nav.Path.Points.Count > 1)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.8f);

            for (int i = 1; i < Nav.Path.Points.Count; i++)
                Gizmos.DrawLine(Nav.Path.Points[i - 1], Nav.Path.Points[i]);
        }
    }
}
