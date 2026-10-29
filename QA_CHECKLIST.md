# QA y checklist de aceptación

Este archivo separa lo que ya se verificó de forma estática de lo que requiere ejecutar la escena en Unity.

## 1. Verificación estática (completada)

| Chequeo | Resultado |
|---|---|
| Compilación de scripts de runtime (`Assets/Scripts/**` sin `Editor/`) | OK, 0 errores |
| Compilación de scripts de editor (`Assets/Scripts/Editor/SceneSetupTools.cs`) | OK, 0 errores |
| Uso de `UnityEngine.Input` | Eliminado; el HUD usa `Event.current` (compatible con Input System nuevo) |
| Un solo `OnGUI` | Solo `FeedbackHUD` |
| `HunterAgent` sin movimiento/ataques/GUI | OK, delega en `HunterMotor`, `HunterCombat` y `FeedbackHUD` |
| Transiciones del cazador dentro de la FSM | OK, solo los estados llaman a `_fsm.ChangeState(...)` |
| Cebos destruidos se desregistran y se destruyen | OK en `InterestObject.DestroyInterest()` |
| Cebo destruido no llama a `BecomeInactive()` | OK, el boid sigue vivo |
| `HunterInterestSpawner` fuera de `HunterPatrolState` | OK, Patrol solo lo activa/detiene |
| Colores por estado con `MaterialPropertyBlock` | OK en boids, cazador y cebos |
| `FeedbackManager` se auto-crea si falta | OK vía `RuntimeInitializeOnLoadMethod` |
| 6 boids en la escena | OK, 6 instancias del prefab `Boid` |
| Separation < ViewRadius | OK, 3 < 6 |
| Valores del prefab y de la escena | `_viewRadius: 6`, `_separationRadius: 3`, `_speed: 6.5`, `_interestSpawnInterval: 4` |

> La compilación estática se hizo con un proyecto temporal que incluye todos los `.cs`. Unity regenerará `Assembly-CSharp.csproj` al compilar; no se modificó el `.csproj` generado.

## 2. Playtest en Unity (pendiente)

Abrir `Assets/Scenes/SampleScene.unity`, presionar Play y verificar:

### Boids

- [ ] Hay 6 boids visibles y el HUD muestra `Boids: 6`.
- [ ] Ningún boid obedece a un líder: cada uno tiene estado y movimiento propios.
- [ ] En `Flocking` se ven verdes y el cardumen se mantiene unido sin zig-zag.
- [ ] Con F3, el radio de separación (rojo) es menor que el de vista (blanco).
- [ ] Al acercar el cazador a un boid, este se pone rojo (`Evading`) y se aleja.
- [ ] Un boid que detecta un cebo se pone cian (`Seeking`) y frena al llegar.
- [ ] Al entrar en el radio de interacción se pone amarillo (`Interacting`) y el cebo baja de vida.
- [ ] Si el cebo se destruye, el boid sigue vivo y vuelve a `Flocking`.
- [ ] Un boid con vida 0 queda gris, con cruz roja y etiqueta `MUERTO`.
- [ ] Al ser recolectado desaparece, muestra la cuenta regresiva y reaparece con flash azul.
- [ ] El contador de respawns aumenta.

### Cazador y FSM

- [ ] El cazador patrulla por los waypoints y el gizmo amarillo muestra la ruta.
- [ ] El HUD muestra `Patrol` y el cazador está azul.
- [ ] Con TBA en espera, el cazador **no** entra a `Attack`.
- [ ] Con TBA listo y un boid en percepción, entra a `Attack` y se pone rojo.
- [ ] Dentro de `MeleeAttackRadius` ataca cuerpo a cuerpo (`MELEE`).
- [ ] Entre melee y `RangeAttackRadius` ataca a distancia (`RANGE`).
- [ ] En `Attack` se ve una línea roja hacia el objetivo y un tracer al atacar.
- [ ] Fuera de rango, persigue al objetivo.
- [ ] Un ataque exitoso reinicia la barra de TBA.
- [ ] Si el objetivo sale de percepción, vuelve a `Patrol` **sin** reiniciar el TBA.
- [ ] Si el objetivo muere, pasa a `Gather` y se pone amarillo.
- [ ] En `Gather` se acerca al cadáver, muestra `RECOLECTANDO N%` y recolecta a los 1.5 s.
- [ ] Sobre el cadáver aparece la etiqueta `GATHERING` y su barra de progreso.
- [ ] Al cambiar de estado se ve un texto flotante con la transición.
- [ ] Si el cadáver desaparece o respawnea antes, cancela Gather y vuelve a Patrol (log `GATHER CANCELADO`).
- [ ] El log registra las transiciones `CAZADOR: Patrol -> Attack`, etc.

### Cebos

- [ ] El cazador genera cebos cada 4 s mientras patrulla.
- [ ] El HUD muestra `Cebos activos: N/5` y nunca supera 5.
- [ ] Al destruirse un cebo, el contador baja y el spawner puede generar otro.
- [ ] El cebo cambia de verde a rojo a medida que pierde vida.
- [ ] La etiqueta del cebo muestra la vida restante y tiene barra de vida.

### Casos borde

- [ ] Todos los boids muertos: el cazador no lanza errores y pasa a Gather/Patrol según corresponda.
- [ ] Cazador en Gather y el cadáver respawnea: cancela sin excepción.
- [ ] Varios boids dañan el mismo cebo: se destruye una sola vez y no quedan cebos fantasma.
- [ ] Un boid muere mientras interactúa: libera el objetivo sin errores.
- [ ] Sin waypoints: el cazador no lanza excepciones (se queda quieto en Patrol).
- [ ] Sin prefab de cebo: el spawner no lanza excepciones.
- [ ] Referencias nulas: el HUD muestra `Sin cazador` / `Sin FeedbackManager` en lugar de romperse.

## 3. Performance (pendiente)

- [ ] Profiler > CPU: 60 FPS estables con 6 boids.
- [ ] Profiler > GC Alloc: sin allocs por frame en los `Update` de agentes.
- [ ] Profiler > Physics: cantidad razonable de `OverlapSphere` por frame (una por boid cada 0.15 s y una del cazador cada 0.15 s).
- [ ] Sin warnings repetidos en consola.
- [ ] Si aparecen costos por mover colliders sin `Rigidbody`, evaluar agregar un `Rigidbody` kinematic al prefab de boid.

## 4. Build standalone (pendiente)

- [ ] `File > Build Settings > Windows x86_64`.
- [ ] Build y ejecutar el `.exe`.
- [ ] Verificar que el HUD, las teclas F1/F2/F3 y la simulación funcionan fuera del editor.

## 5. Evidencia visual sugerida

Grabar un video corto o GIF mostrando, en orden:

1. Flocking y cardumen.
2. Evade al acercar el cazador.
3. Ataque melee.
4. Ataque a distancia.
5. Kill y transición a Gather.
6. Recolección y respawn.
7. Cebo destruido por boids.
8. HUD y leyenda.

## 6. Resultado esperado

- Consola sin errores ni warnings.
- 60 FPS estables.
- Checklist completo.
- Build funcional.
