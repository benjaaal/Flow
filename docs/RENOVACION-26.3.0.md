# MacFlow 26.3.0

## Cambios implementados

- Centro de control con conectividad compacta, dos reguladores verticales reales,
  controles multimedia y accesos funcionales. Se puede desplegar desde batería
  e indicadores arrastrando hacia abajo con cualquiera de los dos botones.
  El cierre hace un pequeño rebote y termina por arriba; no se envía al Dock.
- Notificaciones propias de MacFlow bajo la manzana, entrada descendente y
  retirada ascendente. No se reemplazan las notificaciones de otras aplicaciones.
- Barra y Dock independientes en cada monitor, escala por monitor y colocación
  en píxeles físicos. El Dock permanece en el monitor que muestra escritorio,
  aunque haya una aplicación abierta en el otro. Pantalla completa se evalúa
  por monitor y se distingue de una ventana maximizada.
- Hueco central real en la región de la barra y de su superficie de composición.
  Cápsula negra compacta, controles de música y temporizadores reales, retirada
  con rebote y preferencia para quitarla o recuperarla.
- Widgets reloj, batería y calendario más compactos al migrar el diseño anterior;
  selección de monitor persistente, colocación por DPI y separación por monitor.
- Configuración con navegación fija, iconos coherentes y tarjetas de apariencia
  clara, oscura y automática. No se añadió una barra lateral que se contraiga.
- Los iconos originales quedan acotados incluso si la imagen mide 1024 píxeles.
  Spotlight conserva filas pequeñas, texto legible y un campo con indicación.
- Minimizar desde los controles superiores pasa por la transición hacia el Dock.
  Las transiciones de cierre de paneles superiores se separan de esta ruta.

## Búsqueda y asistente local

Windows sola abre una búsqueda escrita: aplicaciones ordenadas por coincidencia,
cálculos, archivos indexados, acciones locales y búsquedas HTTPS en Chrome.
No inicia dictado. Una migración única habilita esta opción; después se conserva
la elección de Configuración. Windows+E/D y otras combinaciones pasan intactas.

El asistente entiende órdenes locales en español: abrir una aplicación del
catálogo, buscar en Chrome, minimizar, cerrar, escribir texto, volumen, ajustes,
centro de control y Administrador de tareas. Cerrar y escribir requieren revisar
y confirmar. El destino se comprueba con HWND y PID antes de enviar entrada.
No interpreta código de terminal ni ejecuta respuestas de una IA.

«Hey Siri» / «Oye Siri» usa una gramática de dos frases del motor de voz local de
Windows. Dictado completo sólo se activa después de la invocación o de pulsar
Hablar. Hay umbral de confianza, limitación de activaciones y tiempo máximo de
escucha de una orden. Se pausa al bloquear o desconectar la sesión, durante la
presentación, al suspender MacFlow y al usar la búsqueda escrita. El icono de
micrófono y su descripción indican la escucha activa. Se puede desactivar en
Sistema; no hay archivos de audio, transcripciones de fondo ni envío a Internet.

Requiere un motor español de System.Speech instalado y acceso autorizado al
micrófono en Windows. Si falta, se informa y sigue disponible el teclado. En el
entorno de verificación InstalledRecognizers no devolvió motores: no se afirma
que la activación acústica haya sido validada. No se instala ningún modelo ni
se cambia la privacidad del sistema automáticamente.

Puede leer texto accesible de la ventana de Chrome elegida mediante UI Automation
y pronunciarlo con la voz instalada de Windows. Busca secciones visibles de
resumen de Google/Gemini cuando existen. No genera respuestas de IA, no inicia
sesiones, no evita permisos de páginas y no garantiza que todas expongan texto.
La lectura tiene límite de caracteres y espera; una ausencia se informa.

## Animación del asistente y referencias privadas

Se analizaron fotogramas de los vídeos aportados por el usuario. El primero
incluye una lente que emerge, una cápsula negra con luces blancas mientras
trabaja y una reflexión inferior al responder. El segundo muestra un contorno
multicolor alrededor de la pantalla. Son referencias de movimiento aportadas,
no una certificación de funciones oficiales de una versión de iOS.

