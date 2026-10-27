using System;

/// <summary>
/// Configuración de un boid. Se arma desde los campos serializados de
/// BoidAgent para no perder los valores ya ajustados en el prefab.
/// </summary>
[Serializable]
public class BoidSettings
{
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float viewRadius = 5f;
    public float separationRadius = 2f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    public float arriveRadius = 3f;
    public float interactRadius = 1f;
    public float interestDetectionRadius = 10f;
    public float hunterDetectionRadius = 7f;
    public float detectionInterval = 0.15f;
    public float evadeWeight = 1.5f;
    public float arriveWeight = 1f;
    public float maxHealth = 100f;
    public float respawnDelay = 3f;
}
