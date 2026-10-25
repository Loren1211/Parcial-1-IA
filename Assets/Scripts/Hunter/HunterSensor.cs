using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Percepción del cazador. Una sola OverlapSphere por tick sobre la capa Boid.
/// Cachea boids vivos y cadáveres para que la FSM y el HUD no repitan la detección.
/// </summary>
public class HunterSensor
{
    private const int BufferSize = 64;

    private readonly Transform _transform;
    private readonly int _boidMask;
    private readonly float _perceptionRadius;
    private readonly float _interval;

    private readonly Collider[] _buffer = new Collider[BufferSize];
    private readonly List<BoidAgent> _activeBoids = new();
    private readonly List<BoidAgent> _inactiveBoids = new();

    private float _nextTick;

    public IReadOnlyList<BoidAgent> ActiveBoids => _activeBoids;
    public IReadOnlyList<BoidAgent> InactiveBoids => _inactiveBoids;
    public BoidAgent NearestActiveBoid { get; private set; }
    public BoidAgent NearestInactiveBoid { get; private set; }
    public bool HasActiveBoids => _activeBoids.Count > 0;
    public bool HasInactiveBoids => _inactiveBoids.Count > 0;
    public int DetectedBoidsCount => _activeBoids.Count;

    public HunterSensor(Transform transform, LayerMask boidLayer, float perceptionRadius, float interval)
    {
        _transform = transform;
        _boidMask = boidLayer.value;
        _perceptionRadius = perceptionRadius;
        _interval = Mathf.Max(0.02f, interval);
        _nextTick = Time.time + Random.Range(0f, _interval);
    }

    public void Tick()
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + _interval;

        _activeBoids.Clear();
        _inactiveBoids.Clear();
        NearestActiveBoid = null;
        NearestInactiveBoid = null;

        int hitCount = Physics.OverlapSphereNonAlloc(
            _transform.position,
            _perceptionRadius,
            _buffer,
            _boidMask,
            QueryTriggerInteraction.Ignore);

        float closestActiveSqr = float.MaxValue;
        float closestInactiveSqr = float.MaxValue;
        float perceptionSqr = _perceptionRadius * _perceptionRadius;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _buffer[i];
            if (hit == null) continue;

            BoidAgent boid = hit.GetComponentInParent<BoidAgent>();
            if (boid == null) continue;

            float sqrDistance = (boid.transform.position - _transform.position).sqrMagnitude;
            if (sqrDistance > perceptionSqr) continue;

            if (boid.IsDead && !boid.IsCollected)
            {
                _inactiveBoids.Add(boid);

                if (sqrDistance < closestInactiveSqr)
                {
                    closestInactiveSqr = sqrDistance;
                    NearestInactiveBoid = boid;
                }
            }
            else if (boid.IsActive)
            {
                _activeBoids.Add(boid);

                if (sqrDistance < closestActiveSqr)
                {
                    closestActiveSqr = sqrDistance;
                    NearestActiveBoid = boid;
                }
            }
        }
    }

    public void DrawGizmos(Vector3 position)
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(position, _perceptionRadius);
    }
}
