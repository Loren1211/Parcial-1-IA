using UnityEngine;

/// <summary>
/// Nodo del grafo de navegacion. Guarda su posicion en el plano, los indices
/// de sus vecinos y si cae dentro de una zona segura (excluida para lobos).
/// </summary>
public struct NavNode
{
    public Vector3 Position;
    public int[] Neighbors;
    public bool InSafeZone;
    public bool Walkable;
}
