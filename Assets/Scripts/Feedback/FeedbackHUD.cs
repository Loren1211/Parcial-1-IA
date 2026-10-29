using UnityEngine;

/// <summary>
/// Ãšnico OnGUI del proyecto. Muestra HUD, leyenda, log, lista de boids y
/// etiquetas 3D. F1 alterna HUD, F2 etiquetas y F3 gizmos.
/// </summary>
public class FeedbackHUD : MonoBehaviour
{
    private bool _showHud = true;
    private bool _showLabels = true;

    private HunterAgent _hunter;
    private BoidAgent[] _boids = System.Array.Empty<BoidAgent>();
    private WolfAgent[] _wolves = System.Array.Empty<WolfAgent>();
    private InterestObject[] _interests = System.Array.Empty<InterestObject>();
    private InterestObjectTracker _tracker;
    private Camera _camera;

    private float _refreshTimer;

    private GUIStyle _panelStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _centerStyle;
    private Texture2D _panelTexture;

    private void Awake()
    {
        _showHud = true;
        _showLabels = true;
        RefreshReferences();
    }

    private void Update()
    {
        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.5f;
            RefreshReferences();
        }
    }

    private void OnDestroy()
    {
        if (_panelTexture != null)
            Destroy(_panelTexture);
    }

    private void RefreshReferences()
    {
        _camera = Camera.main;
        _hunter = FindFirstObjectByType<HunterAgent>();
        _tracker = FindFirstObjectByType<InterestObjectTracker>();
        _boids = FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        _wolves = FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);
        _interests = FindObjectsByType<InterestObject>(FindObjectsSortMode.None);
    }

    private void OnGUI()
    {
        EnsureStyles();
        HandleInput();

        if (_showLabels)
            DrawWorldLabels();

        if (_showHud)
            DrawHud();
    }

    /// <summary>
    /// Las teclas se leen desde el evento IMGUI y no desde UnityEngine.Input
    /// porque el proyecto usa el Input System nuevo. Así el HUD funciona sin
    /// importar cuál sea el "Active Input Handling" del proyecto.
    /// </summary>
    private void HandleInput()
    {
        Event current = Event.current;

        if (current == null || current.type != EventType.KeyDown) return;

        switch (current.keyCode)
        {
            case KeyCode.F1:
                _showHud = !_showHud;
                current.Use();
                break;

            case KeyCode.F2:
                _showLabels = !_showLabels;
                current.Use();
                break;

            case KeyCode.F3:
                FeedbackManager.ShowGizmos = !FeedbackManager.ShowGizmos;
                FeedbackManager.Instance?.AddLog($"Gizmos: {(FeedbackManager.ShowGizmos ? "ON" : "OFF")}");
                current.Use();
                break;
        }
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null) return;

        _panelTexture = new Texture2D(1, 1);
        _panelTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
        _panelTexture.Apply();

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _panelTexture },
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(8, 8, 8, 8)
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            richText = true,
            normal = { textColor = Color.white }
        };

        _headerStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        _smallStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 10
        };

        _centerStyle = new GUIStyle(_labelStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };
    }

    private void DrawHud()
    {
        FeedbackManager manager = FeedbackManager.Instance;

        DrawHunterPanel(manager);
        DrawLegend();
        DrawZoneLegend();
        DrawEventLog(manager);
        DrawBoidList();
    }

    private void DrawHunterPanel(FeedbackManager manager)
    {
        Rect panel = new Rect(10f, 10f, 420f, 266f);
        DrawPanel(panel);

        string state = _hunter != null ? _hunter.FSM.CurrentStateID.ToString() : "Sin cazador";
        string target = _hunter != null && _hunter.CurrentTarget != null
            ? _hunter.CurrentTarget.name
            : "Ninguno";
        float tba = _hunter != null ? _hunter.TbaRemaining : 0f;
        float tbaMax = _hunter != null ? Mathf.Max(0.01f, _hunter.TBA) : 1f;
        int detected = _hunter != null ? _hunter.DetectedBoidsCount : 0;
        int baits = _tracker != null ? _tracker.ActiveCount : 0;
        int maxBaits = _hunter != null ? _hunter.MaxInterestObjects : 5;
        int kills = manager != null ? manager.Kills : 0;
        int respawns = manager != null ? manager.Respawns : 0;

        int gathers = _hunter != null ? _hunter.Gathers : 0;
        int required = _hunter != null ? _hunter.RequiredGathers : 3;
        int pathNodes = _hunter != null && _hunter.Nav != null && _hunter.Nav.HasPath
            ? _hunter.Nav.Path.Points.Count
            : 0;
        string safeZone = _hunter != null && _hunter.ChosenSafeZone != null
            ? _hunter.ChosenSafeZone.name
            : "Ninguna";
        string immunity = _hunter != null && _hunter.IsImmune ? " | INMUNE" : "";
        string escaped = _hunter != null && _hunter.HasEscaped ? " | ESCAPÓ" : "";

        float y = panel.y + 8f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 22f), "PARCIAL 2 - IA", _headerStyle);
        y += 24f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Cazador: {state}   |   Objetivo: {target}", _labelStyle);
        y += 20f;

        DrawBar(new Rect(panel.x + 10f, y, panel.width - 20f, 9f),
            1f - Mathf.Clamp01(tba / tbaMax),
            tba > 0f ? new Color(0.9f, 0.35f, 0.1f) : new Color(0.2f, 0.9f, 0.3f));
        GUI.Label(new Rect(panel.x + 14f, y - 2f, panel.width - 28f, 14f),
            tba > 0f ? $"TBA {tba:F1}s" : "TBA listo", _smallStyle);
        y += 16f;

        DrawBar(new Rect(panel.x + 10f, y, panel.width - 20f, 8f),
            required > 0 ? Mathf.Clamp01(gathers / (float)required) : 0f,
            new Color(1f, 0.85f, 0.1f));
        GUI.Label(new Rect(panel.x + 14f, y - 3f, panel.width - 28f, 14f),
            $"RECOLECCIONES {gathers}/{required}{escaped}", _smallStyle);
        y += 18f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Lobos: {_wolves.Length}   |   Zona segura: {safeZone}{immunity}", _labelStyle);
        y += 20f;

        string alert = WolfAlertBus.HasRecentAlert
            ? $"ALERTA en {WolfAlertBus.LastKnownPosition.ToString("F1")}"
            : "sin alertas";

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Jauría: {alert}", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Boids: {_boids.Length}   |   Detectados: {detected}   |   A*: {pathNodes} nodos", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Kills: {kills}   |   Cebos activos: {baits}/{maxBaits}", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Respawns: {respawns}", _labelStyle);
        y += 20f;

        if (_hunter != null && _hunter.FSM.CurrentStateID == HunterAgent.States.Gather)
        {
            DrawBar(new Rect(panel.x + 10f, y, panel.width - 20f, 8f),
                _hunter.GatherProgress,
                new Color(1f, 0.85f, 0.1f));
            GUI.Label(new Rect(panel.x + 14f, y - 3f, panel.width - 28f, 14f),
                $"RECOLECTANDO {_hunter.GatherProgress * 100f:F0}%", _smallStyle);
        }
        else
        {
            GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
                $"Última acción: {(_hunter != null ? _hunter.LastAction : "-")}", _smallStyle);
        }

        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            "F1 HUD  |  F2 Etiquetas  |  F3 Gizmos", _smallStyle);
    }

    private void DrawBar(Rect rect, float factor, Color color)
    {
        Color old = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(factor), rect.height),
            Texture2D.whiteTexture);

        GUI.color = old;
    }

    private void DrawLegend()
    {
        Rect panel = new Rect(Screen.width - 300f, 10f, 290f, 34f + 16f * 19f);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 22f), "LEYENDA", _headerStyle);

        float y = panel.y + 34f;

        DrawLegendRow(panel.x + 12f, y, new Color(0.2f, 1f, 0.45f), "Boid: Flocking");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.1f, 0.8f, 1f), "Boid: Seeking / Arrive");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.9f, 0.1f), "Boid: Interacting");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.1f, 0.3f), "Boid: Evading");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.25f, 0.25f, 0.25f), "Boid: Muerto");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.2f, 0.5f, 1f), "Cazador: Search / Patrol");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.15f, 0.15f), "Cazador: Attack");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.85f, 0.1f), "Cazador: Gather");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.7f, 0.4f, 1f), "Cazador: Flee");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.3f, 0.8f, 1f), "Cazador: Exit");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.25f, 0.25f, 0.25f), "Cazador: Dead");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.6f, 0.6f, 0.65f), "Lobo: Patrol");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.55f, 0.1f), "Lobo: Alert");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.9f, 0.1f, 0.15f), "Lobo: Chase");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.25f, 0.45f, 1f), "Lobo: Return");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(0.2f, 1f, 0.4f), "Cebo (vida)");
    }

    private void DrawLegendRow(float x, float y, Color color, string text)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y + 2f, 13f, 13f), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(x + 20f, y, 250f, 18f), text, _labelStyle);
    }

    private void DrawZoneLegend()
    {
        var zones = ZoneVolume.All;
        if (zones == null || zones.Count == 0) return;

        float rowHeight = 18f;
        float height = 34f + zones.Count * rowHeight;
        Rect panel = new Rect(10f, 286f, 420f, height);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 22f), "ZONAS", _headerStyle);

        float y = panel.y + 34f;

        for (int i = 0; i < zones.Count; i++)
        {
            ZoneVolume zone = zones[i];
            if (zone == null) continue;

            DrawLegendRow(panel.x + 12f, y, zone.Color, $"{zone.name} ({ZoneLabel(zone.Kind)})");
            y += rowHeight;
        }
    }

    private static string ZoneLabel(ZoneKind kind)
    {
        switch (kind)
        {
            case ZoneKind.Initial: return "inicio";
            case ZoneKind.HuntingGround: return "coto";
            case ZoneKind.Safe: return "segura";
            case ZoneKind.Exit: return "salida";
            case ZoneKind.Room: return "sala";
            case ZoneKind.Corridor: return "pasillo";
            default: return kind.ToString();
        }
    }

    private void DrawEventLog(FeedbackManager manager)
    {
        Rect panel = new Rect(10f, Screen.height - 178f, 470f, 168f);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 20f), "EVENTOS", _headerStyle);

        if (manager == null)
        {
            GUI.Label(new Rect(panel.x + 10f, panel.y + 34f, panel.width - 20f, 18f),
                "Sin FeedbackManager en la escena.", _smallStyle);
            return;
        }

        float y = panel.y + 32f;

        for (int i = 0; i < manager.Log.Count; i++)
        {
            FeedbackManager.LogEntry entry = manager.Log[i];
            GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 16f),
                $"[{entry.Time}] {entry.Message}", _smallStyle);
            y += 16f;
        }
    }

    private void DrawBoidList()
    {
        Rect panel = new Rect(Screen.width - 410f, Screen.height - 218f, 400f, 208f);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 20f), "BOIDS", _headerStyle);

        float y = panel.y + 32f;

        for (int i = 0; i < _boids.Length; i++)
        {
            BoidAgent boid = _boids[i];
            if (boid == null) continue;

            string status;

            if (boid.IsCollected)
                status = $"RECOLECTADO {boid.RespawnTimeRemaining:F1}s";
            else if (boid.IsDead)
                status = "MUERTO";
            else
                status = $"{boid.State} | HP {boid.Health:F0}/{boid.MaxHealth:F0}";

            string interest = boid.CurrentInterest != null ? " | cebo" : "";

            GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
                $"{boid.name}: {status}{interest}", _smallStyle);

            y += 18f;
        }
    }

    private void DrawWorldLabels()
    {
        if (_camera == null) return;

        for (int i = 0; i < _boids.Length; i++)
        {
            BoidAgent boid = _boids[i];
            if (boid == null) continue;

            Vector3 world = boid.transform.position + Vector3.up * 1.15f;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) continue;

            Vector2 point = new Vector2(screen.x, Screen.height - screen.y);

            if (boid.IsCollected)
            {
                DrawWorldLabel(point, $"{boid.name} | RECOLECTADO {boid.RespawnTimeRemaining:F1}s",
                    new Color(0.35f, 0.65f, 1f));
                continue;
            }

            if (boid.IsDead)
            {
                DrawCross(new Vector2(point.x, point.y + 8f), 14f, new Color(1f, 0.15f, 0.15f));
                DrawWorldLabel(point, $"{boid.name} | MUERTO", new Color(0.7f, 0.7f, 0.7f));
                continue;
            }

            string interest = boid.CurrentInterest != null ? " | cebo" : "";
            DrawWorldLabel(point,
                $"{boid.name} | {boid.State}{interest} | {boid.Health:F0}/{boid.MaxHealth:F0}",
                GetBoidColor(boid.State));

            DrawHealthBar(new Vector2(point.x, point.y + 14f), boid.Health, boid.MaxHealth);
        }

        if (_hunter != null)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_hunter.transform.position + Vector3.up * 1.5f);

            if (screen.z > 0f)
            {
                string target = _hunter.CurrentTarget != null ? _hunter.CurrentTarget.name : "Ninguno";
                string label = $"HUNTER | {_hunter.FSM.CurrentStateID} | TBA {_hunter.TbaRemaining:F1}s | {target}";

                DrawWorldLabel(
                    new Vector2(screen.x, Screen.height - screen.y),
                    label,
                    GetHunterColor(_hunter.FSM.CurrentStateID));
            }

            DrawHunterTargetHelpers();
        }

        for (int i = 0; i < _wolves.Length; i++)
        {
            WolfAgent wolf = _wolves[i];
            if (wolf == null) continue;

            Vector3 screen = _camera.WorldToScreenPoint(wolf.transform.position + Vector3.up * 1.5f);
            if (screen.z <= 0f) continue;

            string label = $"{wolf.name} | {wolf.FSM.CurrentStateID}";

            DrawWorldLabel(
                new Vector2(screen.x, Screen.height - screen.y),
                label,
                GetWolfColor(wolf.FSM.CurrentStateID));
        }

        for (int i = 0; i < _interests.Length; i++)
        {
            InterestObject interest = _interests[i];
            if (interest == null || !interest.IsAlive) continue;

            Vector3 screen = _camera.WorldToScreenPoint(interest.transform.position + Vector3.up * 1.0f);
            if (screen.z <= 0f) continue;

            Vector2 point = new(screen.x, Screen.height - screen.y);

            float healthFactor = interest.MaxHealth > 0f
                ? Mathf.Clamp01(interest.Health / interest.MaxHealth)
                : 0f;

            Color color = Color.Lerp(
                new Color(1f, 0.15f, 0.1f),
                new Color(0.2f, 1f, 0.4f),
                healthFactor);

            DrawWorldLabel(point, $"CEBO {interest.Health:F0}/{interest.MaxHealth:F0}", color);
            DrawHealthBar(new Vector2(point.x, point.y + 14f), interest.Health, interest.MaxHealth);
        }

        FeedbackManager manager = FeedbackManager.Instance;
        if (manager == null) return;

        for (int i = 0; i < manager.Tracers.Count; i++)
        {
            FeedbackManager.Tracer tracer = manager.Tracers[i];

            Vector3 fromScreen = _camera.WorldToScreenPoint(tracer.From);
            Vector3 toScreen = _camera.WorldToScreenPoint(tracer.To);
            if (fromScreen.z <= 0f || toScreen.z <= 0f) continue;

            float alpha = Mathf.Clamp01(tracer.Life / Mathf.Max(0.01f, tracer.MaxLife));

            DrawLine(
                new Vector2(fromScreen.x, Screen.height - fromScreen.y),
                new Vector2(toScreen.x, Screen.height - toScreen.y),
                2f,
                new Color(tracer.Color.r, tracer.Color.g, tracer.Color.b, alpha));
        }

        for (int i = 0; i < manager.FloatingTexts.Count; i++)
        {
            FeedbackManager.FloatingText text = manager.FloatingTexts[i];
            Vector3 screen = _camera.WorldToScreenPoint(text.Position);
            if (screen.z <= 0f) continue;

            float alpha = Mathf.Clamp01(text.Life / Mathf.Max(0.01f, text.MaxLife));

            Color old = GUI.color;
            GUI.color = new Color(text.Color.r, text.Color.g, text.Color.b, alpha);
            GUI.Label(
                new Rect(screen.x - 90f, Screen.height - screen.y - 10f, 180f, 20f),
                text.Text,
                _centerStyle);
            GUI.color = old;
        }
    }

    private void DrawHunterTargetHelpers()
    {
        if (_hunter == null || _hunter.CurrentTarget == null) return;

        Vector3 fromScreen = _camera.WorldToScreenPoint(_hunter.transform.position + Vector3.up * 0.8f);
        Vector3 toScreen = _camera.WorldToScreenPoint(_hunter.CurrentTarget.transform.position + Vector3.up * 0.8f);

        if (fromScreen.z <= 0f || toScreen.z <= 0f) return;

        Vector2 from = new(fromScreen.x, Screen.height - fromScreen.y);
        Vector2 to = new(toScreen.x, Screen.height - toScreen.y);

        if (_hunter.FSM.CurrentStateID == HunterAgent.States.Attack)
            DrawLine(from, to, 1.5f, new Color(1f, 0.2f, 0.2f, 0.55f));

        if (_hunter.FSM.CurrentStateID != HunterAgent.States.Gather) return;

        Vector3 gatherScreen = _camera.WorldToScreenPoint(
            _hunter.CurrentTarget.transform.position + Vector3.up * 1.9f);

        if (gatherScreen.z <= 0f) return;

        Vector2 point = new(gatherScreen.x, Screen.height - gatherScreen.y);

        DrawWorldLabel(
            point,
            $"GATHERING {_hunter.GatherProgress * 100f:F0}%",
            new Color(1f, 0.85f, 0.1f));

        DrawBar(
            new Rect(point.x - 34f, point.y + 18f, 68f, 6f),
            _hunter.GatherProgress,
            new Color(1f, 0.85f, 0.1f));
    }

    private void DrawWorldLabel(Vector2 point, string text, Color color)
    {
        GUIContent content = new GUIContent(text);
        Vector2 size = _labelStyle.CalcSize(content);

        Rect rect = new Rect(
            point.x - size.x * 0.5f - 4f,
            point.y - size.y - 2f,
            size.x + 8f,
            size.y + 4f);

        Color oldBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
        GUI.Box(rect, GUIContent.none);
        GUI.backgroundColor = oldBackground;

        Color oldText = _labelStyle.normal.textColor;
        _labelStyle.normal.textColor = color;
        GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, size.x, size.y), text, _labelStyle);
        _labelStyle.normal.textColor = oldText;
    }

    private void DrawHealthBar(Vector2 point, float health, float maxHealth)
    {
        const float width = 64f;
        const float height = 5f;

        float factor = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

        Rect background = new Rect(point.x - width * 0.5f, point.y, width, height);

        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = Color.Lerp(Color.red, Color.green, factor);
        GUI.DrawTexture(new Rect(background.x, background.y, background.width * factor, background.height),
            Texture2D.whiteTexture);

        GUI.color = old;
    }

    private void DrawPanel(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, _panelStyle);
    }

    private void DrawCross(Vector2 center, float size, Color color)
    {
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.color = color;

        GUIUtility.RotateAroundPivot(45f, center);
        GUI.DrawTexture(
            new Rect(center.x - size * 0.5f, center.y - 1.5f, size, 3f),
            Texture2D.whiteTexture);

        GUIUtility.RotateAroundPivot(-90f, center);
        GUI.DrawTexture(
            new Rect(center.x - size * 0.5f, center.y - 1.5f, size, 3f),
            Texture2D.whiteTexture);

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private void DrawLine(Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.01f) return;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(
            new Rect(from.x, from.y - width * 0.5f, length, width),
            Texture2D.whiteTexture);

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private static Color GetBoidColor(BoidState state)
    {
        return state switch
        {
            BoidState.Flocking => new Color(0.2f, 1f, 0.45f),
            BoidState.Seeking => new Color(0.1f, 0.8f, 1f),
            BoidState.Interacting => new Color(1f, 0.9f, 0.1f),
            BoidState.Evading => new Color(1f, 0.1f, 0.3f),
            BoidState.Dead => new Color(0.7f, 0.7f, 0.7f),
            BoidState.Respawning => new Color(0.35f, 0.65f, 1f),
            _ => Color.white
        };
    }

    private static Color GetHunterColor(HunterAgent.States state)
    {
        return state switch
        {
            HunterAgent.States.Patrol => new Color(0.2f, 0.5f, 1f),
            HunterAgent.States.Attack => new Color(1f, 0.15f, 0.15f),
            HunterAgent.States.Gather => new Color(1f, 0.85f, 0.1f),
            HunterAgent.States.Search => new Color(0.2f, 0.5f, 1f),
            HunterAgent.States.Flee => new Color(0.7f, 0.4f, 1f),
            HunterAgent.States.Exit => new Color(0.3f, 0.8f, 1f),
            HunterAgent.States.Dead => new Color(0.35f, 0.35f, 0.35f),
            _ => Color.white
        };
    }

    private static Color GetWolfColor(WolfAgent.States state)
    {
        return state switch
        {
            WolfAgent.States.Patrol => new Color(0.6f, 0.6f, 0.65f),
            WolfAgent.States.Alert => new Color(1f, 0.55f, 0.1f),
            WolfAgent.States.Chase => new Color(0.9f, 0.1f, 0.15f),
            WolfAgent.States.Return => new Color(0.25f, 0.45f, 1f),
            _ => Color.white
        };
    }
}

