using UnityEngine;

/// <summary>
/// Objeto de interés generado por el cazador. Los boids lo dañan por intervalo
/// hasta destruirlo. Al destruirse desaparece de la escena y se desregistra,
/// para no bloquear el límite de objetos activos.
/// </summary>
public class InterestObject : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _damageInterval = 1f;
    [SerializeField] private float _damagePerTick = 2f;

    private float _health;
    private float _timer;
    private InterestObjectTracker _tracker;
    private bool _isDestroyed;

    public bool IsAlive => !_isDestroyed && _health > 0f;
    public float Health => _health;
    public float MaxHealth => _maxHealth;

    private void Awake()
    {
        _health = _maxHealth;
        _tracker = InterestObjectTracker.Instance;

        if (_tracker == null)
            _tracker = FindFirstObjectByType<InterestObjectTracker>();

        _tracker?.Register(this);
    }

    public void TakeDamageTick(float deltaTime)
    {
        if (!IsAlive) return;

        _timer += deltaTime;

        if (_timer < _damageInterval) return;

        _timer = 0f;
        _health -= _damagePerTick;

        if (_health <= 0f)
            DestroyInterest();
    }

    private void DestroyInterest()
    {
        if (_isDestroyed) return;

        _isDestroyed = true;
        _health = 0f;

        _tracker?.Unregister(this);

        if (TryGetComponent<Collider>(out var collider)) collider.enabled = false;
        if (TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _tracker?.Unregister(this);
    }
}
