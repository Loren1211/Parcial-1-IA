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
        DrawEventLog(manager);
        DrawBoidList();
    }

    private void DrawHunterPanel(FeedbackManager manager)
    {
        Rect panel = new Rect(10f, 10f, 420f, 238f);
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
        int gathers = manager != null ? manager.Gathers : 0;
        int respawns = manager != null ? manager.Respawns : 0;
        int destroyed = manager != null ? manager.BaitsDestroyed : 0;

        float y = panel.y + 8f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 22f), "PARCIAL 1 - IA", _headerStyle);
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

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Boids: {_boids.Length}   |   Detectados: {detected}", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Cebos activos: {baits}/{maxBaits}", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Kills: {kills}   |   Recolecciones: {gathers}", _labelStyle);
        y += 20f;

        GUI.Label(new Rect(panel.x + 10f, y, panel.width - 20f, 18f),
            $"Respawns: {respawns}   |   Cebos destruidos: {destroyed}", _labelStyle);
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
        Rect panel = new Rect(Screen.width - 300f, 10f, 290f, 228f);
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
        DrawLegendRow(panel.x + 12f, y, new Color(0.2f, 0.5f, 1f), "Hunter: Patrol");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.15f, 0.15f), "Hunter: Attack");
        y += 19f;
        DrawLegendRow(panel.x + 12f, y, new Color(1f, 0.85f, 0.1f), "Hunter: Gather");
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
        }

        for (int i = 0; i < _interests.Length; i++)
        {
            InterestObject interest = _interests[i];
            if (interest == null || !interest.IsAlive) continue;

            Vector3 screen = _camera.WorldToScreenPoint(interest.transform.position + Vector3.up * 1.0f);
            if (screen.z <= 0f) continue;

            float healthFactor = interest.MaxHealth > 0f
                ? Mathf.Clamp01(interest.Health / interest.MaxHealth)
                : 0f;

            DrawWorldLabel(
                new Vector2(screen.x, Screen.height - screen.y),
                $"CEBO {interest.Health:F0}/{interest.MaxHealth:F0}",
                Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(0.2f, 1f, 0.4f), healthFactor));
        }

        if (FeedbackManager.Instance == null) return;

        for (int i = 0; i < FeedbackManager.Instance.FloatingTexts.Count; i++)
        {
            FeedbackManager.FloatingText text = FeedbackManager.Instance.FloatingTexts[i];
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
            _ => Color.white
        };
    }
}

