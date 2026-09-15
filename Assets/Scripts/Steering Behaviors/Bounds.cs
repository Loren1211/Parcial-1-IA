using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }
    [SerializeField]
    private float _height = 34f;
    [SerializeField]
    private float _width = 60f;
    [SerializeField]
    private bool _drawGizmos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 CalculateBoundPosition(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > _width / 2f) newPosition.x = -_width / 2f;
        if (position.x < -_width / 2f) newPosition.x = _width / 2f;
        if (position.z > _height / 2f) newPosition.z = -_height / 2f;
        if (position.z < -_height / 2f) newPosition.z = _height / 2f;

        return newPosition;
    }


    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_width, 0f, _height));
    }

}
