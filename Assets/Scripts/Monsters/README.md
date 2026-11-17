# Escena de prueba de monstruos

Abre `Assets/Scenes/MenuScene.unity`, presiona Play y usa el botón Jugar para entrar a `MonsterSandbox`, incluida en Build Settings. También puedes abrir directamente la escena de prueba. Clic dentro de Game captura el mouse; Esc lo libera. WASD mueve, Shift corre y R reinicia al jugador y a los tres enemigos. La interfaz de depuración del sandbox se retiró; se conserva la UI principal del menú.

La escena contiene los cinco modelos: Bracken, Coil-Head, Thumper, Eyeless Dog y Nutcracker. Los dos últimos son solamente exhibición, sin IA, según el alcance solicitado. Los árboles son de Nicrom, con copias de materiales URP sin viento; el plano, paredes y troncos tienen colisiones y NavMesh precalculado.

## Algoritmos

- **Bracken:** máquina de estados (acecho, retirada, furia). Busca un punto detrás del jugador. Ser visto provoca retirada; sostener la mirada 2.5 segundos acumula furia y activa una persecución de 6 segundos. Ataca a corta distancia con línea de visión.
- **Coil-Head:** percepción mediante proyección en la cámara y raycasts a varios puntos del cuerpo. Si cualquier punto es visible se detiene inmediatamente, incluida su velocidad residual. Al dejar de mirarlo persigue usando NavMesh. Las paredes y los troncos bloquean la mirada. Una vez alertado conserva al jugador como objetivo.
- **Thumper:** máquina de estados (patrulla, persecución, búsqueda), cono de visión de 140 grados y memoria de última posición vista durante 7 segundos. Acelera en rectas hasta 10 m/s y frena cuando el próximo segmento requiere un giro mayor a 25 grados. No detecta sonido.
- **Movimiento compartido:** NavMeshAgent con rutas recalculadas como máximo cada 0.15 segundos. La percepción se evalúa cada frame para que Coil-Head responda inmediatamente. Los actores usan la capa integrada Ignore Raycast para que los rayos de percepción no choquen con sus propios modelos; el escenario usa Default.

Los valores se pueden ajustar en los componentes del Inspector. `MonsterMotor.player` y `SandboxPlayer.View` están conectados en la escena. Esta es una implementación de prueba de las mecánicas, sin animaciones ni reproducción completa del juego original.

## Recursos y herramientas

`Assets/Mounstros/Thumper.fbx` se extrajo de las mallas Thumper y Thumper_tong del archivo Blender existente; conserva la pose evaluada como malla estática. `Tools/ExportThumper.py` documenta la exportación reproducible con Blender. No es necesario instalar Blender para abrir el FBX ya generado.

`Tools > Monsters > Create sandbox scene` construye la escena cuando todavía no existe. Se niega a sobrescribir una escena existente. Los modelos importados, escenas del menú y Build Settings no se modifican por este constructor.

Para validación automatizada, con Unity cerrado, ejecutar Unity con `-batchmode -projectPath <proyecto> -executeMethod MonsterSandboxValidation.Run -logFile Logs/monster-playmode.log` (sin `-quit` ni `-nographics`). El proceso termina al acabar; escribe resultados y capturas en Logs. Valida congelación/persecución de Coil-Head, oclusión, estados de Bracken, aceleración/giro/búsqueda de Thumper, daño y reinicio.
