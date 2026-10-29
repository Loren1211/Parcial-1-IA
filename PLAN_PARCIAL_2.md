# Plan del Parcial 2 — Cazador y lobos

Documento de trabajo del Parcial 2. Describe el escenario, la arquitectura, las
fases de implementación y los criterios de aceptación. La documentación puede
estar en español; el código (carpetas, scripts, clases, prefabs) queda en inglés.

## Contexto

El Parcial 1 (boids + cazador con FSM `Patrol/Attack/Gather`) se cierra con el
tag `parcial-1-final` y se conserva intacto en `Assets/Scenes/SampleScene.unity`.
El Parcial 2 agrega escenario, zonas, lobos y una FSM ampliada del Cazador, pero
reutiliza los sistemas de boids, percepción, steering y feedback ya existentes.

Todo el trabajo del Parcial 2 vive en la rama `feature/parcial-2`.

## Integrantes y división del trabajo

- **Lorenzo Nieto** — diseño del escenario, navegación (grafo + A*), lobos,
  FSM del Cazador, feedback, documentación y build.

## Escenario (Variante A: anillo)

Mapa en forma de anillo perimetral con un bloque central ocupado por el Coto de
caza. Los extremos izquierdo y derecho del anillo son el Inicio y la Salida.

| Zona | Rol | Capa |
|---|---|---|
| `Zone_Initial` | Spawn del Cazador. Punto de partida del recorrido. | `InitialZone` |
| `Zone_HuntingGround` | Coto de caza: 6 boids reutilizando `WorldBounds`. | `HuntingGround` |
| `Zone_Safe_A` | Callejón sin salida noreste. Refugio del Cazador. | `SafeZone` |
| `Zone_Safe_B` | Callejón sin salida suroeste. Refugio del Cazador. | `SafeZone` |
| `Zone_Exit` | Meta: el Cazador escapa al completar las recolecciones. | `ExitZone` |
| `Zone_NorthRoom` / `Zone_SouthRoom` | Salas de paso con conectores al Coto. | `Room` |

Reglas del escenario:

- Las zonas se identifican por color de piso, `ZoneVolume` y etiqueta en el HUD.
- Las zonas seguras son callejones sin salida: los lobos no pueden entrar.
- El Coto está cerrado por paredes con dos entradas (norte y sur), de modo que
  la navegación tenga que rodear obstáculos reales.
- Los boids quedan confinados al Coto mediante `WorldBounds` con `wrap=false`.

Generación reproducible: `Tools > Parcial 2 > Build Scene` reconstruye
`Assets/Scenes/Parcial2.unity` desde `Parcial2SceneBuilder`.

## Requisitos funcionales

### Cazador

FSM con seis estados: `Search`, `Attack`, `Gather`, `Flee`, `Exit`, `Dead`.

Prioridad de evaluación: `Dead > Flee > Exit > Gather > Attack > Search`.
La prioridad se resuelve en un estado base compartido; cada estado concreto
solo implementa su comportamiento y expone sus flags/eventos. Las transiciones
se evalúan dentro de los estados, no en un `switch` central.

Variables obligatorias:

- `RequiredGathers = 3`
- `RespawnTime = 5`
- `SafeZoneWaitTime`

Comportamiento:

- `Search`: patrulla el mapa (waypoints + navegación por grafo) buscando boids
  o cadáveres.
- `Attack`: persigue y ataca boids dentro de rango; usa A* hasta tener línea de
  visión y solo entonces steering directo en el tramo final.
- `Gather`: va a un cadáver, lo recolecta y suma al contador.
- `Flee`: si detecta un lobo, huye a la zona segura más cercana y espera ahí
  `SafeZoneWaitTime`; es inmune mientras está dentro.
- `Exit`: con `RequiredGathers` alcanzadas, navega a la Salida y termina.
- `Dead`: al ser capturado por un lobo; espera `RespawnTime`, reaparece en la
  zona inicial y reinicia el ciclo.

### Lobos

Mínimo 4 lobos.

- Patrullan por waypoints (`PatrolRoute`), más lentos que el Cazador en todos
  los estados.
- Perciben con distancia + FOV + línea de visión.
- Al detectar al Cazador emiten un aullido/alerta que comparte la última
  posición conocida con la jauría (bus de eventos con cooldown).
- Navegan con A* sobre el mismo grafo hasta la última posición conocida.
- Persiguen y capturan al Cazador si lo alcanzan.
- No pueden entrar a las zonas seguras (nodos excluidos + guarda lógica +
  collider `WolfBlocker`).

### Navegación

- Grafo único compartido, autogenerado por grilla + raycasts sobre `Obstacle`.
- Nodos de zonas seguras marcados y excluidos para los lobos.
- A* propio con heap binario, heurística euclidiana y suavizado opcional por
  línea de visión.
