using UnityEngine;

/// <summary>
/// Punto de entrada de un boid. Coordina sensor, comportamiento, movimiento,
/// vida e interacción, pero delega cada responsabilidad en una clase aparte.
/// </summary>
public class BoidAgent : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _maxForce = 10f;
    [SerializeField] private float _viewRadius = 6f;

    [Header("Flocking values")]
    [SerializeField] private float _separationRadius = 3f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 0.6f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 0.8f;
    [SerializeField] private float _evadeWeight = 1.5f;
    [SerializeField] private float _arriveWeight = 1f;

    [Header("Interest Object")]
    [SerializeField] private float _arriveRadius = 3f;
    [SerializeField] private float _interactRadius = 1f;
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private float _interestDetectionRadius = 10f;

    [Header("Hunter detection")]
    [SerializeField] private float _hunterDetectionRadius = 7f;
    [SerializeField] private LayerMask _hunterLayer;
    [SerializeField] private LayerMask _boidLayer;
    [SerializeField] private LayerMask _interestLayer;

    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Performance")]
    [SerializeField] private float _detectionInterval = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos;

    private BoidSettings _settings;
    private BoidSensor _sensor;
    private BoidMotor _motor;
    private BoidBehaviorController _behavior;
    private BoidHealth _health;
    private BoidInteraction _interaction;

    public BoidState State { get; private set; } = BoidState.Flocking;
    public Vector3 Velocity => _motor != null ? _motor.Velocity : Vector3.zero;
    public bool IsActive => _health != null && _health.IsActive;
    public bool IsInactive => _health != null && _health.IsInactive;
    public bool IsDead => _health != null && _health.IsDead;
    public bool IsCollected => _health != null && _health.IsCollected;
    public float Health => _health != null ? _health.CurrentHealth : 0f;
    public float MaxHealth => _health != null ? _health.MaxHealth : _maxHealth;
    public BoidSensor Sensor => _sensor;
    public InterestObject CurrentInterest => _interaction != null ? _interaction.CurrentTarget : null;

    private void Awake()
    {
        _settings = new BoidSettings
        {
            maxSpeed = _maxSpeed,
            maxForce = _maxForce,
            viewRadius = _viewRadius,
            separationRadius = _separationRadius,
            separationWeight = _separationWeight,
            alignmentWeight = _alignmentWeight,
            cohesionWeight = _cohesionWeight,
            arriveRadius = _arriveRadius,
            interactRadius = _interactRadius,
            interestDetectionRadius = _interestDetectionRadius,
            hunterDetectionRadius = _hunterDetectionRadius,
            detectionInterval = _detectionInterval,
            evadeWeight = _evadeWeight,
            arriveWeight = _arriveWeight,
            maxHealth = _maxHealth,
            respawnDelay = _respawnDelay
        };

        _motor = new BoidMotor(transform, _maxSpeed, _maxForce);
        _sensor = new BoidSensor(
            this,
            _boidLayer,
            _hunterLayer,
            _interestLayer,
            _viewRadius,
            _separationRadius,
            _hunterDetectionRadius,
            _interestDetectionRadius,
            _detectionInterval);

        _health = new BoidHealth(
            this,
            transform,
            GetComponent<Renderer>(),
            GetComponent<Collider>(),
            _maxHealth,
            _respawnDelay);

        _interaction = new BoidInteraction(this, _interactRadius);
        _behavior = new BoidBehaviorController(this, _sensor, _interaction, _settings);
    }

    private void Start()
    {
        Vector3 randomDirection = new(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f));

        if (randomDirection.sqrMagnitude <= 0.0001f)
            randomDirection = Vector3.forward;

        _motor.SetVelocity(randomDirection.normalized * _maxSpeed);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        _health.Tick(deltaTime);

        if (!_health.IsActive)
        {
            _motor.Stop();
            return;
        }

        _sensor.Tick();

        BoidState previousState = State;
        _behavior.Evaluate();
        State = _behavior.State;

        // Si el cebo desaparece mientras el boid lo perseguía o interactuaba,
        // le damos un impulso aleatorio para que no quede amontonado con
        // velocidad casi cero junto al resto del cardumen.
        if ((previousState == BoidState.Seeking || previousState == BoidState.Interacting)
            && State == BoidState.Flocking)
        {
            ScatterFromInterest();
        }

        _motor.Apply(_behavior.Steering, deltaTime);
        _interaction.Tick(deltaTime);
    }

    private void ScatterFromInterest()
    {
        Vector3 direction = new(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f));

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        _motor.SetVelocity(direction.normalized * _maxSpeed * 0.9f);
    }

    public void TakeDamage(float damage)
    {
        if (!_health.IsActive) return;

        if (_health.TakeDamage(damage))
            Die();
    }

    public void Die()
    {
        if (!_health.IsActive) return;

        _health.Die();
        _interaction.Cancel();
        _motor.Stop();
        State = BoidState.Dead;
    }

    /// <summary>
    /// Se mantiene por compatibilidad con el cazador anterior.
    /// </summary>
    public void BecomeInactive() => Die();

    public void Collect()
    {
        if (!_health.IsDead || _health.IsCollected) return;

        _health.Collect();
        _interaction.Cancel();
        _motor.Stop();
        State = BoidState.Respawning;
    }

    public void OnRespawned()
    {
        Vector3 randomDirection = new(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f));

        if (randomDirection.sqrMagnitude <= 0.0001f)
            randomDirection = Vector3.forward;

        _motor.SetVelocity(randomDirection.normalized * _maxSpeed);
        State = BoidState.Flocking;
    }

    private void OnValidate()
    {
        _separationRadius = Mathf.Min(_separationRadius, Mathf.Max(0.1f, _viewRadius - 0.01f));
        _interactRadius = Mathf.Max(0.05f, _interactRadius);
        _detectionInterval = Mathf.Max(0.02f, _detectionInterval);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _viewRadius);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _hunterDetectionRadius);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _interestDetectionRadius);
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos || !Application.isPlaying || _motor == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + _motor.Velocity);
    }
}
