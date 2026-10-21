MacFlow 27.1.5 corrige un ciclo de reposicionamiento que podía hacer aparecer la barra de Windows y mover el Dock repetidamente.

- Los avisos ABN_POSCHANGED ya no restauran ni vuelven a adquirir la barra de Windows.
- Los avisos repetidos se agrupan; una reserva de espacio idéntica no se vuelve a escribir.
- Recalcular la misma posición no cancela ni reinicia la animación del Dock, en la pantalla principal ni en las secundarias.
- Se conservan la restauración al salir y la recuperación ante un reinicio real de Explorer.

Incluye pruebas de regresión para ráfagas de notificaciones, reservas que generan su propio aviso, coordenadas negativas, reintentos y reposicionamiento durante la ocultación. El instalador sólo se publica si la compilación y las pruebas aisladas terminan correctamente.

Verificación local: compilación x64/C# 5, 264 comprobaciones de regresión y 79 comprobaciones de interfaz aprobadas. El SHA-256 y la versión del instalador coinciden con el manifiesto generado.

No se modificaron la barra real de Windows, el inicio automático ni Explorer durante las pruebas locales. La reproducción en dos pantallas físicas y la combinación con otros reemplazos de barra siguen pendientes de validación manual; no se afirma ausencia de todos los errores.
