MacFlow 1.0.5 — Windows 11 de 64 bits

INSTALAR Y PROBAR
Ejecutá MacFlow-Setup-1.0.5.exe. El asistente instala por usuario en
%LOCALAPPDATA%\Programs\MacFlow, crea el desinstalador y permite elegir
inicio con Windows y acceso directo. Usa .NET Framework 4.8, incluido en
Windows 11. No requiere administrador. El instalador no tiene firma comercial.
La versión portable se ejecuta desde portable\MacFlow.exe.

DOCK Y ESCRITORIO
Dock compacto con iconos reales, ampliación gradual de iconos vecinos,
indicadores de aplicaciones abiertas y una elevación suave al abrir una app.
Clic activa o abre; clic derecho permite fijar, quitar o elegir una ventana.
También permite Cerrar ventana, o elegir qué ventana cerrar si hay varias.
La vista de ventanas incluye un botón de cierre por ventana. Se solicita el
cierre normal y la aplicación puede pedir guardar o cancelarlo. No se terminan
procesos a la fuerza. Apps elevadas/protegidas pueden rechazar la solicitud.
Al abrir, el icono se eleva y recupera su forma suavemente. Al desaparecer
la última ventana de una app visible, su icono se contrae y se asienta antes
de actualizar el Dock. Si está fijado, permanece disponible. Cancelar el
cierre para guardar no se interpreta como una app cerrada. La ampliación
del cursor usa otra capa y no compite con estas animaciones. Los paneles
se retiran con una transición breve. La apertura ocurre fuera del hilo de UI.
Las ventanas de otros programas conservan las transiciones de Windows;
MacFlow no deforma sus ventanas ni intercepta su dibujo.
Arrastrá los iconos fijados para reordenarlos. Configuración > Dock permite
elegir tamaño, posición inferior/lateral y ocultamiento automático.
En el escritorio el Dock permanece visible aunque la ocultación automática
esté activada. En las aplicaciones se aparta y vuelve desde el borde.
El botón del extremo abre la cuadrícula de aplicaciones reales instaladas.
La barra superior muestra ventana activa, reloj, red, batería y volumen.
Junto al nombre de la ventana activa aparecen controles rojo, amarillo y verde
para cerrarla, minimizarla y ampliarla/restaurarla. Se valida su identidad antes
de actuar y se respeta la cancelación de cierre. No cubren los controles propios
de Chrome, Codex ni otros programas; no reemplazan globalmente sus títulos.
La manzana abre los menús; IOS abre la configuración de MacFlow; los indicadores abren controles reales
o la página de configuración oficial correspondiente de Windows.

BUSCADOR Y CONTROL
Alt+Espacio abre el buscador. Otros atajos disponibles en Configuración.
En el buscador, Enter abre y Ctrl+D fija/quita. Archivos: busca nombres locales
en Escritorio, Documentos y Descargas, sin subir archivos. El índice tiene
un límite de 15.000 archivos. También calcula expresiones como 2*(3+4).
El prefijo > muestra acciones permitidas de MacFlow, no ejecuta shell arbitrario.
El centro de control cambia volumen y silencio reales, envía controles al
reproductor y permite ajustar brillo cuando el hardware ofrece WMI compatible.
El icono Wi-Fi de la barra superior abre un panel propio de redes reales.
Wi-Fi y Bluetooth del centro de control abren el mismo panel con sus pestañas.
Actualizar consulta en segundo plano sin bloquear la interfaz. Redes guardadas
se conectan usando el perfil existente; una nueva contraseña se configura en
Windows. Sólo se informa conectada cuando Windows lo reporta al consultar.
Bluetooth muestra dispositivos clásicos guardados y su conexión real; los BLE
del inventario se identifican como registrados, sin afirmar que están conectados.
Los interruptores encienden o apagan Wi-Fi y Bluetooth mediante Windows.Devices.Radios,
sin administrador. Se solicita permiso sólo cuando tocás el interruptor y se
comprueba el estado efectivo; hardware o políticas pueden rechazarlo. No se
cambian radios automáticamente al iniciar, cerrar o ejecutar pruebas.
Agregar, emparejar y gestionar dispositivos abre Configuración de Windows.
MacFlow no cambia permisos de ubicación; si Windows impide leer
redes, el panel ofrece Configuración. No hay escaneo periódico en segundo plano.
Concentración abre el control oficial de Windows.
La barra superior se extiende por la pantalla, con 40 puntos de altura,
sin contorno exterior y con esquinas suaves. Incluye fecha/hora, ventana activa,
Wi-Fi, batería, volumen y acceso al centro de control. MacFlow abre sus ajustes.
La vista de ventanas ofrece miniaturas DWM y cambio de ventana.
Opcionales: widget de fecha/hora y selección de fondo de escritorio.
Configuración > Fondos: cuatro fondos originales (Aurora, Océano, Alba y
Noche), vista previa y selección de imágenes JPG/PNG/BMP de tu computadora.
Elegir una imagen la aplica con la API oficial de Windows y guarda la selección.
Se verifica el estado con una instancia nueva de IDesktopWallpaper, evitando
la información anterior que podía devolver la instancia ya existente.
MacFlow recupera tu selección al volver a iniciarse. Restaurar anterior la
desactiva; cambiar otras opciones no descarta el fondo guardado.
Los fondos y miniaturas se preparan fuera del hilo de interfaz y no se animan
en el escritorio. Se conserva el fondo previo para restaurarlo al pausar,
salir o desinstalar, si no lo cambiaste desde otra aplicación.

