using UnityEngine;

/// <summary>
/// Movimiento del lobo: traslada en el plano, orienta hacia la direccion de
/// avance y frena si el siguiente paso cae dentro de una zona segura.
/// </summary>
public class WolfMotor
{
    private readonly Transform _transform;
    private readonly float _turnSpeed;

    private Vector3 _velocity;

    public Vector3 Velocity => _velocity;
    public float Speed => _velocity.magnitude;

    public WolfMotor(Transform transform, float turnSpeed)
    {
        _transform = transform;
        _turnSpeed = Mathf.Max(1f, turnSpeed);
    }

    public void MoveTowards(Vector3 target, float speed, float deltaTime)
    {
        if (deltaTime <= 0f) return;

        Vector3 position = _transform.position;
        Vector3 flatTarget = new(target.x, position.y, target.z);
        Vector3 direction = flatTarget - position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            _velocity = Vector3.zero;
            return;
        }

        direction.Normalize();
        Vector3 next = position + direction * (speed * deltaTime);

        if (ZoneVolume.IsInsideWolfExcluded(next))
        {
            _velocity = Vector3.zero;
            return;
        }

        _transform.position = next;
        _velocity = direction * speed;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        _transform.rotation = Quaternion.RotateTowards(
            _transform.rotation,
            targetRotation,
            _turnSpeed * deltaTime);
    }

    public void Stop()
    {
        _velocity = Vector3.zero;
    }
}
