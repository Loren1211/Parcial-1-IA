# Plan de mejoras — Parcial 1 IA (Flocking + FSM)

> Documento de trabajo para resolver las correcciones del profesor y los bugs detectados durante la revisión del código.
>
> Proyecto: `E:\Unity\Parcial 1 IA` — Unity 6000.3.17f1 — URP.
>
> **Estado:** bloques 0–8 implementados. Compilación estática sin errores. El playtest, el profiler y el build standalone quedan pendientes de ejecutar en Unity (ver `QA_CHECKLIST.md`).

---

## 0. Resumen ejecutivo

El proyecto ya cumple la mayor parte de la consigna: hay 6 boids, flocking con radios distintos, un cazador con FSM (Patrol / Attack / Gather), waypoints, generación de cebos, Arrive, interacción con daño, muerte, recolección y respawn.

Las correcciones del profesor apuntan a 5 ejes:

1. **Organización de la escena**: los waypoints están sueltos en el Hierarchy.
2. **God Scripts**: `Agent`, `HunterAgent`, `HunterPatrolState` e `InterestObject` concentran demasiadas responsabilidades.
3. **Listas `static`**: `_allAgents` (y también `_inactiveAgents` e `InterestObject._active`) deben desaparecer y reemplazarse por sensores.
4. **Feedback visual**: hoy casi todo se reduce a `OnGUI`; no se distingue muerte real de bug, no se ve la kill, el cebo muerto queda en escena, etc.
5. **Performance**: `OverlapSphere` en cada `Update`, varias veces por frame y con allocaciones.

Además encontré **dos bugs funcionales importantes** que explican parte de lo que el profesor vio como “boid bugueado”:

- El boid llama a `BecomeInactive()` cuando **destruye un cebo** (`Agent.cs`, línea 65). O sea, muere al completar la interacción. Eso hace que quede quieto y parezca un bug.
- Cuando la vida de un cebo llega a 0 solo se desactiva su collider (`InterestObject.cs`, línea 38). El renderer sigue visible, el objeto sigue en `_active` y `ActiveCount` nunca baja. Después de 5 cebos destruidos **dejan de generarse cebos nuevos** y quedan “cebos fantasma” en escena.

El plan está dividido en 9 bloques, con orden recomendado, criterios de aceptación y una tabla final de mapeo contra la consigna.

---

## 1. Diagnóstico

### 1.1 Lo que ya está bien

- 6 boids instanciados desde `Boid.prefab`.
- Flocking con Separation, Alignment y Cohesion.
- Separation usa `_separationRadius = 2` y Alignment/Cohesion usan `_viewRadius = 5`: cumple el radio menor.
- Cazador con FSM real (`FiniteStateMachine<T>` + `State<T>`), estados Patrol / Attack / Gather.
- Variables TBA, RangeAttackRadius y MeleeAttackRadius presentes.
- Patrol con waypoints y recorrido ping-pong.
- Generación de objetos de interés cada 8 s, con máximo 5.
- Arrive, interacción con daño por intervalo, muerte, recolección y respawn.
- Bounds con wrap del escenario.
- Feedback básico por `OnGUI`.

### 1.2 Correcciones del profesor (traducidas a problemas concretos)

| Corrección | Evidencia en el proyecto | Impacto |
|---|---|---|
| Waypoints sueltos en Hierarchy | `Waypoint_1..7` están en la raíz de `SampleScene` | Organización |
| God Scripts | `Agent.cs` (264 líneas), `HunterAgent.cs` (93), `HunterPatrolState.cs` (85), `InterestObject.cs` (45) | Arquitectura |
| Lista `static` de agentes | `Agent.cs` línea 28: `private static readonly List<Agent> _allAgents = new();` | Arquitectura / consigna |
| Feedback insuficiente | Solo `HunterAgent.OnGUI()`; no hay indicadores 3D, VFX, salud, muerte, kill, gather, respawn | Visual / evaluación |
| `OverlapSphere` en cada `Update` | `Agent.cs` línea 186; `HunterAgent.cs` línea 75 | Performance |
| Múltiples detecciones por frame | `HunterAgent.Update` llama a `GetBoidsInPerception()`; Patrol la vuelve a llamar; Attack también; HUD también | Performance |
| Allocaciones por frame | `GetBoidsInPerception()` crea `new List`; `GetInactiveAgentsInRange()` crea `new List`; `Physics.OverlapSphere` crea array | Performance / GC |

### 1.3 Bugs y deuda técnica extra detectados

