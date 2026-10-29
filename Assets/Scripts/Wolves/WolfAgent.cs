using UnityEngine;

/// <summary>
/// Lobo de la jauria. Patrulla por waypoints, percibe al Cazador con FOV y
/// linea de vision, comparte alertas con el resto y lo persigue con A*.
/// No puede entrar a las zonas seguras: el NavAgent excluye esos nodos y el
/// motor frena cualquier paso que caiga dentro.
/// </summary>
public class WolfAgent : MonoBehaviour
{
    public enum States { Patrol, Alert, Chase, Return }

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 3.2f;
    [SerializeField] private float _alertSpeed = 4f;
    [SerializeField] private float _chaseSpeed = 4.4f;
    [SerializeField] private float _turnSpeed = 540f;

    [Header("Perception")]
    [SerializeField] private float _viewRadius = 9f;
    [SerializeField] private float _fovAngle = 120f;
    [SerializeField] private float _detectionInterval = 0.15f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Chase")]
    [SerializeField] private float _captureRadius = 1.1f;
    [SerializeField] private float _memoryDuration = 4f;
    [SerializeField] private float _searchDuration = 2.5f;

    [Header("Patrol")]
    [SerializeField] private PatrolRoute _patrolRoute;

    [Header("Navigation")]
    [SerializeField] private NavGraph _navGraph;

    public float PatrolSpeed => _patrolSpeed;
    public float AlertSpeed => _alertSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float ViewRadius => _viewRadius;
    public float FovAngle => _fovAngle;
    public float CaptureRadius => _captureRadius;
    public float MemoryDuration => _memoryDuration;
    public float SearchDuration => _searchDuration;
    public PatrolRoute PatrolRoute => _patrolRoute;
    public WolfSensor Sensor { get; private set; }
    public WolfMotor Motor { get; private set; }
    public NavAgent Nav { get; private set; }
    public WolfFeedback Feedback { get; private set; }
    public HunterAgent Hunter { get; private set; }
    public FiniteStateMachine<States> FSM => _fsm;

    public bool SeesHunter => !IgnoresHunter
        && Sensor != null && Sensor.SeesTarget
        && Hunter != null && !Hunter.IsCaptured
        && !ZoneVolume.IsInsideWolfExcluded(Hunter.transform.position);
    public bool IgnoresHunter => Time.time < _ignoreHunterUntil;
    public bool HasLastKnownPosition { get; private set; }
    public Vector3 LastKnownHunterPosition { get; private set; }
    public float LastSeenTime => Sensor != null ? Sensor.LastSeenTime : -999f;
    public Vector3 PackAlertPosition { get; private set; }
    public bool HasPackAlert => !IgnoresHunter && _hasPackAlert && Time.time - _packAlertTime <= _memoryDuration;
    public string LastAction { get; set; } = "Ninguna";

    private FiniteStateMachine<States> _fsm;
    private bool _started;
    private bool _hasPackAlert;
    private float _packAlertTime = -999f;
    private float _ignoreHunterUntil;

    private void Awake()
    {
        _fsm = new FiniteStateMachine<States>();

        if (_patrolRoute == null)
            _patrolRoute = FindFirstObjectByType<PatrolRoute>();

        if (_navGraph == null)
            _navGraph = FindFirstObjectByType<NavGraph>();

        Hunter = FindFirstObjectByType<HunterAgent>();

        if (_obstacleMask.value == 0)
            _obstacleMask = GameLayers.Mask(GameLayers.Obstacle);

        Sensor = new WolfSensor(
            transform,
            Hunter != null ? Hunter.transform : null,
            _viewRadius,
            _fovAngle,
            _detectionInterval,
            _obstacleMask);

        Motor = new WolfMotor(transform, _turnSpeed);
        Nav = new NavAgent(transform, _navGraph, true, 0.45f, 1.2f, 3.5f);
        Feedback = new WolfFeedback(GetComponent<Renderer>());
    }

    private void Start()
    {
        _fsm.AddState(States.Patrol, new WolfPatrolState(this));
        _fsm.AddState(States.Alert, new WolfAlertState(this));
        _fsm.AddState(States.Chase, new WolfChaseState(this));
        _fsm.AddState(States.Return, new WolfReturnState(this));
        _fsm.OnStateChanged += OnStateChanged;
        _fsm.ChangeState(States.Patrol);
        _started = true;
    }

