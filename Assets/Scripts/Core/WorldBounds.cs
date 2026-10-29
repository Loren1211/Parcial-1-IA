using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [SerializeField] private float _height = 34f;
    [SerializeField] private float _width = 60f;
    [SerializeField] private bool _wrap = true;
    [SerializeField] private bool _drawGizmos;

    public float Width => _width;
    public float Height => _height;
    public bool Wrap => _wrap;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Mantiene la posicion dentro del escenario. Con wrap activo aparece por
    /// el borde opuesto; con wrap desactivado se recorta al borde. El Coto del
    /// Parcial 2 usa recorte para que los boids nunca salgan de la zona.
    /// </summary>
    public Vector3 CalculateBoundPosition(Vector3 position)
    {
        if (!_wrap)
        {
            return new Vector3(
                Mathf.Clamp(position.x, -_width / 2f, _width / 2f),
                position.y,
                Mathf.Clamp(position.z, -_height / 2f, _height / 2f));
        }

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
    public Vector3 RandomPosition(float margin = 1f, float y = 0f)
    {
        float halfWidth = Mathf.Max(0f, _width / 2f - margin);
        float halfHeight = Mathf.Max(0f, _height / 2f - margin);
        Vector3 center = transform.position;

        return center + new Vector3(
            Random.Range(-halfWidth, halfWidth),
            y,
            Random.Range(-halfHeight, halfHeight));
    }

    /// <summary>
    /// Configuracion usada por el generador de escena del Parcial 2.
    /// </summary>
    public void Configure(float width, float height, bool wrap = true, bool drawGizmos = true)
    {
        _width = Mathf.Max(0.1f, width);
        _height = Mathf.Max(0.1f, height);
        _wrap = wrap;
        _drawGizmos = drawGizmos;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(_width, 0f, _height));
    }
}
