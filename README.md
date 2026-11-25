# MacFlow 28 Final Edition

Entorno de escritorio nativo para Windows 11 x64. Incluye Dock, barra superior, Mission Control, Launchpad, Spotlight, widgets, una pantalla de privacidad y aplicaciones propias. Las ventanas de Windows siguen disponibles en el Dock y en Mission Control.

- **Menú principal.** Un clic abre el menú anclado a la manzanita. La apertura tiene un rebote corto y el material usa el desenfoque nativo de Windows cuando está disponible.
- **Finder.** Lista, iconos, galería y columnas. Spotlight busca en las carpetas que agregues desde Configuración → Preferencias.
- **Aplicaciones.** Calculadora, Calendario, Notas, Monitor de actividad, Finder, Configuración y Vista rápida. La Biblioteca permite abrirlas y fijarlas en el Dock. Papelera y Terminal abren las herramientas de Windows.
- **Asistente.** Órdenes locales por defecto. En Configuración → Privacidad podés elegir un modelo instalado en Ollama o habilitar Gemini en Chrome. Cerrar ventanas, escribir en otra aplicación y resumir pestañas requiere confirmación.
- **Recuperación.** Podés volver a Windows desde el menú de MacFlow. El instalador ofrece actualización o reparación y permite conservar los datos al desinstalar.

[Descargas de GitHub](https://github.com/benjaaal/Flow/releases) · [Cambios y límites](RELEASE-28.md) · [Arquitectura](docs/ARQUITECTURA-28.md)

## Compilar en Windows

Necesitás Windows x64 y .NET Framework 4.8. El script usa el compilador C# 5 de Windows y comprueba el SHA-256 de las dependencias nativas.

```powershell
./build.ps1 -RunTests -OutputDirectory ./artifacts -UpdateFeedUrl https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json
```

`-CoreOnly` compila el núcleo para iterar. `-Offline` usa la caché local y exige las imágenes de prueba en `build/model-tests` si también activás `-RunTests`. La aceptación de la versión se ejecuta mediante `artifacts/portable/MacFlow.exe --acceptance-28 <carpeta>` con un perfil aislado.

La gestión de escritorios virtuales pertenece a Vista de tareas y la autenticación al bloqueo de Windows. MacFlow conserva su propio historial de notificaciones. Las notificaciones de otras aplicaciones requieren permiso y una identidad de paquete compatible. Consultá la licencia y los avisos de terceros incluidos en la distribución.