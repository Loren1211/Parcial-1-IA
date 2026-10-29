using UnityEngine;

/// <summary>
/// Movimiento del cazador. No decide nada: solo traslada, orienta y expone
/// la velocidad real para que los boids puedan predecir en Evade.
/// </summary>
public class HunterMotor
{
    private readonly Transform _transform;
    private readonly float _baseSpeed;

    private Vector3 _lastPosition;
    private Vector3 _velocity;

    public Vector3 Velocity => _velocity;
    public float Speed => _baseSpeed * SpeedMultiplier;
    public float SpeedMultiplier { get; set; } = 1f;

    public HunterMotor(Transform transform, float speed)
    {
        _transform = transform;
        _baseSpeed = Mathf.Max(0.1f, speed);
        _lastPosition = transform.position;
    }

    public void MoveTowards(Vector3 position, float deltaTime)
    {
        if (deltaTime <= 0f) return;

        Vector3 direction = position - _transform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;

        direction.Normalize();
        float step = Mathf.Min(Speed * deltaTime, (position - _transform.position).magnitude);
        _transform.position += direction * step;
        FaceDirection(direction);
    }

    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
            _transform.forward = direction.normalized;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            _velocity = Vector3.zero;
            return;
        }

        _velocity = (_transform.position - _lastPosition) / deltaTime;
        _lastPosition = _transform.position;
    }

    public void Stop()
    {
        _velocity = Vector3.zero;
        _lastPosition = _transform.position;
    }
}
