using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* propio sobre el grafo de navegacion. Heuristica euclidiana y heap
/// binario. El parametro avoidSafeZones excluye los nodos de refugio para que
/// los lobos nunca tracen un camino que entre a una zona segura.
/// </summary>
public static class AStarPathfinder
{
    public static bool FindPath(
        NavGraphData graph,
        int start,
        int goal,
        bool avoidSafeZones,
        List<int> result)
    {
        result.Clear();

        if (graph == null || !graph.IsValid) return false;
        if (start < 0 || goal < 0) return false;
        if (start >= graph.Nodes.Length || goal >= graph.Nodes.Length) return false;
        if (!graph.Nodes[start].Walkable || !graph.Nodes[goal].Walkable) return false;
        if (avoidSafeZones && graph.Nodes[goal].InSafeZone) return false;

        if (start == goal)
        {
            result.Add(goal);
            return true;
        }

        int count = graph.Nodes.Length;
        float[] gScore = new float[count];
        float[] fScore = new float[count];
        int[] cameFrom = new int[count];
        bool[] closed = new bool[count];

        for (int i = 0; i < count; i++)
        {
            gScore[i] = float.PositiveInfinity;
            cameFrom[i] = -1;
        }

        Vector3 goalPosition = graph.Nodes[goal].Position;
        MinHeap open = new(count);

        gScore[start] = 0f;
        fScore[start] = Heuristic(graph.Nodes[start].Position, goalPosition);
        open.Push(start, fScore[start]);

        while (open.Count > 0)
        {
            int current = open.Pop();

            if (current == goal)
            {
                Reconstruct(cameFrom, current, result);
                return true;
            }

            if (closed[current]) continue;
            closed[current] = true;

            int[] neighbors = graph.Nodes[current].Neighbors;

            for (int i = 0; i < neighbors.Length; i++)
            {
                int next = neighbors[i];

                if (closed[next]) continue;
                if (avoidSafeZones && graph.Nodes[next].InSafeZone) continue;

                float tentative = gScore[current] + Vector3.Distance(
                    graph.Nodes[current].Position,
                    graph.Nodes[next].Position);

                if (tentative >= gScore[next]) continue;

                cameFrom[next] = current;
                gScore[next] = tentative;
                fScore[next] = tentative + Heuristic(graph.Nodes[next].Position, goalPosition);
                open.Push(next, fScore[next]);
            }
        }

        return false;
    }

    private static float Heuristic(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }

    private static void Reconstruct(int[] cameFrom, int current, List<int> result)
    {
        result.Clear();
        result.Add(current);

        while (cameFrom[current] >= 0)
        {
            current = cameFrom[current];
            result.Add(current);
        }

        result.Reverse();
    }
}
