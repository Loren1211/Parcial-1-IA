using System;
using UnityEngine;

/// <summary>
/// Bus de alertas de la jauria. Un lobo que ve al Cazador aulla y comparte su
/// ultima posicion conocida. El cooldown evita saturar el log y el feedback.
/// </summary>
public static class WolfAlertBus
{
    public static event Action<WolfAgent, Vector3> AlertRaised;

    private const float Cooldown = 2f;
    private const float MemoryDuration = 7f;

    private static float _lastAlertTime = -999f;
    private static Vector3 _lastKnownPosition;

    public static Vector3 LastKnownPosition => _lastKnownPosition;
    public static float LastAlertTime => _lastAlertTime;
    public static bool HasRecentAlert => Time.time - _lastAlertTime <= MemoryDuration;

    public static void RaiseAlert(WolfAgent source, Vector3 position)
    {
        _lastKnownPosition = position;

        if (Time.time - _lastAlertTime < Cooldown) return;

        _lastAlertTime = Time.time;

        AlertRaised?.Invoke(source, position);

        FeedbackManager manager = FeedbackManager.Instance;
        if (manager == null) return;

        manager.AddLog("AULLIDO: " + source.name + " marco al Cazador");
        manager.SpawnFloatingText(
            position + Vector3.up * 2.2f,
            "AULLIDO",
            new Color(1f, 0.55f, 0.1f));
    }

    public static void Reset()
    {
        _lastAlertTime = -999f;
        _lastKnownPosition = Vector3.zero;
        AlertRaised = null;
    }
}