1. **Boid muere al destruir un cebo** (`Agent.cs`, línea 65). Debe liberar el objetivo y volver a flocking, no morir.
2. **Cebo destruido no desaparece** (`InterestObject.cs`, línea 38): solo se desactiva el collider. Sigue renderizado, sigue en `_active` y bloquea el spawn de nuevos cebos.
3. **`InterestObject._active` es otra lista estática** (línea 13). No la marcó el profesor, pero conviene eliminarla por el mismo criterio.
4. **`_inactiveAgents` también es estática** (`Agent.cs`, línea 29) y el cazador la usa para Gather. Debe reemplazarse por detección física.
5. **Integración de steering con doble `deltaTime`**: `CalculateSteering()` multiplica por `Time.deltaTime` (línea 175-180) y después `Update()` vuelve a multiplicar por `Time.deltaTime` al mover (línea 80). El comportamiento depende del framerate y las fuerzas quedan amortiguadas.
6. **`CalculateFlee` no es Evade**: no predice la posición futura del cazador. La consigna pide Evade.
7. **El boid no tiene vida**: el cazador lo mata instantáneamente con melee o rango. La consigna habla de “cuando la vida del agente llegue a cero”.
8. **Respawn y spawn con bounds hardcodeados**: `Random.Range(-30f, 30f)` y `Random.Range(-17f, 17f)` en `Agent.cs` línea 240 y `HunterPatrolState.cs` línea 79, en vez de usar `Bounds`.
9. **`PatrolData.transform` duplica el transform del cazador** y la lista de waypoints se carga a mano. Es fácil desincronizar ruta y agente.
10. **`HunterAgent.Update` calcula `DetectedBoidsCount` todos los frames** con una lista nueva.
11. **`HunterPatrolState` mezcla patrullaje con spawn de cebos**.
12. **`InterestObject` mezcla vida, registro estático y destrucción**.
13. **Código muerto**: `FSMAgent.cs`, `IdleState.cs`, `PatrolState.cs` y `TutorialInfo` no se usan en la escena. Se pueden eliminar o mover a una carpeta de ejemplos.
14. **`Debug.Log` en cada cambio de estado/ataque**: ruido en consola; conviene un log visual de eventos.
15. **Encoding**: `HunterAgent.OnGUI` muestra `�ltima` y `Recolect�`; hay que corregir el texto a UTF-8.
16. **Sin documentación ni mapeo de requisitos**: para la entrega conviene un README y una tabla consigna → implementación.

---

## 2. Arquitectura objetivo

### 2.1 Estructura de carpetas propuesta

```text
Assets/Scripts/
├── Core/
│   ├── WorldBounds.cs
│   ├── PatrolRoute.cs
│   └── Detection/
│       └── PhysicsSensor.cs          (helper de OverlapSphereNonAlloc)
├── Steering/
│   └── SteeringCalculator.cs         (Seek, Flee, Evade, Arrive, Separation, Alignment, Cohesion)
├── Boids/
│   ├── BoidAgent.cs                  (coordinador)
│   ├── BoidSensor.cs                 (percepción periódica)
│   ├── BoidMotor.cs                  (velocidad, movimiento, bounds)
│   ├── BoidBehaviorController.cs     (prioridad de comportamientos)
│   ├── BoidHealth.cs                 (vida, muerte, recolección, respawn)
│   ├── BoidInteraction.cs            (daño al objeto de interés)
│   └── BoidState.cs                  (enum de estado para feedback)
├── Hunter/
│   ├── HunterAgent.cs                (config + FSM)
│   ├── HunterSensor.cs               (percepción periódica)
│   ├── HunterMotor.cs                (movimiento)
│   ├── HunterCombat.cs               (TBA, rangos, daño)
│   ├── HunterInterestSpawner.cs      (spawn de cebos)
│   └── States/
│       ├── HunterStateBase.cs
│       ├── HunterPatrolState.cs
│       ├── HunterAttackState.cs
│       └── HunterGatherState.cs
├── FSM/
│   ├── FiniteStateMachine.cs
│   └── State.cs
├── World/
│   ├── InterestObject.cs             (vida + eventos)
│   └── InterestObjectTracker.cs      (registro no estático para contar activos)
├── Feedback/
│   ├── FeedbackManager.cs            (eventos, log, floating text)
│   ├── FeedbackEvents.cs
│   ├── FeedbackHUD.cs                (HUD centralizado)
│   ├── BoidFeedback.cs
│   ├── HunterFeedback.cs
│   ├── InterestObjectFeedback.cs
│   ├── WorldLabel.cs
│   └── FloatingText.cs
└── Editor/
    └── SceneSetupTools.cs            (opcional: organizar escena por menú)
```

> No es obligatorio crear todas las clases. La idea es que cada archivo tenga **una responsabilidad clara** y que ningún MonoBehaviour supere ~150 líneas.

### 2.2 Responsabilidades

