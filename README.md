# Parcial 1 — IA en Videojuegos

Simulación de un cardumen de **boids autónomos** y un **cazador con máquina de estados finitos (FSM)**. Cada boid decide por su cuenta usando percepción limitada y steering (Separation, Alignment, Cohesion, Arrive y Evade). El cazador patrulla por waypoints, genera cebos, ataca con TBA y recolecta cadáveres.

El proyecto está pensado para que cada requisito de la consigna sea visible en pantalla: colores por estado, etiquetas 3D, barras de vida/TBA, log de eventos y gizmos de radios.

## Requisitos

- Unity **6000.3.17f1** (URP).
- Escena principal: `Assets/Scenes/SampleScene.unity`.
- No requiere paquetes adicionales.

## Cómo ejecutar

1. Abrir el proyecto en Unity Hub.
2. Abrir `Assets/Scenes/SampleScene.unity`.
3. Presionar **Play**.
4. Opcional, antes de Play: `Tools > Parcial 1 > Organize Scene`.
   - Organiza la jerarquía (`--- ENVIRONMENT ---`, `--- AGENTS ---`, `--- PATROL ROUTES ---`, `--- SYSTEMS ---`).
   - Crea `Feedback` con `FeedbackManager` + `FeedbackHUD`.
   - Ajusta los parámetros del cazador y del prefab de boids.
5. Opcional: `Tools > Parcial 1 > Validate Scene` para revisar boids, ruta, tracker, feedback y parámetros.

## Controles

| Tecla | Acción |
|---|---|
| `F1` | Mostrar/ocultar el HUD |
| `F2` | Mostrar/ocultar etiquetas 3D |
| `F3` | Mostrar/ocultar gizmos de radios |

Los agentes son autónomos: no hay control manual ni líder. La simulación se observa.

## Qué se ve en pantalla

- **Boids**
  - Verde: Flocking.
  - Cian: Seeking (Arrive a un cebo).
  - Amarillo: Interacting (dañando un cebo).
  - Rojo: Evading (huye del cazador).
  - Gris + cruz roja: muerto.
  - Azul intermitente: respawn.
  - Barra de vida sobre cada boid.
- **Cazador**
  - Azul: Patrol.
  - Rojo: Attack.
  - Amarillo: Gather.
  - Barra de TBA y progreso de recolección en el HUD.
- **Cebos**
  - Color de verde (sano) a rojo (a punto de destruirse).
  - Etiqueta con vida restante.
  - Al destruirse: texto flotante y contador.
- **HUD**
  - Estado del cazador, objetivo, TBA, boids totales/detectados, cebos activos, kills, recolecciones, respawns y cebos destruidos.
  - Log de eventos.
  - Lista de boids con estado y vida.
  - Leyenda de colores.

## Parámetros ajustables

### Prefab `Assets/Prefabs/Boid.prefab` (`BoidAgent`)

| Campo | Valor actual | Uso |
|---|---|---|
| `_maxSpeed` | 5 | Velocidad máxima |
| `_maxForce` | 10 | Fuerza máxima de steering |
| `_viewRadius` | 6 | Radio de percepción de vecinos |
| `_separationRadius` | 3 | Radio de separación (menor que vista) |
| `_separationWeight` | 1.5 | Peso de Separation |
| `_alignmentWeight` | 0.8 | Peso de Alignment |
| `_cohesionWeight` | 0.6 | Peso de Cohesion |
| `_evadeWeight` | 1.5 | Peso de Evade |
| `_arriveWeight` | 1 | Peso de Arrive |
| `_hunterDetectionRadius` | 10 | Radio para detectar al cazador |
| `_interestDetectionRadius` | 10 | Radio para detectar cebos |
| `_interactRadius` | 1 | Distancia para dañar un cebo |
| `_maxHealth` | 100 | Vida |
| `_respawnDelay` | 3 | Segundos hasta reaparecer tras ser recolectado |
| `_detectionInterval` | 0.15 | Intervalo de detección |

### Cazador (`HunterAgent` en escena)

| Campo | Valor actual | Uso |
|---|---|---|
| `_speed` | 6.5 | Velocidad de movimiento |
| `_tba` | 2 | Tiempo entre ataques |
| `_perceptionRadius` | 9 | Radio de detección de boids |
| `_rangeAttackRadius` | 6 | Radio de ataque a distancia |
| `_meleeAttackRadius` | 1.5 | Radio de ataque melee |
| `_meleeDamage` | 100 | Daño melee |
| `_rangeDamage` | 50 | Daño a distancia |
| `_interestSpawnInterval` | 4 | Intervalo de spawn de cebos |
| `_maxInterestObjects` | 5 | Máximo de cebos activos |
| `_detectionInterval` | 0.15 | Intervalo de percepción |

## Arquitectura resumida

- `BoidAgent` coordina, pero delega:
  - `BoidSensor`: percepción limitada con una `OverlapSphereNonAlloc` por tick.
  - `BoidBehaviorController`: prioridad Evade > Interacting > Seeking > Flocking.
  - `BoidMotor`: aceleración, límite de velocidad, orientación y wrap de bordes.
  - `BoidHealth`: vida, muerte, recolección y respawn.
  - `BoidInteraction`: daño por intervalo a cebos.
  - `BoidFeedback`: color por estado con `MaterialPropertyBlock`.
- `HunterAgent` solo configura y actualiza la FSM:
  - `HunterSensor`: percepción cacheada de boids vivos y cadáveres.
  - `HunterMotor`: movimiento y velocidad expuesta para Evade.
  - `HunterCombat`: TBA, daño y eventos.
  - `HunterInterestSpawner`: spawn de cebos fuera de los estados.
  - Estados `Patrol`, `Attack` y `Gather`: cada uno decide sus transiciones.
- `FeedbackManager` centraliza contadores, log y textos flotantes.
- `FeedbackHUD` es el único `OnGUI` del proyecto.

Ver `ARQUITECTURA.md` para el diagrama y el detalle de responsabilidades.

## Documentación

- `REQUISITOS.md`: tabla consigna → implementación → evidencia visual.
- `ARQUITECTURA.md`: diagrama de clases, FSM y flujo.
- `QA_CHECKLIST.md`: verificación estática y checklist de playtest.
- `PLAN_MEJORAS.md`: plan de trabajo original.

## Estado de la entrega

- Compilación estática de runtime y editor: **sin errores**.
- QA de runtime, profiler y build standalone: **pendiente de ejecutar en el editor** (ver `QA_CHECKLIST.md`).
- La escena puede ejecutarse sin el objeto `Feedback`: `FeedbackManager` se auto-crea al iniciar.
