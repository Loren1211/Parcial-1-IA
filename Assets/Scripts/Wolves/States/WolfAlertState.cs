using UnityEngine;

/// <summary>
/// Alerta: el lobo va a la ultima posicion conocida que compartio la jauria y
/// barre la zona girando sobre si mismo para cubrir los 360 grados con su FOV.
/// Si no encuentra al Cazador vuelve a patrullar. El giro se ve en los gizmos
/// del cono de vision.
/// </summary>
public class WolfAlertState : WolfStateBase
{
    private const float TravelGrace = 4f;
    private const float ScanDegreesPerSecond = 150f;

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
            else if (Time.time - _startTime > Wolf.SearchDuration + TravelGrace)
            {
                _fsm.ChangeState(WolfAgent.States.Return);
            }

            return;
        }

        Wolf.Stop();

        // Gira en el lugar: el cono de vision recorre la zona sin que el lobo
        // se aleje del punto de alerta ni acampe el Coto de caza.
        Wolf.transform.Rotate(0f, ScanDegreesPerSecond * Time.deltaTime, 0f, Space.World);

        if (Time.time - _arrivedTime >= Wolf.SearchDuration)
            _fsm.ChangeState(WolfAgent.States.Return);
    }

    public override void Exit() { }
}
