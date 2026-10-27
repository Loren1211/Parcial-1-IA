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

        EnsureFeedback(systems.transform);

        if (hunter != null)
        {
            SerializedObject hunterSerialized = new(hunter);
            SetObjectReference(hunterSerialized, "_patrolRoute", route);
            SetInt(hunterSerialized, "_boidLayer", 1 << LayerMask.NameToLayer("Boid"));
            SetFloat(hunterSerialized, "_speed", 6.5f);
            SetFloat(hunterSerialized, "_detectionInterval", 0.15f);
            SetFloat(hunterSerialized, "_tba", 2f);
            SetFloat(hunterSerialized, "_perceptionRadius", 9f);
            SetFloat(hunterSerialized, "_rangeAttackRadius", 6f);
            SetFloat(hunterSerialized, "_meleeAttackRadius", 1.5f);
            SetFloat(hunterSerialized, "_meleeDamage", 100f);
            SetFloat(hunterSerialized, "_rangeDamage", 50f);
            SetFloat(hunterSerialized, "_interestSpawnInterval", 4f);
            SetInt(hunterSerialized, "_maxInterestObjects", 5);
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
        FeedbackManager feedback = UnityEngine.Object.FindFirstObjectByType<FeedbackManager>();
        FeedbackHUD hud = UnityEngine.Object.FindFirstObjectByType<FeedbackHUD>();

        Debug.Log($"[Parcial 1] Boids: {boids.Length} (mínimo 6)");
        Debug.Log($"[Parcial 1] Hunter: {(hunter != null ? "OK" : "FALTA")}");
        Debug.Log($"[Parcial 1] Ruta: {(route != null ? $"{route.Count} waypoints" : "FALTA")}");
        Debug.Log($"[Parcial 1] Tracker de cebos: {(tracker != null ? "OK" : "FALTA")}");
        Debug.Log($"[Parcial 1] Feedback: {(feedback != null ? "OK" : "FALTA")} | HUD: {(hud != null ? "OK" : "FALTA")}");
        Debug.Log($"[Parcial 1] Layer Interest: {LayerMask.NameToLayer("Interest")}");

        if (boids.Length < 6)
            Debug.LogWarning("[Parcial 1] Hay menos de 6 boids: la consigna pide un cardumen autónomo.");

        if (hunter == null)
        {
            Debug.LogWarning("[Parcial 1] No hay HunterAgent en la escena.");
            return;
        }

        if (hunter.PatrolRoute == null)
            Debug.LogWarning("[Parcial 1] El cazador no tiene asignada la PatrolRoute.");

        if (hunter.InterestObjectPrefab == null)
            Debug.LogWarning("[Parcial 1] El cazador no tiene asignado el prefab de cebo.");

        if (hunter.TBA <= 0f)
            Debug.LogWarning("[Parcial 1] El TBA del cazador debe ser mayor a 0.");

        if (hunter.RangeAttackRadius <= hunter.MeleeAttackRadius)
            Debug.LogWarning("[Parcial 1] RangeAttackRadius debería ser mayor que MeleeAttackRadius.");

        if (feedback == null || hud == null)
            Debug.LogWarning("[Parcial 1] Ejecutá 'Organize Scene' para crear el sistema de feedback.");
    }

    private static void EnsureFeedback(Transform systems)
    {
        GameObject feedbackObject = GetOrCreateChild("Feedback", systems);

        if (feedbackObject.GetComponent<FeedbackManager>() == null)
            feedbackObject.AddComponent<FeedbackManager>();

        if (feedbackObject.GetComponent<FeedbackHUD>() == null)
            feedbackObject.AddComponent<FeedbackHUD>();

        EditorUtility.SetDirty(feedbackObject);
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
        SetFloat(serialized, "_maxSpeed", 5f);
        SetFloat(serialized, "_maxForce", 10f);
        SetFloat(serialized, "_viewRadius", 6f);
        SetFloat(serialized, "_separationRadius", 3f);
        SetFloat(serialized, "_separationWeight", 1.5f);
        SetFloat(serialized, "_alignmentWeight", 0.8f);
        SetFloat(serialized, "_cohesionWeight", 0.6f);
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