| Clase | Responsabilidad | No debe hacer |
|---|---|---|
| `BoidAgent` | Coordinar sensor, comportamiento, motor y vida | Detectar, calcular steering, dibujar GUI |
| `BoidSensor` | Detectar boids, cazador y cebos cada N segundos | Decidir comportamientos |
| `BoidBehaviorController` | Elegir Evade / Interact / Arrive / Flocking según prioridad | Mover el transform |
| `BoidMotor` | Aplicar aceleración, velocidad, rotación y bounds | Decidir a dónde ir |
| `SteeringCalculator` | Matemática pura de steering | Tocar transforms ni Time |
| `BoidHealth` | Vida, daño, muerte, recolección, respawn | Detectar ni decidir |
| `BoidInteraction` | Dañar el cebo mientras está en rango | Elegir objetivo |
| `HunterAgent` | Configuración y actualización de la FSM | Patrullar, atacar, detectar o dibujar |
| `HunterSensor` | Detectar boids vivos y boids inactivos | Cambiar de estado |
| `HunterMotor` | Mover y rotar al cazador | Decidir destino |
| `HunterCombat` | TBA, rangos, daño, ataques | Detectar objetivos |
| `HunterInterestSpawner` | Generar cebos en Patrol, máximo 5 | Patrullar |
| `PatrolRoute` | Exponer waypoints hijos y dibujar la ruta | Mover al cazador |
| `InterestObject` | Vida del cebo y eventos de destrucción | Llevar registro global |
| `InterestObjectTracker` | Contar cebos activos sin `static` | Controlar agentes |
| `FeedbackManager` | Recibir eventos y disparar feedback | Decidir comportamientos |
| `FeedbackHUD` | Dibujar HUD, leyenda y log | Lógica de simulación |

### 2.3 Jerarquía de escena propuesta

```text
SampleScene
├── --- ENVIRONMENT ---
│   ├── Bounds
│   ├── Main Camera
│   ├── Directional Light
│   └── Global Volume
├── --- AGENTS ---
│   ├── Hunter
│   └── Boids
│       ├── Boid
│       ├── Boid (1)
│       ├── Boid (2)
│       ├── Boid (3)
│       ├── Boid (4)
│       └── Boid (5)
├── --- PATROL ROUTES ---
│   └── Hunter Route
│       ├── Waypoint_1
│       ├── Waypoint_2
│       ├── ...
│       └── Waypoint_7
└── --- SYSTEMS ---
    └── Feedback Manager
```

**Importante:** `Hunter Route` **no** debe ser hijo de `Hunter`, porque los waypoints se moverían junto con el cazador. Debe ser un Empty independiente (o dentro de `--- PATROL ROUTES ---`).

`PatrolRoute` se agrega a `Hunter Route`, toma automáticamente los hijos en `Awake`/`OnValidate` y expone `IReadOnlyList<Transform> Waypoints`. Así se elimina la lista manual de `PatrolData`.

---

## 3. Bloques de trabajo

### Bloque 0 — Preparación y red de seguridad

**Objetivo:** poder refactorizar sin romper la escena.

- [ ] Crear branch `refactor/feedback-performance`.
- [ ] Commit del estado actual.
- [ ] Duplicar `SampleScene.unity` y los prefabs como backup (o commit + tag).
- [ ] Verificar layers: `Hunter` = 6, `Boid` = 7. Crear `Interest` = 8 para los cebos.
- [ ] Anotar valores actuales de tuning (velocidad, fuerzas, radios, TBA, daño).
- [ ] Agregar teclas de debug: F1 HUD, F2 labels, F3 gizmos.

**DoD:** branch creado, escena abre y corre igual que antes.

**Estimación:** 30 min.

---

### Bloque 1 — Organización de escena y limpieza del proyecto

**Objetivo:** resolver la corrección de Hierarchy y sacar código muerto.

- [ ] Crear los Empty `--- ENVIRONMENT ---`, `--- AGENTS ---`, `--- PATROL ROUTES ---`, `--- SYSTEMS ---`.
- [ ] Mover `Bounds`, cámara, luz y Global Volume a `--- ENVIRONMENT ---`.
- [ ] Mover `Hunter` a `--- AGENTS ---`.
- [ ] Crear `Boids` y mover los 6 boids ahí.
- [ ] Crear `Hunter Route` dentro de `--- PATROL ROUTES ---` y mover `Waypoint_1..7` como hijos.
- [ ] Agregar `PatrolRoute` a `Hunter Route`; auto-colección de hijos.
- [ ] Quitar `PatrolData` de `HunterAgent` y reemplazar por la referencia a `PatrolRoute`.
- [ ] Eliminar o mover a `Assets/_Samples/` los scripts sin uso: `FSMAgent.cs`, `IdleState.cs`, `PatrolState.cs`, `TutorialInfo`.
- [ ] Reorganizar carpetas según la estructura objetivo.
- [ ] Renombrar `Agent` → `BoidAgent` y `Bounds` → `WorldBounds` (preservando los `.meta`).
- [ ] (Opcional) Agregar namespaces `ParcialIA.Boids`, `ParcialIA.Hunter`, etc.

**Archivos:** `SampleScene.unity`, `HunterAgent.cs`, `PatrolRoute.cs`, `Agent.cs`, `Bounds.cs`.

**DoD:**
- No queda ningún waypoint en la raíz del Hierarchy.
- No quedan scripts sin uso en `Assets/Scripts`.
- La escena corre y el cazador sigue patrullando.

**Estimación:** 1–2 h.

---

### Bloque 2 — Refactor de Boids: partir el God Script

**Objetivo:** dividir `Agent.cs` en clases con una sola responsabilidad y eliminar las listas estáticas.

