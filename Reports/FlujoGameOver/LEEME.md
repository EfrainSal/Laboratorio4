# Derrota, Game Over y Reintentar

La muerte termina la run y conserva una única puntuación en el ranking. `GameManager.TerminarRunPorMuerte` emite `OnRunTerminada` sin cargar escenas ni iniciar otra partida. `DeathScreenController` vuelve a ejecutar la secuencia original: bloqueo de controles, cámara lenta breve, captura desenfocada, pausa y resumen de derrota.

Mientras la pantalla de Game Over permanece abierta, el mapa no cambia. El botón **Reintentar** inicia una run nueva con el mismo personaje y carga **Recepcion** con el fundido entre escenas. Se restauran vida e inventario base y se limpia la pausa anterior; la ayuda de controles de recepción conserva su pausa hasta que se cierre. El botón **Menú** mantiene su destino original MainMenu.

Las pulsaciones repetidas de Reintentar durante la carga se ignoran. La muerte repetida tampoco duplica el ranking. Se conserva la persistencia de vida entre pisos y el loot de vendas.

Pruebas: `Laboratorio 4 > Partida > Probar vida loot y derrota` y `Laboratorio 4 > Progresion > Probar recorrido completo`. La prueba de derrota espera en Game Over, verifica que la escena no cambia, envía el evento del botón real de UI Toolkit y comprueba el reinicio en Recepcion. Los archivos de guardado/ranking de prueba están aislados y no se sincronizan externamente.

La prueba específica aprobó **90 comprobaciones sin errores**, con tres derrotas (Jason, Milena y mapa del jefe). Las capturas `perdiste-*.png` permiten revisar la pantalla original restaurada. El script `python Tools/validate_flujo_game_over.py` comprueba además los hashes de los archivos preexistentes de Resources y de las escenas.

La regresión de progresión aprobó **63 comprobaciones sin errores**. Total: **153 comprobaciones en Play**. Se conservaron sin cambios los **271 archivos preexistentes de Resources** y las **13 escenas** de la carpeta Scenes.
