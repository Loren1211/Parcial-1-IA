# Requisitos de la consigna y evidencia

Tabla de trazabilidad entre cada requisito, la clase que lo implementa y la evidencia visible en la simulación.

## Boids

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Mínimo 6 agentes | 6 instancias de `Assets/Prefabs/Boid.prefab` en `SampleScene` | HUD: `Boids: 6` y lista de boids |
| Agentes autónomos, sin líder ni controlador global | Un `BoidAgent` por boid; no existe un manager que mueva al cardumen | Cada boid tiene estado, vida y etiqueta propios |
| Percepción limitada | `BoidSensor` con `_viewRadius`, `_hunterDetectionRadius`, `_interestDetectionRadius` y `_detectionInterval` | Gizmos de radios (F3) y cambio de estado por proximidad |
| Separation | `SteeringCalculator.Separation` con peso `_separationWeight` | Gizmo rojo; los boids no se superponen |
| Alignment | `SteeringCalculator.Alignment` con peso `_alignmentWeight` | Estado `Flocking` (verde) con movimiento coordinado |
| Cohesion | `SteeringCalculator.Cohesion` escalada por distancia media | Cardumen que se mantiene unido sin zig-zag |
| Separation con radio menor | `_separationRadius = 3` < `_viewRadius = 6` | Gizmos comparables (F3) |
| Evade con prioridad | `BoidBehaviorController` evalúa al cazador antes que cualquier otro comportamiento | Boid rojo + etiqueta `Evading` |
| Evitar superposición | Separation + fallback al vecino más cercano si las fuerzas se cancelan | Los boids se repelen incluso en simetrías |
| Boids usan Arrive | `SteeringCalculator.Arrive` hacia el cebo | Boid cian + etiqueta `Seeking` |
| Interacción con daño por intervalo | `BoidInteraction` llama a `InterestObject.TakeDamageTick` | Cebo cambia de verde a rojo y muestra vida |
| Vida 0 → inactivo | `BoidHealth.Die()`; el cadáver queda para recolección | Boid gris + cruz roja + etiqueta `MUERTO` |
| Recolectado → desaparece y respawnea | `BoidHealth.Collect()` oculta renderer/collider y `Tick()` respawnea tras `_respawnDelay` | Etiqueta `RECOLECTADO` con cuenta regresiva, luego flash azul y contador de respawn |
| Un boid que destruye un cebo sigue vivo | `InterestObject.DestroyInterest()` no daña ni desactiva al boid | El boid vuelve a `Flocking` y sigue en escena |

## Cazador y FSM

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| FSM obligatoria | `FiniteStateMachine<HunterAgent.States>` con `Patrol`, `Attack`, `Gather` | HUD: estado actual y color del cazador |
| Transiciones dentro de la FSM | Cada estado llama a `_fsm.ChangeState(...)`; `HunterAgent` no decide transiciones | Log `CAZADOR: Patrol -> Attack` |
| TBA | `HunterCombat` con `_tba`, `CanAttack` y `TbaRemaining` | Barra de TBA en el HUD (verde listo / naranja en espera) |
| RangeAttackRadius | `HunterCombat.TryRangeAttack` + `_rangeAttackRadius` | Gizmo naranja |
| MeleeAttackRadius | `HunterCombat.TryMeleeAttack` + `_meleeAttackRadius` | Gizmo rojo |
| Patrol con waypoints | `PatrolRoute` auto-colecciona hijos `Waypoint_*` | Gizmo amarillo de ruta y waypoints |
| Fin de ruta: volver o invertir | `PatrolRoute.RouteMode.PingPong` o `Loop` | Movimiento visible del cazador |
| Spawn de cebos cada intervalo, máx. 5 | `HunterInterestSpawner` + `InterestObjectTracker` | HUD: `Cebos activos: N/5` y texto `+ CEBO` |
| Attack solo con TBA y boid en percepción | `HunterPatrolState` verifica `CanAttack()` y `Sensor.HasActiveBoids` | El cazador no cambia a Attack con la barra de TBA en espera |
| Melee si está en radio | `HunterAttackState` usa `MeleeAttackRadius` | Texto flotante `MELEE` y daño alto |
| Rango si está en radio | `HunterAttackState` usa `RangeAttackRadius` | Texto flotante `RANGE` |
| Perseguir si fuera de rango | `HunterAttackState` llama a `MoveTowards` del objetivo | El cazador reduce distancia antes de atacar |
| Ataque exitoso → reset TBA | `HunterCombat` reinicia el timer en cada ataque | La barra de TBA vuelve a cero |
| Ataque exitoso → salir de Attack | Si el objetivo muere, `HunterAttackState` pasa a `Gather`; si sigue vivo, continúa la persecución | Transición visible a `Gather` y log |
| Objetivo fuera de visión → Patrol sin reset TBA | `HunterAttackState.StillInPerception()` cambia a Patrol sin tocar `HunterCombat` | Log de transición y barra de TBA conservada |
| Línea/tracer de ataque | `FeedbackHUD.DrawHunterTargetHelpers` y `FeedbackManager.SpawnTracer` | Línea roja al objetivo y tracer `MELEE`/`RANGE` |
| Gather al detectar cadáver | `HunterPatrolState` prioriza `Sensor.HasInactiveBoids` | Cazador amarillo + etiqueta `Gather` |
| Recolección con tiempo | `HunterGatherState` acumula 1.5 s dentro de `StopRadius` | Barra `RECOLECTANDO N%` en el HUD y sobre el cadáver |
| Cancelar Gather si el objetivo no está | `HunterGatherState.Update` vuelve a Patrol y registra el evento | Log `GATHER CANCELADO` |

## Feedback y performance

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Feedback visual y funcional | `FeedbackManager`, `FeedbackHUD`, `BoidFeedback`, `HunterFeedback`, textos flotantes | Leyenda, log, etiquetas, colores, barras y contadores |
| Performance: detección periódica | `BoidSensor` y `HunterSensor` usan `OverlapSphereNonAlloc` cada `_detectionInterval` | Profiler: sin allocs por frame en detección |
| Sin `OnGUI` dispersos | Un único `FeedbackHUD.OnGUI` | Un solo HUD central |
| Colores sin instanciar materiales | `MaterialPropertyBlock` en boids, cazador y cebos | Cambios de color sin aumentar draw calls por material |

## Notas de diseño

- **Persecución continua:** el ataque exitoso resetea el TBA, pero el cazador no abandona `Attack` mientras el objetivo siga vivo y dentro de percepción. Esto evita el comportamiento errático de entrar y salir de Attack entre ataques. Si el objetivo muere, pasa a `Gather`.
- **Gather solo desde Patrol:** se eligió que un ataque en curso no se interrumpa por un cadáver. Al terminar el ataque (o al morir el objetivo), el cazador entra a `Gather`.
- **Cohesión escalada:** se activa gradualmente cuando el grupo supera el radio de separación, para evitar el zig-zag por fuerzas opuestas.