- [ ] Crear `BoidAgent` como coordinador:
  - Referencias a `BoidSensor`, `BoidBehaviorController`, `BoidMotor`, `BoidHealth`, `BoidInteraction`.
  - `Update()` solo: si está vivo → sensor.Tick() → behavior.Calculate() → motor.Apply().
- [ ] Crear `SteeringCalculator` (clase pura, sin MonoBehaviour):
  - `Seek`, `Flee`, `Evade`, `Arrive`, `Separation`, `Alignment`, `Cohesion`, `CalculateSteering`.
  - Sin `Time.deltaTime` adentro.
- [ ] Crear `BoidSensor`:
  - `OverlapSphereNonAlloc` con máscara combinada (Boid + Hunter + Interest).
  - Buffer reutilizable.
  - Expone `Neighbors`, `NearestHunter`, `NearestInterest`.
  - Intervalo de detección configurable + fase aleatoria para repartir el costo.
- [ ] Crear `BoidMotor`:
  - `Velocity`, `MaxSpeed`, `MaxForce`.
  - `Apply(steering, dt)`: `velocity += steering * dt`, clamp, `position += velocity * dt`.
  - `Stop()`, `Teleport()`, `FaceVelocity()`, clamp contra `WorldBounds`.
- [ ] Crear `BoidBehaviorController` con prioridad explícita:
  1. **Evade** si hay cazador detectado.
  2. **Interact** si hay cebo dentro del radio de interacción.
  3. **Arrive** si hay cebo dentro del radio de interés.
  4. **Flocking** en caso contrario.
- [ ] Crear `BoidHealth`:
  - `MaxHealth`, `CurrentHealth`, `TakeDamage()`, `Die()`, `IsDead`, `IsCollected`.
  - Al morir: velocidad cero, queda en el lugar, collider activo para que el cazador lo detecte.
  - Al ser recolectado: ocultar renderer/collider, esperar respawn, reaparecer en posición aleatoria de `WorldBounds`.
- [ ] Crear `BoidInteraction`:
  - Dañar el cebo solo mientras está dentro del radio.
  - Si el cebo muere, liberar objetivo y volver a flocking. **No morir.**
- [ ] Crear `BoidState` (enum) para feedback: `Flocking`, `Seeking`, `Interacting`, `Evading`, `Dead`, `Respawning`.
- [ ] Eliminar `_allAgents` y `_inactiveAgents`.
- [ ] Eliminar `CalculateSteering` con `Time.deltaTime` doble.
- [ ] Implementar Evade real: predecir posición del cazador con su velocidad.
- [ ] Mantener Separation con radio menor que Alignment/Cohesion.
- [ ] (Opcional) Agregar una corrección mínima de separación si dos boids quedan superpuestos.

**Archivos:** `Agent.cs` → `BoidAgent.cs`, `BoidSensor.cs`, `BoidMotor.cs`, `BoidBehaviorController.cs`, `BoidHealth.cs`, `BoidInteraction.cs`, `BoidState.cs`, `SteeringCalculator.cs`.

**DoD:**
- Ningún `static List<Agent>`.
- Ningún `new List` por frame.
- Cada clase < 150 líneas.
- Los boids siguen haciendo flocking, evaden y buscan cebos.
- Al destruir un cebo el boid **no** queda inactivo.

**Estimación:** 3–4 h.

---

### Bloque 3 — Sensores y performance

**Objetivo:** resolver la corrección de `OverlapSphere` y las allocaciones.

- [ ] Crear `PhysicsSensor` o helper con `OverlapSphereNonAlloc` y buffer compartido.
- [ ] `BoidSensor`:
  - Una sola esfera por tick con máscara combinada.
  - Filtrar resultados por distancia a `_viewRadius`, `_separationRadius`, `_hunterDetectionRadius`, `_interestDetectionRadius`.
  - Intervalo sugerido: 0.1–0.2 s.
  - Fase inicial aleatoria por agente.
- [ ] `HunterSensor`:
  - Una sola esfera por tick sobre la capa Boid.
  - Cachear `List<BoidAgent> ActiveBoids` y `List<BoidAgent> InactiveBoids`.
  - Exponer `NearestActiveBoid`, `NearestInactiveBoid`, `HasActiveBoids`, `HasInactiveBoids`.
  - No crear listas nuevas por llamada.
- [ ] Reemplazar en `HunterPatrolState` las llamadas a `GetInactiveBoidsInRange()` y `GetBoidsInPerception()` por el sensor cacheado.
- [ ] Reemplazar en `HunterAttackState` la detección duplicada por `HunterSensor`.
- [ ] Reemplazar en `HunterAgent.Update` el cálculo de `DetectedBoidsCount` por el sensor.
- [ ] `InterestObjectTracker`:
  - Componente de escena (no estático) donde los cebos se registran/desregistran.
  - Expone `ActiveCount`.
  - El `HunterInterestSpawner` lo consulta para el máximo de 5.
- [ ] Los boids detectan cebos por sensor, no por lista global.
- [ ] Medir con Profiler: GC alloc en steady state y llamadas de física.

