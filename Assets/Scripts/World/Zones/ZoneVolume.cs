using System.Collections.Generic;
using UnityEngine;

public enum ZoneKind
{
    Room,
    Corridor,
    Initial,
    HuntingGround,
    Safe,
    Exit
}

/// <summary>
/// Volumen logico de una zona del escenario. Sirve para consultar pertenencia,
/// elegir puntos aleatorios y dibujar el area en el editor. No mueve agentes:
/// cada sistema decide como usarlo.
/// </summary>
[DisallowMultipleComponent]
public class ZoneVolume : MonoBehaviour
{
    private static readonly List<ZoneVolume> Zones = new();

    [SerializeField] private ZoneKind _kind = ZoneKind.Room;
    [SerializeField] private Vector3 _size = new(4f, 3f, 4f);
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private Transform _entryPoint;

    public ZoneKind Kind => _kind;
    public Vector3 Size => _size;
    public Color Color => _color;
    public Transform EntryPoint => _entryPoint;
    public Vector3 Center => transform.position;
    public Bounds Bounds => new(transform.position, _size);

    public static IReadOnlyList<ZoneVolume> All => Zones;

    private void OnEnable()
    {
        if (!Zones.Contains(this)) Zones.Add(this);
    }

    private void OnDisable()
    {
        Zones.Remove(this);
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - transform.position;
        return Mathf.Abs(local.x) <= _size.x * 0.5f
            && Mathf.Abs(local.y) <= _size.y * 0.5f
            && Mathf.Abs(local.z) <= _size.z * 0.5f;
    }

    public Vector3 ClosestPoint(Vector3 worldPosition) => Bounds.ClosestPoint(worldPosition);

    public Vector3 RandomPoint(float margin = 0.5f, float y = 0f)
    {
        float halfX = Mathf.Max(0f, _size.x * 0.5f - margin);
        float halfZ = Mathf.Max(0f, _size.z * 0.5f - margin);

        return transform.position + new Vector3(
            Random.Range(-halfX, halfX),
            y,
            Random.Range(-halfZ, halfZ));
    }

    public static ZoneVolume FindNearest(ZoneKind kind, Vector3 position)
    {
        ZoneVolume nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < Zones.Count; i++)
        {
            ZoneVolume zone = Zones[i];
            if (zone == null || zone._kind != kind) continue;

            float sqr = (zone.transform.position - position).sqrMagnitude;
            if (sqr >= nearestSqr) continue;

            nearestSqr = sqr;
            nearest = zone;
        }

        return nearest;
    }

    public static bool IsInside(ZoneKind kind, Vector3 position)
    {
        for (int i = 0; i < Zones.Count; i++)
        {
            ZoneVolume zone = Zones[i];
            if (zone != null && zone._kind == kind && zone.Contains(position))
                return true;
        }

        return false;
    }

    public static bool IsInsideSafeZone(Vector3 position) => IsInside(ZoneKind.Safe, position);

    /// <summary>
    /// Zonas en las que un lobo no puede entrar: los refugios seguros y la
    /// zona inicial, para que la jauria no pueda acampar el spawn del Cazador.
    /// </summary>
    public static bool IsInsideWolfExcluded(Vector3 position)
    {
        return IsInside(ZoneKind.Safe, position) || IsInside(ZoneKind.Initial, position);
    }

    /// <summary>
    /// Configuracion usada por el generador de escena. Mantiene los campos
    /// serializados para que el dato quede guardado en el .unity.
    /// </summary>
    public void Configure(ZoneKind kind, Vector3 size, Color color, Transform entryPoint = null)
    {
        _kind = kind;
        _size = size;
        _color = color;
        _entryPoint = entryPoint;
    }

    private void OnDrawGizmos()
    {
        if (!FeedbackManager.ShowGizmos) return;

        Gizmos.color = new Color(_color.r, _color.g, _color.b, 0.35f);
        Gizmos.DrawCube(transform.position, _size);

        Gizmos.color = _color;
        Gizmos.DrawWireCube(transform.position, _size);

        if (_entryPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_entryPoint.position, 0.35f);
        }
    }
}
