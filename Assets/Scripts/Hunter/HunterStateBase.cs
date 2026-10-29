using UnityEngine;

/// <summary>
/// Base común de los estados del cazador. Evita repetir el acceso al motor,
/// el movimiento y la evaluación de prioridad entre estados.
/// Prioridad del Parcial 2: Dead > Flee > Exit > Gather > Attack > Search.
/// </summary>
public abstract class HunterStateBase : State<HunterAgent.States>
{
    protected readonly HunterAgent Hunter;

    protected HunterStateBase(HunterAgent hunter)
    {
        Hunter = hunter;
    }

    /// <summary>
    /// Evalúa las transiciones de mayor prioridad desde el estado actual.
    /// Devuelve true si ya cambió de estado y el Update debe cortar.
    /// </summary>
    protected bool CheckPriorityTransitions()
    {
        if (!Hunter.IsParcial2) return false;

        HunterAgent.States current = _fsm.CurrentStateID;

        // Se evalua en orden de prioridad y se corta en la primera condicion
        // aplicable, aunque ya sea el estado actual. Si no, al estar en Gather
        // con un cadaver cerca se saltaba a Attack por un boid vivo y la FSM
        // oscilaba entre ambos sin completar la recoleccion.
        if (Hunter.IsCaptured)
        {
            if (current != HunterAgent.States.Dead)
            {
                _fsm.ChangeState(HunterAgent.States.Dead);
                return true;
            }

            return false;
        }

        if (Hunter.ShouldFlee)
        {
            if (current != HunterAgent.States.Flee)
            {
                _fsm.ChangeState(HunterAgent.States.Flee);
                return true;
            }

            return false;
        }

        if (Hunter.ShouldExit)
        {
            if (current != HunterAgent.States.Exit)
            {
                _fsm.ChangeState(HunterAgent.States.Exit);
                return true;
            }

            return false;
        }

        if (Hunter.HasGatherableBoid)
        {
            if (current != HunterAgent.States.Gather)
            {
                _fsm.ChangeState(HunterAgent.States.Gather);
                return true;
            }

            return false;
        }

        if (Hunter.CanAttack() && Hunter.Sensor.HasActiveBoids)
        {
            if (current != HunterAgent.States.Attack)
            {
                _fsm.ChangeState(HunterAgent.States.Attack);
                return true;
            }

            return false;
        }

        return false;
    }

    protected void MoveTowards(Vector3 position)
    {
        Hunter.Motor.MoveTowards(position, Time.deltaTime);
    }

    protected void NavigateTowards(Vector3 position, bool force = false)
    {
        if (!Hunter.IsParcial2 || Hunter.Nav == null)
        {
            MoveTowards(position);
            return;
        }

        Vector3 flat = new(position.x, Hunter.transform.position.y, position.z);
        Hunter.Nav.SetDestination(flat, force);
        Hunter.Motor.MoveTowards(Hunter.Nav.GetMoveTarget(), Time.deltaTime);
    }

    protected void Stop()
    {
        Hunter.Motor.Stop();
    }

    protected void FaceTarget(Vector3 position)
    {
        Vector3 direction = position - Hunter.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            Hunter.Motor.FaceDirection(direction);
    }
}
