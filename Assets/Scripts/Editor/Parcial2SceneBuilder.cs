using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera la escena del Parcial 2 (Variante A: anillo) con primitivas.
/// Menu: Tools > Parcial 2 > Build Scene.
/// El escenario queda guardado como Assets/Scenes/Parcial2.unity para que
/// el resto del parcial trabaje sobre una base reproducible.
/// </summary>
public static class Parcial2SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";
    private const string MaterialFolder = "Assets/Materials/Parcial2";
    private const string BoidPrefabPath = "Assets/Prefabs/Boid.prefab";
    private const string InterestPrefabPath = "Assets/Prefabs/InterestObject.prefab";

    private const float WallHeight = 3f;
    private const float WallThickness = 0.5f;

    [MenuItem("Tools/Parcial 2/Build Scene")]
    public static void BuildScene()
    {
        EnsureFolder(MaterialFolder);
        EnsureLayers();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material floorNeutral = CreateMaterial("Floor_Neutral", new Color(0.68f, 0.69f, 0.72f));
        Material floorInitial = CreateMaterial("Floor_Initial", new Color(0.22f, 0.62f, 0.35f));
        Material floorHunting = CreateMaterial("Floor_HuntingGround", new Color(0.85f, 0.55f, 0.16f));
        Material floorSafe = CreateMaterial("Floor_SafeZone", new Color(0.55f, 0.30f, 0.70f));
        Material floorExit = CreateMaterial("Floor_Exit", new Color(0.20f, 0.45f, 0.82f));
        Material floorBorder = CreateMaterial("Floor_Border", new Color(0.10f, 0.11f, 0.14f));
        Material wallMaterial = CreateMaterial("Wall", new Color(0.17f, 0.20f, 0.26f));
        Material hunterMaterial = CreateMaterial("Hunter", new Color(0.85f, 0.15f, 0.15f));
        Material wolfMaterial = CreateMaterial("Wolf", new Color(0.55f, 0.55f, 0.60f));

        GameObject environment = new("--- ENVIRONMENT ---");
        GameObject world = CreateChild("World", environment.transform);
        GameObject zones = CreateChild("Zones", environment.transform);

        BuildFloors(world.transform, floorNeutral, floorBorder, floorInitial, floorHunting, floorSafe, floorExit);
        BuildWalls(world.transform, wallMaterial);
        BuildZones(zones.transform);

        GameObject agents = new("--- AGENTS ---");
        GameObject hunterObject = BuildHunter(agents.transform, hunterMaterial);
        BuildBoids(agents.transform);
        BuildWolfSpawnPoints(agents.transform);

        GameObject routes = new("--- PATROL ROUTES ---");
        BuildRoutes(routes.transform, hunterObject);

        GameObject systems = new("--- SYSTEMS ---");
        BuildSystems(systems.transform);

        BuildCamera(environment.transform);
        BuildLight(environment.transform);
        ConfigureRenderSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Parcial 2] Escena construida en " + ScenePath);
        LogValidation();
    }

    [MenuItem("Tools/Parcial 2/Validate Scene")]
    public static void ValidateScene()
    {
        Scene active = SceneManager.GetActiveScene();

        if (active.path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        LogValidation();
    }

    private static void BuildFloors(
        Transform parent,
        Material neutral,
        Material border,
        Material initial,
        Material hunting,
        Material safe,
        Material exit)
    {
        CreateFloor(parent, "Floor_Base", new Vector3(0f, -0.05f, 0f), new Vector3(45f, 0.1f, 33f), neutral);

        CreateZoneFloor(parent, "Floor_Initial", new Vector3(-18.5f, 0f, 0f), new Vector2(4.8f, 5.8f), initial, border);
        CreateZoneFloor(parent, "Floor_Exit", new Vector3(18.5f, 0f, 0f), new Vector2(4.8f, 5.8f), exit, border);
        CreateZoneFloor(parent, "Floor_HuntingGround", new Vector3(0f, 0f, 0f), new Vector2(11.6f, 7.6f), hunting, border);
        CreateZoneFloor(parent, "Floor_SafeZone_A", new Vector3(10f, 0f, 13f), new Vector2(3.6f, 3.6f), safe, border);
        CreateZoneFloor(parent, "Floor_SafeZone_B", new Vector3(-10f, 0f, -13f), new Vector2(3.6f, 3.6f), safe, border);
        CreateZoneFloor(parent, "Floor_NorthRoom", new Vector3(0f, 0f, 11.25f), new Vector2(7.6f, 4.1f), neutral, border);
        CreateZoneFloor(parent, "Floor_SouthRoom", new Vector3(0f, 0f, -11.25f), new Vector2(7.6f, 4.1f), neutral, border);
    }

    private static void BuildWalls(Transform parent, Material material)
    {
        // Borde exterior del anillo. Los huecos conectan Inicio, Salida,
        // sala norte, sala sur y los pasillos de las zonas seguras.
        Wall(parent, "OuterNorth_A", new Vector3(-7.95f, 1.5f, 9.4f), new Vector3(12.9f, WallHeight, WallThickness), material);
        Wall(parent, "OuterNorth_B", new Vector3(5.125f, 1.5f, 9.4f), new Vector3(7.25f, WallHeight, WallThickness), material);
        Wall(parent, "OuterNorth_C", new Vector3(12.825f, 1.5f, 9.4f), new Vector3(3.15f, WallHeight, WallThickness), material);

        Wall(parent, "OuterSouth_A", new Vector3(-12.825f, 1.5f, -9.4f), new Vector3(3.15f, WallHeight, WallThickness), material);
        Wall(parent, "OuterSouth_B", new Vector3(-5.125f, 1.5f, -9.4f), new Vector3(7.25f, WallHeight, WallThickness), material);
        Wall(parent, "OuterSouth_C", new Vector3(7.95f, 1.5f, -9.4f), new Vector3(12.9f, WallHeight, WallThickness), material);

        Wall(parent, "OuterWest_North", new Vector3(-14.4f, 1.5f, 5.45f), new Vector3(WallThickness, WallHeight, 7.9f), material);
        Wall(parent, "OuterWest_South", new Vector3(-14.4f, 1.5f, -5.45f), new Vector3(WallThickness, WallHeight, 7.9f), material);

        Wall(parent, "OuterEast_North", new Vector3(14.4f, 1.5f, 5.45f), new Vector3(WallThickness, WallHeight, 7.9f), material);
        Wall(parent, "OuterEast_South", new Vector3(14.4f, 1.5f, -5.45f), new Vector3(WallThickness, WallHeight, 7.9f), material);

        // Coto de caza: dos entradas, una al norte y otra al sur.
        Wall(parent, "CotoNorth_West", new Vector3(-3.875f, 1.5f, 4.25f), new Vector3(4.75f, WallHeight, WallThickness), material);
        Wall(parent, "CotoNorth_East", new Vector3(3.875f, 1.5f, 4.25f), new Vector3(4.75f, WallHeight, WallThickness), material);
        Wall(parent, "CotoSouth_West", new Vector3(-3.875f, 1.5f, -4.25f), new Vector3(4.75f, WallHeight, WallThickness), material);
        Wall(parent, "CotoSouth_East", new Vector3(3.875f, 1.5f, -4.25f), new Vector3(4.75f, WallHeight, WallThickness), material);
        Wall(parent, "CotoWest", new Vector3(-6.25f, 1.5f, 0f), new Vector3(WallThickness, WallHeight, 8.5f), material);
        Wall(parent, "CotoEast", new Vector3(6.25f, 1.5f, 0f), new Vector3(WallThickness, WallHeight, 8.5f), material);

        // Sala norte.
        Wall(parent, "NorthRoom_West", new Vector3(-4.25f, 1.5f, 11.575f), new Vector3(WallThickness, WallHeight, 4.35f), material);
        Wall(parent, "NorthRoom_East", new Vector3(4.25f, 1.5f, 11.575f), new Vector3(WallThickness, WallHeight, 4.35f), material);
        Wall(parent, "NorthRoom_North", new Vector3(0f, 1.5f, 13.75f), new Vector3(8.5f, WallHeight, WallThickness), material);

        // Sala sur.
        Wall(parent, "SouthRoom_West", new Vector3(-4.25f, 1.5f, -11.575f), new Vector3(WallThickness, WallHeight, 4.35f), material);
        Wall(parent, "SouthRoom_East", new Vector3(4.25f, 1.5f, -11.575f), new Vector3(WallThickness, WallHeight, 4.35f), material);
        Wall(parent, "SouthRoom_South", new Vector3(0f, 1.5f, -13.75f), new Vector3(8.5f, WallHeight, WallThickness), material);

        // Zona inicial.
        Wall(parent, "Initial_West", new Vector3(-21.25f, 1.5f, 0f), new Vector3(WallThickness, WallHeight, 6.5f), material);
        Wall(parent, "Initial_North", new Vector3(-18.625f, 1.5f, 3.25f), new Vector3(5.25f, WallHeight, WallThickness), material);
        Wall(parent, "Initial_South", new Vector3(-18.625f, 1.5f, -3.25f), new Vector3(5.25f, WallHeight, WallThickness), material);
        Wall(parent, "Initial_East_North", new Vector3(-16f, 1.5f, 2.375f), new Vector3(WallThickness, WallHeight, 1.75f), material);
        Wall(parent, "Initial_East_South", new Vector3(-16f, 1.5f, -2.375f), new Vector3(WallThickness, WallHeight, 1.75f), material);
        Wall(parent, "Initial_Corridor_North", new Vector3(-15.2f, 1.5f, 1.5f), new Vector3(1.6f, WallHeight, WallThickness), material);
        Wall(parent, "Initial_Corridor_South", new Vector3(-15.2f, 1.5f, -1.5f), new Vector3(1.6f, WallHeight, WallThickness), material);

        // Salida.
        Wall(parent, "Exit_East", new Vector3(21.25f, 1.5f, 0f), new Vector3(WallThickness, WallHeight, 6.5f), material);
        Wall(parent, "Exit_North", new Vector3(18.625f, 1.5f, 3.25f), new Vector3(5.25f, WallHeight, WallThickness), material);
        Wall(parent, "Exit_South", new Vector3(18.625f, 1.5f, -3.25f), new Vector3(5.25f, WallHeight, WallThickness), material);
        Wall(parent, "Exit_West_North", new Vector3(16f, 1.5f, 2.375f), new Vector3(WallThickness, WallHeight, 1.75f), material);
        Wall(parent, "Exit_West_South", new Vector3(16f, 1.5f, -2.375f), new Vector3(WallThickness, WallHeight, 1.75f), material);
        Wall(parent, "Exit_Corridor_North", new Vector3(15.2f, 1.5f, 1.5f), new Vector3(1.6f, WallHeight, WallThickness), material);
        Wall(parent, "Exit_Corridor_South", new Vector3(15.2f, 1.5f, -1.5f), new Vector3(1.6f, WallHeight, WallThickness), material);

        // Zona segura A: callejon sin salida al noreste.
        Wall(parent, "SafeA_West", new Vector3(8f, 1.5f, 13f), new Vector3(WallThickness, WallHeight, 4f), material);
        Wall(parent, "SafeA_East", new Vector3(12f, 1.5f, 13f), new Vector3(WallThickness, WallHeight, 4f), material);
        Wall(parent, "SafeA_North", new Vector3(10f, 1.5f, 15f), new Vector3(4f, WallHeight, WallThickness), material);
        Wall(parent, "SafeA_South_West", new Vector3(8.375f, 1.5f, 11f), new Vector3(0.75f, WallHeight, WallThickness), material);
        Wall(parent, "SafeA_South_East", new Vector3(11.625f, 1.5f, 11f), new Vector3(0.75f, WallHeight, WallThickness), material);
        Wall(parent, "SafeA_Corridor_West", new Vector3(8.75f, 1.5f, 10.2f), new Vector3(WallThickness, WallHeight, 1.6f), material);
        Wall(parent, "SafeA_Corridor_East", new Vector3(11.25f, 1.5f, 10.2f), new Vector3(WallThickness, WallHeight, 1.6f), material);

        // Zona segura B: callejon sin salida al suroeste.
        Wall(parent, "SafeB_West", new Vector3(-12f, 1.5f, -13f), new Vector3(WallThickness, WallHeight, 4f), material);
        Wall(parent, "SafeB_East", new Vector3(-8f, 1.5f, -13f), new Vector3(WallThickness, WallHeight, 4f), material);
        Wall(parent, "SafeB_South", new Vector3(-10f, 1.5f, -15f), new Vector3(4f, WallHeight, WallThickness), material);
        Wall(parent, "SafeB_North_West", new Vector3(-11.625f, 1.5f, -11f), new Vector3(0.75f, WallHeight, WallThickness), material);
        Wall(parent, "SafeB_North_East", new Vector3(-8.375f, 1.5f, -11f), new Vector3(0.75f, WallHeight, WallThickness), material);
        Wall(parent, "SafeB_Corridor_West", new Vector3(-11.25f, 1.5f, -10.2f), new Vector3(WallThickness, WallHeight, 1.6f), material);
        Wall(parent, "SafeB_Corridor_East", new Vector3(-8.75f, 1.5f, -10.2f), new Vector3(WallThickness, WallHeight, 1.6f), material);
    }

    private static void BuildZones(Transform parent)
    {        Transform initialSpawn = CreateMarker(parent, "Initial_Spawn", new Vector3(-18.5f, 0f, 0f));
        Transform exitPoint = CreateMarker(parent, "Exit_Point", new Vector3(18.5f, 0f, 0f));
        Transform huntingEntry = CreateMarker(parent, "HuntingGround_Entry", new Vector3(0f, 0f, 5.5f));
        Transform safeAEntry = CreateMarker(parent, "SafeZone_A_Entry", new Vector3(10f, 0f, 10.5f));
        Transform safeBEntry = CreateMarker(parent, "SafeZone_B_Entry", new Vector3(-10f, 0f, -10.5f));

        CreateZone(
            parent,
            "Zone_Initial",
            ZoneKind.Initial,
            new Vector3(-18.5f, 1.5f, 0f),
            new Vector3(5f, 3f, 6f),
            new Color(0.22f, 0.62f, 0.35f),
            GameLayers.Index(GameLayers.InitialZone),
            initialSpawn);

        CreateZone(
            parent,
            "Zone_HuntingGround",
            ZoneKind.HuntingGround,
            new Vector3(0f, 1.5f, 0f),
            new Vector3(11.8f, 3f, 7.8f),
            new Color(0.85f, 0.55f, 0.16f),
            GameLayers.Index(GameLayers.HuntingGround),
            huntingEntry);

        CreateZone(
            parent,
            "Zone_Safe_A",
            ZoneKind.Safe,
            new Vector3(10f, 1.5f, 13f),
            new Vector3(3.8f, 3f, 3.8f),
            new Color(0.55f, 0.30f, 0.70f),
            GameLayers.Index(GameLayers.SafeZone),
            safeAEntry);

        CreateZone(
            parent,
            "Zone_Safe_B",
            ZoneKind.Safe,
            new Vector3(-10f, 1.5f, -13f),
            new Vector3(3.8f, 3f, 3.8f),
            new Color(0.55f, 0.30f, 0.70f),
            GameLayers.Index(GameLayers.SafeZone),
            safeBEntry);

        CreateZone(
            parent,
            "Zone_Exit",
            ZoneKind.Exit,
            new Vector3(18.5f, 1.5f, 0f),
            new Vector3(5f, 3f, 6f),
            new Color(0.20f, 0.45f, 0.82f),
            GameLayers.Index(GameLayers.ExitZone),
            exitPoint);

        CreateZone(
            parent,
            "Zone_NorthRoom",
            ZoneKind.Room,
            new Vector3(0f, 1.5f, 11.25f),
            new Vector3(7.8f, 3f, 4.3f),
            new Color(0.72f, 0.72f, 0.72f),
            0);

        CreateZone(
            parent,
            "Zone_SouthRoom",
            ZoneKind.Room,
            new Vector3(0f, 1.5f, -11.25f),
            new Vector3(7.8f, 3f, 4.3f),
            new Color(0.72f, 0.72f, 0.72f),
            0);
    }

    private static GameObject BuildHunter(Transform parent, Material material)
    {
        GameObject hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hunter.name = "Hunter";
        hunter.layer = GameLayers.Index(GameLayers.Hunter);
        hunter.transform.SetParent(parent, false);
        hunter.transform.position = new Vector3(-18.5f, 1f, 0f);
        hunter.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Renderer renderer = hunter.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        Rigidbody body = hunter.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        HunterAgent agent = hunter.AddComponent<HunterAgent>();
        GameObject interestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InterestPrefabPath);

        SerializedObject serialized = new(agent);
        SetInt(serialized, "_boidLayer", GameLayers.Mask(GameLayers.Boid));
        SetFloat(serialized, "_speed", 7f);
        SetFloat(serialized, "_tba", 2f);
        SetFloat(serialized, "_rangeAttackRadius", 6f);
        SetFloat(serialized, "_meleeAttackRadius", 1.5f);
        SetFloat(serialized, "_perceptionRadius", 10f);
        SetFloat(serialized, "_meleeDamage", 100f);
        SetFloat(serialized, "_rangeDamage", 50f);
        SetFloat(serialized, "_detectionInterval", 0.15f);
        SetFloat(serialized, "_interestSpawnInterval", 4f);
        SetInt(serialized, "_maxInterestObjects", 5);
        SetObject(serialized, "_interestObjectPrefab", interestPrefab);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(agent);
        return hunter;
    }

    private static void BuildBoids(Transform parent)
    {
        GameObject boidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoidPrefabPath);
        if (boidPrefab == null)
        {
            Debug.LogError("[Parcial 2] No se encontro " + BoidPrefabPath);
            return;
        }

        GameObject boidsParent = CreateChild("Boids", parent);
        Vector3[] positions =
        {
            new(-3.5f, 1f, -2.5f),
            new(-1.5f, 1f, 2.5f),
            new(1.5f, 1f, -1.5f),
            new(3.5f, 1f, 2f),
            new(0f, 1f, 0.5f),
            new(-4.5f, 1f, 1f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject boid = (GameObject)PrefabUtility.InstantiatePrefab(boidPrefab, boidsParent.transform);
            boid.name = "Boid_" + (i + 1);
            boid.transform.position = positions[i];
            boid.transform.rotation = Quaternion.identity;
        }
    }

    private static void BuildWolfSpawnPoints(Transform parent)
    {
        GameObject spawns = CreateChild("Wolf Spawn Points", parent);

        CreateMarker(spawns.transform, "WolfSpawn_1", new Vector3(-12f, 1f, 7.2f));
        CreateMarker(spawns.transform, "WolfSpawn_2", new Vector3(-11f, 1f, 7.2f));
        CreateMarker(spawns.transform, "WolfSpawn_3", new Vector3(12f, 1f, 7.2f));
        CreateMarker(spawns.transform, "WolfSpawn_4", new Vector3(-11f, 1f, -7.2f));
    }

    private static void BuildRoutes(Transform parent, GameObject hunterObject)
    {
        PatrolRoute hunterRoute = CreateRoute(
            parent,
            "Hunter Route",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(-18.5f, 1f, 0f));

        if (hunterObject != null)
        {
            HunterAgent hunter = hunterObject.GetComponent<HunterAgent>();
            SerializedObject serialized = new(hunter);
            SetObject(serialized, "_patrolRoute", hunterRoute);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        CreateRoute(
            parent,
            "Wolf Route L1",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(-12f, 1f, 7.2f),
            new Vector3(-12f, 1f, -7.2f));

        CreateRoute(
            parent,
            "Wolf Route L2",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(-11f, 1f, 7.2f),
            new Vector3(-4f, 1f, 7.2f),
            new Vector3(0f, 1f, 7.2f),
            new Vector3(0f, 1f, 12f),
            new Vector3(4f, 1f, 7.2f),
            new Vector3(11f, 1f, 7.2f));

        CreateRoute(
            parent,
            "Wolf Route L3",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(12f, 1f, 7.2f),
            new Vector3(12f, 1f, -7.2f));

        CreateRoute(
            parent,
            "Wolf Route L4",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(-11f, 1f, -7.2f),
            new Vector3(-4f, 1f, -7.2f),
            new Vector3(0f, 1f, -7.2f),
            new Vector3(0f, 1f, -12f),
            new Vector3(4f, 1f, -7.2f),
            new Vector3(11f, 1f, -7.2f));
    }

    private static PatrolRoute CreateRoute(Transform parent, string name, PatrolRoute.RouteMode mode, params Vector3[] points)
    {
        GameObject routeObject = CreateChild(name, parent);
        PatrolRoute route = routeObject.AddComponent<PatrolRoute>();
        route.Configure(mode);

        for (int i = 0; i < points.Length; i++)
            CreateMarker(routeObject.transform, "Waypoint_" + (i + 1), points[i]);

        route.RefreshWaypoints();
        EditorUtility.SetDirty(route);
        return route;
    }

    private static void BuildSystems(Transform parent)
    {
        GameObject boundsObject = CreateChild("Bounds", parent);
        WorldBounds bounds = boundsObject.AddComponent<WorldBounds>();
        bounds.Configure(11.2f, 7.2f, false, true);
        EditorUtility.SetDirty(bounds);

        GameObject trackerObject = CreateChild("Interest Object Tracker", parent);
        trackerObject.AddComponent<InterestObjectTracker>();

        GameObject feedbackObject = CreateChild("Feedback", parent);
        feedbackObject.AddComponent<FeedbackManager>();
        feedbackObject.AddComponent<FeedbackHUD>();
    }

    private static void BuildCamera(Transform parent)
    {
        GameObject cameraObject = CreateChild("Main Camera", parent);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 40f, 0f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 18.5f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 200f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);

        cameraObject.AddComponent<AudioListener>();
    }

    private static void BuildLight(Transform parent)
    {
        GameObject lightObject = CreateChild("Directional Light", parent);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.97f, 0.92f);
        light.shadows = LightShadows.Soft;
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.38f, 0.45f);
        RenderSettings.fog = false;
    }

    private static void CreateFloor(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.layer = GameLayers.Index(GameLayers.Walkable);
        floor.transform.SetParent(parent, false);
        floor.transform.position = center;
        floor.transform.localScale = size;

        Collider collider = floor.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static void CreateZoneFloor(
        Transform parent,
        string name,
        Vector3 center,
        Vector2 size,
        Material material,
        Material borderMaterial)
    {
        CreateFloor(
            parent,
            name + "_Border",
            new Vector3(center.x, 0.015f, center.z),
            new Vector3(size.x + 0.4f, 0.03f, size.y + 0.4f),
            borderMaterial);

        CreateFloor(
            parent,
            name,
            new Vector3(center.x, 0.03f, center.z),
            new Vector3(size.x, 0.03f, size.y),
            material);
    }

    private static void Wall(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.layer = GameLayers.Index(GameLayers.Obstacle);
        wall.transform.SetParent(parent, false);
        wall.transform.position = center;
        wall.transform.localScale = size;

        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static ZoneVolume CreateZone(
        Transform parent,
        string name,
        ZoneKind kind,
        Vector3 center,
        Vector3 size,
        Color color,
        int layer,
        Transform entryPoint = null)
    {
        GameObject zoneObject = CreateChild(name, parent);
        zoneObject.layer = layer >= 0 ? layer : 0;
        zoneObject.transform.position = center;

        ZoneVolume zone = zoneObject.AddComponent<ZoneVolume>();
        zone.Configure(kind, size, color, entryPoint);

        BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = Vector3.zero;
        collider.size = size;

        EditorUtility.SetDirty(zone);
        return zone;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = CreateChild(name, parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("[Parcial 2] No se encontro un shader para " + name);
            return null;
        }

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path);
        string folderName = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent.Replace("\\", "/"));

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void EnsureLayers()
    {
        string[] layers =
        {
            GameLayers.Wolf,
            GameLayers.Obstacle,
            GameLayers.Walkable,
            GameLayers.SafeZone,
            GameLayers.HuntingGround,
            GameLayers.ExitZone,
            GameLayers.InitialZone,
            GameLayers.WolfBlocker
        };

        foreach (string layer in layers)
            EnsureLayer(layer);
    }

    private static void EnsureLayer(string layerName)
    {
        if (GameLayers.Exists(layerName)) return;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        SerializedObject tagManager = new(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return;
        }

        Debug.LogError("[Parcial 2] No hay un layer libre para " + layerName);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void LogValidation()
    {
        BoidAgent[] boids = Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None);
        HunterAgent hunter = Object.FindFirstObjectByType<HunterAgent>();
        ZoneVolume[] zones = Object.FindObjectsByType<ZoneVolume>(FindObjectsSortMode.None);
        PatrolRoute[] routes = Object.FindObjectsByType<PatrolRoute>(FindObjectsSortMode.None);
        WorldBounds bounds = Object.FindFirstObjectByType<WorldBounds>();
        InterestObjectTracker tracker = Object.FindFirstObjectByType<InterestObjectTracker>();
        FeedbackManager feedback = Object.FindFirstObjectByType<FeedbackManager>();

        int safeZones = 0;
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].Kind == ZoneKind.Safe) safeZones++;
        }

        Debug.Log("[Parcial 2] Boids: " + boids.Length + " | Hunter: " + (hunter != null) +
            " | Zonas: " + zones.Length + " (seguras: " + safeZones + ")" +
            " | Rutas: " + routes.Length + " | Bounds: " + (bounds != null) +
            " | Tracker: " + (tracker != null) + " | Feedback: " + (feedback != null));
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
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
}
