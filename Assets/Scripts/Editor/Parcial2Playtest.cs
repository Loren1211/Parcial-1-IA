using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Playtest automatico del Parcial 2. Entra en Play, acelera el tiempo y
/// registra que la FSM del Cazador y la de los lobos recorran sus estados,
/// que las recolecciones avancen y que ningun lobo entre a una zona segura.
/// Menu: Tools > Parcial 2 > Run Playtest.
/// En batchmode se ejecuta con -executeMethod Parcial2Playtest.RunPlaytest.
/// </summary>
public static class Parcial2Playtest
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";
    private const string ActiveKey = "Parcial2Playtest.Active";
    private const float MaxRealSeconds = 45f;
    private const float TimeScale = 4f;

    private static readonly HashSet<HunterAgent.States> HunterStatesVisited = new();
    private static readonly HashSet<WolfAgent.States> WolfStatesVisited = new();
    private static readonly HashSet<string> WolfStatesPerWolf = new();

    private static HunterAgent _hunter;
    private static WolfAgent[] _wolves;
    private static BoidAgent[] _boids;
    private static bool _entered;
    private static bool _finished;
    private static float _startRealtime;
    private static float _nextSample;
    private static int _safeZoneViolations;
    private static int _captures;
    private static int _peakGathers;
    private static int _totalGathered;
    private static int _lastGathers;
    private static bool _sawAlert;
    private static HunterAgent.States _lastHunterState;

    [MenuItem("Tools/Parcial 2/Run Playtest")]
    public static void RunPlaytest()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Playtest] Ya hay una partida en curso.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        SessionState.SetBool(ActiveKey, true);

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// El ingreso a Play provoca un domain reload que borra la suscripcion y
    /// los contadores. Al recargar, se vuelve a enganchar el update si la
    /// sesion de playtest sigue activa.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void ReattachAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = false;
        _finished = false;
        _safeZoneViolations = 0;
        _captures = 0;
        _peakGathers = 0;
        _totalGathered = 0;
        _lastGathers = 0;
        _sawAlert = false;
        _nextSample = 0f;
        HunterStatesVisited.Clear();
        WolfStatesVisited.Clear();
        WolfStatesPerWolf.Clear();

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (_finished) return;

        if (!EditorApplication.isPlaying)
        {
            if (!_entered) return;

            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            return;
        }

        if (!_entered)
        {
            _entered = true;
            _startRealtime = Time.realtimeSinceStartup;
            Time.timeScale = TimeScale;

            _hunter = Object.FindFirstObjectByType<HunterAgent>();
            _wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);
            _boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);

            Debug.Log("[Playtest] Inicio: cazador=" + (_hunter != null) +
                " lobos=" + _wolves.Length + " boids=" + _boids.Length +
                " timeScale=" + TimeScale);

            return;
        }

        if (_hunter != null)
        {
            HunterStatesVisited.Add(_hunter.FSM.CurrentStateID);

            // Gathers vuelve a cero al morir; se guarda el pico y el total
            // acumulado para que el reporte no pierda el progreso real.
            if (_hunter.Gathers > _peakGathers) _peakGathers = _hunter.Gathers;

            if (_hunter.Gathers > _lastGathers)
                _totalGathered += _hunter.Gathers - _lastGathers;

            _lastGathers = _hunter.Gathers;

            if (_hunter.FSM.CurrentStateID != _lastHunterState)
            {
                Debug.Log("[Playtest] Cazador " + _lastHunterState + " -> " +
                    _hunter.FSM.CurrentStateID + " | recolecciones " + _hunter.Gathers);

                if (_hunter.FSM.CurrentStateID == HunterAgent.States.Dead) _captures++;

                _lastHunterState = _hunter.FSM.CurrentStateID;
            }
        }

        for (int i = 0; i < _wolves.Length; i++)
        {
            WolfAgent wolf = _wolves[i];
            if (wolf == null) continue;

            WolfAgent.States state = wolf.FSM.CurrentStateID;
            WolfStatesVisited.Add(state);
            WolfStatesPerWolf.Add(wolf.name + ":" + state);

            if (ZoneVolume.IsInsideSafeZone(wolf.transform.position))
                _safeZoneViolations++;
        }

        if (WolfAlertBus.HasRecentAlert) _sawAlert = true;

        if (Time.time >= _nextSample)
        {
            _nextSample = Time.time + 5f;

            Debug.Log("[Playtest] t=" + Time.time.ToString("F1") +
                " | cazador " + (_hunter != null ? _hunter.FSM.CurrentStateID.ToString() : "-") +
                " | pos " + (_hunter != null ? _hunter.transform.position.ToString("F1") : "-") +
                " | recolecciones " + (_hunter != null ? _hunter.Gathers : 0) +
                " | nav " + DescribeNav() +
                " | lobos " + DescribeWolves() +
                " | boids " + DescribeBoids());
        }

        if (Time.realtimeSinceStartup - _startRealtime < MaxRealSeconds) return;

        Finish();
    }

    private static void Finish()
    {
        _finished = true;

        StringBuilder builder = new();
        builder.AppendLine("[Playtest] RESULTADO");
        builder.AppendLine("  Tiempo simulado: " + Time.time.ToString("F1") + " s");
        builder.AppendLine("  Estados del Cazador: " + Join(HunterStatesVisited));
        builder.AppendLine("  Estados de lobos: " + Join(WolfStatesVisited));
        builder.AppendLine("  Lobos en zona segura (muestras): " + _safeZoneViolations);
        builder.AppendLine("  Alertas de jauria: " + (_sawAlert ? "SI" : "NO"));
        builder.AppendLine("  Capturas: " + _captures);
        builder.AppendLine("  Recolecciones: " + (_hunter != null ? _hunter.Gathers : 0) +
            "/" + (_hunter != null ? _hunter.RequiredGathers : 0));
        builder.AppendLine("  Pico de recolecciones: " + _peakGathers +
            " | total acumulado: " + _totalGathered);
        builder.AppendLine("  Boids: " + DescribeBoids());
        builder.AppendLine("  Escape: " + (_hunter != null && _hunter.HasEscaped));
        builder.AppendLine("  Estado final: " + (_hunter != null ? _hunter.FSM.CurrentStateID.ToString() : "-"));

        foreach (string entry in WolfStatesPerWolf)
            builder.AppendLine("  " + entry);

        Debug.Log(builder.ToString());

        Time.timeScale = 1f;
        SessionState.SetBool(ActiveKey, false);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
            return;
        }

        EditorApplication.ExitPlaymode();
    }

    private static string DescribeNav()
    {
        if (_hunter == null || _hunter.Nav == null) return "-";

        return (_hunter.Nav.HasPath ? "path " + _hunter.Nav.Path.Points.Count : "sin path") +
            " | dest " + _hunter.Nav.Destination.ToString("F1") +
            " | idx " + _hunter.Nav.CurrentWaypointIndex;
    }

    private static string DescribeWolves()
    {
        StringBuilder builder = new();

        for (int i = 0; i < _wolves.Length; i++)
        {
            if (_wolves[i] == null) continue;
            if (i > 0) builder.Append(", ");
            builder.Append(_wolves[i].FSM.CurrentStateID);
            builder.Append("@");
            builder.Append(_wolves[i].transform.position.ToString("F1"));
        }

        return builder.ToString();
    }

    private static string DescribeBoids()
    {
        if (_boids == null) return "-";

        int active = 0;
        int dead = 0;
        int collected = 0;
        StringBuilder builder = new();

        for (int i = 0; i < _boids.Length; i++)
        {
            BoidAgent boid = _boids[i];
            if (boid == null) continue;

            if (boid.IsCollected) { collected++; continue; }
            if (boid.IsDead) { dead++; continue; }

            active++;
            if (builder.Length > 0) builder.Append(", ");
            builder.Append(boid.transform.position.ToString("F1"));
        }

        return "vivos=" + active + " muertos=" + dead + " recogidos=" + collected +
            (builder.Length > 0 ? " | " + builder.ToString() : string.Empty);
    }

    private static string Join<T>(HashSet<T> set)
    {
        StringBuilder builder = new();

        foreach (T value in set)
        {
            if (builder.Length > 0) builder.Append(", ");
            builder.Append(value);
        }

        return builder.ToString();
    }
}
