using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [SerializeField] private float _height = 34f;
    [SerializeField] private float _width = 60f;
    [SerializeField] private bool _drawGizmos;

    public float Width => _width;
    public float Height => _height;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Mantiene la posición dentro del escenario. Si se sale por un borde,
    /// aparece por el borde opuesto (wrap).
    /// </summary>
    public Vector3 CalculateBoundPosition(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > _width / 2f) newPosition.x = -_width / 2f;
        if (position.x < -_width / 2f) newPosition.x = _width / 2f;
        if (position.z > _height / 2f) newPosition.z = -_height / 2f;
        if (position.z < -_height / 2f) newPosition.z = _height / 2f;

        return newPosition;
    }

    public Vector3 Clamp(Vector3 position) => CalculateBoundPosition(position);

    /// <summary>
    /// Devuelve una posición aleatoria válida dentro del escenario.
    /// Se usa para respawn de boids y para el spawn de objetos de interés.
    /// </summary>
    public Vector3 RandomPosition(float margin = 1f)
    {
        float halfWidth = Mathf.Max(0f, _width / 2f - margin);
        float halfHeight = Mathf.Max(0f, _height / 2f - margin);

        return new Vector3(
            Random.Range(-halfWidth, halfWidth),
            0f,
            Random.Range(-halfHeight, halfHeight));
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_width, 0f, _height));
    }
}
