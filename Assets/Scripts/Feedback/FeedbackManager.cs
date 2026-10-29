using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centro de feedback: contadores, log de eventos y textos flotantes.
/// Se auto-crea si la escena no lo tiene, para que el HUD funcione siempre.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }
    public static bool ShowGizmos { get; set; } = true;

    [SerializeField] private int _maxLogEntries = 8;
    [SerializeField] private float _floatingTextLifetime = 1.6f;

    private List<LogEntry> _log = new();
    private List<FloatingText> _floatingTexts = new();
    private List<Tracer> _tracers = new();

    public int Kills { get; private set; }
    public int Gathers { get; private set; }
    public int BaitsSpawned { get; private set; }
    public int BaitsDestroyed { get; private set; }
    public int Respawns { get; private set; }

    public IReadOnlyList<LogEntry> Log => _log;
    public IReadOnlyList<FloatingText> FloatingTexts => _floatingTexts;
    public IReadOnlyList<Tracer> Tracers => _tracers;

    public class LogEntry
    {
        public string Time;
        public string Message;
    }

    public class FloatingText
    {
        public Vector3 Position;
        public string Text;
        public Color Color;
        public float Life;
        public float MaxLife;
    }

    public class Tracer
    {
        public Vector3 From;
        public Vector3 To;
        public Color Color;
        public float Life;
        public float MaxLife;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        FeedbackManager existing = UnityEngine.Object.FindFirstObjectByType<FeedbackManager>();

        if (existing != null)
        {
            Instance = existing;

            if (UnityEngine.Object.FindFirstObjectByType<FeedbackHUD>() == null)
                existing.gameObject.AddComponent<FeedbackHUD>();

            return;
        }

        GameObject go = new GameObject("--- FEEDBACK ---");
        go.AddComponent<FeedbackManager>();
        go.AddComponent<FeedbackHUD>();
    }

    private void Awake()
    {
        _log ??= new List<LogEntry>();
        _floatingTexts ??= new List<FloatingText>();
        _tracers ??= new List<Tracer>();

        if (_maxLogEntries <= 0) _maxLogEntries = 8;
        if (_floatingTextLifetime <= 0f) _floatingTextLifetime = 1.6f;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            FloatingText text = _floatingTexts[i];
            text.Life -= Time.deltaTime;
            text.Position += Vector3.up * (0.6f * Time.deltaTime);

            if (text.Life <= 0f)
                _floatingTexts.RemoveAt(i);
        }

        for (int i = _tracers.Count - 1; i >= 0; i--)
        {
            Tracer tracer = _tracers[i];
            tracer.Life -= Time.deltaTime;

            if (tracer.Life <= 0f)
                _tracers.RemoveAt(i);
        }
    }

    public void AddLog(string message)
    {
        _log ??= new List<LogEntry>();
        _log.Add(new LogEntry
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = message
        });

        while (_log.Count > Mathf.Max(1, _maxLogEntries))
            _log.RemoveAt(0);
    }

    public void SpawnFloatingText(Vector3 position, string text, Color color)
    {
        _floatingTexts ??= new List<FloatingText>();
        _floatingTexts.Add(new FloatingText
        {
            Position = position,
            Text = text,
            Color = color,
            Life = _floatingTextLifetime,
            MaxLife = _floatingTextLifetime
        });
    }

    public void SpawnTracer(Vector3 from, Vector3 to, Color color, float lifetime = 0.18f)
    {
        _tracers ??= new List<Tracer>();
        _tracers.Add(new Tracer
        {
            From = from,
            To = to,
            Color = color,
            Life = lifetime,
            MaxLife = lifetime
        });
    }

    public void NotifyHunterStateChanged(HunterAgent.States previous, HunterAgent.States current)
    {
        if (previous == current) return;

        AddLog($"CAZADOR: {previous} -> {current}");
    }

    public void NotifyAttack(BoidAgent target, bool isMelee, Vector3 from)
    {
        if (target == null) return;

        string label = isMelee ? "MELEE" : "RANGE";
        Color color = isMelee ? Color.red : new Color(1f, 0.55f, 0f);

        AddLog($"{label}: {target.name}");
        SpawnFloatingText(target.transform.position + Vector3.up * 1.2f, label, color);
        SpawnTracer(from, target.transform.position + Vector3.up * 0.8f, color);
    }

    public void NotifyKill(BoidAgent target)
    {
        if (target == null) return;

        Kills++;
        AddLog($"KILL: {target.name}");
        SpawnFloatingText(target.transform.position + Vector3.up * 1.6f, "KILL", Color.red);
    }

    public void NotifyGather(BoidAgent target)
    {
        if (target == null) return;

        Gathers++;
        AddLog($"RECOLECTADO: {target.name}");
        SpawnFloatingText(target.transform.position + Vector3.up * 1.2f, "GATHER", Color.yellow);
    }

    public void NotifyInterestSpawned(Vector3 position)
    {
        BaitsSpawned++;
        AddLog("CEBO GENERADO");
        SpawnFloatingText(position + Vector3.up, "+ CEBO", Color.cyan);
    }

    public void NotifyInterestDestroyed(Vector3 position)
    {
        BaitsDestroyed++;
        AddLog("CEBO DESTRUIDO");
        SpawnFloatingText(position + Vector3.up, "CEBO DESTRUIDO", new Color(0.8f, 0.8f, 0.8f));
    }

    public void NotifyBoidDied(BoidAgent boid)
    {
        if (boid == null) return;

        AddLog($"MUERTO: {boid.name}");
        SpawnFloatingText(boid.transform.position + Vector3.up, "MUERTO", new Color(0.6f, 0.6f, 0.6f));
    }

    public void NotifyRespawn(BoidAgent boid)
    {
        if (boid == null) return;

        Respawns++;
        AddLog($"RESPAWN: {boid.name}");
        SpawnFloatingText(boid.transform.position + Vector3.up, "RESPAWN", Color.blue);
    }
}


