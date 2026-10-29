using UnityEngine;

/// <summary>
/// Muerte por captura de un lobo: oculta al Cazador, espera RespawnTime y
/// reaparece en la zona inicial con el contador de recolecciones en cero.
/// </summary>
public class HunterDeadState : HunterStateBase
{
    private float _timer;

    public HunterDeadState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        _timer = Hunter.RespawnTime;
        Hunter.LastAction = "Capturado";
        Hunter.SetRendererEnabled(false);
        Stop();

        string captor = Hunter.LastCaptor != null ? Hunter.LastCaptor.name : "un lobo";

        FeedbackManager.Instance?.AddLog("CAZADOR CAPTURADO por " + captor);
        FeedbackManager.Instance?.SpawnFloatingText(
            Hunter.transform.position + Vector3.up * 2f,
            "CAPTURADO",
            new Color(1f, 0.15f, 0.15f));
    }

    public override void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer > 0f) return;

        Hunter.transform.position = Hunter.GetRespawnPosition();
        Hunter.SetRendererEnabled(true);
        Hunter.NotifyRespawned();

        FeedbackManager.Instance?.AddLog("CAZADOR: respawn en la zona inicial");
        FeedbackManager.Instance?.SpawnFloatingText(
            Hunter.transform.position + Vector3.up * 2f,
            "RESPAWN",
            new Color(0.3f, 0.6f, 1f));

        _fsm.ChangeState(Hunter.IdleState);
    }

    public override void Exit()
    {
        Hunter.SetRendererEnabled(true);
    }
}
