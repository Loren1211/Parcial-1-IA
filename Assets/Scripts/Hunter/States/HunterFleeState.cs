using UnityEngine;

/// <summary>
/// Huida: elige la zona segura mas cercana, navega con A* y espera
/// SafeZoneWaitTime adentro. Mientras esta en la zona es inmune a los lobos.
/// </summary>
public class HunterFleeState : HunterStateBase
{
    private float _waitTimer;
    private float _totalTimer;
    private bool _completed;

    public HunterFleeState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Hunter.LastAction = "Huyendo";
        _waitTimer = 0f;
        _totalTimer = 0f;
        _completed = false;
        Hunter.Motor.SpeedMultiplier = Hunter.FleeSpeedMultiplier;

        ZoneVolume safeZone = ZoneVolume.FindNearest(ZoneKind.Safe, Hunter.transform.position);
        Hunter.ChosenSafeZone = safeZone;

        if (safeZone == null)
        {
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        Hunter.LastAction = "Huir a " + safeZone.name;
        FeedbackManager.Instance?.AddLog("CAZADOR: huye a " + safeZone.name);
        FeedbackManager.Instance?.SpawnFloatingText(
            Hunter.transform.position + Vector3.up * 2f,
            "HUIR",
            new Color(0.7f, 0.4f, 1f));

        Vector3 destination = safeZone.RandomPoint(1f, 0f) + Vector3.up;
        Hunter.Nav?.SetDestination(destination, true);
    }

    public override void Update()
    {
        if (Hunter.ChosenSafeZone == null)
        {
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        _totalTimer += Time.deltaTime;

        // Salvaguarda: si no logra entrar o esperar (p. ej. queda oscilando
        // en el umbral) no se queda en Flee para siempre.
        if (_totalTimer >= Hunter.SafeZoneWaitTime + 6f)
        {
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        if (Hunter.IsImmune)
        {
            Vector3 center = Hunter.ChosenSafeZone.Center;
            Vector3 flatCenter = new(center.x, Hunter.transform.position.y, center.z);

            if (Vector3.Distance(Hunter.transform.position, flatCenter) > 0.75f)
            {
                MoveTowards(flatCenter);
                return;
            }

            Stop();
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= Hunter.SafeZoneWaitTime)
            {
                _completed = true;
                _fsm.ChangeState(Hunter.IdleState);
            }

            return;
        }

        _waitTimer = 0f;
        NavigateTowards(Hunter.ChosenSafeZone.Center + Vector3.up);
    }

    public override void Exit()
    {
        Hunter.Motor.SpeedMultiplier = 1f;

        // Solo si completo la espera: interrumpir la huida (muerte, escape)
        // no debe bloquear la siguiente reaccion ante un lobo.
        if (_completed)
            Hunter.CommitAfterRefuge();

        Hunter.ChosenSafeZone = null;
        _waitTimer = 0f;
        _completed = false;
    }
}
