using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Percepción limitada del boid. Hace una sola OverlapSphere por tick,
/// con máscara combinada, y filtra los resultados por tipo y distancia.
/// </summary>
public class BoidSensor
{
    private const int BufferSize = 64;

    private readonly BoidAgent _owner;
    private readonly Transform _transform;
    private readonly int _mask;
    private readonly float _viewRadius;
    private readonly float _separationRadius;
    private readonly float _hunterDetectionRadius;
    private readonly float _interestDetectionRadius;
    private readonly float _interval;

    private readonly Collider[] _buffer = new Collider[BufferSize];
    private readonly List<BoidAgent> _neighbors = new();
    private readonly List<BoidAgent> _separationNeighbors = new();

    private float _nextTick;

    public IReadOnlyList<BoidAgent> Neighbors => _neighbors;
    public IReadOnlyList<BoidAgent> SeparationNeighbors => _separationNeighbors;
    public HunterAgent NearestHunter { get; private set; }
    public InterestObject NearestInterest { get; private set; }

    public float MaxRadius => Mathf.Max(
        Mathf.Max(_viewRadius, _separationRadius),
        Mathf.Max(_hunterDetectionRadius, _interestDetectionRadius));

    public BoidSensor(
        BoidAgent owner,
        LayerMask boidLayer,
        LayerMask hunterLayer,
        LayerMask interestLayer,
        float viewRadius,
        float separationRadius,
        float hunterDetectionRadius,
        float interestDetectionRadius,
        float interval)
    {
        _owner = owner;
        _transform = owner.transform;
        _mask = boidLayer.value | hunterLayer.value | interestLayer.value;
        _viewRadius = viewRadius;
        _separationRadius = separationRadius;
        _hunterDetectionRadius = hunterDetectionRadius;
        _interestDetectionRadius = interestDetectionRadius;
        _interval = Mathf.Max(0.02f, interval);

        // Fase aleatoria para repartir el costo entre los boids.
        _nextTick = Time.time + Random.Range(0f, _interval);
    }

    public void Tick()
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + _interval;

        _neighbors.Clear();
        _separationNeighbors.Clear();
        NearestHunter = null;
        NearestInterest = null;

        int hitCount = Physics.OverlapSphereNonAlloc(
            _transform.position,
            MaxRadius,
            _buffer,
            _mask,
            QueryTriggerInteraction.Ignore);

        float closestHunterSqr = float.MaxValue;
        float closestInterestSqr = float.MaxValue;

        float viewSqr = _viewRadius * _viewRadius;
        float separationSqr = _separationRadius * _separationRadius;
        float hunterSqr = _hunterDetectionRadius * _hunterDetectionRadius;
        float interestSqr = _interestDetectionRadius * _interestDetectionRadius;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _buffer[i];
            if (hit == null) continue;

            Vector3 delta = hit.transform.position - _transform.position;
            float sqrDistance = delta.sqrMagnitude;

            BoidAgent boid = hit.GetComponentInParent<BoidAgent>();
            if (boid != null)
            {
                if (boid == _owner || !boid.IsActive) continue;

                if (sqrDistance <= viewSqr) _neighbors.Add(boid);
                if (sqrDistance <= separationSqr) _separationNeighbors.Add(boid);
                continue;
            }

            HunterAgent hunter = hit.GetComponentInParent<HunterAgent>();
            if (hunter != null && sqrDistance <= hunterSqr && sqrDistance < closestHunterSqr)
            {
                closestHunterSqr = sqrDistance;
                NearestHunter = hunter;
                continue;
            }

            InterestObject interest = hit.GetComponentInParent<InterestObject>();
            if (interest != null && interest.IsAlive && sqrDistance <= interestSqr && sqrDistance < closestInterestSqr)
            {
                closestInterestSqr = sqrDistance;
                NearestInterest = interest;
            }
        }
    }

    public void DrawGizmos(Vector3 position)
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(position, _viewRadius);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(position, _separationRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
        Gizmos.DrawWireSphere(position, _hunterDetectionRadius);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(position, _interestDetectionRadius);
    }
}
