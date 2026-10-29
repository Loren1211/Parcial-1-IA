using UnityEngine;

/// <summary>
/// Objeto de interés generado por el cazador. Los boids lo dañan por intervalo
/// hasta destruirlo. Al destruirse desaparece de la escena y se desregistra,
/// para no bloquear el límite de objetos activos.
/// El color va de verde (sano) a rojo (a punto de romperse) para dar feedback.
/// </summary>
public class InterestObject : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _damageInterval = 1f;
    [SerializeField] private float _damagePerTick = 2f;

    private MaterialPropertyBlock _block;

    private float _health;
    private float _timer;
    private InterestObjectTracker _tracker;
    private Renderer _renderer;
    private Color _lastColor = Color.clear;
    private bool _isDestroyed;

    public bool IsAlive => !_isDestroyed && _health > 0f;
    public float Health => _health;
    public float MaxHealth => _maxHealth;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _health = _maxHealth;
        _renderer = GetComponent<Renderer>();
        _tracker = InterestObjectTracker.Instance;

        if (_tracker == null)
            _tracker = FindFirstObjectByType<InterestObjectTracker>();

        _tracker?.Register(this);
        UpdateVisual(true);
    }

    public void TakeDamageTick(float deltaTime)
    {
        if (!IsAlive) return;

        _timer += deltaTime;

        if (_timer < _damageInterval) return;

        _timer = 0f;
        _health = Mathf.Max(0f, _health - _damagePerTick);
        UpdateVisual();

        if (_health <= 0f)
            DestroyInterest();
    }

    private void Update()
    {
        if (!IsAlive || _renderer == null) return;
        if (_health >= _maxHealth) return;

        UpdateVisual();
    }

    private void UpdateVisual(bool force = false)
    {
        if (_renderer == null) return;

        float factor = _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;
        Color color = Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(0.2f, 1f, 0.4f), factor);

        if (factor < 0.5f)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 12f);
            color = new Color(color.r * pulse, color.g * pulse, color.b * pulse, 1f);
        }

        if (!force && color == _lastColor) return;

        _lastColor = color;
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_block);
    }

    private void DestroyInterest()
    {
        if (_isDestroyed) return;

        _isDestroyed = true;
        _health = 0f;

        _tracker?.Unregister(this);
        FeedbackManager.Instance?.NotifyInterestDestroyed(transform.position);

        if (TryGetComponent<Collider>(out var collider)) collider.enabled = false;
        if (_renderer != null) _renderer.enabled = false;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _tracker?.Unregister(this);
    }
}
