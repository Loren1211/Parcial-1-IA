using UnityEngine;

/// <summary>
/// Salida: con las recolecciones requeridas, navega hasta la zona de salida
/// y termina la partida.
/// </summary>
public class HunterExitState : HunterStateBase
{
    private const float ArriveRadius = 1.5f;

    public HunterExitState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Hunter.LastAction = "Yendo a la salida";

        ZoneVolume exit = ZoneVolume.FindNearest(ZoneKind.Exit, Hunter.transform.position);

        if (exit == null)
        {
            FeedbackManager.Instance?.AddLog("SALIDA NO ENCONTRADA");
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        FeedbackManager.Instance?.AddLog("CAZADOR: va a la salida");
        FeedbackManager.Instance?.SpawnFloatingText(
            Hunter.transform.position + Vector3.up * 2f,
            "SALIDA",
            new Color(0.3f, 0.7f, 1f));

        Hunter.Nav?.SetDestination(exit.Center, true);
    }

    public override void Update()
    {
        ZoneVolume exit = ZoneVolume.FindNearest(ZoneKind.Exit, Hunter.transform.position);

        if (exit == null)
        {
            _fsm.ChangeState(Hunter.IdleState);
            return;
        }

        if (Vector3.Distance(Hunter.transform.position, exit.Center) <= ArriveRadius)
        {
            Stop();
            Hunter.NotifyEscaped();
            Hunter.LastAction = "Escape completado";

            FeedbackManager.Instance?.AddLog("CAZADOR: ESCAPE COMPLETADO");
            FeedbackManager.Instance?.SpawnFloatingText(
                Hunter.transform.position + Vector3.up * 2f,
                "ESCAPE",
                new Color(0.3f, 0.8f, 1f));

            return;
        }

        NavigateTowards(exit.Center);
    }

    public override void Exit() { }
}
