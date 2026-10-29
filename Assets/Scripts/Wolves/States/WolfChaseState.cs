using UnityEngine;

/// <summary>
/// Persecucion: navega con A* hacia la posicion actual del Cazador, actualiza
/// la ultima posicion conocida y captura si lo alcanza fuera de una zona
/// segura. Si lo pierde, sigue un tiempo la ultima posicion y luego regresa.
/// </summary>
public class WolfChaseState : WolfStateBase
{
    private const float RefugeGiveUpTime = 0.75f;

    private float _blockedTimer;

    public WolfChaseState(WolfAgent wolf) : base(wolf) { }

    public override void Enter()
    {
        Wolf.LastAction = "Persecucion";
        _blockedTimer = 0f;

        HunterAgent hunter = Wolf.Hunter;
        if (hunter != null)
            Wolf.GoTo(hunter.transform.position, true);
    }

    public override void Update()
    {
        HunterAgent hunter = Wolf.Hunter;

        if (hunter == null || hunter.IsCaptured)
        {
            _fsm.ChangeState(WolfAgent.States.Return);
            return;
        }

        // El Cazador esta en un refugio: el lobo no puede entrar. Tras un
        // tiempo prudencial pierde el rastro y vuelve a patrullar, para no
        // acampar la salida de la zona segura.
        if (ZoneVolume.IsInsideWolfExcluded(hunter.transform.position))
        {
            _blockedTimer += UnityEngine.Time.deltaTime;
            Wolf.Stop();

            if (_blockedTimer >= RefugeGiveUpTime)
            {
                Wolf.LastAction = "Pierde el rastro en el refugio";
                Wolf.IgnoreHunter(4f);
                _fsm.ChangeState(WolfAgent.States.Return);
            }

            return;
        }

        _blockedTimer = Mathf.Max(0f, _blockedTimer - UnityEngine.Time.deltaTime);

        if (Wolf.SeesHunter)
        {
            Wolf.GoTo(hunter.transform.position);
        }
        else if (UnityEngine.Time.time - Wolf.LastSeenTime > Wolf.MemoryDuration)
        {
            _fsm.ChangeState(WolfAgent.States.Return);
            return;
        }

        Wolf.MoveAlongPath(Wolf.ChaseSpeed);

        if (Wolf.CanCapture(hunter))
        {
            hunter.OnCapturedByWolf(Wolf);
            _fsm.ChangeState(WolfAgent.States.Return);
        }
    }

    public override void Exit() { }
}
