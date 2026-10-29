using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camino calculado por A*: ids de nodo y puntos en el mundo. El primer punto
/// se reemplaza por la posicion real del agente para que no retroceda.
/// </summary>
public class NavPath
{
    private readonly List<Vector3> _points = new();
    private readonly List<int> _nodes = new();

    public IReadOnlyList<Vector3> Points => _points;
    public IReadOnlyList<int> Nodes => _nodes;
    public bool Found { get; private set; }
    public float Length { get; private set; }

    internal List<Vector3> MutablePoints => _points;

    public void Clear()
    {
        _points.Clear();
        _nodes.Clear();
        Found = false;
        Length = 0f;
    }

    public void Build(List<int> nodes, NavGraphData graph, Vector3 start)
    {
        Clear();
        if (nodes == null || nodes.Count == 0 || graph == null) return;

        _nodes.AddRange(nodes);

        for (int i = 0; i < nodes.Count; i++)
            _points.Add(graph.Nodes[nodes[i]].Position);

        if (_points.Count > 0)
            _points[0] = new Vector3(start.x, _points[0].y, start.z);

        Found = true;
        RecalculateLength();
    }

    public void RecalculateLength()
    {
        float length = 0f;

        for (int i = 1; i < _points.Count; i++)
            length += Vector3.Distance(_points[i - 1], _points[i]);

        Length = length;
    }
}
