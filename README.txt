MacFlow 26.5.0 — Windows 11 x64

Descarga: https://github.com/benjaaal/Flow/releases/tag/v26.5.0
Ejecutá MacFlow-Setup-26.5.0.exe y elegí Actualizar. Conserva preferencias.

CAMBIOS
- Isla vacía sin acciones al tocarla; actividad desplegada debajo de la barra.
- Barra más compacta, Centro de control reorganizado y galería visual de widgets.
- Ciclo de vida de ventanas y widgets corregido; cierre temprano de eventos y una sola alerta fatal.
- Voz local española incluida: Hey Siri, seguida de tu pedido.
  No requiere instalar reconocimiento español de Windows. Activala en Sistema.
  Windows debe permitir el micrófono a las aplicaciones de escritorio.
- Preguntas libres del asistente: Gemini en Chrome; requiere sesión y accesibilidad.
  Las acciones del equipo y el reconocimiento de voz se procesan localmente.
- Ctrl + Windows: Privacidad, con reloj alto, carátula del reproductor y avisos de MacFlow.
  Música y tareas continúan; mantiene pantalla y equipo despiertos mientras está abierta.
  Respeta apagado manual, tapa, bloqueo de Windows y batería crítica.
- PIN propio de 6–12 números; cinco intentos incorrectos activan una espera de 30 s.
- Reconocimiento facial EXPERIMENTAL propio, sin Windows Hello: primero creá tu PIN,
  elegí Registrar rostro y completá el movimiento. Luego usá Reconocer rostro.
  YuNet + SFace comparan tu registro; MiniFASNet comprueba presencia real y solicita un giro.
  Hay controles contra fotos y pantallas; una webcam RGB no garantiza rechazar todos los engaños.
  No equivale a Face ID de Apple. La cámara solo se abre al pulsar registrar/reconocer.
  Datos del rostro y PIN protegidos con DPAPI de tu cuenta; no se guardan fotos ni audio.
- Ctrl + Windows + Alt: recuperar el escritorio si olvidaste el PIN.
  Privacidad es una cortina recuperable de MacFlow; Win + L sigue bloqueando Windows.
- Pliegue: prueba manual independiente del interruptor, alternativa 3D sin shader 3
  y apertura diferida hasta desbloquear Windows. El movimiento físico requiere que
  Windows exponga sensor o avisos de tapa; una suspensión inmediata puede interrumpir el cierre.
- Seis demostraciones renovadas después de Hola; controles reales en escenas aisladas.

RECURSOS
Incluye modelos locales de voz y rostro. Solo los carga su proceso correspondiente;
la cámara se libera al terminar o cancelar el intento. El instalador es más grande
porque los modelos se incluyen para funcionar sin descargas en el primer uso.
Las bibliotecas y modelos conservan sus licencias: NATIVE-THIRD-PARTY.txt.
Diseño y recursos originales; no se distribuyen fuentes ni imágenes de interfaz de Apple.

ANIMACIONES DE APPS DE WINDOWS
MacFlow representa aperturas/cierres compatibles hacia el Dock sin inyectar código
ni reemplazar el compositor de Windows. Las aplicaciones protegidas, diálogos de
seguridad y algunos cierres no ofrecen una captura o evento utilizable.

RECUPERACIÓN
Salir desde la bandeja restaura la barra de Windows. MacFlow.exe --restore también
restaura la barra. Los procesos de voz/cámara terminan al salir. Actualizaciones:
Configuración → Sistema → Actualizaciones.

ACTUALIZACIÓN DESDE 26.4.1 O ANTERIORES
Descargá el instalador desde GitHub y ejecutalo manualmente: esas versiones tienen
un límite de descarga de 64 MB. La versión 26.5.0 amplía ese límite para sus modelos.

