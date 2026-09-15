using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _maxForce = 10f;
    [SerializeField] private float _viewRadius = 5f;

    [Header("Flocking values")]
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1f;

    [Header("Interest Object")]
    [SerializeField] private float _arriveRadius = 3f;
    [SerializeField] private float _interactRadius = 1f;
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private float _interestDetectionRadius = 10f;

    [Header("Hunter detection")]
    [SerializeField] private float _hunterDetectionRadius = 7f;
    [SerializeField] private LayerMask _hunterLayer;

    private static readonly List<Agent> _allAgents = new();
    private static readonly List<Agent> _inactiveAgents = new();

    private Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    private bool _isInactive;
    public bool IsInactive => _isInactive;

    private void Awake() => _allAgents.Add(this);

    private void Start()
    {
        Vector3 randomVector = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity += randomVector.normalized * _maxSpeed;
    }

    private void Update()
    {
        if (_isInactive) return;

        HunterAgent hunter = DetectHunter();

        if (hunter != null){
            _velocity += CalculateFlee(hunter.transform.position) + CalculateSeparation(_allAgents, _separationRadius) * _separationWeight;}
        else
        {
            InterestObject interest = FindNearestActiveInterest();

            if (interest != null)
            {
                float dist = Vector3.Distance(transform.position, interest.transform.position);

                if (dist <= _interactRadius)
                {
                    _velocity = CalculateSeparation(_allAgents, _separationRadius) * _separationWeight;
                    interest.TakeDamageTick(Time.deltaTime);
                    if (!interest.IsAlive) BecomeInactive();
                }
                else
                {
                    Vector3 arrive = CalculateArrive(interest.transform.position);
                    Vector3 separation = CalculateSeparation(_allAgents, _separationRadius) * _separationWeight * 0.5f;
                    _velocity += arrive + separation;
                }
            }
            else
            {
                CalculateFlocking();
            }
        }
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        transform.position += _velocity * Time.deltaTime;
        if (_velocity.sqrMagnitude > 0.01f) transform.forward = _velocity;
        transform.position = Bounds.Instance.CalculateBoundPosition(transform.position);
    }

    #region Flocking
    private void CalculateFlocking()
    {
        Vector3 separation = CalculateSeparation(_allAgents, _separationRadius); 
        bool tooClose = separation != Vector3.zero; 
        Vector3 alignment = CalculateAlignment(_allAgents, _viewRadius); 
        Vector3 cohesion = CalculateCohesion(_allAgents, _viewRadius);
        float dampen = tooClose ? 0.2f : 1f; _velocity += separation * _separationWeight * 1.5f + alignment * _alignmentWeight * dampen + cohesion * _cohesionWeight * dampen; }

    private Vector3 CalculateCohesion(IEnumerable<Agent> agents, float radius)
    {
        Vector3 desiredPosition = default;
        int count = 0;
        foreach (Agent item in agents)
        {
            if (item == this || item.IsInactive) continue;
            if (InRange(item.transform.position, radius))
            {
                desiredPosition += item.transform.position;
                count++;
            }
        }
        if (count == 0) return Vector3.zero;
        desiredPosition /= count;

        float distToCenter = Vector3.Distance(transform.position, desiredPosition);
        if (distToCenter < _separationRadius) return Vector3.zero;

        return CalculateSeek(desiredPosition);
    }

    private Vector3 CalculateSeparation(IEnumerable<Agent> agents, float radius)
    {
        Vector3 desired = Vector3.zero; int count = 0; foreach (Agent item in agents)
        {
            if (item == this || item.IsInactive) continue; Vector3 diff = transform.position - item.transform.position; float dist = diff.magnitude; if (dist > 0f && dist <= radius)
            {
                desired += diff.normalized / dist; // mas peso cuanto mas cerca esta
                count++; } } if (count == 0) return Vector3.zero; desired /= count; 
        return CalculateSteering(desired.normalized * _maxSpeed); 
    }

    private Vector3 CalculateAlignment(IEnumerable<Agent> agents, float radius)
    {
        Vector3 desired = default;
        int count = 0;
        foreach (Agent item in agents)
        {
            if (item == this || item.IsInactive) continue;
            if (InRange(item.transform.position, radius))
            {
                desired += item.Velocity;
                count++;
            }
        }
        if (count == 0) return Vector3.zero;
        desired /= count;
        return CalculateSteering(desired.normalized * _maxSpeed);
    }

    private bool InRange(Vector3 position, float radius) =>
        (position - transform.position).sqrMagnitude <= radius * radius;
    #endregion

    #region Steering base
    private Vector3 CalculateSeek(Vector3 targetPosition)
    {
        Vector3 desired = (targetPosition - transform.position).normalized * _maxSpeed;
        return CalculateSteering(desired);
    }

    private Vector3 CalculateFlee(Vector3 targetPosition)
    {
        Vector3 desired = (targetPosition - transform.position).normalized * _maxSpeed;
        return CalculateSteering(-desired);
    }

    private Vector3 CalculateArrive(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        float distance = dir.magnitude;
        float speed = _maxSpeed;

        if (distance <= _arriveRadius)
            speed *= distance / _arriveRadius;

        Vector3 desired = dir.normalized * speed;
        return CalculateSteering(desired);
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = desired - _velocity;
        steering = Vector3.ClampMagnitude(steering, _maxForce);
        return steering * Time.deltaTime;
    }
    #endregion

    #region Hunter & Interest lookup
    private HunterAgent DetectHunter()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _hunterDetectionRadius, _hunterLayer);
        foreach (var hit in hits)
            if (hit.TryGetComponent<HunterAgent>(out var h)) return h;
        return null;
    }

    private InterestObject FindNearestActiveInterest()
    {
        InterestObject nearest = null;
        float minDist = float.MaxValue;
        foreach (var obj in InterestObject.AllActive)
        {
            if (!obj.IsAlive) continue;

            float sqrDist = (obj.transform.position - transform.position).sqrMagnitude;

            if (sqrDist > _interestDetectionRadius * _interestDetectionRadius) continue;

            if (sqrDist < minDist) { minDist = sqrDist; nearest = obj; }
        }
        return nearest;
    }
    #endregion

    #region Inactive / Respawn
    public void BecomeInactive()
    {
        _isInactive = true;
        _velocity = Vector3.zero;
        _inactiveAgents.Add(this);
    }

    public static List<Agent> GetInactiveAgentsInRange(Vector3 position, float radius)
    {
        List<Agent> result = new();
        foreach (var a in _inactiveAgents)
            if ((a.transform.position - position).sqrMagnitude <= radius * radius)
                result.Add(a);
        return result;
    }

    public void Collect()
    {
        _inactiveAgents.Remove(this);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (TryGetComponent<Renderer>(out var rend)) rend.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        yield return new WaitForSeconds(_respawnDelay);

        Vector3 random = new(Random.Range(-30f, 30f), 0f, Random.Range(-17f, 17f));
        transform.position = Bounds.Instance.CalculateBoundPosition(random);
        Vector3 randomVelocity = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)); _velocity = randomVelocity.normalized * _maxSpeed;

        if (TryGetComponent<Renderer>(out var rend2)) rend2.enabled = true;
        if (TryGetComponent<Collider>(out var col2)) col2.enabled = true;

        _isInactive = false;
    }
    #endregion

    private void OnDestroy()
    {
        _allAgents.Remove(this);
        _inactiveAgents.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _viewRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);
    }
}