MATERIAL DE CRISTAL
El compositor de Windows produce transparencia y desenfoque del fondo real.
Se eliminaron la burbuja de luz añadida y el fondo extra detrás del cristal.
Se quitaron el contorno exterior y el marco del compositor; se conservan
las esquinas suaves. También se quitó el rectángulo interior de brillo que
producía un segundo borde. Apertura y cierre mueven todo el
panel y su material juntos, sin encoger el contenido sobre un fondo inmóvil.
El texto no se desenfoca. No es refracción óptica ni el motor de Apple.
No se usan imágenes de una interfaz.
Se utiliza una política de composición en las ventanas propias para reducir
el tinte del Acrylic. Es una API no documentada: si no está disponible, se
usa el backdrop DWM público de Windows 11. Alto contraste tiene alternativa
sólida. La apariencia también depende de los ajustes de transparencia de Windows.

COMPARTIR PANTALLA
MacFlow ya no excluye sus ventanas de capturas y grabaciones, ni captura el
escritorio continuamente para dibujar el cristal. La captura del escritorio
completo incluye las superficies visibles. Una grabación de una única app
puede omitir las ventanas independientes del Dock y la barra superior.

RENDIMIENTO
WPF nativo, sin navegador ni servidor. Catálogo e iconos se cargan en segundo
plano y usan caché. El desenfoque lo resuelve el compositor de Windows;
no se capturan imágenes periódicas del escritorio para dibujar el material. La ampliación de iconos y
el deslizamiento usan controles basados en tiempo, sin cambios de tamaño del layout
en cada movimiento. Al estabilizarse, dejan de escuchar el reloj de animación.
El objetivo es 60 cuadros por segundo, condicionado por GPU y equipo;
no se garantizan 60 FPS sostenidos en todos los equipos.
Los iconos permanecen dentro de la burbuja, incluso al abrir una aplicación.
El tamaño inicial se aumenta cuatro píxeles una sola vez; después se respeta
lo elegido en Configuración. El Dock se mantiene visible si hay espacio libre
en su posición debajo o al costado de la ventana activa y al mostrar escritorio.
Tocar MacFlow en la barra superior abre Configuración. Abrir de nuevo el EXE
solicita los ajustes de la sesión existente, restaurándolos si estaban minimizados.

PLIEGUE DE TAPA
Configuración > Sistema permite activar la transición y probar el pliegue.
Se consulta HingeAngleSensor: si Windows expone un sensor, el movimiento sigue
el ángulo con suavizado independiente de la frecuencia. Si no hay sensor,
GUID_LIDSWITCH_STATE_CHANGE informa sólo apertura/cierre, no cada grado.
La animación usa una única captura en memoria antes del overlay, proyección
alrededor del borde inferior y desenfoque progresivo en un shader GPU.
No se guardan ni transmiten esas capturas. Se libera la textura al terminar;
se cancela al suspender, bloquear, pausar o salir. No cambia las opciones
de suspensión del equipo ni evita que Windows cierre/suspenda la sesión.
Sólo se utiliza la pantalla principal. No se promete seguimiento continuo
en notebooks sin sensor compatible. Sin shader 3, el efecto se omite.

