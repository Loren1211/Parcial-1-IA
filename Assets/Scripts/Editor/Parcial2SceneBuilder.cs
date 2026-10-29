using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera la escena del Parcial 2 (Variante A: anillo) con primitivas.
/// Menu: Tools > Parcial 2 > Build Scene.
///
/// El mapa se arma sobre una grilla de tiles de 1.5 unidades y reproduce la
/// referencia del enunciado: un anillo perimetral, un Coto central con salidas
/// a izquierda y derecha, salas norte y sur con un bloque oscuro central y
/// zonas seguras en los callejones sin salida del noreste y del suroeste.
/// El escenario queda guardado como Assets/Scenes/Parcial2.unity para que el
/// resto del parcial trabaje sobre una base reproducible.
/// </summary>
public static class Parcial2SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";
    private const string MaterialFolder = "Assets/Materials/Parcial2";
    private const string BoidPrefabPath = "Assets/Prefabs/Boid.prefab";
    private const string InterestPrefabPath = "Assets/Prefabs/InterestObject.prefab";

    // Grilla del mapa: 32 x 28 tiles interiores (columnas 0..31, filas 0..27).
    // El marco exterior ocupa la columna -1 y 32 y las filas -1 y 28.
    private const float Tile = 1.5f;
    private const float WallHeight = 2.5f;
    private const float BarDepth = 0.75f;

    private const int GridMinCol = -8;
    private const int GridMaxCol = 39;
    private const int GridMinRow = -1;
    private const int GridMaxRow = 28;

    private static readonly Vector3 InitialCenter = new(-30.75f, 0f, 0f);
    private static readonly Vector3 ExitCenter = new(30.75f, 0f, 0f);
    private static readonly Vector3 SafeACenter = new(15.75f, 0f, 9.75f);
    private static readonly Vector3 SafeBCenter = new(-15.75f, 0f, -8.25f);

    [MenuItem("Tools/Parcial 2/Build Scene")]
    public static void BuildScene()
    {
        EnsureFolder(MaterialFolder);
        EnsureLayers();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Paleta tomada del mapa de ejemplo: pisos pastel y muros gris azulados.
        Material floorNeutral = CreateMaterial("Floor_Neutral", new Color(0.886f, 0.867f, 0.820f));
        Material floorInitial = CreateMaterial("Floor_Initial", new Color(0.722f, 0.816f, 0.725f));
        Material floorHunting = CreateMaterial("Floor_HuntingGround", new Color(0.902f, 0.816f, 0.659f));
        Material floorSafe = CreateMaterial("Floor_SafeZone", new Color(0.808f, 0.733f, 0.808f));
        Material floorExit = CreateMaterial("Floor_Exit", new Color(0.690f, 0.769f, 0.816f));
        Material borderInitial = CreateMaterial("Border_Initial", new Color(0.420f, 0.600f, 0.450f));
        Material borderHunting = CreateMaterial("Border_HuntingGround", new Color(0.960f, 0.647f, 0.141f));
        Material borderSafe = CreateMaterial("Border_SafeZone", new Color(0.557f, 0.306f, 0.776f));
        Material borderExit = CreateMaterial("Border_Exit", new Color(0.360f, 0.520f, 0.680f));
        Material wallMaterial = CreateMaterial("Wall", new Color(0.290f, 0.318f, 0.361f));
        Material boundaryMaterial = CreateMaterial("Boundary", new Color(0.090f, 0.100f, 0.130f));
        Material hunterMaterial = CreateMaterial("Hunter", new Color(0.85f, 0.15f, 0.15f));
        Material wolfMaterial = CreateMaterial("Wolf", new Color(0.55f, 0.55f, 0.60f));

        GameObject environment = new("--- ENVIRONMENT ---");
        GameObject world = CreateChild("World", environment.transform);
        GameObject zones = CreateChild("Zones", environment.transform);

        BuildFloors(
            world.transform,
            floorNeutral,
            floorInitial,
            floorHunting,
            floorSafe,
            floorExit,
            borderInitial,
            borderHunting,
            borderSafe,
            borderExit);
        BuildWalls(world.transform, wallMaterial, boundaryMaterial);
        BuildZones(zones.transform);

        GameObject agents = new("--- AGENTS ---");
        GameObject systems = new("--- SYSTEMS ---");
        NavGraph navGraph = BuildSystems(systems.transform);

        GameObject hunterObject = BuildHunter(agents.transform, hunterMaterial);
        BuildBoids(agents.transform);
        WolfAgent[] wolves = BuildWolves(agents.transform, wolfMaterial, navGraph);

        GameObject routes = new("--- PATROL ROUTES ---");
        BuildRoutes(routes.transform, hunterObject, wolves);

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

    // ---------------------------------------------------------------------
    // Coordenadas de la grilla
    // ---------------------------------------------------------------------

    private static float EdgeX(int column) => (column - 16f) * Tile;

    private static float EdgeZ(int row) => (14f - row) * Tile;

    private static Vector3 TileCenter(int column, int row, float y = 0f) =>
        new((column + 0.5f - 16f) * Tile, y, (13.5f - row) * Tile);

    // ---------------------------------------------------------------------
    // Pisos
    // ---------------------------------------------------------------------

    private static void BuildFloors(
        Transform parent,
        Material neutral,
        Material initial,
        Material hunting,
        Material safe,
        Material exit,
        Material borderInitial,
        Material borderHunting,
        Material borderSafe,
        Material borderExit)
    {
        // Piso del edificio principal (anillo + salas + Coto).
        CreateFloor(parent, "Floor_Main", new Vector3(0f, -0.05f, 0f), new Vector3(48f, 0.1f, 42f), neutral);

        // Pisos de las salas de Inicio y Salida, y de sus corredores.
        CreateFloor(parent, "Floor_InitialRoom", new Vector3(InitialCenter.x, -0.05f, 0f), new Vector3(7.5f, 0.1f, 9f), neutral);
        CreateFloor(parent, "Floor_InitialCorridor", new Vector3(-25.5f, -0.05f, 0f), new Vector3(3f, 0.1f, 3f), neutral);
        CreateFloor(parent, "Floor_ExitRoom", new Vector3(ExitCenter.x, -0.05f, 0f), new Vector3(7.5f, 0.1f, 9f), neutral);
        CreateFloor(parent, "Floor_ExitCorridor", new Vector3(25.5f, -0.05f, 0f), new Vector3(3f, 0.1f, 3f), neutral);

        // Pisos de color de las zonas logicas.
        CreateZoneFloor(parent, "Floor_Initial", InitialCenter, new Vector2(7.5f, 9f), initial, borderInitial);
        CreateZoneFloor(parent, "Floor_Exit", ExitCenter, new Vector2(7.5f, 9f), exit, borderExit);
        CreateZoneFloor(parent, "Floor_HuntingGround", Vector3.zero, new Vector2(21f, 15f), hunting, borderHunting);
        CreateZoneFloor(parent, "Floor_SafeZone_A", SafeACenter, new Vector2(7.5f, 7.5f), safe, borderSafe);
        CreateZoneFloor(parent, "Floor_SafeZone_B", SafeBCenter, new Vector2(7.5f, 7.5f), safe, borderSafe);
    }

    // ---------------------------------------------------------------------
    // Paredes
    // ---------------------------------------------------------------------

    private static void BuildWalls(Transform parent, Material material, Material boundaryMaterial)
    {
        bool[,] wall = BuildWallGrid();

        int columns = GridMaxCol - GridMinCol + 1;
        int rows = GridMaxRow - GridMinRow + 1;
        bool[,] used = new bool[columns, rows];
        int index = 1;

        // Fusion greedy de celdas contiguas en rectangulos para no crear un
        // cubo por tile. Las celdas nunca se solapan entre rectangulos.
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (!wall[column, row] || used[column, row]) continue;

                int width = 0;
                while (column + width < columns
                    && wall[column + width, row]
                    && !used[column + width, row])
                {
                    width++;
                }

                int height = 1;
                bool grows = true;

                while (grows && row + height < rows)
                {
                    for (int offset = 0; offset < width; offset++)
                    {
                        if (!wall[column + offset, row + height]
                            || used[column + offset, row + height])
                        {
                            grows = false;
                            break;
                        }
                    }

                    if (grows) height++;
                }

                for (int r = 0; r < height; r++)
                {
                    for (int c = 0; c < width; c++)
                        used[column + c, row + r] = true;
                }

                int col0 = column + GridMinCol;
                int col1 = column + width - 1 + GridMinCol;
                int row0 = row + GridMinRow;
                int row1 = row + height - 1 + GridMinRow;

                // El marco exterior se pinta como el fondo para que el piso
                // termine contra el borde sin una banda gris visible.
                bool boundary = col1 < 0 || col0 > 31 || row1 < 0 || row0 > 27;
                Material blockMaterial = boundary ? boundaryMaterial : material;

                CreateWallBlock(
                    parent,
                    "Wall_" + index.ToString("00"),
                    col0,
                    col1,
                    row0,
                    row1,
                    blockMaterial);

                index++;
            }
        }

        // Bloque oscuro que cuelga del techo de cada sala (medio tile).
        CreateWallBlock(parent, "NorthRoom_Bar", 13, 18, 3, 3, material, BarDepth, true);
        CreateWallBlock(parent, "SouthRoom_Bar", 13, 18, 24, 24, material, BarDepth, false);
    }

    /// <summary>
    /// Grilla logica de paredes. Se parte de la masa interior y se excavan los
    /// espacios caminables: salas, Coto, corredores y callejones seguros.
    /// </summary>
    private static bool[,] BuildWallGrid()
    {
        int columns = GridMaxCol - GridMinCol + 1;
        int rows = GridMaxRow - GridMinRow + 1;
        bool[,] wall = new bool[columns, rows];

        void SetRect(int col0, int col1, int row0, int row1, bool value)
        {
            for (int column = col0; column <= col1; column++)
            {
                for (int row = row0; row <= row1; row++)
                    wall[column - GridMinCol, row - GridMinRow] = value;
            }
        }

        // Masa interior del anillo.
        SetRect(2, 29, 2, 25, true);

        // Sala norte: 14 x 4 tiles, con dos accesos al anillo.
        SetRect(9, 22, 3, 6, false);
        SetRect(11, 12, 2, 2, false);
        SetRect(19, 20, 2, 2, false);

        // Zona segura noreste y su callejon sin salida.
        SetRect(24, 28, 5, 9, false);
        SetRect(25, 26, 10, 12, false);

        // Coto central: 14 x 10 tiles, abierto a izquierda y derecha.
        SetRect(9, 22, 9, 18, false);
        SetRect(8, 8, 13, 14, false);
        SetRect(23, 23, 13, 14, false);
        SetRect(2, 8, 13, 14, false);
        SetRect(23, 29, 13, 14, false);

        // Zona segura suroeste y su callejon sin salida.
        SetRect(5, 6, 15, 16, false);
        SetRect(3, 7, 17, 21, false);

        // Sala sur: 14 x 4 tiles, con dos accesos al anillo.
        SetRect(9, 22, 21, 24, false);
        SetRect(11, 12, 25, 25, false);
        SetRect(19, 20, 25, 25, false);

        // Marco exterior con las dos aberturas del corredor principal.
        SetRect(-1, 32, -1, -1, true);
        SetRect(-1, 32, 28, 28, true);
        SetRect(-1, -1, -1, 28, true);
        SetRect(32, 32, -1, 28, true);
        SetRect(-1, -1, 13, 14, false);
        SetRect(32, 32, 13, 14, false);

        // Sala de Inicio (oeste), con puerta al corredor.
        SetRect(-8, -2, 10, 10, true);
        SetRect(-8, -2, 17, 17, true);
        SetRect(-8, -8, 10, 17, true);
        SetRect(-2, -2, 10, 12, true);
        SetRect(-2, -2, 15, 17, true);

        // Sala de Salida (este), con puerta al corredor.
        SetRect(33, 39, 10, 10, true);
        SetRect(33, 39, 17, 17, true);
        SetRect(39, 39, 10, 17, true);
        SetRect(33, 33, 10, 12, true);
        SetRect(33, 33, 15, 17, true);

        return wall;
    }

    private static void CreateWallBlock(
        Transform parent,
        string name,
        int col0,
        int col1,
        int row0,
        int row1,
        Material material,
        float depthOverride = 0f,
        bool alignTop = false)
    {
        float width = (col1 - col0 + 1) * Tile;
        float depth = depthOverride > 0f ? depthOverride : (row1 - row0 + 1) * Tile;
        float centerX = (EdgeX(col0) + EdgeX(col1 + 1)) * 0.5f;

        float centerZ;

        if (depthOverride > 0f)
        {
            centerZ = alignTop
                ? EdgeZ(row0) - depth * 0.5f
                : EdgeZ(row1 + 1) + depth * 0.5f;
        }
        else
        {
            centerZ = (EdgeZ(row0) + EdgeZ(row1 + 1)) * 0.5f;
        }

        Wall(
            parent,
            name,
            new Vector3(centerX, WallHeight * 0.5f, centerZ),
            new Vector3(width, WallHeight, depth),
            material);
    }

    // ---------------------------------------------------------------------
    // Zonas
    // ---------------------------------------------------------------------

    private static void BuildZones(Transform parent)
    {
        Transform initialSpawn = CreateMarker(parent, "Initial_Spawn", InitialCenter);
        Transform exitPoint = CreateMarker(parent, "Exit_Point", ExitCenter);
        Transform huntingEntry = CreateMarker(parent, "HuntingGround_Entry", new Vector3(-10.5f, 0f, 0f));
        Transform safeAEntry = CreateMarker(parent, "SafeZone_A_Entry", new Vector3(SafeACenter.x, 0f, 6f));
        Transform safeBEntry = CreateMarker(parent, "SafeZone_B_Entry", new Vector3(SafeBCenter.x, 0f, -4.5f));

        CreateZone(
            parent,
            "Zone_Initial",
            ZoneKind.Initial,
            new Vector3(InitialCenter.x, 1.5f, 0f),
            new Vector3(7.5f, 3f, 9f),
            new Color(0.45f, 0.72f, 0.50f),
            GameLayers.Index(GameLayers.InitialZone),
            initialSpawn);

        CreateZone(
            parent,
            "Zone_HuntingGround",
            ZoneKind.HuntingGround,
            new Vector3(0f, 1.5f, 0f),
            new Vector3(21f, 3f, 15f),
            new Color(0.88f, 0.68f, 0.30f),
            GameLayers.Index(GameLayers.HuntingGround),
            huntingEntry);

        CreateZone(
            parent,
            "Zone_Safe_A",
            ZoneKind.Safe,
            new Vector3(SafeACenter.x, 1.5f, SafeACenter.z),
            new Vector3(7.5f, 3f, 7.5f),
            new Color(0.72f, 0.55f, 0.85f),
            GameLayers.Index(GameLayers.SafeZone),
            safeAEntry);

        CreateZone(
            parent,
            "Zone_Safe_B",
            ZoneKind.Safe,
            new Vector3(SafeBCenter.x, 1.5f, SafeBCenter.z),
            new Vector3(7.5f, 3f, 7.5f),
            new Color(0.72f, 0.55f, 0.85f),
            GameLayers.Index(GameLayers.SafeZone),
            safeBEntry);

        CreateZone(
            parent,
            "Zone_Exit",
            ZoneKind.Exit,
            new Vector3(ExitCenter.x, 1.5f, 0f),
            new Vector3(7.5f, 3f, 9f),
            new Color(0.40f, 0.65f, 0.85f),
            GameLayers.Index(GameLayers.ExitZone),
            exitPoint);

        CreateZone(
            parent,
            "Zone_NorthRoom",
            ZoneKind.Room,
            new Vector3(0f, 1.5f, 13.5f),
            new Vector3(21f, 3f, 6f),
            new Color(0.72f, 0.72f, 0.72f),
            0);

        CreateZone(
            parent,
            "Zone_SouthRoom",
            ZoneKind.Room,
            new Vector3(0f, 1.5f, -13.5f),
            new Vector3(21f, 3f, 6f),
            new Color(0.72f, 0.72f, 0.72f),
            0);
    }

    // ---------------------------------------------------------------------
    // Agentes
    // ---------------------------------------------------------------------

    private static GameObject BuildHunter(Transform parent, Material material)
    {
        GameObject hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hunter.name = "Hunter";
        hunter.layer = GameLayers.Index(GameLayers.Hunter);
        hunter.transform.SetParent(parent, false);
        hunter.transform.position = new Vector3(InitialCenter.x, 1f, 0f);
        hunter.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Renderer renderer = hunter.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        Rigidbody body = hunter.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        HunterAgent agent = hunter.AddComponent<HunterAgent>();
        GameObject interestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InterestPrefabPath);

        SerializedObject serialized = new(agent);
        SetEnum(serialized, "_mode", (int)HunterAgent.Mode.Parcial2);
        SetInt(serialized, "_boidLayer", GameLayers.Mask(GameLayers.Boid));
        SetInt(serialized, "_wolfLayer", GameLayers.Mask(GameLayers.Wolf));
        SetInt(serialized, "_obstacleMask", GameLayers.Mask(GameLayers.Obstacle));
        SetFloat(serialized, "_speed", 5f);
        SetFloat(serialized, "_tba", 2f);
        SetFloat(serialized, "_rangeAttackRadius", 6f);
        SetFloat(serialized, "_meleeAttackRadius", 1.5f);
        SetFloat(serialized, "_perceptionRadius", 10f);
        SetFloat(serialized, "_wolfDetectionRadius", 5f);
        SetFloat(serialized, "_meleeDamage", 100f);
        SetFloat(serialized, "_rangeDamage", 50f);
        SetFloat(serialized, "_detectionInterval", 0.15f);
        SetInt(serialized, "_requiredGathers", 3);
        SetFloat(serialized, "_respawnTime", 5f);
        SetFloat(serialized, "_safeZoneWaitTime", 3f);
        SetFloat(serialized, "_spawnProtectionTime", 2f);
        SetFloat(serialized, "_fleeCommitTime", 3f);
        SetFloat(serialized, "_fleeSpeedMultiplier", 1.3f);
        SetFloat(serialized, "_panicRadius", 1.2f);
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
            new(-6f, 1f, -4f),
            new(-2f, 1f, 4f),
            new(2f, 1f, -3f),
            new(6f, 1f, 3f),
            new(0f, 1f, 0f),
            new(-7f, 1f, 2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject boid = (GameObject)PrefabUtility.InstantiatePrefab(boidPrefab, boidsParent.transform);
            boid.name = "Boid_" + (i + 1);
            boid.transform.position = positions[i];
            boid.transform.rotation = Quaternion.identity;
        }
    }

    private static WolfAgent[] BuildWolves(Transform parent, Material material, NavGraph navGraph)
    {
        GameObject wolvesParent = CreateChild("Wolves", parent);

        // L1 anillo oeste, L2 anillo norte + sala norte, L3 anillo este,
        // L4 anillo sur + sala sur. Ninguna ruta entra a las zonas seguras.
        Vector3[] positions =
        {
            new(-22.5f, 1f, 15f),
            new(0f, 1f, 19.5f),
            new(22.5f, 1f, 0f),
            new(0f, 1f, -19.5f)
        };

        WolfAgent[] wolves = new WolfAgent[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject wolf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wolf.name = "Wolf_" + (i + 1);
            wolf.layer = GameLayers.Index(GameLayers.Wolf);
            wolf.transform.SetParent(wolvesParent.transform, false);
            wolf.transform.position = positions[i];
            wolf.transform.localScale = new Vector3(0.8f, 0.9f, 1.2f);

            Renderer renderer = wolf.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            Rigidbody body = wolf.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            WolfAgent agent = wolf.AddComponent<WolfAgent>();
            SerializedObject serialized = new(agent);
            SetInt(serialized, "_obstacleMask", GameLayers.Mask(GameLayers.Obstacle));
            SetObject(serialized, "_navGraph", navGraph);
            SetFloat(serialized, "_patrolSpeed", 3.2f);
            SetFloat(serialized, "_alertSpeed", 4f);
            SetFloat(serialized, "_chaseSpeed", 4.2f);
            SetFloat(serialized, "_viewRadius", 10f);
            SetFloat(serialized, "_fovAngle", 120f);
            SetFloat(serialized, "_captureRadius", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(agent);
            wolves[i] = agent;
        }

        return wolves;
    }

    // ---------------------------------------------------------------------
    // Rutas
    // ---------------------------------------------------------------------

    private static void BuildRoutes(Transform parent, GameObject hunterObject, WolfAgent[] wolves)
    {
        // El Cazador patrulla el Coto en busca de boids; la salida la resuelve
        // el estado Exit cuando completa las recolecciones.
        PatrolRoute hunterRoute = CreateRoute(
            parent,
            "Hunter Route",
            PatrolRoute.RouteMode.PingPong,
            new Vector3(-6f, 1f, 0f),
            new Vector3(-6f, 1f, 5.5f),
            new Vector3(0f, 1f, 5.5f),
            new Vector3(6f, 1f, 5.5f),
            new Vector3(6f, 1f, 0f),
            new Vector3(6f, 1f, -5.5f),
            new Vector3(0f, 1f, -5.5f),
            new Vector3(-6f, 1f, -5.5f));

        if (hunterObject != null)
        {
            HunterAgent hunter = hunterObject.GetComponent<HunterAgent>();
            SerializedObject serialized = new(hunter);
            SetObject(serialized, "_patrolRoute", hunterRoute);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        PatrolRoute[] wolfRoutes =
        {
            // L1: anillo oeste, ida y vuelta.
            CreateRoute(
                parent,
                "Wolf Route L1",
                PatrolRoute.RouteMode.PingPong,
                new Vector3(-22.5f, 1f, 15f),
                new Vector3(-22.5f, 1f, 0f),
                new Vector3(-22.5f, 1f, -15f)),

            // L2: anillo norte + sala norte, entrando por los dos accesos.
            CreateRoute(
                parent,
                "Wolf Route L2",
                PatrolRoute.RouteMode.PingPong,
                new Vector3(-6f, 1f, 19.5f),
                new Vector3(-6f, 1f, 13.5f),
                new Vector3(0f, 1f, 13.5f),
                new Vector3(6f, 1f, 13.5f),
                new Vector3(6f, 1f, 19.5f)),

            // L3: anillo este, ida y vuelta.
            CreateRoute(
                parent,
                "Wolf Route L3",
                PatrolRoute.RouteMode.PingPong,
                new Vector3(22.5f, 1f, 15f),
                new Vector3(22.5f, 1f, 0f),
                new Vector3(22.5f, 1f, -15f)),

            // L4: anillo sur + sala sur, entrando por los dos accesos.
            CreateRoute(
                parent,
                "Wolf Route L4",
                PatrolRoute.RouteMode.PingPong,
                new Vector3(-6f, 1f, -19.5f),
                new Vector3(-6f, 1f, -13.5f),
                new Vector3(0f, 1f, -13.5f),
                new Vector3(6f, 1f, -13.5f),
                new Vector3(6f, 1f, -19.5f))
        };

        if (wolves == null) return;

        for (int i = 0; i < wolves.Length && i < wolfRoutes.Length; i++)
        {
            WolfAgent wolf = wolves[i];
            if (wolf == null) continue;

            SerializedObject serialized = new(wolf);
            SetObject(serialized, "_patrolRoute", wolfRoutes[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wolf);
        }
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

    // ---------------------------------------------------------------------
    // Sistemas
    // ---------------------------------------------------------------------

    private static NavGraph BuildSystems(Transform parent)
    {
        GameObject boundsObject = CreateChild("Bounds", parent);
        WorldBounds bounds = boundsObject.AddComponent<WorldBounds>();
        bounds.Configure(20f, 14f, false, true);
        EditorUtility.SetDirty(bounds);

        GameObject navObject = CreateChild("Nav Graph", parent);
        NavGraph navGraph = navObject.AddComponent<NavGraph>();
        navGraph.Configure(
            Vector3.zero,
            new Vector2(72f, 45f),
            1f,
            0.45f,
            1f,
            GameLayers.Mask(GameLayers.Obstacle),
            new Vector3(InitialCenter.x, 1f, 0f),
            true);
        EditorUtility.SetDirty(navGraph);

        GameObject trackerObject = CreateChild("Interest Object Tracker", parent);
        trackerObject.AddComponent<InterestObjectTracker>();

        GameObject feedbackObject = CreateChild("Feedback", parent);
        feedbackObject.AddComponent<FeedbackManager>();
        feedbackObject.AddComponent<FeedbackHUD>();

        return navGraph;
    }

    private static void BuildCamera(Transform parent)
    {
        GameObject cameraObject = CreateChild("Main Camera", parent);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 40f, 0f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 24f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 200f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.10f, 0.13f);

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

    // ---------------------------------------------------------------------
    // Helpers de construccion
    // ---------------------------------------------------------------------

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
        WolfAgent[] wolves = Object.FindObjectsByType<WolfAgent>(FindObjectsSortMode.None);
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

        NavGraph navGraph = Object.FindFirstObjectByType<NavGraph>();
        int navNodes = 0;
        int safeNodes = 0;
        int wolfBlockedNodes = 0;

        if (navGraph != null)
        {
            navGraph.Bake();
            navNodes = navGraph.NodeCount;
            safeNodes = navGraph.SafeNodeCount;
            wolfBlockedNodes = navGraph.WolfBlockedNodeCount;
        }

        Debug.Log("[Parcial 2] Boids: " + boids.Length + " | Hunter: " + (hunter != null) +
            " | Lobos: " + wolves.Length +
            " | Zonas: " + zones.Length + " (seguras: " + safeZones + ")" +
            " | Rutas: " + routes.Length + " | Bounds: " + (bounds != null) +
            " | Nav: " + navNodes + " nodos (" + safeNodes + " seguros, " +
            wolfBlockedNodes + " bloqueados para lobos)" +
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

    private static void SetEnum(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.enumValueIndex = value;
    }
}
