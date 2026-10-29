using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera el grafo de navegacion a partir de una grilla y raycasts contra la
/// capa Obstacle. No usa NavMesh ni rutas predefinidas: los nodos y las
/// conexiones salen del escenario real.
/// </summary>
public static class NavGraphBaker
{
    private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] StepZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

    public static NavGraphData Bake(
        Vector3 center,
        Vector2 size,
        float spacing,
        float agentRadius,
        float sampleHeight,
        LayerMask obstacleMask,
        Vector3 seed)
    {
        spacing = Mathf.Max(0.25f, spacing);
        agentRadius = Mathf.Max(0.05f, agentRadius);

        int columns = Mathf.Max(1, Mathf.RoundToInt(size.x / spacing) + 1);
        int rows = Mathf.Max(1, Mathf.RoundToInt(size.y / spacing) + 1);

        Vector3 origin = new(
            center.x - (columns - 1) * spacing * 0.5f,
            sampleHeight,
            center.z - (rows - 1) * spacing * 0.5f);

        NavNode[] nodes = new NavNode[columns * rows];
        List<int>[] links = new List<int>[nodes.Length];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                Vector3 position = new(
                    origin.x + column * spacing,
                    sampleHeight,
                    origin.z + row * spacing);

                bool blocked = Physics.CheckSphere(
                    position,
                    agentRadius,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore);

                nodes[index] = new NavNode
                {
                    Position = position,
                    Neighbors = System.Array.Empty<int>(),
                    Walkable = !blocked
                };

                links[index] = blocked ? null : new List<int>(8);
            }
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                if (!nodes[index].Walkable) continue;

                for (int step = 0; step < StepX.Length; step++)
                {
                    int nextColumn = column + StepX[step];
                    int nextRow = row + StepZ[step];

                    if (nextColumn < 0 || nextRow < 0) continue;
                    if (nextColumn >= columns || nextRow >= rows) continue;

                    int neighbor = nextRow * columns + nextColumn;
                    if (!nodes[neighbor].Walkable) continue;
                    if (!IsClear(nodes[index].Position, nodes[neighbor].Position, obstacleMask)) continue;

                    links[index].Add(neighbor);
                }
            }
        }

        KeepComponentFrom(nodes, links, seed);

        for (int i = 0; i < nodes.Length; i++)
        {
            if (links[i] == null || links[i].Count == 0)
            {
                nodes[i].Walkable = false;
                nodes[i].Neighbors = System.Array.Empty<int>();
                continue;
            }

            nodes[i].Neighbors = links[i].ToArray();
        }

        return new NavGraphData
        {
            Nodes = nodes,
            Columns = columns,
            Rows = rows,
            Origin = origin,
            Spacing = spacing
        };
    }

    private static bool IsClear(Vector3 from, Vector3 to, LayerMask obstacleMask)
    {
        return !Physics.Linecast(from, to, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Deja solo la componente conexa alcanzable desde el punto semilla (el
    /// spawn del Cazador). Evita quedarse con la region exterior al mapa, que
    /// tambien tiene piso, y descarta huecos cerrados inaccesibles.
    /// </summary>
    private static void KeepComponentFrom(NavNode[] nodes, List<int>[] links, Vector3 seed)
    {
        int seedNode = -1;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].Walkable) continue;

            float sqr = (nodes[i].Position - seed).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            seedNode = i;
        }

        if (seedNode < 0) return;

        bool[] keep = new bool[nodes.Length];
        Queue<int> queue = new();
        keep[seedNode] = true;
        queue.Enqueue(seedNode);

        while (queue.Count > 0)
        {
            int node = queue.Dequeue();
            List<int> neighbors = links[node];
            if (neighbors == null) continue;

            for (int i = 0; i < neighbors.Count; i++)
            {
                int next = neighbors[i];
                if (keep[next]) continue;

                keep[next] = true;
                queue.Enqueue(next);
            }
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].Walkable || keep[i]) continue;

            nodes[i].Walkable = false;
            links[i] = null;
        }
    }
}
