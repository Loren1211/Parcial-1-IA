using System.Collections.Generic;
using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _damageInterval = 1f;
    [SerializeField] private float _damagePerTick = 2f;

    private float _health;
    private float _timer;

    private static readonly List<InterestObject> _active = new();
    public static IReadOnlyList<InterestObject> AllActive => _active;
    public static int ActiveCount => _active.Count;

    public bool IsAlive => _health > 0f;

    private void Awake()
    {
        _health = _maxHealth;
        _active.Add(this);
    }

    public void TakeDamageTick(float deltaTime)
    {
        if (!IsAlive) return;

        _timer += deltaTime;
        if (_timer >= _damageInterval)
        {
            _timer = 0f;
            _health -= _damagePerTick;
            Debug.Log($"{name}: vida restante {_health}/{_maxHealth}");
            if (_health <= 0f)
            {
                _health = 0f;
                if (TryGetComponent<Collider>(out var col)) col.enabled = false;
                Debug.Log($"{name}: destruido");
            }
        }
    }

    private void OnDestroy() => _active.Remove(this);
}