MENÚ WINDOWS Y ENERGÍA
Pulsar y soltar la tecla Windows sola abre el menú de MacFlow. Incluye
aplicaciones, buscador, configuración, Apagar, Reiniciar, Suspender,
Cerrar sesión y Bloquear. Ctrl+Alt+M y el menú superior también lo abren.
Las combinaciones Win+E, Win+L y otras se dejan pasar al sistema.
Configuración > Sistema > Menú con la tecla Windows permite desactivarlo.
El listener usa hooks de bajo nivel en un hilo propio, sin inyectar código
en Explorer, sin cambiar el registro y sin guardar texto ni historial.
Al pausar o salir se desconecta. Si Windows lo impide (por ejemplo en una
ventana elevada), se conserva Inicio nativo y quedan los accesos de MacFlow.
Las acciones de energía requieren confirmar en un panel propio. Cancelar
es el foco inicial. Las solicitudes no fuerzan el cierre de otras apps;
Windows puede pedir guardar o rechazar la operación por permisos/políticas.
Las pruebas no apagan, reinician, bloquean ni suspenden el equipo.

RESTAURAR Y DESINSTALAR
Desde la bandeja: Pausar/reanudar o Restaurar Windows y salir.
Ocultar la barra original combina SHAppBarMessage (libera su espacio) con
ShowWindowAsync para ocultar sus superficies mientras MacFlow está activo.
Se filtran exclusivamente las ventanas de barra de tareas de explorer.exe.
Un evento de aparición vuelve a ocultarlas y una comprobación breve respalda
el evento. No se cambian estilos, archivos de Explorer ni ventanas de apps.
La visibilidad original se guarda antes de ocultar cada superficie y un
supervisor la restaura junto con el estado previo si MacFlow termina.
Referencias: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindowasync
https://learn.microsoft.com/en-us/windows/win32/shell/abm-setstate
Se respetan cambios posteriores que hagas por tu cuenta. MacFlow.exe --restore
reintenta restaurar la barra. El fondo elegido por MacFlow se restaura al
pausar, salir o desinstalar si todavía es el fondo que aplicó MacFlow.
Desinstalar: Configuración de Windows > Aplicaciones > MacFlow, o el acceso
Desinstalar MacFlow del menú Inicio. Podés conservar preferencias. Sólo se
eliminan archivos propios; se conservan archivos ajenos en la carpeta.

