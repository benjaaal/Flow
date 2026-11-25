# MacFlow 28.0.1 · Final Edition

La versión de distribución es 28.0.1 para permitir la actualización desde instalaciones locales de 28.0.0.

- El menú de la manzanita responde al primer clic. Se abre debajo del botón con un rebote corto y usa Desktop Acrylic donde Windows lo admite.
- El Dock y las ventanas propias comparten la física del motor. Podés invertir una minimización desde el Dock. Las transiciones respetan el ajuste de animación de Windows. En modo ahorro las pistas WPF tienen un máximo de 60 cuadros y en modo normal Windows controla su cadencia.
- Finder añade columnas y descarta las selecciones viejas durante la navegación. Launchpad y Spotlight incluyen Calculadora, Calendario, Notas, Monitor de actividad y la Biblioteca de MacFlow.
- El asistente consulta el índice autorizado de Spotlight, informa sobre el equipo y permite configurar Ollama o Gemini. Las respuestas del modelo se muestran como texto.
- El instalador repara o actualiza en una ventana normal con progreso de archivo y cancelación antes de confirmar. El proceso usa la instalación por usuario y admite desinstalación con conservación opcional de datos.

## Integración con Windows

Mission Control muestra miniaturas DWM de las ventanas compatibles. Vista de tareas gestiona los escritorios virtuales. La Papelera y Terminal se abren con sus herramientas de Windows. La Biblioteca administra las aplicaciones incluidas. Los avisos externos dependen del permiso y de una identidad de paquete compatible.

Las pruebas aisladas validan animaciones, recuperación y distribución. La cadencia visual final depende del compositor, de la GPU y de la pantalla. Dos monitores físicos, gestos de precisión, suspensión y una sesión real de los proveedores de IA requieren validación adicional.