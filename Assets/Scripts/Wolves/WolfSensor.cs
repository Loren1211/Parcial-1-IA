using UnityEngine;

/// <summary>
/// Percepcion del lobo: distancia, campo de vision y linea de vision contra
/// los obstaculos. Cachea la ultima posicion conocida del Cazador.
/// </summary>
public class WolfSensor
{
    private readonly Transform _transform;
    private Transform _target;
    private readonly float _viewRadius;
    private readonly float _halfFov;
    private readonly float _interval;
    private readonly LayerMask _obstacleMask;

    private float _nextTick;

    public bool SeesTarget { get; private set; }
    public bool HasSeenTarget { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public float LastSeenTime { get; private set; } = -999f;

    public WolfSensor(
        Transform transform,
        Transform target,
        float viewRadius,
        float fovAngle,
        float interval,
        LayerMask obstacleMask)
    {
        _transform = transform;
        _target = target;
        _viewRadius = Mathf.Max(0.1f, viewRadius);
        _halfFov = Mathf.Clamp(fovAngle, 1f, 360f) * 0.5f;
        _interval = Mathf.Max(0.02f, interval);
        _obstacleMask = obstacleMask;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        SeesTarget = false;
    }

    public void Tick()
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + _interval;

        if (_target == null)
        {
            SeesTarget = false;
            return;
        }

        Vector3 origin = _transform.position + Vector3.up * 0.6f;
        Vector3 targetPoint = _target.position + Vector3.up * 0.6f;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        SeesTarget = false;

        if (distance > _viewRadius) return;
        if (Vector3.Angle(_transform.forward, toTarget) > _halfFov) return;
        if (Physics.Linecast(origin, targetPoint, _obstacleMask, QueryTriggerInteraction.Ignore)) return;

        SeesTarget = true;
        HasSeenTarget = true;
        LastSeenPosition = _target.position;
        LastSeenTime = Time.time;
    }
}
