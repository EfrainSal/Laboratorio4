# HUD, Game Over, armas y spawn

Iniciar desde `Assets/Scenes/MainMenu.unity`. El destino sigue siendo la escena original `Assets/LaboratorioYiba/Piso3_V10/Scenes/Piso3_V10.unity`.

- **HUD y muerte:** `Assets/Prefabs/UI/HUDGameplay.prefab` reutiliza Canvas, DeathScreen, PauseMenu, UpgradePanel, EventSystem y LevelManager de Debugscene, con sus referencias internas. Debugscene no se modifica. El inicio del mapa entrega su jugador existente a LevelManager para conectar vida, munición, inventario y habilidad sin crear un segundo jugador. `PlayerHealth` mantiene el evento de muerte original de GameManager, que ahora encuentra a DeathScreen. Se detienen todos los spawners al morir.
- **Arma independiente:** ambos prefabs tienen `ItemArma/Visual`, controlado por `ArmaIndependiente`. El pivote está a 0.85 unidades sobre los pies, en la cadera; el arma apunta al mouse y usa flip vertical al mirar a la izquierda. FirePoint y AttackPoint pertenecen al pivote. Se conserva WeaponManager para cambiar entre subfusil y machete y se inicia con el subfusil. Las bibliotecas de Jason y Milena excluyen Disparo; disparar no cambia los sprites del cuerpo. Milena utiliza Correr sin arma para la locomoción.
- **Mira:** `MiraPersonalizada` genera una textura blanca de 32×32 en memoria, sin archivo de imagen. Recupera el cursor normal en pausa, paneles, Game Over y al salir del gameplay; reintentar restaura la mira.
- **Enemigos:** `SpawnersSorbos` contiene tres instancias del prefab original `Spawner_lvl2`, con el script EnemySpawner existente. Cada una admite ambos Sorbos, radio de activación 5, cooldown 10 segundos, oleadas de 1 a 3 y radio de dispersión 0.5. Sustituyen la creación fija de enemigos del inicio anterior. Se mantienen los puntos de aparición del mapa y el registro de progreso de LevelManager.

Herramientas en **Laboratorio 4 > Gameplay**:

1. **Integrar HUD armas y spawners** reconstruye la integración a partir de los prefabs y la interfaz originales. Guardar el trabajo abierto antes de usarlo.
2. **Probar HUD arma mira spawn y Game Over** recorre MainMenu, gameplay, daño, apuntado a ambos lados, cambio de arma, pausa, oleadas, muerte, reintento y vuelta al menú para Jason y Milena. El archivo de guardado de las pruebas se aísla dentro de Reports/Gameplay; el guardado personal no se consume. Termina en MainMenu en modo edición.

Los resultados están en `playmode.txt`; las capturas muestran el HUD y Game Over de ambos personajes. `protection.json` verifica por SHA-256 que los 271 archivos iniciales de Assets/Resources permanecen intactos y que no hay GUID duplicados.

No se movieron las carpetas originales ni se modificaron los assets existentes de Resources.
