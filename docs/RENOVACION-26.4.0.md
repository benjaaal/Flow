# MacFlow 26.4.0

## Cambios

- Isla con un solo resorte que conserva velocidad al cambiar de estado. La
  integración no realimenta el redondeo de píxeles del HWND: evita tamaños
  intermedios atascados con DPI 125 %. Expansión por permanencia del cursor,
  contracción al salir y control por clic. Indicador de reproducción sin punto
  de estado ficticio. Relojes desconectados al terminar u ocultar.
- Asistente anclado bajo la isla, con entrada legible, sugerencias editables,
  historial temporal de doce órdenes, detener, copiar y leer respuesta. Lente,
  luces blancas y reflexión se enlazan a estados de trabajo reales; se mantiene
  movimiento reducido. Se revisaron fotogramas de las referencias previas y de
  la grabación de pantalla del 17 de octubre aportada por el usuario.
- Preguntas libres y «Gemini pregunta» pasan a un puente de accesibilidad con
  Gemini visible en Chrome. No requiere claves, API, depuración remota ni scripts
  inyectados. Valida la URL en el control de dirección ajeno al documento, conserva
  borradores, comprueba el foco y el texto antes de enviar una sola vez. Lee una
  respuesta identificada y estabilizada; no ejecuta el contenido recibido.
  Cancelar impide respuestas o voz tardías; un envío efectuado no se puede retirar.
- Cierres y minimizaciones nativas se observan mediante WinEvent fuera del
  proceso y comprobación del estado. No se bloquea WM_CLOSE ni se modifican estilos
  o ventanas ajenas. Se anima una representación propia después del cierre real.
  Se conserva una única captura reciente en RAM, sin archivos ni transmisión;
  cuando no está disponible, se usa una tarjeta con icono y título. Los avisos de
  guardar y cierres cancelados quedan a cargo de cada aplicación.
- Destino por icono de aplicación y monitor; el destino de una acción iniciada
  desde MacFlow se fija antes del cierre. La cola de la malla llega primero al Dock
  y después absorbe el cuerpo. La representación no deja visible simultáneamente
  el panel original. Paneles de la barra superior siguen retirándose hacia arriba.
- Paneles de contenido con tinte translúcido suficiente y texto estable según el
  tema; el Centro de control ya no usa el material casi transparente del Dock.
- «Ocultar automáticamente» refleja todas las reglas del Dock. Apagarlo desactiva
  también ocultar dentro de aplicaciones y Codex. Aplicar ajustes conserva la
  elección de animaciones nativas. Desactivar ocultación de la barra original
  restaura sus superficies y quita ABS_AUTOHIDE, incluso si estaba guardado antes.

## Comprobación y límites

El modo `--polish-smoke carpeta` comprueba cambios de tamaño interrumpidos,
redondeo DPI, movimiento reducido, controles reales de Configuración y persistencia,
destino del icono, preguntas libres con proveedor aislado, cancelación y respuestas
tardías, validación del host, cierres nativos, minimización sin captura y cancelación
del cierre, además de tres direcciones de la malla. Guarda capturas de ventanas propias.
El informe de la entrega contiene los recuentos y resultados finales.

El puente de Gemini está implementado y probado con proveedor aislado. La prueba
interactiva con Chrome se detuvo porque Computer Use no pudo verificar la URL;
por ello NO se declara comprobado el envío y la lectura contra Gemini real. Si
Chrome cambia sus controles accesibles, falta sesión o hay un aviso, MacFlow lo
informa y deja el control al usuario. No se automatizan autenticaciones o permisos.
La voz requiere un motor español de System.Speech; micrófono real pendiente.

La animación nativa es una representación posterior al cierre y puede coexistir
con la transición de Windows. No reemplaza el compositor del sistema ni garantiza
todas las apps: ventanas protegidas, sin eventos/identidad, elevadas, muy recientes
o fuera de los límites de captura pueden usar tarjeta o conservar el comportamiento
de Windows. Las pruebas nativas usan documentos aislados, no cierran apps del usuario.
No se afirma validación de todas las apps, de dos pantallas físicas o de reinicio real.
La prueba de desactivar ocultación usa ajustes aislados: el cambio de estado de la
barra real del usuario no se forzó durante las pruebas.

## Referencias

- [Microsoft: WinEvents](https://learn.microsoft.com/windows/win32/winauto/event-constants).
- [Microsoft: SetWinEventHook](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwineventhook).
- [Microsoft: entrada y ValuePattern](https://learn.microsoft.com/dotnet/api/system.windows.automation.valuepattern.setvalue).
- [Google: acceso a Gemini](https://support.google.com/gemini/answer/13278668?hl=es).

Las grabaciones, fotogramas y perfiles privados quedan fuera del repositorio.