**DoD:**
- Una sola `OverlapSphere` por sensor por tick.
- Ninguna detección en cada `Update`.
- Sin allocaciones por frame en estado estable (o solo las inevitables de IMGUI).
- La FSM y el flocking siguen funcionando.

**Estimación:** 2–3 h.

---

### Bloque 4 — Correcciones de comportamiento y bugs

**Objetivo:** alinear el comportamiento con la consigna y eliminar los bugs que confunden al profesor.

- [ ] **Cebo destruido:** al llegar a vida 0:
  - Disparar evento `OnDestroyed`.
  - Desregistrar del tracker.
  - Reproducir VFX.
  - Ocultar/desactivar renderer y collider.
  - Destruir el GameObject después de un pequeño delay.
  - Verificar que `ActiveCount` baje y se puedan generar nuevos cebos.
- [ ] **Boid que destruye cebo:** liberar objetivo, volver a Flocking, no `BecomeInactive()`.
- [ ] **Vida del boid:** agregar `BoidHealth` con daño configurable.
  - Melee: daño alto (o letal).
  - Rango: daño menor (2 impactos, configurable).
  - Al llegar a 0: morir, quedarse quieto en el lugar, collider activo para Gather.
- [ ] **Evade con prioridad:** mientras haya cazador detectado, Evade + Separation tienen prioridad sobre Arrive y Flocking.
- [ ] **Attack:**
  - Entrar solo si TBA listo y hay al menos un boid en percepción.
  - Si el objetivo está en MeleeAttackRadius → ataque melee.
  - Si está en RangeAttackRadius → ataque a distancia.
  - Si está fuera de ambos → perseguir.
  - Ataque exitoso → reset TBA + salir de Attack.
  - Objetivo fuera de visión → volver a Patrol **sin** resetear TBA.
- [ ] **Gather:**
  - Entrar si hay un boid inactivo en percepción.
  - Ir hacia él, ejecutar recolección durante un tiempo.
  - Al terminar: `Collect()` → desaparece y respawnea después del delay.
  - Si el objetivo deja de estar disponible → cancelar y volver a Patrol.
- [ ] **Patrol:**
  - Waypoints ping-pong.
  - Generar cebos solo en Patrol.
  - Máximo 5 activos.
- [ ] **Respawn:**
  - Posición aleatoria usando `WorldBounds.RandomPosition()`.
  - Resetear vida, velocidad y estado.
  - Evitar spawn dentro del cazador o de un cebo (opcional: reintentar).
- [ ] **Bounds:**
  - `WorldBounds.Clamp()` y `WorldBounds.RandomPosition()`.
  - Eliminar los `Random.Range(-30, 30)` hardcodeados.
- [ ] **Superposición:**
  - Separation con radio menor.
  - Si dos boids quedan muy cerca, aumentar el peso de separación y amortiguar alignment/cohesion.
  - (Opcional) Corrección mínima de posición si la distancia es menor al diámetro de colisión.

**DoD:**
- Checklist de la sección 5 completo.
- No quedan cebos fantasma.
- Los boids no mueren al destruir un cebo.
- El cazador mata, recolecta y respawnea correctamente.

**Estimación:** 2–3 h.

---

### Bloque 5 — Refactor del cazador y la FSM

**Objetivo:** partir `HunterAgent` y dejar la FSM limpia, con transiciones solo dentro de los estados.

- [ ] `HunterAgent`:
  - Solo configuración, referencias y `_fsm.Update()`.
  - Sin movimiento, detección, ataques ni GUI.
- [ ] `HunterSensor` (del Bloque 3).
- [ ] `HunterMotor`:
  - `MoveTowards(position)`, `Stop()`, `FaceDirection()`.
  - Expone `Velocity` para que los boids puedan predecir en Evade.
- [ ] `HunterCombat`:
  - TBA, `CanAttack()`, `ResetTBA()`.
  - `TryMeleeAttack(BoidAgent)`, `TryRangeAttack(BoidAgent)`.
  - Daño configurable y eventos para feedback.
- [ ] `HunterInterestSpawner`:
  - Sale de `HunterPatrolState`.
  - `BeginPatrolSpawning()` / `StopSpawning()`.
  - Consulta `InterestObjectTracker.ActiveCount < MaxInterestObjects`.
  - Usa `WorldBounds.RandomPosition()`.
- [ ] `PatrolRoute`:
  - Auto-colección de hijos.
  - Modo ping-pong o loop.
  - Gizmos de la ruta.
- [ ] `HunterStateBase`:
  - Helper común de movimiento y utilidades para evitar duplicación.
- [ ] `HunterPatrolState`:
  - Recorrer la ruta.
  - Consultar sensor para Gather y Attack.
  - Transiciones con `_fsm.ChangeState(...)`.
- [ ] `HunterAttackState`:
  - Elegir objetivo del sensor.
  - Perseguir / melee / rango.
  - Salir a Patrol si el objetivo desaparece.
- [ ] `HunterGatherState`:
  - Elegir cadáver del sensor.
  - Moverse, temporizador de recolección, cancelación.
- [ ] `FiniteStateMachine`:
  - Agregar evento `OnStateChanged` para feedback.
  - Mantener las transiciones en los estados (no en `HunterAgent.Update`).