La implementación es vectorial propia: lente con capas y reflejo, seis luces
blancas animadas, cápsula que cambia de tamaño y reflexión de respuesta. Los
estados se enlazan a invocación, escucha, trabajo y resultado reales. No se alarga
una operación para simular que una IA está pensando. No hay vídeo de fondo ni
recursos de Apple copiados. Los sonidos son tonos PCM originales sintetizados.

El contorno ilumina sólo el monitor de la invocación: cuatro ventanas estrechas
con transparencia por píxel y paso de clics, sin una superficie opaca a pantalla
completa. Entrada desde el lateral, color lento a 30 Hz y salida finita; las luces
de la cápsula admiten hasta 60 Hz. No se promete una frecuencia real constante.
Los relojes continuos se desconectan al terminar, ocultarse o reducir movimiento.
La reflexión de respuesta deja de actualizarse tras 2,2 segundos.

## Estabilidad y límites

Se mantiene C# 5 / WPF / .NET Framework 4.8 x64, sin paquetes nuevos. Las
referencias System.Speech y UIAutomation vienen con el framework. Se conserva
la copia local del código anterior y la versión pública v26.2.0 para reversión.
No hay control remoto, servicios nuevos, modificación de Winlogon, inyección en
Explorer ni cambio de archivos protegidos. El instalador conserva preferencias.

El desenfoque nativo existente usa Windows.UI.Composition; el material no se
presenta como refracción óptica del escritorio. No se reescribieron las X ni las
barras de título de Chrome, Codex u otros procesos. MacFlow anima sus paneles y
las acciones compatibles solicitadas desde sus controles; los botones nativos
de otras aplicaciones siguen perteneciendo a Windows y a sus propietarios.

El inicio sigue registrado mediante acceso por usuario y el mutex evita
duplicados. Una sesión de pruebas no sustituye apagar e iniciar sesión realmente.
Dos pantallas se verifican con ventanas y regiones independientes y con fixtures
de coordenadas/DPI; este entorno sólo expone un monitor físico. Quedan pendientes
la prueba física de dos monitores, voz con micrófono, lectura de páginas reales
de Chrome/Gemini, reproducción y radios del equipo del usuario.

## Fuentes técnicas y de diseño

- [Apple: materiales](https://developer.apple.com/design/human-interface-guidelines/materials): jerarquía, legibilidad y material adaptado al contexto.
- [Apple: movimiento](https://developer.apple.com/design/human-interface-guidelines/motion): continuidad, respuesta y movimiento reducido.
- [Apple: widgets](https://developer.apple.com/design/human-interface-guidelines/widgets): tamaños contenidos y lectura rápida.
- [Apple: actividades en vivo](https://developer.apple.com/design/human-interface-guidelines/live-activities): estados compactos y expandidos, información real.
- [Microsoft: DPI por monitor](https://learn.microsoft.com/windows/win32/hidpi/dpi-awareness-context): coordenadas y superficies por pantalla.
- [Microsoft: reconocimiento asíncrono](https://learn.microsoft.com/dotnet/api/system.speech.recognition.speechrecognitionengine.recognizeasync?view=netframework-4.8.1): gramáticas, eventos y cancelación.
- [Microsoft: GrammarBuilder](https://learn.microsoft.com/dotnet/api/system.speech.recognition.grammarbuilder): lista restringida de frases de activación.
- [Microsoft: SessionSwitch](https://learn.microsoft.com/dotnet/api/microsoft.win32.systemevents.sessionswitch): detener escucha en bloqueo y desconexión.
- [Microsoft: texto accesible](https://learn.microsoft.com/dotnet/api/system.windows.automation.textpattern.documentrange): lectura de contenido expuesto por una aplicación.
- [Microsoft: SendInput](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-sendinput): restricciones de entrada y UIPI, sin eludir elevación.
- [ModernWpf](https://github.com/Kinnara/ModernWpf), MIT: referencia de controles WPF; no se añadió esta dependencia ni se copió código.

Los vídeos, capturas y perfiles privados de prueba no forman parte de la
publicación del repositorio. Resultados y medidas concretas están en el informe
local INFORME-26.3.0.md y las carpetas de verificación de la versión.
