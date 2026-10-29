using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Percepcion de lobos del Cazador. A diferencia de los lobos, el Cazador
/// detecta en 360 grados: una OverlapSphere por tick y linea de vision.
/// </summary>
public class HunterWolfSensor
{
    private const int BufferSize = 16;

    private readonly Transform _transform;
    private readonly LayerMask _wolfMask;
    private readonly LayerMask _obstacleMask;
    private readonly float _radius;
    private readonly float _interval;

    private readonly Collider[] _buffer = new Collider[BufferSize];
    private readonly List<WolfAgent> _wolves = new();

    private float _nextTick;

    public IReadOnlyList<WolfAgent> Wolves => _wolves;
    public bool HasVisibleWolf => _wolves.Count > 0;
    public WolfAgent NearestWolf { get; private set; }

    public HunterWolfSensor(
        Transform transform,
        LayerMask wolfMask,
        LayerMask obstacleMask,
        float radius,
        float interval)
    {
        _transform = transform;
        _wolfMask = wolfMask;
        _obstacleMask = obstacleMask;
        _radius = Mathf.Max(0.1f, radius);
        _interval = Mathf.Max(0.02f, interval);
    }

    public void Tick()
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + _interval;

        _wolves.Clear();
        NearestWolf = null;

        int count = Physics.OverlapSphereNonAlloc(
            _transform.position,
            _radius,
            _buffer,
            _wolfMask,
            QueryTriggerInteraction.Ignore);

        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            WolfAgent wolf = _buffer[i].GetComponentInParent<WolfAgent>();
            if (wolf == null) continue;
            if (_wolves.Contains(wolf)) continue;

            Vector3 origin = _transform.position + Vector3.up * 0.6f;
            Vector3 target = wolf.transform.position + Vector3.up * 0.6f;

            if (Physics.Linecast(origin, target, _obstacleMask, QueryTriggerInteraction.Ignore))
                continue;

            _wolves.Add(wolf);

            float sqr = (wolf.transform.position - _transform.position).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            NearestWolf = wolf;
        }
    }
}
