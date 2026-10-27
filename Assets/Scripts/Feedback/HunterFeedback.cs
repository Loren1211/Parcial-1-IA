using UnityEngine;

/// <summary>
/// Color del cazador según su estado de FSM.
/// </summary>
public class HunterFeedback
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly Renderer _renderer;
    private readonly MaterialPropertyBlock _block = new();

    private Color _lastColor = Color.clear;

    public HunterFeedback(Renderer renderer)
    {
        _renderer = renderer;
    }

    public void SetState(HunterAgent.States state)
    {
        if (_renderer == null) return;

        Color color = state switch
        {
            HunterAgent.States.Patrol => new Color(0.2f, 0.5f, 1f),
            HunterAgent.States.Attack => new Color(1f, 0.15f, 0.15f),
            HunterAgent.States.Gather => new Color(1f, 0.85f, 0.1f),
            _ => Color.white
        };

        if (color == _lastColor) return;

        _lastColor = color;
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_block);
    }
}