    private void Update()
    {
        if (Hunter == null)
        {
            Hunter = FindFirstObjectByType<HunterAgent>();
            Sensor?.SetTarget(Hunter != null ? Hunter.transform : null);
        }

        Sensor.Tick();

        // Mientras el Cazador esta en una zona excluida (inicial o segura) la
        // jauria pierde el rastro. Se refresca cada frame para que el lobo no
        // se quede esperando en la boca del callejon y el Cazador pueda salir.
        if (Hunter != null && ZoneVolume.IsInsideWolfExcluded(Hunter.transform.position))
            IgnoreHunter(1.5f);

        if (SeesHunter)
            RememberHunterPosition(Hunter.transform.position);

        _fsm.Update();
    }

    private void OnEnable()
    {
        WolfAlertBus.AlertRaised += OnPackAlert;
    }

    private void OnDisable()
    {
        WolfAlertBus.AlertRaised -= OnPackAlert;
    }

    private void OnDestroy()
    {
        if (_fsm != null)
            _fsm.OnStateChanged -= OnStateChanged;
    }

    public bool GoTo(Vector3 position, bool force = false)
    {
        return Nav != null && Nav.SetDestination(position, force);
    }

    public void MoveAlongPath(float speed)
    {
        if (Nav == null) return;

        Vector3 target = Nav.GetMoveTarget();
        Motor.MoveTowards(target, speed, Time.deltaTime);
    }

    public void MoveTowards(Vector3 position, float speed)
    {
        Motor.MoveTowards(position, speed, Time.deltaTime);
    }

    public void Stop()
    {
        Motor.Stop();
    }

    public float DistanceTo(Vector3 position)
    {
        return Vector3.Distance(transform.position, position);
    }

    public int NearestWaypointIndex(Vector3 position)
    {
        if (_patrolRoute == null || _patrolRoute.Count == 0) return 0;

        int best = 0;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < _patrolRoute.Count; i++)
        {
            float sqr = (_patrolRoute.Waypoints[i].position - position).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            best = i;
        }

        return best;
    }

    public void RememberHunterPosition(Vector3 position)
    {
        LastKnownHunterPosition = position;
        HasLastKnownPosition = true;
        WolfAlertBus.RaiseAlert(this, position);
    }

    /// <summary>
    /// Pierde el rastro del Cazador durante un tiempo. Se usa cuando se
    /// refugia en una zona segura para que la jauria no acampe la salida.
    /// </summary>
    public void IgnoreHunter(float duration)
    {
        _ignoreHunterUntil = Time.time + Mathf.Max(0f, duration);
        _hasPackAlert = false;
    }

    public bool CanCapture(HunterAgent hunter)
    {
        if (hunter == null || hunter.IsCaptured) return false;
        if (IgnoresHunter) return false;
        if (hunter.IsSpawnProtected) return false;
        if (ZoneVolume.IsInsideWolfExcluded(hunter.transform.position)) return false;
        if (DistanceTo(hunter.transform.position) > _captureRadius) return false;

        return HasLineOfSight(hunter.transform.position);
    }

    public bool HasLineOfSight(Vector3 position)
    {
        Vector3 origin = transform.position + Vector3.up * 0.6f;
        Vector3 target = new(position.x, origin.y, position.z);

        return !Physics.Linecast(origin, target, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void OnPackAlert(WolfAgent source, Vector3 position)
    {
        if (source == this) return;

        PackAlertPosition = position;
        _hasPackAlert = true;
        _packAlertTime = Time.time;

        if (!_started) return;
        if (_fsm.CurrentStateID != States.Patrol && _fsm.CurrentStateID != States.Return) return;

        _fsm.ChangeState(States.Alert);
    }

    private void OnStateChanged(States previous, States current)
    {
        Feedback?.SetState(current);

        if (previous == current) return;

        LastAction = $"Estado: {previous} -> {current}";

        FeedbackManager manager = FeedbackManager.Instance;
        if (manager == null) return;

        manager.AddLog($"{name}: {previous} -> {current}");
    }

    private void OnDrawGizmos()
    {
        if (!FeedbackManager.ShowGizmos) return;

        Vector3 origin = transform.position + Vector3.up * 0.6f;

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(origin, _viewRadius);

        Vector3 left = Quaternion.Euler(0f, -_fovAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, _fovAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.85f);
        Gizmos.DrawLine(origin, origin + left * _viewRadius);
        Gizmos.DrawLine(origin, origin + right * _viewRadius);

        if (HasLastKnownPosition)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(LastKnownHunterPosition, 0.4f);
            Gizmos.DrawLine(origin, LastKnownHunterPosition + Vector3.up * 0.6f);
        }

        if (Nav != null && Nav.Path.Found && Nav.Path.Points.Count > 1)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);

            for (int i = 1; i < Nav.Path.Points.Count; i++)
                Gizmos.DrawLine(Nav.Path.Points[i - 1], Nav.Path.Points[i]);
        }
    }
}