- [ ] Eliminar `PatrolData` y los scripts de FSM de ejemplo.

**DoD:**
- `HunterAgent` < 120 líneas.
- Cada estado < 80 líneas.
- Ninguna transición decidida fuera de la FSM/estados.
- El cazador se comporta igual o mejor.

**Estimación:** 2–3 h.

---

### Bloque 6 — Feedback visual (prioridad máxima)

**Objetivo:** que se entienda todo sin mirar la GUI y que se distinga claramente un comportamiento de un bug.

#### 6.1 Base de feedback

- [ ] `FeedbackManager` en `--- SYSTEMS ---`:
  - Log de eventos con timestamp.
  - Floating text.
  - Contadores: kills, recolecciones, cebos generados, respawns.
  - Referencias a HUD y VFX.
- [ ] `FeedbackEvents`:
  - `BoidStateChanged`, `BoidDied`, `BoidCollected`, `BoidRespawned`.
  - `HunterStateChanged`, `HunterAttack`, `HunterKill`, `HunterGather`.
  - `InterestSpawned`, `InterestDamaged`, `InterestDestroyed`.
- [ ] `FeedbackHUD`:
  - Un solo `OnGUI` (o UI Toolkit) en vez de GUI repartida.
  - Estilos con colores y fondos.
  - Toggle F1.
- [ ] `WorldLabel`:
  - Proyectar posición 3D a pantalla con `Camera.WorldToScreenPoint`.
  - Mostrar etiqueta con fondo, distancia y color.
  - Toggle F2.

#### 6.2 Feedback de los boids

- [ ] Color por estado con `MaterialPropertyBlock` (sin instanciar materiales):
  - Flocking: verde/blanco.
  - Seeking/Arrive: cian.
  - Interacting: amarillo.
  - Evading: rojo/magenta.
  - Dead: gris oscuro.
  - Respawning: azul intermitente.
- [ ] Etiqueta sobre cada boid: `Boid 3 | Flocking` o icono + estado.
- [ ] Barra de vida sobre el boid.
- [ ] Barra/aro de progreso de interacción con el cebo.
- [ ] Flecha de velocidad (Gizmos o LineRenderer).
- [ ] Gizmos de percepción:
  - Vista (blanco).
  - Separación (rojo).
  - Detección de cazador (naranja).
  - Detección de cebo (cian).
- [ ] Muerte:
  - Quedarse quieto, color gris.
  - Cruz roja / calavera sobre el cuerpo.
  - Etiqueta `MUERTO`.
  - VFX de impacto.
  - **No confundir con “recolectado”.**
- [ ] Recolectado:
  - Desaparecer con VFX.
  - Etiqueta `RECOLECTADO` y contador de respawn.
- [ ] Respawn:
  - VFX de aparición.
  - Flash breve.
  - Etiqueta `RESPAWN`.

#### 6.3 Feedback del cazador

- [ ] Color/aro por estado:
  - Patrol: azul.
  - Attack: rojo.
  - Gather: amarillo.
- [ ] Etiqueta sobre el cazador:
  - `Hunter | ATTACK`
  - `Target: Boid 2`
  - `TBA: 1.2 s`
- [ ] Barra de TBA sobre el cazador.
- [ ] Gizmos de rangos:
  - Melee (rojo).
  - Range (naranja).
  - Percepción (blanco).
- [ ] Línea al objetivo actual (LineRenderer o Gizmos).
- [ ] Ataque melee: VFX de slash/impacto + floating text `MELEE`.
- [ ] Ataque a distancia: tracer/línea + VFX en el objetivo + `RANGE`.
- [ ] Kill: VFX + floating text `KILL` + contador en HUD.
- [ ] Gather: aro/barra de progreso sobre el cadáver + `GATHERING`.
- [ ] Cambio de estado: banner/floating text `PATROL → ATTACK`.

#### 6.4 Feedback de los cebos

- [ ] Barra de vida sobre cada cebo.
- [ ] Color/pulso según vida (verde → amarillo → rojo).
- [ ] Etiqueta `CEBO` y vida actual.
- [ ] VFX de aparición + floating text `+ CEBO`.
- [ ] Al destruirse: VFX, desaparecer del todo, `CEBO DESTRUIDO`, desregistrar.
- [ ] Contador en HUD: `Cebos activos: 3/5`.

#### 6.5 HUD

- [ ] Panel superior izquierdo:
  - Estado del cazador.
  - Objetivo actual.
  - Boids detectados.
  - TBA restante.
  - Cebos activos.
  - Kills / recolecciones.
- [ ] Panel superior derecho: leyenda de colores e iconos.
- [ ] Panel inferior izquierdo: log de últimos 8 eventos con hora.
- [ ] Lista de boids con estado, vida y objetivo.
- [ ] Ayuda de teclas: F1 HUD, F2 labels, F3 gizmos.
- [ ] Colores consistentes entre cuerpo, etiqueta y HUD.

#### 6.6 Opcional

