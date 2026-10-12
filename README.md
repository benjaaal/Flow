# MacFlow

Aplicación nativa para Windows 11 de 64 bits: Dock, barra superior, paneles de cristal, buscador de aplicaciones y vistas de ventanas abiertas. Proyecto independiente, creado con C# y WPF sobre .NET Framework 4.8.

El instalador funciona por usuario, conserva las preferencias al actualizar y permite desinstalar. No modifica archivos críticos de Windows ni inyecta código en Explorer. Los controles de aplicaciones externas usan las funciones normales de Windows; no reemplazan sus barras de título.

## Compilar

En Windows de 64 bits con .NET Framework 4.8:

```powershell
./build.ps1 -RunTests -OutputDirectory ./artifacts
```

Se generan el instalador, la versión portable, el manifiesto de actualización y los informes de prueba. Las pruebas usan carpetas y ventanas propias; no alteran la configuración de Windows del usuario.

## Actualizaciones públicas

El workflow de GitHub Actions compila, ejecuta pruebas y prepara una publicación desde la rama principal, una ejecución manual o un tag que coincida con la versión. Incluye el enlace estable del canal dentro del instalador, por lo que el EXE descargado solo puede configurar su canal sin archivos del equipo del desarrollador.

Una versión ya publicada no se sobrescribe. Para una nueva publicación se incrementan AssemblyVersion, AssemblyFileVersion y la identidad del manifiesto.

También se puede usar un servidor HTTPS propio:

```powershell
./build.ps1 -RunTests -OutputDirectory ./artifacts -UpdateFeedUrl https://TU-SERVIDOR/macflow/MacFlow-update.json
```

El manifiesto y su instalador deben estar en la misma carpeta pública. La descarga valida tamaño, SHA-256, versión, producto y contenido del instalador. La instalación requiere la acción del usuario. SHA-256 comprueba integridad; no equivale a una firma comercial.

Descargas oficiales: https://github.com/benjaaal/Flow/releases. El canal se genera después de una compilación y pruebas exitosas en GitHub Actions.

## Licencia

Código bajo MIT: [LICENSE.txt](LICENSE.txt). Atribuciones en [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Las capacidades y limitaciones de Windows se explican en [README.txt](README.txt).

## MacFlow 26.0.3

Los iconos del Dock se amplían un poco más al acercar el cursor. Se utiliza el espacio ya reservado: conserva tamaño del Dock, posición, animación y contención, incluso durante el impulso de apertura. Este ajuste no incluye modificaciones de Widgets, Centro de control ni Stage Manager.

## MacFlow 26.0.2

- Se retiraron los accesos fijos a Aplicaciones y Pilas del Dock; siguen disponibles en la barra superior.
- Dock más compacto: iconos y accesos del mismo tamaño, con espacio para que la ampliación desplace suavemente los vecinos. La retargetización conserva la velocidad y no recalcula el layout cada cuadro.
- Cristal neutral con reflejos más finos; iluminación del puntero sin una segunda animación retrasada. Desenfoque nativo del compositor, sin capturas continuas del escritorio ni refracción óptica declarada.
- Familia de 26 símbolos vectoriales originales para Wi-Fi, Bluetooth, sonido, controles y navegación. Nuevos accesos de carpetas, aplicaciones, ventanas, Ajustes, concentración y vista rápida. Los iconos de programas externos conservan su identidad.
- Configuración con seis iconos específicos y transiciones entre controles reales; las páginas salientes dejan de recibir clics. Los menús se cierran localmente, los paneles se recogen hacia el Dock.
- Hola y la manzanita pueden mostrar tu fondo de Windows, con reflejos y un desplazamiento sutil de esa imagen dentro de sus trazos. El shader usa la imagen de esta pantalla de bienvenida; no refracta el escritorio ni otras aplicaciones. La imagen se decodifica fuera del hilo de interfaz y se limita a 1920 × 1200 puntos.
- Tras actualizar, Hola conduce a tres tarjetas de novedades y Comenzar. La bienvenida se registra cuando efectivamente carga; Escape permite salir.
- Se mantiene la reparación del inicio automático y el destino comprobado de la instalación. La ejecución después de reiniciar depende también de que Windows mantenga habilitado MacFlow en Aplicaciones de inicio.

Las ideas de movimiento se contrastaron con la guía Motion de Apple y los proyectos [magnification-effect](https://github.com/Kaww/magnification-effect), [Wave](https://github.com/jtrivedi/Wave) y [Motion](https://github.com/b3ll/Motion). Se implementan en WPF con código propio, sin un navegador ni dependencias de Swift. Los ejemplos web de [liquid-glass-react](https://github.com/rdev/liquid-glass-react) y [liquid-glass](https://github.com/samasante/liquid-glass) no permiten refractar el escritorio de Windows desde WPF.

## MacFlow 26.0.1

- Bienvenida sobre el fondo vivo del compositor: manzanita de cristal → Hola manuscrito → Iniciar.
- Silueta proporcionada de Simple Icons, iluminada con un shader original de máscara.
- 30 saludos locales: trazos manuscritos latinos y fuentes nativas para otros sistemas de escritura; transiciones suaves y dirección derecha a izquierda cuando corresponde.
- Escape funciona durante la entrada; cerrar detiene el ciclo de idiomas. Con animaciones desactivadas, la bienvenida es inmediata.
- Un poco más de luz y transparencia en el Dock, sin cambiar sus dimensiones ni la contención de los iconos.
- Cerrar o minimizar los paneles de MacFlow recoge material y contenido hacia el centro del Dock, incluso si éste está oculto. Restaurar recupera la posición y el tamaño originales. Las ventanas de otros programas mantienen las animaciones de Windows.
- Nuevo icono vectorial original inspirado en Ajustes de iOS, en Configuración y los accesos de configuración del Dock.
- Inicio automático por acceso nativo en la carpeta de inicio del usuario, con destino verificado a la instalación fija. Reparar inicio automático corrige rutas viejas; actualizar conserva una solicitud de inicio aunque su ruta estuviera rota. Windows mantiene el control para deshabilitarlo desde Aplicaciones de inicio.

El material aproxima el cristal mediante desenfoque del compositor y reflejos. El shader de la manzanita muestrea su propia máscara vectorial, sin refracción óptica del escritorio. No requiere un navegador, captura continua de pantalla ni servicios de traducción.

Prueba visual aislada: `MacFlow.exe --welcome-smoke RUTA`. Comprueba secuencia, idiomas, cierre, movimiento desactivado y geometría; las capturas corresponden a ventanas propias de prueba.

Prueba del recorrido nativo y restauración: `MacFlow.exe --panel-dock-smoke RUTA`. Comprueba destino físico, disminución de distancia y recorte, Dock inferior/lateral, cierre durante la entrada y posición restaurada. No altera ventanas de otros programas.

## Funciones incluidas desde 1.1.5

- Pilas de carpetas fijables en el Dock, con accesos a Descargas, Documentos y Escritorio.
- Vista rápida de imágenes y texto: Espacio en una pila o Ctrl+P en Spotlight > Archivos.
- Concentración con 25/45/60 minutos, pausa, persistencia y contador en la barra superior.
- Menú propio de escritorio en cristal: manzana > Tu escritorio o Ctrl+Alt+D.
- Iconos vectoriales originales y autoocultado general del Dock, configurable.

El menú de Explorer conserva su apariencia. Concentración no cambia No molestar de Windows. La vista rápida no ejecuta archivos ni usa extensiones externas. Las lecturas de carpetas y vistas previas se hacen en segundo plano y tienen límites de tamaño.
