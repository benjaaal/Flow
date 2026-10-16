# MacFlow 26.2.0 — decisiones y alcance

## Auditoría y reversión

Se conserva C# / WPF, .NET Framework 4.8 y compilación x64. El proyecto no necesita
un navegador incrustado, Electron, un runtime nuevo ni paquetes NuGet para esta
versión. Se revisaron el arranque, la composición, movimiento, catálogo, controles,
persistencia, ciclo de vida de paneles, instalador y canal de actualizaciones.
El acceso remoto quedó fuera por indicación del usuario.

Antes de cambiar el código se guardó una copia local comprimida del origen. GitHub
conserva la versión anterior en v26.1.4. El instalador prepara y verifica su payload
antes de aplicar y restaura los archivos anteriores si falla o se cancela la copia.
El cierre de MacFlow restaura la barra de Windows y su integración de escritorio.
No hay cambios a Winlogon, arranque, archivos protegidos, UAC ni Explorer.

## Material y movimiento

- El desenfoque sigue siendo real: Windows.UI.Composition, HostBackdropBrush y un
  recorte redondeado en una superficie propia. No se dibuja una foto del escritorio
  como fondo de los controles. No se afirma refracción óptica del escritorio.
- Material claro en Dock/navegación y algo más de densidad en paneles de texto.
  Reflejos finos con opacidad variable, sin añadir un rectángulo detrás.
- El modo ahorro usa reflejos vectoriales. Mantiene el desenfoque por GPU y evita
  un ShaderEffect fuera de pantalla por cada tarjeta. La iluminación por shader
  permanece disponible al desactivar ahorro; no se deshabilita la GPU del sistema.
- El camino nativo ya no inicializa la lente, blur y caché del antiguo capturador
  de fondo. Las muestras de contraste son pocos píxeles sin guardar imágenes.
  Se espacian cuando el tono está estable y reaccionan a eventos de ventana/cursor.
- Se reutilizan pinceles congelados con una caché acotada. Las ventanas de un mismo
  proceso comparten la consulta de ruta dentro de cada enumeración.
- Cierre y minimización completan su geometría antes de liberar la ventana, incluso
  cuando llega tarde un cuadro. Movimiento reducido también se aplica a botones y
  minimización. El Dock conserva sus dimensiones y la contención de los iconos.

## Producto e inicio

- Apariencia automática sigue la preferencia de aplicaciones de Windows mediante
  eventos; la suscripción se elimina al cerrar. Claro y oscuro conservan su elección.
- Centro de actividad propio: avisos reales de MacFlow, historial local acotado a
  60 entradas, agrupación de duplicados y borrado. No lee las notificaciones privadas
  de otros programas ni presenta registros de prueba como avisos reales.
- Centro de control más compacto con controles reales y desplazamiento discreto.
- Gesto local a la barra: clic derecho y arrastre hacia abajo en sus últimos 300 DIP.
  El panel sigue la distancia y completa o cancela al soltar. No hay hook global de mouse.
- Manzana con arbitraje del doble clic de Windows: clic simple abre menú, doble abre
  Configuración sin abrir primero el menú. IOS no es un botón. La tecla Windows sola
  queda sin acción por defecto, conservando combinaciones; Sistema permite cambiarlo.
- Configuración renovada: búsqueda accesible que navega a secciones reales, tarjetas
  consistentes y diagnóstico de inicio. Los controles conservan su implementación real.
  La última referencia de barra lateral inspira iconos uniformes y selección suave;
  la barra permanece abierta, como pidió el usuario. No se incorpora contracción.
  Administrador de tareas está disponible en Sistema y en el menú de la manzana,
  utilizando Taskmgr.exe de la carpeta de sistema de Windows, sin elevación forzada.
- Energía tiene símbolos vectoriales, jerarquía centrada, explicación legible y botones
  separados. Cancelar y Escape mantienen la sesión; las acciones reales siguen pasando
  por confirmación y APIs normales de Windows, sin forzar el cierre de documentos.
- Demostración posterior a Hola actualizada a estos cambios, con seis pasos manuales,
  cursor ilustrado y ventanas propias. No ejecuta energía ni cambia ajustes durante la guía.
- El catálogo se entrega a la interfaz sin esperar todos sus iconos. Los iconos
  se solicitan al necesitarlos; los servicios secundarios no bloquean esa entrega.
- El inicio usa un acceso directo documentado por usuario. Se comprueba destino,
  argumentos y carpeta de trabajo; el instalador conserva la elección y la retirada
  elimina el registro propio. No se usan tareas ocultas ni servicios elevados.
- Se elimina la pausa fija de 750 ms y se limita la espera de Explorer a 8 segundos.
  La barra original no se oculta hasta que existe un Dock pintado; la recuperación
  de Explorer sigue funcionando por separado.
