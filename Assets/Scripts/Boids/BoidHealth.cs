using UnityEngine;

/// <summary>
/// Vida, muerte, recolección y respawn del boid.
/// Un boid muerto queda en el lugar (collider activo) para que el cazador
/// pueda recolectarlo. Al recolectarlo se oculta y respawnea tras un delay.
/// </summary>
public class BoidHealth
{
    private readonly BoidAgent _owner;
    private readonly Transform _transform;
    private readonly Renderer _renderer;
    private readonly Collider _collider;
    private readonly float _maxHealth;
    private readonly float _respawnDelay;

    private float _currentHealth;
    private float _respawnTimer;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float RespawnTimeRemaining => Mathf.Max(0f, _respawnTimer);

    public bool IsDead { get; private set; }
    public bool IsCollected { get; private set; }
    public bool IsActive => !IsDead && !IsCollected;
    public bool IsInactive => !IsActive;

    public BoidHealth(
        BoidAgent owner,
        Transform transform,
        Renderer renderer,
        Collider collider,
        float maxHealth,
        float respawnDelay)
    {
        _owner = owner;
        _transform = transform;
        _renderer = renderer;
        _collider = collider;
        _maxHealth = Mathf.Max(1f, maxHealth);
        _respawnDelay = Mathf.Max(0.1f, respawnDelay);
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// Devuelve true si el daño dejó al boid sin vida.
    /// </summary>
    public bool TakeDamage(float damage)
    {
        if (!IsActive || damage <= 0f) return false;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        return _currentHealth <= 0f;
    }

    public void Die()
    {
        if (IsDead || IsCollected) return;

        IsDead = true;
        _currentHealth = 0f;
        // El collider queda activo para que HunterSensor pueda detectar el cadáver.
    }

    public void Collect()
    {
        if (!IsDead || IsCollected) return;

        IsCollected = true;
        _respawnTimer = _respawnDelay;

        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
    }

    public void Tick(float deltaTime)
    {
        if (!IsCollected) return;

        _respawnTimer -= deltaTime;
        if (_respawnTimer <= 0f) Respawn();
    }

    private void Respawn()
    {
        IsDead = false;
        IsCollected = false;
        _currentHealth = _maxHealth;

        if (_renderer != null) _renderer.enabled = true;
        if (_collider != null) _collider.enabled = true;

        Vector3 position = WorldBounds.Instance != null
            ? WorldBounds.Instance.RandomPosition(1f, _transform.position.y)
            : Vector3.zero;

        _transform.position = position;
        _owner.OnRespawned();
    }
}