- `NavAgent` sigue el camino devuelto por A*.
- Sin NavMesh ni rutas predefinidas para el movimiento.

### Feedback

- Detecciones, alertas y última posición conocida de los lobos.
- Caminos A* dibujados con Gizmos y en el HUD.
- Estado y recolecciones del Cazador.
- Zona segura elegida al huir.
- Gizmos de radios de boids, radios del Cazador y FOV de los lobos.
- Leyenda de zonas en el HUD.

## Arquitectura prevista

```
Assets/Scripts/
  Core/           GameLayers, WorldBounds, PatrolRoute
  World/Zones/    ZoneVolume
  Navigation/     NavGraph, NavGraphBaker, AStarPathfinder, NavAgent, NavPath
  Wolves/         WolfAgent, WolfSensor, WolfMotor, WolfStates/, WolfAlertBus
  Hunter/         HunterAgent + States/ (Search, Attack, Gather, Flee, Exit, Dead)
  Feedback/       FeedbackManager, FeedbackHUD
  Editor/         Parcial2SceneBuilder, Parcial2Preview
```

## Fases

### Fase 0 — Preparación

- [x] Rama `feature/parcial-2` y tag `parcial-1-final`.
- [x] Capas nuevas en `TagManager.asset` y `GameLayers`.
- [x] `WorldBounds` con `wrap` y configuración del Coto.
- [x] `PatrolRoute.Configure`.

### Fase 1 — Escenario y zonas

- [x] `ZoneVolume` con registro estático, pertenencia y gizmos.
- [x] `Parcial2SceneBuilder` con mapa anillo, zonas, boids, rutas y sistemas.
- [x] `Parcial2.unity` guardada y primera en build settings.
- [x] Materiales URP del mapa.
- [x] HUD con título del Parcial 2 y leyenda de zonas.
- [x] Preview renderizada y validación batchmode.

### Fase 2 — Navegación

- [x] `NavGraph` y `NavGraphBaker` (grilla + raycasts).
- [x] `AStarPathfinder` con heap y suavizado por LOS.
- [x] `NavAgent` para seguir caminos.
- [x] Gizmos del grafo.
- [x] Gizmos del camino activo de cada agente (Fase 3/4).

### Fase 3 — Lobos

- [x] `WolfAgent`, `WolfSensor`, `WolfMotor`.
- [x] FSM de lobo: `Patrol`, `Alert`, `Chase`, `Return`.
- [x] Alerta compartida y última posición conocida.
- [x] Exclusión de zonas seguras.
- [x] 4 lobos en escena con rutas.

### Fase 4 — FSM ampliada del Cazador

- [x] Estados `Search`, `Flee`, `Exit`, `Dead`.
- [x] Prioridad `Dead > Flee > Exit > Gather > Attack > Search`.
- [x] `RequiredGathers`, `RespawnTime`, `SafeZoneWaitTime`.
- [x] Integración con navegación y percepción de lobos.

### Fase 5 — Feedback y QA

- [x] HUD con estado del Cazador, lobos, alertas y caminos.
- [x] Gizmos de FOV y radios.
- [x] Playtest completo y checklist.
- [ ] PDF `Nieto Lorenzo Parcial 2.pdf`.
- [ ] Push del repo público y merge a `main`.

## Resultado del playtest automatizado

`Parcial2Playtest` ejecuta la escena en batchmode con `timeScale = 4` durante
180 s simulados y registra estados, recolecciones, capturas, alertas y
violaciones de zonas seguras.

Última corrida (rutas de lobos en las salas norte/sur):

- Recolecciones: **3/3**.
- Escape por la Salida: **sí**.
- Capturas: **0**.
- Lobos dentro de zonas seguras: **0 muestras**.
- Estados del Cazador visitados: `Search`, `Attack`, `Gather`, `Flee`, `Exit`.
- Estados de lobos visitados: `Patrol`, `Alert`, `Chase`, `Return`.
- Alertas de jauría: **sí**.

El estado `Dead` queda cubierto por la FSM y por las pruebas de captura
anteriores; en esta corrida el Cazador llegó a la Salida sin ser capturado.

## Criterios de aceptación

- La escena `Parcial2.unity` corre sin errores en Unity 6000.3.17f1.
- Hay al menos 6 boids dentro del Coto y 4 lobos fuera de él.
- Existen las zonas Inicial, Coto, dos Zonas seguras y Salida, identificables.
- El Cazador recorre los seis estados y respeta la prioridad indicada.
- Los lobos patrullan, alertan, persiguen con A* y no entran a zonas seguras.
- El Cazador escapa por la Salida tras 3 recolecciones.
- El feedback permite leer en pantalla qué está pasando sin abrir el inspector.
- El Parcial 1 sigue funcionando en `SampleScene.unity`.
