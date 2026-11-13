# Avisos de acceso y barra del jefe

El aviso al pulsar F sin tarjeta comparte la presentación del objetivo de recepción: fondo oscuro, borde verde izquierdo, texto claro en negrita y posición inferior centrada. `EstiloAvisoRecepcion`, en `Assets/Scripts/Menus/AvisoPuertaUI.cs`, aplica la misma presentación a ambos paneles. La recepción reutiliza su objetivo y vuelve al mensaje habitual al expirar el aviso; los otros pisos crean un panel temporal sin bloquear clics. Pulsar F de nuevo renueva el tiempo del aviso.

La referencia antigua de `PuertaNivel.textoMensajeUI` se conserva para compatibilidad, pero el texto y su hijo FondoGris permanecen ocultos. No se cambia el requisito de tarjeta ni el destino de las puertas. Los ajustes visuales se encuentran en `EstiloAvisoRecepcion.Aplicar`; la duración sigue siendo `PuertaNivel.duracionMensaje`.

`BossHealth.AcomodarBarraVida` coloca la vida del jefe arriba a la derecha, separada del área completa de vida/habilidad del jugador. Conserva el arte de la barra y adapta su escala al espacio disponible. En ventanas estrechas la sitúa debajo del HUD para evitar superposición. Los campos **Margen Barra**, **Ancho Maximo Barra** y **Separacion Del Jugador**, en el componente Boss Health, permiten retocar la posición. El ajuste se recalcula al cambiar la resolución.

Prueba: `Laboratorio 4 > UI > Probar avisos y vida del jefe`. Comprueba F real, estilo compartido, duración y ausencia de la franja gris en Recepcion, Entrada, Piso3 y Laboratorio. Comprueba la vida del jefe en Escenario4_Jefe y Nivel1, su actualización al recibir daño y la distribución a proporciones 16:9, 4:3 y vertical. Las capturas se guardan aquí. Los guardados de prueba están aislados y no se publican rankings.

La integración se realiza en tiempo de ejecución. No se modifican escenas, prefabs ni archivos existentes de Resources.

Resultado: **51 comprobaciones aprobadas sin errores ni excepciones**. Se revisaron las capturas de recepción, el aviso en Entrada y las barras del jefe en Escenario4. El validador aprobó la conservación por SHA-256 de los **271 archivos preexistentes de Resources** y las **13 escenas**. Las seis comprobaciones de distribución usan clones temporales de los RectTransform reales en proporciones 16:9, 4:3 y vertical; no cambian GameView ni la configuración del proyecto.