- [ ] TrailRenderer en los boids para visualizar el movimiento.
- [ ] Indicador de “detectado” cuando el cazador tiene un boid en percepción.
- [ ] Sonidos simples para kill/gather/spawn (no requerido).

**DoD:**
- Se distingue a simple vista:
  - Boid vivo, evadiendo, interactuando, muerto, recolectado y respawneando.
  - Cazador patrullando, atacando, persiguiendo y recolectando.
  - Cebo sano, dañado, destruido y reaparecido.
- No hace falta mirar la GUI para entender las acciones principales.
- La GUI complementa, no reemplaza, el feedback 3D.

**Estimación:** 4–6 h.

---

### Bloque 7 — QA, performance e integración

**Objetivo:** verificar que todo funciona y que no hay regresiones.

- [ ] Playtest con la checklist de la sección 5.
- [ ] Verificar 6+ boids y que no haya líder ni controlador global.
- [ ] Verificar Separation < Alignment/Cohesion.
- [ ] Verificar prioridad de Evade.
- [ ] Verificar que el cazador no entra a Attack sin TBA.
- [ ] Verificar que salir de Attack por perder visión **no** resetea TBA.
- [ ] Verificar que un ataque exitoso resetea TBA y sale de Attack.
- [ ] Verificar Gather y cancelación.
- [ ] Verificar máximo 5 cebos y que los destruidos liberan lugar.
- [ ] Verificar muerte, recolección y respawn.
- [ ] Verificar que un boid que destruye un cebo sigue vivo.
- [ ] Profiler:
  - CPU.
  - GC alloc.
  - Physics.
  - Llamadas de `OverlapSphere`.
- [ ] Casos borde:
  - Todos los boids muertos.
  - Cazador en Gather y el cadáver respawnea.
  - Cebo destruido por varios boids a la vez.
  - Boid muere mientras interactúa.
  - Sin waypoints.
  - Sin prefab de cebo.
  - Referencias nulas.
- [ ] Build Windows standalone y prueba.
- [ ] (Opcional) Tests EditMode para `SteeringCalculator` y `FiniteStateMachine`.

**DoD:**
- 60 FPS estables.
- Consola sin errores ni warnings.
- Checklist completo.
- Build funcional.

**Estimación:** 2 h.

---

### Bloque 8 — Documentación y entrega

**Objetivo:** que el profesor encuentre rápido la evidencia de cada requisito.

- [ ] `README.md`:
  - Descripción.
  - Controles.
  - Cómo ejecutar.
  - Parámetros ajustables.
  - Arquitectura resumida.
- [ ] `REQUISITOS.md`:
  - Tabla consigna → script/clase → feedback visual.
- [ ] `ARQUITECTURA.md`:
  - Diagrama (Mermaid) de clases y flujo.
  - Responsabilidades.
- [ ] Capturas/GIF/video corto mostrando:
  - Flocking.
  - Evade.
  - Ataque melee.
  - Ataque a distancia.
  - Kill.
  - Gather.
  - Cebo destruido.
  - Respawn.
- [ ] Limpiar `Debug.Log` o gatearlos con `[Conditional("UNITY_EDITOR")]`.
- [ ] Commit final y tag `entrega-parcial-1`.

**DoD:**
- Documentación clara.
- Evidencia visual de cada requisito.
- Proyecto limpio.

**Estimación:** 1–2 h.

---

## 4. Orden recomendado y estimación

### Orden completo (recomendado)

```text
0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8
```

**Total estimado:** 18–25 h de trabajo efectivo.

### Si hay poco tiempo (prioridad absoluta)

1. **Bloque 1** — Hierarchy y limpieza (1–2 h).
2. **Bloque 4** — Bugs de cebo y muerte del boid (2–3 h).
3. **Bloque 3** — Sensores y performance (2–3 h).
4. **Bloque 6 (MVP)** — Colores de estado, muerte, kill, cebo, HUD y leyenda (3–4 h).
5. **Bloque 2/5** — Refactor de God Scripts (resto).

Ese recorte ataca directamente las 5 correcciones del profesor, aunque deje el refactor para después.

### Quick wins (primeras 2 horas)

- [ ] Mover waypoints a `Hunter Route`.
- [ ] Arreglar la destrucción/desregistro de cebos.
- [ ] Quitar `BecomeInactive()` al destruir un cebo.
- [ ] Color por estado + cruz de muerte.
- [ ] Cachear la detección del cazador.

---

## 5. Checklist de aceptación contra la consigna

