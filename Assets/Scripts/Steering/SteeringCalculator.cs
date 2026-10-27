using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Matemática pura de steering. No toca transforms, no usa Time.deltaTime
/// y no conoce la escena: recibe posiciones/velocidades y devuelve fuerzas.
/// </summary>
public static class SteeringCalculator
{
    public static Vector3 CalculateSteering(Vector3 velocity, Vector3 desired, float maxForce)
    {
        Vector3 steering = desired - velocity;
        return Vector3.ClampMagnitude(steering, maxForce);
    }

    public static Vector3 Seek(Vector3 position, Vector3 velocity, Vector3 target, float maxSpeed, float maxForce)
    {
        Vector3 desired = (target - position).normalized * maxSpeed;
        return CalculateSteering(velocity, desired, maxForce);
    }

    public static Vector3 Flee(Vector3 position, Vector3 velocity, Vector3 target, float maxSpeed, float maxForce)
    {
        Vector3 desired = (position - target).normalized * maxSpeed;
        return CalculateSteering(velocity, desired, maxForce);
    }

    public static Vector3 Evade(
        Vector3 position,
        Vector3 velocity,
        Vector3 hunterPosition,
        Vector3 hunterVelocity,
        float maxSpeed,
        float maxForce,
        float leadTime = 1f)
    {
        // Si el cazador es más rápido que el boid, una predicción fija de 1 s
        // puede pasarse detrás del boid y hacer que huya hacia el cazador.
        // Limitamos el tiempo de anticipación para que el punto predicho
        // quede siempre entre el cazador y el boid.
        float distance = Vector3.Distance(position, hunterPosition);
        float hunterSpeed = hunterVelocity.magnitude;

        float safeLeadTime = hunterSpeed > 0.01f
            ? Mathf.Min(leadTime, distance / hunterSpeed * 0.9f)
            : leadTime;

        Vector3 predictedPosition = hunterPosition + hunterVelocity * safeLeadTime;
        return Flee(position, velocity, predictedPosition, maxSpeed, maxForce);
    }

    public static Vector3 Arrive(
        Vector3 position,
        Vector3 velocity,
        Vector3 target,
        float maxSpeed,
        float maxForce,
        float slowRadius)
    {
        Vector3 direction = target - position;
        float distance = direction.magnitude;

        if (distance <= 0.0001f) return Vector3.zero;

        float speed = maxSpeed;
        if (distance <= slowRadius)
            speed *= distance / Mathf.Max(0.0001f, slowRadius);

        Vector3 desired = direction.normalized * speed;
        return CalculateSteering(velocity, desired, maxForce);
    }

    public static Vector3 Separation(
        Vector3 position,
        Vector3 velocity,
        IReadOnlyList<BoidAgent> neighbors,
        float maxSpeed,
        float maxForce)
    {
        if (neighbors == null || neighbors.Count == 0) return Vector3.zero;

        Vector3 desired = Vector3.zero;
        Vector3 nearestOffset = Vector3.zero;
        float nearestSqr = float.MaxValue;
        int count = 0;

        foreach (BoidAgent other in neighbors)
        {
            if (other == null) continue;

            Vector3 difference = position - other.transform.position;
            float sqrDistance = difference.sqrMagnitude;

            if (sqrDistance <= 0.0001f) continue;

            float distance = Mathf.Sqrt(sqrDistance);
            desired += difference / (distance * distance);

            if (sqrDistance < nearestSqr)
            {
                nearestSqr = sqrDistance;
                nearestOffset = difference;
            }

            count++;
        }

        if (count == 0) return Vector3.zero;

        desired /= count;

        // En configuraciones simétricas (por ejemplo, boids rodeándose entre sí)
        // las repulsiones pueden cancelarse y dar cero. En ese caso usamos la
        // dirección del vecino más cercano para que siempre haya separación.
        if (desired.sqrMagnitude <= 0.0001f)
            desired = nearestOffset;

        return CalculateSteering(velocity, desired.normalized * maxSpeed, maxForce);
    }

    public static Vector3 Alignment(
        Vector3 velocity,
        IReadOnlyList<BoidAgent> neighbors,
        float maxSpeed,
        float maxForce)
    {
        if (neighbors == null || neighbors.Count == 0) return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        foreach (BoidAgent other in neighbors)
        {
            if (other == null) continue;

            averageVelocity += other.Velocity;
            count++;
        }

        if (count == 0 || averageVelocity.sqrMagnitude <= 0.0001f) return Vector3.zero;

        averageVelocity /= count;
        return CalculateSteering(velocity, averageVelocity.normalized * maxSpeed, maxForce);
    }

    public static Vector3 Cohesion(
        Vector3 position,
        Vector3 velocity,
        IReadOnlyList<BoidAgent> neighbors,
        float maxSpeed,
        float maxForce)
    {
        if (neighbors == null || neighbors.Count == 0) return Vector3.zero;

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (BoidAgent other in neighbors)
        {
            if (other == null) continue;

            center += other.transform.position;
            count++;
        }

        if (count == 0) return Vector3.zero;

        center /= count;
        return Seek(position, velocity, center, maxSpeed, maxForce);
    }
}
