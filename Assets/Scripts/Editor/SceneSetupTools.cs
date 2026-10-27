using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramientas de editor para dejar la escena organizada sin tener que
/// arrastrar objetos a mano. Menú: Tools > Parcial 1.
/// </summary>
public static class SceneSetupTools
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BoidPrefabPath = "Assets/Prefabs/Boid.prefab";
    private const string InterestPrefabPath = "Assets/Prefabs/InterestObject.prefab";

    [MenuItem("Tools/Parcial 1/Organize Scene")]
    public static void OrganizeScene()
    {
        OpenSceneIfNeeded();

        EnsureLayer("Interest");
        SetupBoidPrefab();
        SetupInterestPrefab();

        GameObject environment = GetOrCreateRoot("--- ENVIRONMENT ---");
        GameObject agents = GetOrCreateRoot("--- AGENTS ---");
        GameObject patrolRoutes = GetOrCreateRoot("--- PATROL ROUTES ---");
        GameObject systems = GetOrCreateRoot("--- SYSTEMS ---");

        MoveToParent(GameObject.Find("Bounds"), environment.transform);
        MoveToParent(GameObject.Find("Main Camera"), environment.transform);
        MoveToParent(GameObject.Find("Directional Light"), environment.transform);
        MoveToParent(GameObject.Find("Global Volume"), environment.transform);

        HunterAgent hunter = UnityEngine.Object.FindFirstObjectByType<HunterAgent>();
        if (hunter != null)
        {
            hunter.gameObject.layer = LayerMask.NameToLayer("Hunter");
            MoveToParent(hunter.gameObject, agents.transform);
        }

        GameObject boidsParent = GetOrCreateChild("Boids", agents.transform);
        BoidAgent[] boids = UnityEngine.Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);

        foreach (BoidAgent boid in boids)
            MoveToParent(boid.gameObject, boidsParent.transform);

        GameObject routeObject = GetOrCreateChild("Hunter Route", patrolRoutes.transform);
        PatrolRoute route = routeObject.GetComponent<PatrolRoute>();

        if (route == null)
            route = routeObject.AddComponent<PatrolRoute>();

        Transform[] waypoints = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith("Waypoint_"))
            .OrderBy(t => t.name)
            .ToArray();

        foreach (Transform waypoint in waypoints)
            MoveToParent(waypoint.gameObject, routeObject.transform);

        route.RefreshWaypoints();
        EditorUtility.SetDirty(route);

        GameObject trackerObject = GetOrCreateChild("Interest Object Tracker", systems.transform);
        if (trackerObject.GetComponent<InterestObjectTracker>() == null)
            trackerObject.AddComponent<InterestObjectTracker>();

        if (hunter != null)
        {
            SerializedObject hunterSerialized = new(hunter);
            SetObjectReference(hunterSerialized, "_patrolRoute", route);
            SetInt(hunterSerialized, "_boidLayer", 1 << LayerMask.NameToLayer("Boid"));
            SetFloat(hunterSerialized, "_detectionInterval", 0.15f);
            SetFloat(hunterSerialized, "_meleeDamage", 100f);
            SetFloat(hunterSerialized, "_rangeDamage", 50f);
            hunterSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log($"[Parcial 1] Escena organizada. Boids: {boids.Length}. Waypoints: {waypoints.Length}.");
    }

    [MenuItem("Tools/Parcial 1/Setup Layers and Prefabs")]
    public static void SetupLayersAndPrefabs()
    {
        EnsureLayer("Interest");
        SetupBoidPrefab();
        SetupInterestPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[Parcial 1] Layers y prefabs configurados.");
    }

    [MenuItem("Tools/Parcial 1/Validate Scene")]
    public static void ValidateScene()
    {
        OpenSceneIfNeeded();

        BoidAgent[] boids = UnityEngine.Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        HunterAgent hunter = UnityEngine.Object.FindFirstObjectByType<HunterAgent>();
        PatrolRoute route = UnityEngine.Object.FindFirstObjectByType<PatrolRoute>();
        InterestObjectTracker tracker = UnityEngine.Object.FindFirstObjectByType<InterestObjectTracker>();

        Debug.Log($"[Parcial 1] Boids: {boids.Length} (mínimo 6)");
        Debug.Log($"[Parcial 1] Hunter: {(hunter != null ? "OK" : "FALTA")}");
        Debug.Log($"[Parcial 1] Ruta: {(route != null ? $"{route.Count} waypoints" : "FALTA")}");
        Debug.Log($"[Parcial 1] Tracker de cebos: {(tracker != null ? "OK" : "FALTA")}");
        Debug.Log($"[Parcial 1] Layer Interest: {LayerMask.NameToLayer("Interest")}");

        if (hunter != null && hunter.PatrolRoute == null)
            Debug.LogWarning("[Parcial 1] El cazador no tiene asignada la PatrolRoute.");
    }

    private static void OpenSceneIfNeeded()
    {
        Scene active = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(active.path))
            EditorSceneManager.OpenScene(ScenePath);
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        return new GameObject(name);
    }

    private static GameObject GetOrCreateChild(string name, Transform parent)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            if (existing.transform.parent != parent)
                existing.transform.SetParent(parent, false);

            return existing;
        }

        GameObject created = new(name);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void MoveToParent(GameObject target, Transform parent)
    {
        if (target == null || parent == null) return;

        target.transform.SetParent(parent, false);
    }

    private static void EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);

            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return;
        }

        Debug.LogError($"[Parcial 1] No hay un layer libre para '{layerName}'.");
    }

    private static void SetupBoidPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoidPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Parcial 1] No se encontró {BoidPrefabPath}");
            return;
        }

        BoidAgent agent = prefab.GetComponent<BoidAgent>();
        if (agent == null)
        {
            Debug.LogError("[Parcial 1] El prefab Boid no tiene BoidAgent.");
            return;
        }

        prefab.layer = LayerMask.NameToLayer("Boid");

        SerializedObject serialized = new(agent);
        SetInt(serialized, "_boidLayer", 1 << LayerMask.NameToLayer("Boid"));
        SetInt(serialized, "_hunterLayer", 1 << LayerMask.NameToLayer("Hunter"));
        SetInt(serialized, "_interestLayer", 1 << LayerMask.NameToLayer("Interest"));
        SetFloat(serialized, "_maxHealth", 100f);
        SetFloat(serialized, "_detectionInterval", 0.15f);
        SetFloat(serialized, "_evadeWeight", 1.5f);
        SetFloat(serialized, "_arriveWeight", 1f);
        SetBool(serialized, "_drawGizmos", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(prefab);
    }

    private static void SetupInterestPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InterestPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Parcial 1] No se encontró {InterestPrefabPath}");
            return;
        }

        prefab.layer = LayerMask.NameToLayer("Interest");
        EditorUtility.SetDirty(prefab);
    }

    private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.intValue = value;
    }

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.floatValue = value;
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
    }
}
