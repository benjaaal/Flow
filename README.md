# MacFlow 26.1.4

Aplicación nativa para Windows 11 x64, con C# y WPF sobre .NET Framework 4.8.

[Descargar instalador](https://github.com/benjaaal/Flow/releases/tag/v26.1.4).
Actualizar conserva las preferencias y vuelve a abrir MacFlow. La instalación
es por usuario y el inicio con Windows es opcional.

## Revisión visual y de interacción 26.1.4

- El Dock usa la posición física del cursor: un hover retenido ya no lo mantiene
  encima de otra app. La espera al ocultarse es menor y el escritorio lo conserva.
- Barras auxiliares, como la de grabación, conservan el contexto de escritorio
  cuando no hay aplicaciones normales visibles; los widgets pueden permanecer.
- Configuración muestra una sola página desde el primer cuadro de cada cambio.
- El cristal combina blur recortado con una pequeña parte del fondo nítido;
  conserva reflejos y texto fuera del efecto, sin afirmar refracción real.
- El contraste reacciona a cambios de ventana y al cursor, con menos sondeo en reposo.
- Botones, notas del centro de control y calendario usan texto más legible.
  El calendario distingue el día actual con una selección roja.
- El volumen agrupa movimientos rápidos y conserva el último valor seleccionado.
- Buscador y Actualizaciones vuelven de minimizado sin crear otra ventana.
- Las piezas individuales sólo crean su lente y caché si existe una textura
  para usarlas; el compositor nativo no necesita esas capas de captura.

El inicio automático sigue usando un acceso por usuario. Su funcionamiento
tras un reinicio real queda pendiente de confirmación. El fallo AMD al terminar
la prueba aislada del Dock no se presenta como resuelto en esta revisión.

## Movimiento y cristal 26.1.3

- El Dock aparece con un único resorte continuo de rebote delicado. Se oculta sin
  una sacudida previa, y conserva su destino al cambiar de dirección.
- El resorte usa el tiempo real transcurrido: un cuadro tardío ya no ralentiza
  artificialmente el movimiento. Se eliminaron consultas LINQ en cada cuadro.
- Navegación con acercamiento del 1,5% y fundido suave sin recalcular el tamaño.
- Dock, paneles, botones y tarjetas tienen menos tinte opaco; conservan el blur
  recortado y los textos nítidos. No se agregan bordes ni se agrandan las ventanas.
- La composición sólo vuelve a enviar tamaño, clip y posición si cambian.
- Movimiento reducido de Windows también se respeta al pasar por los iconos.

Referencias consultadas: [Motion para iOS/macOS](https://github.com/b3ll/Motion),
[Sprung](https://github.com/heyadam/sprung) y
[Composition para WPF/Win32](https://github.com/microsoft/Windows.UI.Composition-Win32-Samples).
Implementación propia en C#/WPF, sin incorporar dependencias Swift o JavaScript.

## Correcciones 26.1.2

- Presentación completa: oculta barras, widgets y aplicaciones, con fondo continuo
  entre Hola, los cuatro pasos manuales y Bienvenido. Avanza sólo con tus botones.
- Letras de cristal con alfa real y reflejos neutros; sin relleno celeste.
- Material recortado con Windows.UI.Composition: sin placa rectangular ni borde extra,
  conserva tamaños y radios, desenfoca detrás y aparece en capturas de pantalla.
- Wi-Fi redibujado con proporciones compactas, arcos redondeados y corte transparente.
- Paneles propios, incluidos los del sistema y menús, cierran hacia el Dock.
- Cerrar desde los controles de MacFlow pide el cierre normal de una aplicación y,
  cuando se puede obtener una vista, anima una representación propia hacia el Dock.
  Si la app pide guardar o cancela, permanece visible y no se muestra una despedida falsa.
- El instalador cierra correctamente; no muestra una página final bloqueada.
- Inicio automático: espera a Explorer y su primer cuadro antes de crear la interfaz,
  con un límite de espera y registro de diagnóstico. La validación real de inicio
  con Windows requiere reiniciar después de actualizar.

Referencias visuales: [bienvenida de macOS Tahoe](https://support.apple.com/es-es/guide/mac-help/apd07d671600/mac)
y [sistema de símbolos de Apple](https://developer.apple.com/sf-symbols/).
Los controles y vectores de MacFlow son reales; no se distribuyen imágenes de interfaz
ni una fuente SF Symbols.

## Novedades

- Widgets reales: relojes, calendario interactivo, clima, batería, CPU/RAM/disco,
  música, notas, tareas, temporizador, cronómetro, aplicaciones y conexión.
  Tres tamaños, cuadrícula, arrastre, configuración y persistencia con respaldo.
- Centro de control renovado: radios reales, sonido, silencio y brillo; accesos
  compatibles a salida de audio, energía, luz nocturna, No molestar, captura y proyección.
- Stage Manager: miniaturas DWM, selección de ventanas, grupos con geometría
  guardada, lado, tamaño, etiquetas, modo automático/manual y ahorro de recursos.
- Iconos claros, oscuros, automáticos u originales en Configuración → Dock.
  Siete marcas con variantes vectoriales de Simple Icons; avisos de licencia incluidos.
- Después de actualizar: Hola → Continuar → demostración sobre los componentes reales →
  Bienvenido → Iniciar → escritorio. El cursor ilustrado no mueve el cursor del usuario.
  La bienvenida usa el fondo seleccionado, puede omitirse y aparece una vez por versión.

Se conservan el Dock, buscador, vista de ventanas abiertas, pilas de carpetas,
vista rápida, concentración, fondos, menú de energía y recuperación de Windows.
El material reutiliza el sistema Liquid Glass existente, sin agregar una imagen de interfaz.

## Compatibilidad

MacFlow no inyecta código en Explorer ni modifica archivos críticos o barras de título
externas. Abrir, activar, minimizar y cerrar aplicaciones utiliza las API de Windows.
Los paneles propios de sistema aparecen desde arriba y todos los paneles propios
cierran hacia el Dock. Desde los controles de MacFlow, una vista temporal de la
ventana externa puede acompañar el cierre exitoso hacia el Dock; cerrar con su X
nativa conserva la animación de Windows. Apps protegidas usan el cierre normal.

El brillo directo requiere WMI compatible. Los reproductores deben ofrecer sesiones
multimedia de Windows. Algunas aplicaciones protegidas no permiten miniaturas DWM.
Stage Manager recupera las ventanas disponibles y avisa si faltan o Windows impide moverlas.

El clima usa HTTPS de Open-Meteo/GeoNames con atribución, selección explícita de ciudad
y consultas limitadas. El endpoint gratuito es para uso no comercial; no tiene garantía
de disponibilidad. Los errores dejan un estado claro y se reintenta sin bloquear la interfaz.

Los widgets ocultos no sondean dispositivos; los temporizadores conservan su vencimiento.
Los paneles cerrados liberan suscripciones y vistas DWM. No se capturan ventanas
continuamente para Stage Manager. El cristal aproxima reflejos y desenfoque; no afirma
refracción óptica real del contenido del escritorio.

## Compilar y verificar

```powershell
./build.ps1 -RunTests -OutputDirectory ./artifacts -UpdateFeedUrl https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json
```

Pruebas aisladas adicionales:

```powershell
./artifacts/portable/MacFlow.exe --modules-smoke ./verification/modules
./artifacts/portable/MacFlow.exe --dock-smoke ./verification/dock
./artifacts/portable/MacFlow.exe --weather-test ./verification/weather
```

Las pruebas visuales usan ventanas y perfiles propios. Los controles de radios, audio
y brillo se verifican con proveedores aislados sin cambiar el equipo del usuario.
La comprobación de clima realiza consultas HTTPS reales y no guarda ubicación del usuario.

## Publicación

GitHub Actions compila y prueba antes de publicar el instalador, su configuración y
el manifiesto con tamaño y SHA-256. El enlace estable del canal se incluye en el paquete.
Una versión ya publicada no se reemplaza. Las futuras publicaciones deben aumentar
AssemblyVersion, AssemblyFileVersion y la identidad del manifiesto.

Proyecto independiente de Apple. Código MIT; atribuciones en THIRD-PARTY-NOTICES.md.