ALCANCE Y LÍMITES
Se retiró la capa de botones Apple sobre programas ajenos a pedido del usuario.
MacFlow no tapa sus controles originales. No existe un archivo universal para
cambiar esos controles: cada programa dibuja su marco. MacFlow no parchea
programas ni Windows. Los botones Apple pertenecen a las ventanas propias.
Windows puede rechazar acciones sobre aplicaciones elevadas/protegidas.
Minimizar programas de Windows: clic derecho en su icono del Dock >
Minimizar ventana. Si tienen varias ventanas, también se ofrece Minimizar
todas sus ventanas. En «Tus ventanas», el control amarillo minimiza cada
ventana. Tocá el icono del Dock para recuperar una ventana minimizada;
si hay varias, elegila en el menú. Se usa ShowWindowAsync de Windows,
con validación de la identidad del proceso y sin modificar sus bordes.
Aplicaciones y Configuración tienen controles estilo Apple: rojo para
cerrar, amarillo para minimizar y verde para ampliar/restaurar. Al pasar
el cursor aparecen sus símbolos; el área de clic es mayor que cada círculo.
Aplicaciones se cierra con el rojo, Escape, un segundo clic en su acceso
o al cambiar a otra ventana. Minimizar conserva el panel: tocá Aplicaciones
en el Dock para recuperarlo, o usá Alt+Tab. El cierre tiene un respaldo
breve si se interrumpe la animación.
Configuración > Dock > Apartar el Dock en Codex: el Dock baja suavemente
después de 1,2 segundos sin interacción para dejar libres los botones.
Acercá el cursor al borde inferior para recuperarlo. La detección sólo
consulta el proceso activo, sin leer conversaciones ni controles de Codex.
Dock/barra sólo en monitor principal. Windows conserva Win, Alt+Tab, sus
notificaciones y escritorios. Configuración > Barra superior > Borde del Dock protegido añade una franja
propia de 4 píxeles al borde inferior para reducir la activación accidental
de la barra original con el cursor. Se retira al pausar, salir o entrar
en pantalla completa y se puede desactivar. No renderiza cristal ni usa
un temporizador propio. Si Windows pone otra ventana encima de la franja,
se recupera la posición superior de la franja propia sin activar ventanas
ni cambiar estilos de Explorer. La ocultación adicional conserva la barra
nativa disponible al pausar o salir. Cambios de Explorer, restricciones o
otras utilidades de escritorio pueden interferir; no se garantiza bloquear
todo comportamiento del sistema. La barra superior muestra el título
activo; no exporta menús internos de apps ajenas. No se inyecta código en
Explorer, no se alteran estilos de ventanas ajenas ni archivos críticos.
Windows puede impedir activar ventanas elevadas/protegidas. Algunas apps
Store pueden aparecer además con el icono de su proceso mientras están abiertas.
Actualizaciones: gestor con progreso y paquetes locales verificados. El canal
remoto de GitHub requiere un repositorio publicado, todavía no configurado.

DATOS Y VERIFICACIÓN
Preferencias y log rotativo en %LOCALAPPDATA%\MacFlow. El log contiene errores
técnicos, no documentos. Sin telemetría. La conexión a GitHub sólo se utiliza
si configurás un repositorio y solicitás una comprobación de actualizaciones.
Las pruebas ejecutadas usan perfiles y carpetas aislados; no modifican el
registro de inicio, barra de tareas ni el fondo del usuario. Se verificaron
persistencia, catálogo, ventanas, lectura de audio, iconos, cálculo y pins;
extracción/actualización/desinstalación del paquete y conservación de archivos.
La prueba visual abre paneles reales y guarda capturas de sus propias ventanas,
sobre una ventana de fondo de prueba. No son imágenes de una interfaz simulada.
La instalación real en HKCU, accesos directos y arranque al iniciar sesión
requieren una comprobación posterior fuera del modo aislado.

COMPILAR
Desde source: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -RunTests
Opcional: -OutputDirectory RUTA. No necesita SDK adicional ni descarga NuGet.
El shader se compila localmente con d3dcompiler_47 y se incorpora al ejecutable.
MacFlow.exe --self-test RUTA: comprobaciones de lectura y datos.
MacFlow.exe --connections-smoke RUTA_NUEVA: capturas de paneles reales con
datos de prueba inyectados, sin consultas de redes ni cambios de conexión.
MacFlow.exe --smoke RUTA_NUEVA: prueba visual completa y consumo en reposo.
MacFlow.exe --preview RUTA_NUEVA: deja abierta una sesión aislada para revisar.
MacFlow-Setup-1.0.5.exe --setup-test RUTA: ciclo aislado del instalador sin HKCU.
El workflow .github/workflows/windows.yml está preparado para un repositorio
cuyo directorio raíz sea source. No se ha publicado ni ejecutado en GitHub.

BIENVENIDA
Después de actualizar, MacFlow muestra Hola escribiéndose a pantalla completa
y un botón Iniciar para continuar al escritorio. Aparece una sola vez por
versión nueva, sin repetirse al reiniciar. Se ejecuta después de entrar
en tu sesión de Windows; no reemplaza el arranque ni el inicio de sesión.
Configuración > Sistema permite desactivar el saludo tras actualizar o verlo de nuevo.
La escritura termina y libera su reloj; no mantiene animaciones permanentes.
Las letras se adaptaron del proyecto MIT kumailnanji/letters de GitHub.
Se respetan la opción de animaciones y el tamaño elegido para el Dock.