- last-launch.json registra versión, modo manual/Windows, fase y tiempo hasta el
  primer cuadro. No registra nombres de ventanas ni credenciales.
- Bienvenida opcional tras iniciar sesión: fondo de pantalla, marca y Hola, con
  Continuar y salida automática breve. La bienvenida de actualización conserva
  sus pasos manuales. Ninguna sustituye la autenticación o pantalla segura de Windows.
- Preferencias con reemplazo atómico y copia anterior. Un principal dañado no puede
  sobrescribir una copia válida. Se valida NaN/infinito antes del renderizado.

## Cambios a partir de los videos del 16 de octubre

Se revisaron muestras de 16 fotogramas de cada video, además de capturas nativas
de los componentes propios. Los videos y fotos privados no forman parte del repositorio.

- Cierre y minimización curvos: una malla WPF de 441 vértices compartidos deforma
  una sola textura en memoria durante 380 ms. La cola se acerca primero al Dock;
  después sigue el cuerpo. No es una escala uniforme ni una animación de macOS
  ejecutándose en Windows. La textura es sólo la representación transitoria al salir;
  la interfaz normal sigue siendo controles reales y material de composición en vivo.
  Hay límite de dos transiciones, cuatro millones de píxeles por captura, reloj finito
  y liberación al cerrar/ocultar. Sin GPU, fuera del escritorio virtual, ante error
  o al interrumpir una entrada se conserva la transición geométrica ligera. No hay
  capturas continuas ni archivos de imagen guardados por la animación de producción.
- Widgets sin encabezado permanente, opciones al acercar el cursor, dato principal
  grande y calendario pequeño de fecha. El mes completo sigue disponible y conserva
  selección/navegación. Se corrigieron textos de estado que excedían el widget pequeño.
  La fecha del calendario cambia al pasar medianoche. Material individual claro, oscuro
  o transparente, persistente y accesible desde galería o menú.
  La batería pequeña muestra un anillo y dato principal sin apretar dos columnas.
  Cambiar tamaño/contenido tiene una transición breve de los controles reales.
  Corregido un tinte de panel regular que reaparecía al cambiar el tema del widget;
  ahora conserva el material claro de navegación sin añadir una placa opaca.
- Avisos propios con cristal real, entrada desde el borde derecho, resorte finito,
  pausa al pasar el mouse, descarte y un máximo de tres ventanas. El historial queda
  en Actividad. Los avisos del sistema y de otros procesos conservan Windows.
- Cápsula negra de actividad, compacta y expandible. Multimedia mediante eventos
  de Windows, temporizador/cronómetro de widgets, concentración y acciones realizadas
  desde MacFlow. No indica grabaciones ni tareas inexistentes. Pausar y cambiar pistas
  invoca el proveedor real. No sondea multimedia en reposo. La × guarda desactivación
  y retira la cápsula hacia arriba con un rebote corto; Barra superior la reactiva.

## Investigación

Consultadas el 15–16 de octubre de 2026. Las referencias de diseño son inspiración,
no recursos redistribuidos. No se ejecutó código de referencias externas ni se añadieron paquetes. Un intento de descargar el SDK oficial se descartó incompleto; no se instaló ni ejecutó.

