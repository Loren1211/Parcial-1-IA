using UnityEngine;

/// <summary>
/// Se dirige a la ultima posicion conocida que compartio otro lobo, espera un
/// momento buscando y vuelve a la patrulla.
/// </summary>
public class WolfAlertState : WolfStateBase
{
    private float _startTime;
    private float _arrivedTime;
    private bool _arrived;

    public WolfAlertState(WolfAgent wolf) : base(wolf) { }

    public override void Enter()
    {
        Wolf.LastAction = "Alerta";
        _startTime = Time.time;
        _arrived = false;
        Wolf.GoTo(Wolf.PackAlertPosition, true);
    }

    public override void Update()
    {
        if (Wolf.SeesHunter)
        {
            _fsm.ChangeState(WolfAgent.States.Chase);
            return;
        }

        if (!_arrived)
        {
            Wolf.MoveAlongPath(Wolf.AlertSpeed);

            if (Wolf.Nav.ReachedDestination || Wolf.DistanceTo(Wolf.PackAlertPosition) <= 1.5f)
            {
                _arrived = true;
                _arrivedTime = Time.time;
            }
            else if (Time.time - _startTime > Wolf.SearchDuration + 4f)
            {
                _fsm.ChangeState(WolfAgent.States.Return);
                return;
            }

            return;
        }

        Wolf.Stop();

        if (Time.time - _arrivedTime >= Wolf.SearchDuration)
            _fsm.ChangeState(WolfAgent.States.Return);
    }

    public override void Exit() { }
}