ACTUALIZACIONES
Configuración > Sistema > Abrir actualizaciones, o el menú de la bandeja,
abre el gestor propio. Muestra versión, estado, progreso y acciones reales.
Elegir paquete permite seleccionar directamente el .exe, o MacFlow-update.json junto a su instalador.
Se rechazan versiones iguales o anteriores, otros productos, rutas fuera del
paquete, tamaño incorrecto, hash alterado y archivos sin el recurso de instalación.
Se prepara una copia privada verificada antes de ofrecer Actualizar ahora.
La integridad se vuelve a comprobar al abrir el asistente. El asistente conserva
las preferencias y cierra MacFlow normalmente al confirmar la instalación.
No se instala silenciosamente. Cerrar el gestor cancela la descarga y elimina
sus archivos parciales, preservando paquetes preparados anteriormente.
El canal opcional usa GitHub Releases con HTTPS y el SHA-256 informado por GitHub.
Una vez configurado, la búsqueda automática consulta al iniciar y cada 12 horas,
y sólo avisa si encuentra una versión nueva. No descarga ni instala por su cuenta.
Todavía no hay un repositorio/release publicado de MacFlow: las actualizaciones
por internet requieren publicar ese canal. La actualización local es funcional.
Compilar genera el instalador y MacFlow-update.json con versión, tamaño y SHA-256.
El workflow de GitHub puede publicar ambos al recibir un tag vVERSION coincidente.

Código MIT: LICENSE.txt. Atribuciones: THIRD-PARTY-NOTICES.md.

Controles y animaciones: Tus ventanas incluye tres botones por aplicación:
rojo cierra con la solicitud normal (permite guardar/cancelar), amarillo
minimiza y verde amplía/restaura cuando la ventana lo admite. El Dock también
ofrece esas acciones. Sus iconos reaccionan al cierre confirmado, incluyendo
una de varias ventanas de la misma app. Los paneles de MacFlow se desplazan
y se desvanecen juntos al cerrar/minimizar y reaparecen al restaurar. Windows conserva la animación
de las ventanas externas. No se superponen botones ni se parchea su marco.

