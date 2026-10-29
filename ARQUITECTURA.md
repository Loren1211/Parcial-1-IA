# Arquitectura

El proyecto separa **decisión**, **percepción**, **movimiento**, **vida** y **presentación**. Los `MonoBehaviour` son puntos de entrada delgados; la lógica vive en clases C# planas que se componen desde ellos.

## Diagrama de clases

```mermaid
classDiagram
    class BoidAgent {
        +BoidState State
        +Vector3 Velocity
        +bool IsActive
        +bool IsDead
        +bool IsCollected
        +float Health
        +float RespawnTimeRemaining
        +TakeDamage(float)
        +Die()
        +Collect()
        +OnRespawned()
    }

    class BoidSensor {
        +Neighbors
        +SeparationNeighbors
        +NearestHunter
        +NearestInterest
        +Tick()
    }

    class BoidBehaviorController {
        +BoidState State
        +Vector3 Steering
        +Evaluate()
    }

    class BoidMotor {
        +Vector3 Velocity
        +Apply(Vector3, float)
        +SetVelocity(Vector3)
        +Stop()
    }

    class BoidHealth {
        +float CurrentHealth
        +bool IsDead
        +bool IsCollected
        +TakeDamage(float)
        +Die()
        +Collect()
        +Tick(float)
    }

    class BoidInteraction {
        +InterestObject CurrentTarget
        +SetTarget(InterestObject)
        +Cancel()
        +Tick(float)
    }

    class BoidFeedback {
        +Tick(...)
    }

    class SteeringCalculator {
        <<static>>
        +Seek()
        +Flee()
        +Evade()
        +Arrive()
        +Separation()
        +Alignment()
        +Cohesion()
    }

    BoidAgent *-- BoidSensor
    BoidAgent *-- BoidBehaviorController
    BoidAgent *-- BoidMotor
    BoidAgent *-- BoidHealth
    BoidAgent *-- BoidInteraction
    BoidAgent *-- BoidFeedback
    BoidBehaviorController --> SteeringCalculator
    BoidBehaviorController --> BoidSensor
    BoidBehaviorController --> BoidInteraction
```

```mermaid
classDiagram
    class HunterAgent {
        +HunterSensor Sensor
        +HunterMotor Motor
        +HunterCombat Combat
        +HunterInterestSpawner InterestSpawner
        +HunterFeedback Feedback
        +FiniteStateMachine~States~ FSM
        +float TbaRemaining
        +float GatherProgress
        +Update()
    }

    class HunterSensor {
        +ActiveBoids
        +InactiveBoids
        +NearestActiveBoid
        +NearestInactiveBoid
        +Tick()
    }

    class HunterMotor {
        +Vector3 Velocity
        +MoveTowards(Vector3, float)
        +FaceDirection(Vector3)
        +Tick(float)
        +Stop()
    }

    class HunterCombat {
        +float TbaRemaining
        +bool CanAttack
        +TryMeleeAttack(BoidAgent)
        +TryRangeAttack(BoidAgent)
        +Tick(float)
        +event Attacked
        +event Killed
    }

    class HunterInterestSpawner {
        +BeginPatrolSpawning()
        +StopSpawning()
        +Tick(float)
    }

    class FiniteStateMachine~T~ {
        +T CurrentStateID
        +ChangeState(T)
        +Update()
        +event OnStateChanged
    }

    class State~T~ {
        <<abstract>>
        +Enter()
        +Update()
        +Exit()
    }

    class HunterStateBase {
        <<abstract>>
        #HunterAgent Hunter
        #MoveTowards(Vector3)
    }

    class HunterPatrolState
    class HunterAttackState
    class HunterGatherState

    HunterAgent *-- HunterSensor
    HunterAgent *-- HunterMotor
    HunterAgent *-- HunterCombat
    HunterAgent *-- HunterInterestSpawner
    HunterAgent *-- HunterFeedback
    HunterAgent *-- FiniteStateMachine~T~
    FiniteStateMachine~T~ o-- State~T~
    State~T~ <|-- HunterStateBase
    HunterStateBase <|-- HunterPatrolState
    HunterStateBase <|-- HunterAttackState
    HunterStateBase <|-- HunterGatherState
```

```mermaid
classDiagram
    class FeedbackManager {
        +static Instance
        +int Kills
        +int Gathers
        +int BaitsSpawned
        +int BaitsDestroyed
        +int Respawns
        +AddLog(string)
        +SpawnFloatingText(...)
        +SpawnTracer(...)
        +NotifyAttack(...)
        +NotifyKill(...)
        +NotifyGather(...)
        +NotifyInterestSpawned(...)
        +NotifyInterestDestroyed(...)
        +NotifyRespawn(...)
    }

    class FeedbackHUD {
        +OnGUI()
        +HandleInput()
    }

    class InterestObjectTracker {
        +int ActiveCount
        +Register(InterestObject)
        +Unregister(InterestObject)
    }

    class InterestObject {
        +float Health
        +float MaxHealth
        +TakeDamageTick(float)
    }

    FeedbackHUD --> FeedbackManager
    FeedbackHUD --> BoidAgent
    FeedbackHUD --> HunterAgent
    FeedbackHUD --> InterestObject
    HunterInterestSpawner --> FeedbackManager
    HunterCombat --> FeedbackManager
    BoidAgent --> FeedbackManager
    InterestObject --> FeedbackManager
    InterestObject --> InterestObjectTracker
    HunterInterestSpawner --> InterestObjectTracker
```

## FSM del cazador

