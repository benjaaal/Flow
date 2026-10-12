# MacFlow 26.1.1

Aplicación nativa para Windows 11 x64, con C# y WPF sobre .NET Framework 4.8.

[Descargar instalador](https://github.com/benjaaal/Flow/releases/tag/v26.1.1).
Actualizar conserva las preferencias y vuelve a abrir MacFlow. La instalación
es por usuario y el inicio con Windows es opcional.

## Correcciones 26.1.1

- El instalador cierra antes de iniciar MacFlow; elimina la pantalla final bloqueada.
- Hola espera Continuar. La demostración espera Siguiente y añade Atrás y Ver de nuevo.
- Configuración permite repetir Hola y la demostración. Una versión nueva los vuelve a mostrar.
- Se eliminan el marco nativo y la sombra exterior adicional de las ventanas propias.
  El desenfoque del compositor todavía puede extenderse a las esquinas del rectángulo;
  no se afirma un recorte óptico completo. Se conserva el radio y tamaño del cristal.

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
Las ventanas externas mantienen su animación nativa; los paneles propios de sistema
se desplazan desde arriba. Las demás ventanas propias conservan su recorrido hacia el Dock.

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
