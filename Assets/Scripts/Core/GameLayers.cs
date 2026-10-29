using UnityEngine;

/// <summary>
/// Nombres e indices de capas compartidos por los sistemas del Parcial 2.
/// Evita repetir strings en sensores, navegacion y herramientas de editor.
/// </summary>
public static class GameLayers
{
    public const string Hunter = "Hunter";
    public const string Boid = "Boid";
    public const string Interest = "Interest";
    public const string Wolf = "Wolf";
    public const string Obstacle = "Obstacle";
    public const string Walkable = "Walkable";
    public const string SafeZone = "SafeZone";
    public const string HuntingGround = "HuntingGround";
    public const string ExitZone = "ExitZone";
    public const string InitialZone = "InitialZone";
    public const string WolfBlocker = "WolfBlocker";

    public static int Index(string layerName) => LayerMask.NameToLayer(layerName);

    public static int Mask(string layerName)
    {
        int index = Index(layerName);
        return index >= 0 ? 1 << index : 0;
    }

    public static bool Exists(string layerName) => Index(layerName) >= 0;
}