1. [Apple HIG: Materials](https://developer.apple.com/design/human-interface-guidelines/materials):
   separación entre materiales de navegación y de contenido, densidad y legibilidad.
2. [Apple HIG: Motion](https://developer.apple.com/design/human-interface-guidelines/motion):
   transiciones breves, precisas, interrumpibles y movimiento reducido.
3. [Apple Design Resources: licencia](https://developer.apple.com/support/downloads/terms/apple-design-resources/Apple-Design-Resources-License-20230621-English.pdf):
   no se incorporan fuentes SF ni componentes propietarios de kits de Apple.
4. [Figma: kits de Apple](https://help.figma.com/hc/en-us/articles/24037833895831-Get-started-with-Apple-s-UI-kit):
   referencia de jerarquía y proporciones, sin copiar el kit a Windows.
5. [Behance: macOS Big Sur Control Center](https://www.behance.net/gallery/101069787/macOS-Big-Sur-Control-Center-Freebie):
   agrupación compacta de conectividad y controles. No se descargó su archivo.
6. [Dribbble: Jeniffer Deschamps](https://dribbble.com/jenifferdeschamps):
   exploraciones de Liquid Glass y separación de capas; no se reutilizan imágenes.
7. [Microsoft: rendimiento de WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/performance),
   [renderizado](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-taking-advantage-of-hardware),
   [objetos y eventos](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-object-behavior),
   [diseño y layout](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-layout-and-design):
   menos superficies, Freezables, desuscripción y transformaciones sin medir layout.
8. [Microsoft: Run/RunOnce](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys):
   inicio por usuario y ausencia de garantía de ejecución inmediata. Windows puede
   retrasar tanto Run como la carpeta Inicio; no se promete arranque antes del login.
9. [Composición Win32 de Microsoft](https://github.com/microsoft/Windows.UI.Composition-Win32-Samples):
   interoperabilidad del compositor; se conserva el backend propio ya existente.
10. [ModernWpf](https://github.com/Kinnara/ModernWpf) (MIT),
    [Motion](https://github.com/b3ll/Motion) y [Sprung](https://github.com/heyadam/sprung):
    referencia de estados y resortes, sin incorporar dependencias de otro framework.

Para la deformación se contrastaron [gráficos 3D de WPF](https://learn.microsoft.com/en-gb/dotnet/desktop/wpf/graphics-multimedia/3-d-graphics-overview?view=netframeworkdesktop-4.8)
y [rendimiento 3D](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance):
una malla pequeña, índices compartidos, textura congelada y pocas superficies.
La geometría es implementación propia; no se incorporó código de mods que inyecten
en procesos ajenos. [Apple documenta Genie como un efecto del Dock](https://support.apple.com/guide/deployment/dock-payload-settings-depef1fdf19/1/web/1.0).

La investigación inicial también consultó [WGC](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture),
[WebRTC W3C](https://www.w3.org/TR/webrtc/), [MDN getDisplayMedia](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getDisplayMedia)
y [sesiones OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html).
No se habilita ningún servidor ni control remoto en 26.2.0.

## Límites y comprobaciones pendientes

El entorno permite compilar, pruebas aisladas de controles y capturas de ventanas
propias. Una prueba de arranque desde Windows real necesita cerrar sesión o reiniciar;
no se hace sin que el usuario lo decida. El registro válido no demuestra por sí solo
que la aplicación arrancará en todo equipo. Windows conserva su posibilidad de
deshabilitar la entrada. No se escriben sus indicadores internos de aprobación.

Los botones de Apple de MacFlow controlan ventanas con APIs normales. No se reemplaza
la barra de título de otros procesos ni su animación nativa al pulsar su X. El cierre
pedido desde MacFlow respeta los diálogos de guardar/cancelar. Las limitaciones de
captura protegida, brillo de monitores y sesiones multimedia siguen siendo de Windows.

Los resultados de rendimiento son muestras locales, no garantía de 60 FPS. Los
resultados reales y las pruebas restantes se documentan junto al instalador.
El canal usa HTTPS, SHA-256, tamaño y validación de producto/versión/payload; no se
presenta como una firma digital de editor. No hay nuevas dependencias externas que
necesiten un archivo de bloqueo de paquetes.

## Comprobaciones locales del 16 de octubre

Compilación x64 e instalador: 214 comprobaciones de núcleo y 12 del instalador.
Las suites de sesión, renovación, paneles/Dock, bordes, Dock, barra superior,
módulos y cierre de ventanas terminaron con código 0. Se revisaron capturas de
Configuración, presentación, widgets, cápsula y avisos. Las pruebas usan perfiles
aislados y ventanas propias, sin cerrar aplicaciones del usuario ni cambiar radios.

La prueba de bordes utiliza una ventana opaca independiente y comprueba primero
que el fondo patrón está realmente visible; las once comprobaciones pasaron.
La prueba de ventanas confirma cierre, minimización y cancelación de cierre de
un documento. Las peticiones hechas desde MacFlow muestran una representación
transitoria sólo cuando Windows confirma la acción; guardar/cancelar se respeta.
Las X de otras aplicaciones y su apertura nativa conservan el comportamiento de Windows.

Comparación secuencial del mismo arnés, 134 aplicaciones detectadas, render tier 2:

| Muestra local | 26.1.4 | 26.2.0 |
| --- | ---: | ---: |
| Primer cuadro del Dock | 7797,3 ms | 2301,5 ms |
| Memoria privada | 114,5 MB | 90,2 MB |
| Memoria residente | 168,0 MB | 133,4 MB |
| CPU de máquina durante 8 s de reposo | 1,343 % | 0,024 % |
| Intervalo mediano de cuadros WPF | 16,78 ms | 16,77 ms |
| Percentil 95 del intervalo | 33,42 ms | 25,30 ms |

Son dos muestras de este equipo, sensibles a cachés, carga y controlador gráfico;
no son una garantía de arranque real ni de 60 FPS sostenidos. El arnés no habilita
las integraciones secundarias de multimedia ni cambia el escritorio de Windows.
Se conserva un acceso de Inicio por usuario válido hacia la instalación existente
26.1.4; falta comprobar el siguiente inicio de sesión después de actualizar.
No se probó mediante reinicio ni se reemplazó la pantalla de autenticación.
