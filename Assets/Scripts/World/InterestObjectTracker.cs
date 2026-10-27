using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro de objetos de interés activos. Reemplaza la lista estática
/// InterestObject._active y permite al spawner respetar el máximo de 5.
/// No controla agentes: solo cuenta objetos.
/// </summary>
public class InterestObjectTracker : MonoBehaviour
{
    public static InterestObjectTracker Instance { get; private set; }

    private HashSet<InterestObject> _active = new();

    public int ActiveCount => _active != null ? _active.Count : 0;
    public IReadOnlyCollection<InterestObject> Active =>
        _active != null
            ? _active
            : (IReadOnlyCollection<InterestObject>)System.Array.Empty<InterestObject>();

    private void Awake()
    {
        _active ??= new HashSet<InterestObject>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(InterestObject interestObject)
    {
        if (interestObject == null) return;

        _active ??= new HashSet<InterestObject>();
        _active.Add(interestObject);
    }

    public void Unregister(InterestObject interestObject)
    {
        if (interestObject == null) return;

        _active?.Remove(interestObject);
    }
}
