MacOS · 27.2 (compilación interna 27.2.0)

Reemplaza la cápsula flotante por un notch unido al borde superior, con hombros cóncavos, expansión continua, música y progreso reales, temporizadores, avisos y Shelf integrado. Los ajustes permiten cambiar tamaño, expansión al acercar el cursor y contenido. El nombre visible es MacOS; se conservan ejecutable, rutas y metadatos internos de MacFlow para mantener actualizaciones y preferencias compatibles.

Se retiró el modo iPad y su código exclusivo. Ajustes incorpora 17 categorías, búsqueda e importación/exportación. Centro de control usa una distribución Mac compacta. Se añadió un widget de portapapeles que sólo lee por elección y una herramienta de emojis. Tierra y Prisma son fondos originales generados para el proyecto. Tierra usa una textura equirectangular sobre una esfera 3D: gira 15 grados por hora local, con una imagen por minuto únicamente cuando se ve el escritorio. Se utilizan dos archivos alternos acotados, se respeta un cambio externo de fondo y se conserva la restauración anterior. Es una interpretación visual, no una efeméride astronómica ni nubes en tiempo real. No se cambia el fondo real durante las pruebas.

El asistente tiene una presencia independiente del notch: esfera que se transforma en banda con reflejo inferior y transiciones interrumpibles. La investigación se solicita a Gemini en Chrome, con comparación y enlaces HTTPS visibles. El programa cuenta enlaces recibidos, no verifica por sí mismo la veracidad de sus páginas. Si faltan tres fuentes, lo comunica. Gemini requiere una sesión accesible del usuario; no se usan claves ni APIs privadas. La lectura y síntesis en Chrome real sigue pendiente de verificación en esta renovación.

Se conserva la corrección 27.1.5 para el ciclo de avisos de la barra de tareas. Las animaciones de paneles propios tienen menor sobrepaso y endpoints cancelables. Las ventanas de programas ajenos mantienen las animaciones de Windows: no se inyecta código ni se altera Explorer.

Esta compilación se preparó localmente. No se ejecutó el instalador, no se habilitó inicio automático ni se publicó una actualización. Quedan pendientes las pruebas físicas con dos monitores y DPI distintos, suspensión, pantalla completa y la interacción con la barra real de Windows. Respaldo recuperable del código anterior: work/backups/MacFlow-27.1.5-before-notch.zip.

Referencias examinadas:
- Videos adjuntos del 20/10 (notch/Shelf), 21/10 (Hyprland y configuración), 18/10 (lente del asistente), 16/10 (bienvenida).
- https://developer.apple.com/design/human-interface-guidelines/materials — jerarquía, contraste y uso contenido de materiales.
- https://developer.apple.com/design/human-interface-guidelines/motion — respuestas breves, cancelables y coherentes con el gesto.
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/performance — límites del renderizado y mediciones.
- https://github.com/N3uralCreativity/WinNotch y src/WinNotch/Helpers/SpringAnimation.cs — referencia de interacción y solución física; GPL-3.0, no se incorporó código.
- https://github.com/Orvyan/Notchify — referencia de funcionalidades; licencia MIT consultada. El acceso al repositorio fue parcial; no se incorporó código.
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance — esfera texturada con malla indexada y render por demanda.
- https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.compositor.createhostbackdropbrush — muestras del fondo nativo.
- https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.effects.interop/nn-windows-graphics-effects-interop-igraphicseffectd2d1interop — ABI oficial del efecto Gaussian Blur; sin paquete adicional.
- Codeberg no pudo consultarse por robots.txt. No se descargaron ejecutables ni nuevas dependencias.

Las capturas y secuencias de prueba se generaron con ventanas propias, proveedores de datos controlados y perfiles aislados. No se presentan como datos reales del usuario. Los resultados concretos se guardan junto a la compilación local.

Verificación local: 282 regresiones de núcleo y 100 comprobaciones modernas aprobadas. Incluyen material nativo activo, comparación de contraste original/fondo, navegación rápida sin desplazamiento residual, salidas interrumpidas, progreso multimedia, fuentes del proveedor controlado, exportación/importación y WAV TTS real. En la última muestra aislada: 833 ms hasta construir el núcleo, 0,78% de un núcleo en 6 segundos de reposo y 302 MB privados, después de cargar todas las vistas. No equivalen al tiempo de inicio de sesión ni garantizan 60 FPS. Dos renders Tierra 960x540 tardaron 513 ms en la ejecución de prueba; la duración exacta está en checks.txt.

Las capturas nativas de algunas ventanas quedaron ocluidas por el menú Inicio durante la sesión. Se conservaron también capturas WPF y secuencias del renderizador para inspeccionar geometría; estas no muestran por sí solas el backdrop nativo. No se declara terminada la validación visual sobre el escritorio real ni la compatibilidad física de dos monitores.
