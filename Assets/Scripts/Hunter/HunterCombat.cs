using System;
using UnityEngine;

/// <summary>
/// TBA, daño y eventos de combate del cazador.
/// Los estados deciden cuándo atacar; esta clase solo valida el TBA y aplica daño.
/// </summary>
public class HunterCombat
{
    private readonly float _tba;
    private readonly float _meleeDamage;
    private readonly float _rangeDamage;

    private float _tbaTimer;

    public float TbaRemaining => Mathf.Max(0f, _tbaTimer);
    public bool CanAttack => _tbaTimer <= 0f;

    public event Action<BoidAgent, bool> Attacked;
    public event Action<BoidAgent> Killed;

    public HunterCombat(float tba, float meleeDamage, float rangeDamage)
    {
        _tba = Mathf.Max(0.01f, tba);
        _meleeDamage = Mathf.Max(0f, meleeDamage);
        _rangeDamage = Mathf.Max(0f, rangeDamage);
    }

    public void Tick(float deltaTime)
    {
        if (_tbaTimer > 0f)
            _tbaTimer -= deltaTime;
    }

    public bool TryMeleeAttack(BoidAgent target) => TryAttack(target, _meleeDamage, true);

    public bool TryRangeAttack(BoidAgent target) => TryAttack(target, _rangeDamage, false);

    private bool TryAttack(BoidAgent target, float damage, bool isMelee)
    {
        if (!CanAttack || target == null || !target.IsActive)
            return false;

        target.TakeDamage(damage);
        _tbaTimer = _tba;

        Attacked?.Invoke(target, isMelee);

        if (!target.IsActive)
            Killed?.Invoke(target);

        return true;
    }
}
