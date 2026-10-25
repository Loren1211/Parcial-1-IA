using UnityEngine;

/// <summary>
/// Decide el comportamiento del boid por prioridad:
/// Evade > Interactuar > Arrive > Flocking.
/// </summary>
public class BoidBehaviorController
{
    private readonly BoidAgent _owner;
    private readonly BoidSensor _sensor;
    private readonly BoidInteraction _interaction;
    private readonly BoidSettings _settings;

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
        if (interest != null)
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
        bool tooClose = separation.sqrMagnitude > 0.0001f;

        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();
        float dampen = tooClose ? _settings.separationDampen : 1f;

        return separation * _settings.separationWeight * 1.5f
               + alignment * _settings.alignmentWeight * dampen
               + cohesion * _settings.cohesionWeight * dampen;
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
        return SteeringCalculator.Cohesion(
            _owner.transform.position,
            _owner.Velocity,
            _sensor.Neighbors,
            _settings.maxSpeed,
            _settings.maxForce);
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
