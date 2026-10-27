using UnityEngine;

/// <summary>
/// Base común de los estados del cazador. Evita repetir el acceso al motor
/// y el movimiento en cada estado.
/// </summary>
public abstract class HunterStateBase : State<HunterAgent.States>
{
    protected readonly HunterAgent Hunter;

    protected HunterStateBase(HunterAgent hunter)
    {
        Hunter = hunter;
    }

    protected void MoveTowards(Vector3 position)
    {
        Hunter.Motor.MoveTowards(position, Time.deltaTime);
    }

    protected void FaceTarget(Vector3 position)
    {
        Vector3 direction = position - Hunter.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            Hunter.Motor.FaceDirection(direction);
    }
}
