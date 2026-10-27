using UnityEngine;

/// <summary>
/// Color del boid según su estado, usando MaterialPropertyBlock para no
/// instanciar materiales ni romper el batching.
/// </summary>
public class BoidFeedback
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly Renderer _renderer;
    private readonly MaterialPropertyBlock _block;

    private Color _lastColor = Color.clear;

    public BoidFeedback(Renderer renderer)
    {
        _renderer = renderer;
        _block = new MaterialPropertyBlock();
    }

    public void Tick(
        BoidState state,
        bool isDead,
        bool isCollected,
        bool isRespawning,
        float health,
        float maxHealth)
    {
        if (_renderer == null || isCollected) return;

        Color color = GetColor(state, isDead, isRespawning, health, maxHealth);

        if (color == _lastColor) return;

        _lastColor = color;
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_block);
    }

    private static Color GetColor(
        BoidState state,
        bool isDead,
        bool isRespawning,
        float health,
        float maxHealth)
    {
        if (isDead) return new Color(0.25f, 0.25f, 0.25f);

        if (isRespawning)
        {
            float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f);
            return Color.Lerp(new Color(0.1f, 0.2f, 0.8f), Color.white, flash * 0.5f);
        }

        Color color = state switch
        {
            BoidState.Flocking => new Color(0.2f, 1f, 0.45f),
            BoidState.Seeking => new Color(0.1f, 0.8f, 1f),
            BoidState.Interacting => new Color(1f, 0.9f, 0.1f),
            BoidState.Evading => new Color(1f, 0.1f, 0.3f),
            BoidState.Dead => new Color(0.25f, 0.25f, 0.25f),
            BoidState.Respawning => new Color(0.2f, 0.4f, 1f),
            _ => Color.white
        };

        float healthFactor = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
        Color dark = new(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, color.a);

        return Color.Lerp(color, dark, 1f - healthFactor);
    }
}