```mermaid
stateDiagram-v2
    [*] --> Patrol

    Patrol --> Attack: TBA listo + boid activo en percepción
    Patrol --> Gather: cadáver en percepción
    Attack --> Patrol: objetivo nulo o fuera de percepción
    Attack --> Gather: objetivo muerto
    Attack --> Attack: ataque exitoso (resetea TBA)
    Gather --> Patrol: recolectó el cadáver
    Gather --> Patrol: objetivo inválido / respawneó / cancelado
```

Reglas:

- `Attack` solo se alcanza desde `Patrol` y solo si `HunterCombat.CanAttack` es verdadero.
- Salir de `Attack` por perder visión **no** resetea el TBA.
- Un ataque exitoso **sí** resetea el TBA.
- Las transiciones se ejecutan únicamente dentro de los estados, llamando a `_fsm.ChangeState(...)`.

## Flujo por frame

### Boid

```mermaid
flowchart TD
    A[Update] --> B[BoidHealth.Tick]
    B --> C{¿Activo?}
    C -- No --> D[Motor.Stop + feedback de muerte/recolectado]
    C -- Sí --> E[BoidSensor.Tick]
    E --> F[BoidBehaviorController.Evaluate]
    F --> G{NearestHunter?}
    G -- Sí --> H[Evading: Evade + Separation]
    G -- No --> I{¿Cebo en radio de interacción?}
    I -- Sí --> J[Interacting: frenar + Separation]
    I -- No --> K{¿Cebo detectado?}
    K -- Sí --> L[Seeking: Arrive + Separation]
    K -- No --> M[Flocking: Separation + Alignment + Cohesion + Wander + Cruise]
    H --> N[BoidMotor.Apply]
    J --> N
    L --> N
    M --> N
    N --> O[BoidInteraction.Tick]
    O --> P[BoidFeedback.Tick]
```

### Cazador

```mermaid
flowchart TD
    A[Update] --> B[HunterCombat.Tick]
    B --> C[HunterSensor.Tick]
    C --> D[FSM.Update]
    D --> E{Estado actual}
    E -- Patrol --> F[PatrolRoute: mover al waypoint]
    F --> G{¿Cadáver?}
    G -- Sí --> H[ChangeState Gather]
    G -- No --> I{¿TBA listo y boid activo?}
    I -- Sí --> J[ChangeState Attack]
    E -- Attack --> K[Elegir objetivo más cercano]
    K --> L{¿Dentro de melee?}
    L -- Sí --> M[TryMeleeAttack]
    L -- No --> N{¿Dentro de rango?}
    N -- Sí --> O[TryRangeAttack]
    N -- No --> P[Perseguir]
    M --> Q{¿Murió?}
    O --> Q
    Q -- Sí --> R[ChangeState Gather]
    Q -- No --> D
    E -- Gather --> S[Ir al cadáver]
    S --> T{¿En StopRadius?}
    T -- Sí --> U[Acumular 1.5 s y Collect]
    U --> V[ChangeState Patrol]
    D --> W[HunterInterestSpawner.Tick]
    W --> X[HunterMotor.Tick]
```

## Responsabilidades

| Clase | Responsabilidad | No hace |
|---|---|---|
| `BoidAgent` | Coordinar subsistemas y exponer estado | Steering, detección ni vida |
| `BoidSensor` | Percepción limitada y cacheada | Decidir comportamiento |
| `BoidBehaviorController` | Prioridad y combinación de fuerzas | Mover transforms |
| `BoidMotor` | Integración de velocidad, orientación y wrap | Decidir destino |
| `BoidHealth` | Vida, muerte, recolección y respawn | Renderizado |
| `BoidInteraction` | Daño por intervalo al cebo | Elegir objetivo |
| `SteeringCalculator` | Matemática pura de steering | Tocar escena o `Time` |
| `HunterAgent` | Configuración, referencias y `FSM.Update()` | Movimiento, ataques o transiciones |
| `HunterSensor` | Detección de boids vivos y cadáveres | Decidir estados |
| `HunterMotor` | Movimiento, orientación y velocidad | Combate |
| `HunterCombat` | TBA, daño y eventos | Elegir objetivo |
| `HunterInterestSpawner` | Spawn de cebos y límite máximo | Patrullar |
| `HunterStateBase` | Helpers comunes de movimiento | Lógica de un estado |
| `HunterPatrolState` | Waypoints, spawn de cebos y transiciones | Mover directamente el transform |
| `HunterAttackState` | Persecución, melee/rango y salida a Gather | Resetear TBA |
| `HunterGatherState` | Acercarse, esperar y recolectar | Detectar cadáveres |
| `FeedbackManager` | Contadores, log y textos flotantes | Dibujar GUI |
| `FeedbackHUD` | Único `OnGUI`, etiquetas, barras y leyenda | Lógica de simulación |
| `InterestObject` | Vida del cebo, color y destrucción | Spawn |
| `InterestObjectTracker` | Registro y conteo de cebos activos | Spawn |

## Decisiones de implementación

- **Clases planas en lugar de más `MonoBehaviour`:** evita agregar componentes al prefab y mantiene el wiring existente.
- **Namespace global:** reduce el riesgo de romper referencias de escena y GUIDs.
- **`MaterialPropertyBlock`:** cambia colores sin instanciar materiales ni romper batching.
- **Un solo `OnGUI`:** el HUD se centraliza en `FeedbackHUD`; las teclas se leen desde `Event.current` porque el proyecto usa el Input System nuevo.
- **Auto-bootstrap del feedback:** si la escena no tiene `FeedbackManager`, se crea al cargar para que el HUD nunca falte.
- **Detección periódica:** sensores con fase aleatoria e intervalo configurable para repartir el costo.
