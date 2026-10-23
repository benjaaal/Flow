# MacFlow 27.2.2

Renovación de la experiencia de escritorio sobre la base nativa existente.

- Física centralizada: amortiguación real, velocidad conservada al invertir y corrección del salto al destino en el primer cuadro de un movimiento.
- Isla dinámica flotante, expansión continua y contenido presente durante la contracción; música, temporizadores, actividades y Shelf conservados.
- Aperturas y cierres discretos. Minimizar/restaurar ventanas propias sigue una trayectoria proporcional hacia el Dock; se eliminó el embudo deformante.
- Barra continua sin recorte de notch, Dock adaptado al área útil de cada monitor y corrección del recorte durante la ampliación de iconos.
- Configuración, Centro de control, asistente, widgets, búsqueda, menús y paneles con jerarquía y materiales coherentes. Los controles siguen conectados a sus servicios reales.
- Hola manuscrito continuo, tiempos más breves y cierre de su presentación correctamente registrado.
- Pantalla visual con foto/fondo de escritorio, reloj translúcido que refracta sólo su propio fondo y reproductor compacto de escritorio. Tierra continúa como opción.
- Cortina de reloj/notificaciones desde arriba a la izquierda sin bloquear Windows: manipulación directa, velocidad de salida, inversión y borde nativo curvo durante el gesto. Se eliminó el doble desplazamiento al cerrar.
- Notificaciones compactas con icono, hora, vidrio sin contorno dibujado y reacomodo estable.

La primera ejecución aplica la nueva composición de bloqueo una sola vez, conserva la foto elegida y permite volver a activar Tierra. No modifica el bloqueo seguro ni la autenticación de Windows. El lector de avisos externos sigue sujeto a identidad de paquete compatible y permiso del sistema; el historial propio funciona en el EXE clásico.

La compilación y las pruebas automáticas son condiciones de publicación. Las mediciones WPF documentan intervalos de cuadros; no equivalen a certificar 60 FPS de GPU en toda configuración. La validación física de dos monitores con DPI distintos y desconexión en caliente sigue pendiente.

El código conserva las licencias existentes y no añade paquetes de interfaz. Ver [arquitectura y referencias](RENOVACION-27.2.2.md).
