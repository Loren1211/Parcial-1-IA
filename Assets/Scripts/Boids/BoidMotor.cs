using UnityEngine;

/// <summary>
/// Movimiento del boid: aplica aceleración, limita velocidad y mantiene
/// la posición dentro de WorldBounds.
/// </summary>
public class BoidMotor
{
    private readonly Transform _transform;
    private readonly float _maxSpeed;
    private readonly float _maxForce;

    private Vector3 _velocity;

    public Vector3 Velocity => _velocity;

    public BoidMotor(Transform transform, float maxSpeed, float maxForce)
    {
        _transform = transform;
        _maxSpeed = maxSpeed;
        _maxForce = maxForce;
    }

    public void Apply(Vector3 steering, float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // El plano de juego es XZ: cualquier componente vertical del steering
        // (por ejemplo al buscar un cebo a distinta altura) se descarta. Sin
        // esto la velocidad vertical se acumulaba y los boids terminaban
        // cientos de unidades arriba o abajo, fuera del alcance del Cazador.
        steering.y = 0f;

        _velocity += Vector3.ClampMagnitude(steering, _maxForce) * deltaTime;
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        Vector3 nextPosition = _transform.position + _velocity * deltaTime;
        nextPosition.y = _transform.position.y;

        if (WorldBounds.Instance != null)
            nextPosition = WorldBounds.Instance.CalculateBoundPosition(nextPosition);

        _transform.position = nextPosition;

        if (_velocity.sqrMagnitude > 0.01f)
            _transform.forward = _velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(velocity, _maxSpeed);
    }

    public void Stop()
    {
        _velocity = Vector3.zero;
    }

    public void Teleport(Vector3 position)
    {
        _transform.position = position;
    }
}