| Requisito de la consigna | Implementación esperada | Evidencia visual |
|---|---|---|
| Mínimo 6 agentes | 6 boids en escena | HUD: `Boids: 6` |
| Agentes autónomos, sin líder ni controlador global | `BoidAgent` + `BoidSensor` por agente | Cada boid con estado propio |
| Percepción limitada | Radios de sensor configurables | Gizmos de vista/separation |
| Separation | `SteeringCalculator.Separation` | Gizmo rojo |
| Alignment | `SteeringCalculator.Alignment` | Estado Flocking |
| Cohesion | `SteeringCalculator.Cohesion` | Estado Flocking |
| Separation con radio menor | `_separationRadius < _viewRadius` | Gizmos comparables |
| Evade con prioridad | `BoidBehaviorController` prioridad 1 | Color rojo + etiqueta `EVADING` |
| Evitar superposición | Separation + corrección mínima | Boids no se atraviesan |
| Cazador genera cebos | `HunterInterestSpawner` | VFX de spawn + contador |
| Boids usan Arrive | `SteeringCalculator.Arrive` | Color cian + etiqueta `SEEKING` |
| Interacción con daño por intervalo | `BoidInteraction` + `InterestObject` | Barra de vida del cebo |
| Vida del agente a 0 → inactivo | `BoidHealth.Die()` | Gris + cruz + `MUERTO` |
| Recolectado → desaparece y respawnea | `BoidHealth.Collect()` | VFX + contador de respawn |
| FSM obligatoria en cazador | `FiniteStateMachine<T>` | Estado en HUD + color |
| Transiciones dentro de la FSM | Estados llaman `ChangeState` | Log de transiciones |
| TBA | `HunterCombat` | Barra de TBA |
| RangeAttackRadius | `HunterCombat` | Gizmo naranja |
| MeleeAttackRadius | `HunterCombat` | Gizmo rojo |
| Patrol con waypoints | `PatrolRoute` | Gizmo de ruta |
| Fin de ruta: volver o invertir | `PatrolRoute` ping-pong | Movimiento visible |
| Spawn de cebos cada intervalo, máx. 5 | `HunterInterestSpawner` + tracker | Contador `3/5` |
| Attack solo con TBA y boid en percepción | `HunterPatrolState` | HUD `TBA listo` |
| Melee si está en radio | `HunterAttackState` | VFX melee |
| Rango si está en radio | `HunterAttackState` | Tracer |
| Perseguir si fuera de rango | `HunterAttackState` | Línea al objetivo |
| Ataque exitoso → reset TBA + salir | `HunterCombat` + estado | Log + barra |
| Objetivo fuera de visión → Patrol sin reset TBA | `HunterAttackState` | Log de transición |
| Gather al detectar cadáver | `HunterGatherState` | Color amarillo |
| Recolección con tiempo | `HunterGatherState` | Aro de progreso |
| Cancelar Gather si el objetivo no está | `HunterGatherState` | Log `GATHER CANCELADO` |
| Feedback visual y funcional | `FeedbackManager` + HUD + VFX | Leyenda y log |
| Performance: detección periódica | `BoidSensor` / `HunterSensor` | Profiler |

---

## 6. Decisiones a tomar antes de implementar

1. **Vida del boid**
   - Opción A (recomendada): vida real con daño melee/rango configurable. Más fiel a la consigna y mejor feedback.
   - Opción B: mantener kill instantáneo y usar `BoidHealth` solo como estado. Menos cambios de balance.

2. **Interfaz**
   - Opción A (recomendada): IMGUI estilizada, rápida de implementar.
   - Opción B: UI Toolkit, más prolija pero más tiempo.

3. **Renombres y namespaces**
   - Opción A (recomendada): renombrar `Agent` → `BoidAgent`, `Bounds` → `WorldBounds` y agregar namespaces, preservando `.meta`.
   - Opción B: mantener nombres para minimizar riesgo de wiring.

4. **Prioridad Gather vs Attack**
   - Opción A (recomendada): Gather solo desde Patrol, para no cortar un ataque.
   - Opción B: Gather global (si aparece un cadáver, interrumpe Attack). Más fiel a “deberá ingresar cuando detecte”, pero puede sentirse raro.

5. **Edición de escena**
   - Opción A (recomendada): mover objetos a mano en el Editor y usar `PatrolRoute` con auto-colección.
   - Opción B: script de Editor que reorganice todo por menú. Más automático, más código.

6. **Alcance**
   - ¿Implemento solo el plan o arranco con los bloques?

---

## 7. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Romper referencias de escena al renombrar clases | Preservar `.meta`; probar la escena después de cada bloque |
| Cambiar el tuning al corregir el doble `deltaTime` | Anotar valores actuales y retunear con la escena abierta |
| Agregar vida cambia el balance del cazador | Hacer daños configurables y probar melee/rango |
| Feedback con muchos assets | Usar primitivas, `MaterialPropertyBlock`, VFX simples y HUD IMGUI |
| Over-refactor | Mantener el comportamiento actual como baseline y refactorizar por partes |
| Muchos `OnGUI` | Centralizar en `FeedbackHUD` y cachear estilos |
| Cebos fantasma | Destruir/desregistrar siempre al morir; test explícito |

---

## 8. Próximo paso sugerido

Empezar por **Bloque 0 + Bloque 1** (branch, backup, jerarquía, `PatrolRoute`, limpieza) y luego **Bloque 4** (bugs de cebo/muerte), que son los cambios de menor riesgo y mayor impacto visual inmediato.

Después seguir con **Bloque 2 + 3 + 5** (refactor y sensores) y cerrar con **Bloque 6** (feedback completo) y **Bloque 7 + 8** (QA y documentación).
