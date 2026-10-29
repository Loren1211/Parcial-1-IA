using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramientas de editor para hornear y validar el grafo de navegacion del
/// Parcial 2 sin entrar en Play. Menu: Tools > Parcial 2.
/// </summary>
public static class Parcial2NavTools
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";

    private static readonly Vector3 InitialSpawn = new(-18.5f, 1f, 0f);
    private static readonly Vector3 ExitPoint = new(18.5f, 1f, 0f);
    private static readonly Vector3 HuntingCenter = new(0f, 1f, 0f);
    private static readonly Vector3 SafeACenter = new(10f, 1f, 13f);
    private static readonly Vector3 SafeBCenter = new(-10f, 1f, -13f);

    [MenuItem("Tools/Parcial 2/Bake Nav Graph")]
    public static void BakeNavGraph()
    {
        NavGraph graph = FindGraph();
        if (graph == null)
        {
            Debug.LogError("[Parcial 2] No hay NavGraph en la escena.");
            return;
        }

        graph.Bake();

        if (!graph.IsReady)
        {
            Debug.LogError("[Parcial 2] El grafo quedo vacio.");
            return;
        }

        Debug.Log("[Parcial 2] Grafo: " + graph.NodeCount + " nodos | " +
            graph.SafeNodeCount + " en zonas seguras");

        LogSafeZoneNodes(graph);

        NavPath path = new();

        LogPath(graph, path, "Inicio -> Salida", InitialSpawn, ExitPoint, false);
        LogPath(graph, path, "Inicio -> Coto", InitialSpawn, HuntingCenter, false);
        LogPath(graph, path, "Inicio -> Zona segura A", InitialSpawn, SafeACenter, false);
        LogPath(graph, path, "Inicio -> Zona segura B", InitialSpawn, SafeBCenter, false);
        LogPath(graph, path, "Lobo: Inicio -> Zona segura A", InitialSpawn, SafeACenter, true);
        LogPath(graph, path, "Lobo: Inicio -> Zona segura B", InitialSpawn, SafeBCenter, true);
    }

    private static void LogSafeZoneNodes(NavGraph graph)
    {
        ZoneVolume[] zones = Object.FindObjectsByType<ZoneVolume>(FindObjectsSortMode.None);
        NavNode[] nodes = graph.Data.Nodes;

        foreach (ZoneVolume zone in zones)
        {
            if (zone == null || zone.Kind != ZoneKind.Safe) continue;

            int total = 0;
            int walkable = 0;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (!zone.Contains(nodes[i].Position)) continue;

                total++;
                if (nodes[i].Walkable) walkable++;
            }

            Debug.Log("[Parcial 2] " + zone.name + ": " + total +
                " nodos dentro | " + walkable + " caminables | " +
                zone.Bounds.size.ToString("F1"));
        }
    }

    private static void LogPath(
        NavGraph graph,
        NavPath path,
        string label,
        Vector3 from,
        Vector3 to,
        bool avoidSafeZones)
    {
        bool found = graph.GetPath(from, to, avoidSafeZones, path);

        if (!found)
        {
            Debug.LogWarning("[Parcial 2] " + label + ": SIN CAMINO");
            return;
        }

        Vector3 end = path.Points[path.Points.Count - 1];

        Debug.Log("[Parcial 2] " + label + ": " + path.Points.Count +
            " puntos | largo " + path.Length.ToString("F1") +
            " | termina en " + end.ToString("F1"));
    }

    private static NavGraph FindGraph()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        return Object.FindFirstObjectByType<NavGraph>();
    }
}
