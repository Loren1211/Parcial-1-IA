using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decide el comportamiento del boid por prioridad:
/// Evade > Interactuar > Arrive > Flocking.
/// </summary>
public class BoidBehaviorController
{
    private const float WanderWeight = 0.35f;
    private const float WanderJitter = 1.2f;
    private const float CruiseWeight = 0.15f;

    private readonly BoidAgent _owner;
    private readonly BoidSensor _sensor;
    private readonly BoidInteraction _interaction;
    private readonly BoidSettings _settings;

    private float _wanderAngle;

    public Vector3 Steering { get; private set; }
    public BoidState State { get; private set; } = BoidState.Flocking;

    public BoidBehaviorController(
        BoidAgent owner,
        BoidSensor sensor,
        BoidInteraction interaction,
        BoidSettings settings)
    {
        _owner = owner;
        _sensor = sensor;
        _interaction = interaction;
        _settings = settings;
    }

    public void Evaluate()
    {
        if (_sensor.NearestHunter != null)
        {
            State = BoidState.Evading;
            _interaction.Cancel();
            Steering = CalculateEvade() * _settings.evadeWeight
                       + CalculateSeparation() * _settings.separationWeight;
            return;
        }

        InterestObject interest = _sensor.NearestInterest;
        if (interest != null && interest.IsAlive)
        {
            float sqrDistance = (interest.transform.position - _owner.transform.position).sqrMagnitude;

            if (sqrDistance <= _settings.interactRadius * _settings.interactRadius)
            {
                State = BoidState.Interacting;
                _interaction.SetTarget(interest);
                Steering = -_owner.Velocity * 2f
                           + CalculateSeparation() * _settings.separationWeight * 0.5f;
                return;
            }

            State = BoidState.Seeking;
            _interaction.Cancel();
            Steering = CalculateArrive(interest.transform.position) * _settings.arriveWeight
                       + CalculateSeparation() * _settings.separationWeight * 0.5f;
            return;
        }

        State = BoidState.Flocking;
        _interaction.Cancel();
        Steering = CalculateFlocking();
    }

    private Vector3 CalculateFlocking()
    {
        Vector3 separation = CalculateSeparation();
        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();
        Vector3 wander = CalculateWander();
        Vector3 cruise = CalculateCruise();

        // Antes había un "dampen" que apagaba alignment y cohesion de golpe
        // al entrar en el radio de separación. Ese cambio discontinuo producía
        // el zig-zag: se acercaban, se separaban, se volvían a acercar.
        // Ahora los pesos son continuos y wander rompe la simetría.
        return separation * _settings.separationWeight * 1.5f
               + alignment * _settings.alignmentWeight
               + cohesion * _settings.cohesionWeight
               + wander * WanderWeight
               + cruise * CruiseWeight;
    }

    private Vector3 CalculateWander()
    {
        _wanderAngle += Random.Range(-1f, 1f) * WanderJitter * Time.deltaTime;
        _wanderAngle = Mathf.Repeat(_wanderAngle + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;

        Vector3 forward = _owner.transform.forward;
        Vector3 right = _owner.transform.right;
        Vector3 target = _owner.transform.position + forward + right * Mathf.Sin(_wanderAngle);

        return SteeringCalculator.Seek(
            _owner.transform.position,
            _owner.Velocity,
            target,
            _settings.maxSpeed,
            _settings.maxForce);
    }

    private Vector3 CalculateCruise()
    {
        // Mantiene un mínimo de movimiento hacia adelante para que dos boids
        // que se cruzan puedan pasar de largo en vez de quedar orbitando.
        Vector3 desired = _owner.transform.forward * _settings.maxSpeed;

        return SteeringCalculator.CalculateSteering(
            _owner.Velocity,
            desired,
            _settings.maxForce);
    }

    private Vector3 CalculateSeparation()
    {
        return SteeringCalculator.Separation(
            _owner.transform.position,
            _owner.Velocity,
            _sensor.SeparationNeighbors,
            _settings.maxSpeed,
            _settings.maxForce);
    }

    private Vector3 CalculateAlignment()
    {
        return SteeringCalculator.Alignment(
            _owner.Velocity,
            _sensor.Neighbors,
            _settings.maxSpeed,
            _settings.maxForce);
    }

    private Vector3 CalculateCohesion()
    {
        IReadOnlyList<BoidAgent> neighbors = _sensor.Neighbors;
        if (neighbors.Count == 0) return Vector3.zero;

        float totalDistance = 0f;
        int count = 0;

        foreach (BoidAgent other in neighbors)
        {
            if (other == null) continue;

            totalDistance += Vector3.Distance(_owner.transform.position, other.transform.position);
            count++;
        }

        if (count == 0) return Vector3.zero;

        // La cohesión se activa de forma gradual recién cuando el grupo está
        // más lejos que el radio de separación. Así no tira a los boids de
        // vuelta apenas se separan, que era lo que producía el zig-zag.
        float averageDistance = totalDistance / count;
        float scale = Mathf.InverseLerp(
            _settings.separationRadius,
            _settings.viewRadius,
            averageDistance);

        if (scale <= 0f) return Vector3.zero;

        return SteeringCalculator.Cohesion(
            _owner.transform.position,
            _owner.Velocity,
            neighbors,
            _settings.maxSpeed,
            _settings.maxForce) * scale;
    }

    private Vector3 CalculateArrive(Vector3 target)
    {
        return SteeringCalculator.Arrive(
            _owner.transform.position,
            _owner.Velocity,
            target,
            _settings.maxSpeed,
            _settings.maxForce,
            _settings.arriveRadius);
    }

    private Vector3 CalculateEvade()
    {
        HunterAgent hunter = _sensor.NearestHunter;

        return SteeringCalculator.Evade(
            _owner.transform.position,
            _owner.Velocity,
            hunter.transform.position,
            hunter.Velocity,
            _settings.maxSpeed,
            _settings.maxForce);
    }
}