CAMBIOS 1.0.3
Una única superficie ocupa cada ventana: el antiguo margen exterior ahora
forma parte del espacio interior. Esto elimina el doble borde sin desplazar
ni agrandar los controles. Un shader de iluminación direccional añade reflejos
finos en algunas esquinas. No refracta el escritorio ni desenfoca los iconos.
Los reflejos del cursor sólo animan durante la interacción; no hay un reloj
permanente para el shader. El material y las tarjetas adaptan el tinte.
Para el contraste se leen cinco píxeles del margen propio cada 400 ms,
sin guardar imágenes ni realimentarlas al cristal. Se omiten zonas tapadas
por otras ventanas; los umbrales tienen histéresis para evitar parpadeos.
Aplicaciones usa una cuadrícula con buscador y filtros arriba, sin panel
lateral. El centro de control agrupa conexiones a la izquierda, multimedia
a la derecha y controles debajo. Los botones conectados conservan contraste.
Apertura: resorte amortiguado de 320 ms; cierre: retirada breve sin rebotes
exagerados. Los botones responden con una curva de resorte de 240 ms.
El menú superior tiene una marca vectorial en forma de manzana: no depende
del carácter privado F8FF, que suele faltar en las fuentes de Windows.
Al iniciar con Windows, MacFlow espera a que Explorer exista y que el Dock
haya dibujado su contenido antes de ocultar la barra original. Reintenta una
vez por segundo si Explorer tarda o falla; no desactiva tu preferencia.
Si Explorer se reinicia, recupera las barras y el espacio de trabajo.
El Dock se mantiene visible durante los primeros segundos del inicio y
siempre en el escritorio. Si algo falla, se restaura la barra de Windows.
Estas rutas se probaron con estados simulados; no se reinició el equipo.
CANAL DE ACTUALIZACIONES DE ESTA COMPUTADORA
Al instalar desde una carpeta con MacFlow-update.json y el instalador
correspondiente, el asistente verifica su SHA-256 y vincula esa carpeta
sin cambiar tus otros ajustes. No requiere una cuenta ni un servidor.
MacFlow comprueba allí versiones nuevas al iniciar; un observador de archivos
avisa cuando se reemplaza el manifiesto, con agrupación de eventos para
no leerlo mientras se está escribiendo. Al desactivar la búsqueda o salir,
se detiene el observador. El canal GitHub continúa siendo opcional.
Actualizaciones > Buscar detecta la versión nueva; Preparar actualización
verifica una copia privada y Actualizar ahora abre el asistente.
No instala sin tu confirmación. Si el asistente se abre desde su copia
preparada, conserva el canal original. Mantené la carpeta vinculada.
El canal local no es una descarga por internet ni una release publicada.
La lectura de contraste del cristal ocurre fuera del hilo de interfaz,
con una sola lectura pendiente por ventana y descarte de muestras si se mueve.
NOVEDADES 1.0.3
Dock a 5 puntos del borde, zoom mayor sin desbordar y resorte con velocidad continua.
Autoocultado en todas las apps, incluidas ventanas pequeñas. Visible inmediatamente en escritorio.
Cristal sin sombra secundaria dibujada, más reflejo en el Dock. Cierre con retirada del material nativo.
Iconos propios de Explorador, Configuración y MacFlow, sin cambiar iconos del sistema.
Manzana > Ventanas abiertas y Carpetas. Clic derecho en Dock > Ver sus ventanas filtra por app.
Tres fondos nuevos generados e incluidos: Aqua, Medianoche y Sierra; no requieren descarga.
Multimedia muestra título, artista y reproducción de las sesiones compatibles de Windows.
Los controles se habilitan según lo que admite el reproductor; no simulan teclas globales.
Batería vectorial con nivel real, carga y ahorro, en lugar de un carácter rectangular.
Actualización: fondo negro en todos los monitores, manzana y barra sin textos.
Preparación de 120 segundos antes de guardar cambios. Windows tres veces seguidas
(sin combinar con otras teclas, dentro de cuatro segundos entre pulsaciones) o Esc cancela.
Los archivos anteriores se restauran si se cancela durante la copia; la fase final
de registro no se interrumpe. El asistente advierte estos pasos antes de empezar.
Los botones de otras apps no se reemplazan: se controlan desde la barra de MacFlow.

CORRECCIONES 1.0.4
La animación de los botones sólo cambia su cara; el área de clic queda fija.
El Dock no reconstruye sus botones mientras se está pulsando sobre él y al
hacer clic valida la lista actual de ventanas. El icono propio abre Configuración.
La configuración conserva la carpeta y el enlace del canal de actualizaciones.
Actualizaciones > Elegir paquete acepta el instalador .exe sin manifiesto,
incluso renombrado o recibido desde otro equipo. Verifica versión, producto,
payload e integridad de su copia antes de abrirlo; no es una firma comercial.
También se puede abrir directamente el nuevo instalador para actualizar.
El canal HTTPS opcional apunta a MacFlow-update.json, con el instalador en
la misma carpeta pública. La comprobación combina los canales disponibles,
y un canal local perdido no impide consultar uno en línea configurado.
Para publicar: build.ps1 -UpdateFeedUrl https://TU-SERVIDOR/RUTA/MacFlow-update.json
Subir el instalador y el manifiesto juntos. No hay un servidor público configurado.
Hola ocupa los límites físicos completos, incluida la zona de la barra de tareas,
y cubre monitores adicionales. Iniciar o Esc cierra toda la bienvenida; la
transición desvanece el contenido manteniendo el fondo a pantalla completa.
No cambia el tamaño ni el estilo del Dock o de los demás paneles.
PREPARACION DEL CANAL PUBLICO 1.0.5
Al compilar con -UpdateFeedUrl, el enlace HTTPS se incluye dentro del EXE.
Funciona tanto al instalar el EXE suelto como con manifiesto. Los ajustes de
un usuario que ya eligió otro canal se respetan. La búsqueda automática puede
desactivarse. El workflow configura el enlace estable de Releases del repositorio
real; los instaladores se resuelven desde ese enlace aunque GitHub redirija la
descarga del manifiesto a un archivo temporal. Falta conectar un repositorio
accesible y publicar la primera versión. No hay una URL ficticia de producción.