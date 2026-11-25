# Arquitectura de MacFlow 28 Final Edition

MacFlow usa el escritorio y las sesiones de Windows. El Dock, la barra y las ventanas propias son superficies WPF. Las aplicaciones externas conservan su HWND y sus controles. Mission Control usa miniaturas de DWM y delega la gestión de escritorios virtuales a la Vista de tareas de Windows.

## Hallazgos de la revisión

- Varias transiciones fijan un máximo de 60 cuadros y mantienen relojes independientes. El Dock animado por puntero ya integra su resorte por tiempo. La reversión de un reloj WPF debe usar el tiempo real de ese reloj, además de la posición visible.
- La minimización coordina una textura temporal, la posición y la opacidad de la ventana. La restauración durante el recorrido necesita invalidar el cierre pendiente antes de invertirlo. Cada ruta debe devolver al HWND la opacidad, la interacción y la geometría.
- Finder usa enumeración en segundo plano y metadatos por perfil. Durante un cambio de carpeta hay que invalidar los elementos y vistas previas anteriores antes de aceptar operaciones sobre la selección.
- El repositorio público lleva documentación y automatización de 27.2.2. La prueba de actualización necesita el instalador y su manifiesto. Una compilación del núcleo sirve para iterar y la aceptación de distribución requiere el paquete completo.

## Límites entre componentes

- `Motion` define la política de cuadros, la dinámica y la cancelación. `PanelMotion`, `GenieTransition` y `DockSlide` coordinan la geometría y la vida útil de cada superficie. Las pistas propias respetan la configuración de animaciones y el alto contraste.
- El menú de la manzanita usa transparencia por píxel y un solo contorno WPF. Prepara fuera del hilo de interfaz el desenfoque y la refracción simulada de una muestra tomada al abrir. La entrada, el cierre y la inversión mueven la superficie compuesta sin volver a muestrear el escritorio.
- `Desktop` integra las superficies y un catálogo de aplicaciones. Spotlight, Launchpad y el asistente pasan por el mismo punto de apertura. Las aplicaciones propias se crean al abrirlas y guardan sus datos en el perfil activo.
- Los servicios de Windows se invocan con la identidad de la sesión actual. El bloqueo seguro queda a cargo de Windows. La integración de escritorios usa los controles documentados del sistema, sin acoplarse a servicios privados de Explorer.
- Las órdenes locales del asistente siguen un conjunto explícito de acciones. Los proveedores de texto devuelven respuestas para mostrar al usuario. El sistema de archivos, la terminal y las acciones sensibles quedan fuera de las respuestas del modelo.

## Verificación

La aceptación se ejecuta por etapas en perfiles aislados. El núcleo se compila con el compilador C# 5 del proyecto. El paquete completo comprueba los hashes de dependencias, la identidad del instalador y la instalación y desinstalación en una carpeta temporal. Los ensayos visuales validan el ciclo de vida de las ventanas y las trayectorias. Los intervalos medidos corresponden a callbacks WPF del equipo de prueba. La validación en dos pantallas físicas, con distintas escalas y frecuencias, requiere ese hardware.
