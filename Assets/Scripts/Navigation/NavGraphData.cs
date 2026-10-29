using UnityEngine;

/// <summary>
/// Resultado de un bake: nodos, grilla y parametros usados. Es un objeto plano
/// para que A* y el agente no dependan de un MonoBehaviour.
/// </summary>
public class NavGraphData
{
    public NavNode[] Nodes;
    public int Columns;
    public int Rows;
    public Vector3 Origin;
    public float Spacing;

    public bool IsValid => Nodes != null && Nodes.Length > 0;

    public int NodeAt(int column, int row)
    {
        if (column < 0 || row < 0 || column >= Columns || row >= Rows) return -1;

        int index = row * Columns + column;
        return index < Nodes.Length && Nodes[index].Walkable ? index : -1;
    }

    public bool TryGetCell(Vector3 position, out int column, out int row)
    {
        column = Mathf.RoundToInt((position.x - Origin.x) / Spacing);
        row = Mathf.RoundToInt((position.z - Origin.z) / Spacing);

        return column >= 0 && row >= 0 && column < Columns && row < Rows;
    }
}
