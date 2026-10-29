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

## Parcial 2 — escenario, lobos y FSM ampliada

### Escenario y zonas

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Mapa con zonas | `Parcial2SceneBuilder` + `ZoneVolume` | Pisos de color, leyenda del HUD y gizmos |
| Zona Inicial | `Zone_Initial` con `ZoneKind.Initial` | Piso y etiqueta; spawn y respawn del Cazador |
| Coto de caza | `Zone_HuntingGround` + `WorldBounds` | 6 boids confinados en el Coto central (14 x 10 tiles), abierto a izquierda y derecha |
| Al menos 2 zonas seguras | `Zone_Safe_A` y `Zone_Safe_B` en callejones sin salida al noreste y suroeste | Pisos violetas y etiqueta `ZONA SEGURA` |
| Salida | `Zone_Exit` con `ZoneKind.Exit` | Piso y meta; el Cazador escapa al entrar |
| Identificación visual | Materiales URP por zona + leyenda | Colores distintos y log de zona elegida |

### Lobos

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Mínimo 4 lobos | 4 instancias de `WolfAgent` | HUD y etiquetas 3D |
| Patrulla por waypoints | `WolfPatrolState` + `PatrolRoute` | Gizmo de ruta y estado `Patrol` |
| Más lentos que el Cazador | `_patrolSpeed` 3.2, `_alertSpeed` 4, `_chaseSpeed` 4.2 vs. 5 (6.5 al huir) | El Cazador puede escapar |
| Percepción con FOV y LOS | `WolfSensor` con `_viewRadius` 10, `_fovAngle` 120, `_obstacleMask` | Gizmo del cono y cambio a `Chase` |
| Aullido/alerta compartida | `WolfAlertBus` con cooldown de 2 s | Log `ALERTA` y HUD |
| A* a la última posición conocida | `WolfAlertState` + `NavAgent` | Gizmo del camino y punto rojo de última posición |
| Persecución y captura | `WolfChaseState` + `CanCapture` | Estado `Chase`, texto `CAPTURADO` |
| Prohibido entrar a zonas seguras | Nodos `WolfBlocked` + guarda en `WolfMotor` + `WolfBlocker` | 0 muestras en el playtest |

### Navegación

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Grafo propio | `NavGraph` + `NavGraphBaker` (grilla + raycasts) | Gizmos de nodos y aristas |
| A* propio | `AStarPathfinder` con heap binario y heurística euclidiana | Camino dibujado en gizmos |
| Sin NavMesh ni rutas predefinidas | `NavAgent` sigue el camino de A* | El movimiento cambia al mover obstáculos |
| Steering directo solo al final | `NavAgent` usa el objetivo directo dentro de `_directRange` con LOS | Tramo final recto hacia el objetivo |

### FSM ampliada del Cazador

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Seis estados | `HunterAgent.States`: `Search`, `Attack`, `Gather`, `Flee`, `Exit`, `Dead` | HUD con el estado actual |
| Prioridad obligatoria | `HunterStateBase.CheckPriorityTransitions` en orden `Dead > Flee > Exit > Gather > Attack > Search` | Log de transiciones y HUD |
| Transiciones dentro de los estados | Cada estado llama a `_fsm.ChangeState` | No hay un `switch` central |
| `RequiredGathers = 3` | `HunterAgent._requiredGathers` | Barra de recolecciones `N/3` |
| `RespawnTime = 5` | `HunterDeadState` | Cuenta regresiva y respawn en la zona inicial |
| `SafeZoneWaitTime` | `HunterFleeState` | El Cazador espera en la zona segura y luego sale |
| Huida a zona segura | `HunterFleeState` elige la más cercana | Log `CAZADOR: huye a Zone_Safe_*` |
| Fin de partida | `HunterExitState` + panel `PARTIDA COMPLETADA` | Panel con tiempo, recolecciones y capturas; `R` reinicia |

### Feedback del Parcial 2

| Requisito | Implementación | Evidencia visual |
|---|---|---|
| Detecciones y alertas | `FeedbackManager`, `WolfAgent.LastAction`, `WolfAlertBus` | Log, etiquetas y textos flotantes |
| Última posición conocida | `WolfAgent.LastKnownHunterPosition` | Punto rojo y línea en gizmos |
| Caminos A* | `NavAgent.Path` + `OnDrawGizmos` | Línea amarilla del camino activo y `A*: N nodos` en el HUD |
| Estado y recolecciones | `FeedbackHUD.DrawHunterPanel` | Estado, TBA, recolecciones, capturas y respawns |
| Zona segura elegida | `HunterAgent.ChosenSafeZone` | Log y etiqueta de la zona |
| Gizmos de radios y FOV | `BoidAgent`, `HunterAgent`, `WolfAgent` | F3 alterna los gizmos |
| Fin de partida | `FeedbackHUD.DrawEndPanel` | Panel central con resultado y tecla `R` |

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
