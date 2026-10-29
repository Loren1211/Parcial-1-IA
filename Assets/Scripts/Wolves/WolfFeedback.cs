using UnityEngine;

/// <summary>
/// Color del lobo segun su estado de FSM.
/// </summary>
public class WolfFeedback
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly Renderer _renderer;
    private readonly MaterialPropertyBlock _block;

    private Color _lastColor = Color.clear;

    public WolfFeedback(Renderer renderer)
    {
        _renderer = renderer;
        _block = new MaterialPropertyBlock();
    }

    public void SetState(WolfAgent.States state)
    {
        if (_renderer == null) return;

        Color color = state switch
        {
            WolfAgent.States.Patrol => new Color(0.6f, 0.6f, 0.65f),
            WolfAgent.States.Alert => new Color(1f, 0.55f, 0.1f),
            WolfAgent.States.Chase => new Color(0.9f, 0.1f, 0.15f),
            WolfAgent.States.Return => new Color(0.25f, 0.45f, 1f),
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
