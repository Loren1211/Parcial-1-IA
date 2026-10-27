using UnityEngine;

/// <summary>
/// Spawner de cebos del cazador. Vive fuera de los estados; Patrol lo activa
/// y lo detiene al entrar/salir. Respeta el máximo de cebos activos.
/// </summary>
public class HunterInterestSpawner
{
    private readonly HunterAgent _hunter;

    private bool _active;
    private float _timer;

    public HunterInterestSpawner(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public void BeginPatrolSpawning()
    {
        _active = true;
        _timer = 0f;
    }

    public void StopSpawning()
    {
        _active = false;
        _timer = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (!_active) return;

        _timer += deltaTime;

        if (_timer < _hunter.InterestSpawnInterval) return;

        _timer = 0f;
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (_hunter.InterestObjectPrefab == null) return;

        int activeCount = InterestObjectTracker.Instance != null
            ? InterestObjectTracker.Instance.ActiveCount
            : Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None).Length;

        if (activeCount >= _hunter.MaxInterestObjects) return;

        Vector3 position = WorldBounds.Instance != null
            ? WorldBounds.Instance.RandomPosition()
            : _hunter.transform.position;

        Object.Instantiate(_hunter.InterestObjectPrefab, position, Quaternion.identity);
        FeedbackManager.Instance?.NotifyInterestSpawned(position);
    }
}
