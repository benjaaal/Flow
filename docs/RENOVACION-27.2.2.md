# Renovación 27.2.2: implementación y referencias

## Base y recuperación

La fuente de partida es MacFlow 27.2.1 nativo; se preservó una copia ZIP de sus 191 archivos y un inventario SHA256 antes de editar. La entrega anterior permanece disponible. La nueva entrega incluye fuente completa, portable e instalador; no reemplaza el proyecto por una plantilla.

## Movimiento y coste

`Motion.cs` resuelve el oscilador amortiguado de forma analítica. La retargetización conserva velocidad; la base de la propiedad conserva la pose visible hasta comenzar el reloj WPF. Los relojes son finitos y se retiran al terminar. Se respetan la preferencia de animaciones, el contraste alto y la reducción de movimiento del sistema.

`IslandMotion` usa tiempo transcurrido real y deja de escuchar Rendering al asentarse. Su HWND permanece estable y la forma interior cambia sin recortes de notch. `PanelMotion` limita escala y recorrido de ventanas comunes. `GenieTransition` conserva proporción y utiliza una sola textura temporal de la ventana propia, limitada a 1440 píxeles en su eje mayor y un máximo de dos transiciones. No se inyectan animaciones en aplicaciones ajenas.

`NotificationPullGesture` y `PrivacyScreen` despliegan la cortina. Durante el cierre se emplea una sola posición en coordenadas de pantalla, evitando la realimentación causada por medir respecto a una ventana que se está moviendo. La velocidad del dedo se conserva al soltar. La región nativa curva deja ver el escritorio real durante el recorrido y vuelve a rectangular al asentarse. Se libera cada región GDI que Windows no haya adquirido.

`WallpaperGlass` usa únicamente el fondo propio, desenfoque de la superficie y refracción corta en el borde; los textos y controles permanecen nítidos. `LockGlass.hlsl` refracta el fondo de la escena dentro de los glifos del reloj. Hay alternativa sin shader. No se capturan otras apps para fabricar el bloqueo.

`LockNotice` conserva controles al cambiar el historial y anima su reacomodo. No se reinicia toda la pila como una imagen. Las entradas de los perfiles de prueba están identificadas como pruebas y nunca se insertan en el perfil real.

## Referencias revisadas

Los videos privados del usuario se inspeccionaron mediante secuencias temporales, incluidos los del 18/10, la grabación de MacFlow, las referencias del chat previo y los del 22/10 a las 20:13, 20:56 y 21:02. La revisión final prioriza la captura de bloqueo de Mac: foto, reloj de vidrio y reproductor compacto. El movimiento de la cortina toma continuidad y seguimiento del gesto del video; la composición permanece de escritorio. No se distribuyen videos ni capturas privadas con el código.

- [Apple: Animate with springs, WWDC23](https://developer.apple.com/videos/play/wwdc2023/10158/): continuidad de velocidad y respuesta amortiguada.
- [Apple HIG: Motion](https://developer.apple.com/design/human-interface-guidelines/motion): movimiento con propósito y respuesta coherente.
- [Microsoft: WPF animation tips](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/animation-tips-and-tricks): ciclo de vida y costes de animación.
- [Microsoft: layout and design performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-layout-and-design): evitar trabajo de layout y objetos innecesarios.
- [Microsoft: 2D graphics and imaging performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging): límites de texturas, imágenes y efectos.
- [WPF UI, arquitectura](https://github.com/lepoco/wpfui/blob/main/docs/architecture/cross-cutting/navigation.md): separación de servicios de navegación y presentación; no se incorporó el paquete.
- Se intentó consultar el efecto Magic Lamp de KWin en GitLab; no se obtuvo acceso verificable a su implementación. No se atribuye código a esa consulta.

## Pruebas y límites

`--self-test` verifica persistencia, recuperación, actualizaciones, geometría, física, avisos y servicios. `--modern-smoke` comprueba componentes reales en claro/oscuro, las 17 páginas, controles multimedia, isla, cortina y materiales con lectura de píxeles. `--renewal-27-test` añade inversiones de movimiento, continuidad de Hola y notificaciones sin autenticación. `--curtain-27-test` verifica foto y música compacta, posición al soltar, recorrido de cierre, inversión, curvatura y liberación del HWND; permite grabar ventanas propias con un grabador externo.

Las pruebas usan perfiles aislados, no adquieren la barra de Windows ni cambian el fondo o el inicio automático. Las pruebas del instalador usan un destino temporal y verifican actualización, cancelación, reversión y conservación de archivos ajenos.

En la medición local del Dock con 13 iconos: mediana WPF 16,64 ms y percentil 95 18,12 ms; fueron 176 intervalos durante 3,02 s. Es una medición del render de WPF, no una garantía de presentación GPU. La ejecución física con dos monitores y DPI distintos, reconexión, cámara/micrófono y consultas Gemini con sesión real quedan pendientes.
