# RenovaciÃ³n 27.2.2: implementaciÃ³n y referencias

## Base y recuperaciÃ³n

La fuente de partida es MacFlow 27.2.1 nativo; se preservÃ³ una copia ZIP de sus 191 archivos y un inventario SHA256 antes de editar. La entrega anterior permanece disponible. La nueva entrega incluye fuente completa, portable e instalador; no reemplaza el proyecto por una plantilla.

## Movimiento y coste

`Motion.cs` resuelve el oscilador amortiguado de forma analÃ­tica. La retargetizaciÃ³n conserva velocidad; la base de la propiedad conserva la pose visible hasta comenzar el reloj WPF. Los relojes son finitos y se retiran al terminar. Se respetan la preferencia de animaciones, el contraste alto y la reducciÃ³n de movimiento del sistema.

`IslandMotion` usa tiempo transcurrido real y deja de escuchar Rendering al asentarse. Su HWND permanece estable y la forma interior cambia sin recortes de notch. `PanelMotion` limita escala y recorrido de ventanas comunes. `GenieTransition` conserva proporciÃ³n y utiliza una sola textura temporal de la ventana propia, limitada a 1440 pÃ­xeles en su eje mayor y un mÃ¡ximo de dos transiciones. No se inyectan animaciones en aplicaciones ajenas.

`NotificationPullGesture` y `PrivacyScreen` despliegan la cortina. Durante el cierre se emplea una sola posiciÃ³n en coordenadas de pantalla, evitando la realimentaciÃ³n causada por medir respecto a una ventana que se estÃ¡ moviendo. La velocidad del dedo se conserva al soltar. La regiÃ³n nativa curva deja ver el escritorio real durante el recorrido y vuelve a rectangular al asentarse. Se libera cada regiÃ³n GDI que Windows no haya adquirido.

`WallpaperGlass` usa Ãºnicamente el fondo propio, desenfoque de la superficie y refracciÃ³n corta en el borde; los textos y controles permanecen nÃ­tidos. `LockGlass.hlsl` refracta el fondo de la escena dentro de los glifos del reloj. Hay alternativa sin shader. No se capturan otras apps para fabricar el bloqueo.

`LockNotice` conserva controles al cambiar el historial y anima su reacomodo. No se reinicia toda la pila como una imagen. Las entradas de los perfiles de prueba estÃ¡n identificadas como pruebas y nunca se insertan en el perfil real.

## Referencias revisadas

Los videos privados del usuario se inspeccionaron mediante secuencias temporales, incluidos los del 18/10, la grabaciÃ³n de MacFlow, las referencias del chat previo y los del 22/10 a las 20:13, 20:56 y 21:02. La revisiÃ³n final prioriza la captura de bloqueo de Mac: foto, reloj de vidrio y reproductor compacto. El movimiento de la cortina toma continuidad y seguimiento del gesto del video; la composiciÃ³n permanece de escritorio. No se distribuyen videos ni capturas privadas con el cÃ³digo.

- [Apple: Animate with springs, WWDC23](https://developer.apple.com/videos/play/wwdc2023/10158/): continuidad de velocidad y respuesta amortiguada.
- [Apple HIG: Motion](https://developer.apple.com/design/human-interface-guidelines/motion): movimiento con propÃ³sito y respuesta coherente.
- [Microsoft: WPF animation tips](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/animation-tips-and-tricks): ciclo de vida y costes de animaciÃ³n.
- [Microsoft: layout and design performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-layout-and-design): evitar trabajo de layout y objetos innecesarios.
- [Microsoft: 2D graphics and imaging performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging): lÃ­mites de texturas, imÃ¡genes y efectos.
- [WPF UI, arquitectura](https://github.com/lepoco/wpfui/blob/main/docs/architecture/cross-cutting/navigation.md): separaciÃ³n de servicios de navegaciÃ³n y presentaciÃ³n; no se incorporÃ³ el paquete.
- Se intentÃ³ consultar el efecto Magic Lamp de KWin en GitLab; no se obtuvo acceso verificable a su implementaciÃ³n. No se atribuye cÃ³digo a esa consulta.

## Pruebas y lÃ­mites

`--self-test` verifica persistencia, recuperaciÃ³n, actualizaciones, geometrÃ­a, fÃ­sica, avisos y servicios. `--modern-smoke` comprueba componentes reales en claro/oscuro, las 17 pÃ¡ginas, controles multimedia, isla, cortina y materiales con lectura de pÃ­xeles. `--renewal-27-test` aÃ±ade inversiones de movimiento, continuidad de Hola y notificaciones sin autenticaciÃ³n. `--curtain-27-test` verifica foto y mÃºsica compacta, posiciÃ³n al soltar, recorrido de cierre, inversiÃ³n, curvatura y liberaciÃ³n del HWND; permite grabar ventanas propias con un grabador externo.

Las pruebas usan perfiles aislados, no adquieren la barra de Windows ni cambian el fondo o el inicio automÃ¡tico. Las pruebas del instalador usan un destino temporal y verifican actualizaciÃ³n, cancelaciÃ³n, reversiÃ³n y conservaciÃ³n de archivos ajenos.

En la mediciÃ³n local del Dock con 13 iconos: mediana WPF 16,64 ms y percentil 95 18,12 ms; fueron 176 intervalos durante 3,02 s. Es una mediciÃ³n del render de WPF, no una garantÃ­a de presentaciÃ³n GPU. La ejecuciÃ³n fÃ­sica con dos monitores y DPI distintos, reconexiÃ³n, cÃ¡mara/micrÃ³fono y consultas Gemini con sesiÃ³n real quedan pendientes.

La cortina mantiene el fondo anclado a la pantalla y muestrea recortes de la foto ya cargada. Evita regenerar la escena completa en cada cuadro. El runner Windows Server 2022 verifica la alternativa y registra SKIP para el compositor de Windows 11; la prueba de GPU y píxeles permanece obligatoria en Windows 11 y se ejecutó localmente.
