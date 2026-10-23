# MacFlow 27.2.1 — entrega local

El notch cambiaba el tamaño del HWND durante cada cuadro y cambiaba su contenido antes de terminar la expansión. Ahora conserva un marco nativo estable, anima su superficie y vincula el contenido a la geometría. El resorte conserva velocidad al invertir el destino, tiene un sobrepaso inferior al 3% y deja de renderizar al asentarse. Los paneles superiores recorren entre 24 y 42 píxeles con un rebote corto; el Dock conserva su política de reposo para no reintroducir el ciclo con la barra de Windows. No se añaden animaciones a ventanas de otros procesos ni se altera Explorer.

La pantalla visual propia adopta la composición elegida por el usuario: reloj arriba, Tierra debajo y reproductor horizontal discreto, con carátula/título a la izquierda y controles a la derecha. Se muestra música únicamente en reproducción y avisos sólo si existen. La carátula permite ampliar música sobre un fondo desenfocado; el control expandido tiene ancho independiente de la imagen. Las inversiones rápidas cancelan el cierre anterior y recuperan el fondo sin capas residuales.

Tierra usa una esfera WPF 3D y textura NASA acreditada. Su acercamiento responde al gesto y al cierre. La dirección solar y la orientación cambian según la hora local, como interpretación visual sin ubicación ni efemérides. Se actualiza una vez por minuto mientras la pantalla propia está visible, sin un renderizador permanente. Tierra se retiró de la galería del escritorio y de su actualizador periódico. Una selección antigua de Tierra restaura el fondo guardado al siguiente inicio normal. No se aplica ni restaura el fondo real durante estas pruebas.

Se añadió una puerta de suspensión/reanudación que sólo presenta la cortina después de desbloquear Windows y una vez por ciclo. Puede desactivarse en Privacidad. No reemplaza la pantalla segura ni agrega otra autenticación al volver de suspensión. El PIN local de la cortina manual sigue siendo recuperable y no equivale a Windows Hello.

El lector de avisos externos usa UserNotificationListener, autorización explícita y datos transitorios. **La distribución EXE actual carece de identidad de paquete compatible: esta función figura como no disponible.** Habilitarla exige preparar/verificar una distribución MSIX con la capacidad correspondiente y consentimiento del sistema. No se accede a bases privadas, no se suprimen avisos nativos y no se instala ningún certificado. Los avisos propios funcionan; las previsualizaciones de otras apps en el bloqueo están desactivadas por defecto.

El asistente tiene una esfera independiente que pasa a banda con reflejos más definidos. Se conserva el TTS real y se detiene al cerrar. El centro de control usa un tinte menos opaco sobre desenfoque nativo; texto e iconos quedan separados del efecto. No se afirma refracción óptica real. Seda es un nuevo fondo original; no se distribuyen fondos ni animaciones de Apple.

## Verificación

- Compilación C# 5 x64 con build.ps1, dependencias en caché verificadas y modo Offline. El instalador se genera sin ejecutarlo.
- Núcleo: `qa-core/self-tests.txt`. Cubre configuración, controles de ventanas propias, actualización/integridad, doble instancia, cierre, Dock, recuperación y multimedia con proveedores controlados. No modifica el inicio automático ni la barra real.
- Interfaz y movimiento: `qa-release/checks.txt`, secuencias `notch-motion`, `assistant-motion` y `assistant-presence`, y capturas propias. Incluye reversión del notch, marco estable, contenido durante cierre, cierre/reapertura de música, iluminación por hora, cortina tras reanudación sintética, accesibilidad de movimiento reducido, navegación y WAV TTS real.
- La comprobación de paso de clics por el marco transparente pasó en una ejecución anterior (`qa-lock`); en la final puede quedar pendiente si otra ventana ocluye la fixture. No se considera aprobada si el informe indica PENDING.
- Las capturas WPF verifican geometría y contenido; no demuestran el desenfoque nativo. Su prueba independiente compara el contraste de píxeles de una ventana propia. Las capturas nativas pueden quedar ocluidas por ventanas del usuario.

Pendientes: suspensión/reanudación física, dos monitores reales con DPI distintos, pantalla completa, interacción con la barra real de Windows, autorización real de avisos externos en MSIX, micrófono físico y consultas reales a Gemini. Las mediciones de CPU/memoria son muestras de una fixture aislada, no un benchmark de inicio de sesión ni garantía de 60 FPS. Persisten advertencias de compilación de DTOs y la compatibilidad de System.Memory declarada por OpenCvSharp; no se ocultan como errores inexistentes.

No se ejecutaron instaladores, no se habilitó inicio automático y no se publicó esta compilación. Reversión del código: `work/backups/MacFlow-27.2-before-27.2.1.zip`; ajustes visuales intermedios en `work/checkpoints-27.2.1-lock`. El instalador anterior permanece en outputs/MacFlow-27.2.

## Referencias

- Vídeos: [auditoría cronológica](VIDEO-REVIEW-27.2.1.md). Dos referencias adicionales de 42,49 s y 81,34 s se decodificaron completas, con 1.274 y 2.440 cuadros respectivamente; sus paneles muestran origen claro, vidrio contenido y frenado corto. El vídeo de Tierra reenviado tiene el mismo SHA-256 que la referencia ya analizada.
- [Apple: Motion](https://developer.apple.com/design/human-interface-guidelines/motion), [Materials](https://developer.apple.com/design/human-interface-guidelines/materials): jerarquía, respuesta al gesto y claridad. Sin recursos propietarios copiados.
- [Microsoft: WPF 3D y rendimiento](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance): geometría y texturas compartidas congeladas, sin renderizado continuo innecesario.
- [Microsoft: Notification Listener](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/notification-listener): identidad, capacidad y consentimiento obligatorios.
- [NASA: Blue Marble Next Generation](https://science.nasa.gov/earth/earth-observatory/blue-marble-next-generation/base-map/): textura y atribución detalladas en THIRD-PARTY-NOTICES.md.
