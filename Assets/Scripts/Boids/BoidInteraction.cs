using UnityEngine;

/// <summary>
/// Interacción del boid con un objeto de interés.
/// Solo daña mientras está dentro del radio. Si el objeto desaparece,
/// libera el objetivo y el boid vuelve a su comportamiento normal.
/// </summary>
public class BoidInteraction
{
    private readonly BoidAgent _owner;
    private readonly float _interactRadius;

    private InterestObject _target;

    public InterestObject CurrentTarget => _target;
    public bool IsInteracting => _target != null && _target.IsAlive;

    public BoidInteraction(BoidAgent owner, float interactRadius)
    {
        _owner = owner;
        _interactRadius = Mathf.Max(0.05f, interactRadius);
    }

    public void SetTarget(InterestObject target)
    {
        _target = target;
    }

    public void Cancel()
    {
        _target = null;
    }

    public void Tick(float deltaTime)
    {
        if (_target == null) return;

        if (!_target.IsAlive)
        {
            _target = null;
            return;
        }

        float sqrDistance = (_target.transform.position - _owner.transform.position).sqrMagnitude;
        if (sqrDistance > _interactRadius * _interactRadius)
        {
            _target = null;
            return;
        }

        _target.TakeDamageTick(deltaTime);

        // Si el objeto se destruyó en este tick, soltamos la referencia.
        if (_target != null && !_target.IsAlive) _target = null;
    }
}
