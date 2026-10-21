# MacFlow 27.1.5

Aplicación nativa para Windows 11 x64, C# 5, WPF y .NET Framework 4.8.

Dock, barra, Centro de control sin contorno, Spotlight, Configuración, widgets, notificaciones, actividades en vivo, voz local y modo iPad. [Cambios, pruebas, referencias y límites](docs/27.1.5.md).

Compilación desde esta carpeta: `./build.ps1 -Offline -OutputDirectory ..`.
Genera `../portable/MacFlow.exe` y `../MacFlow-Setup-27.1.5.exe`.
La caché local es necesaria en modo Offline. Sin él, el script puede descargar paquetes oficiales con hashes fijados en BuildTools.

Pruebas aisladas: `MacFlow.exe --self-test <carpeta>`, `--modern-smoke <carpeta>` y `--stability-smoke <carpeta>`. No ejecutan el instalador. No usar `build.ps1 -RunTests` para esta verificación: el flujo heredado prueba el instalador y puede descargar ejemplos.

Modo iPad desde la manzana o Configuración → Sistema. Arrastrá arriba desde el borde inferior para Inicio; mantené para multitarea. Esc regresa a Mac. No ejecuta iPadOS ni sus aplicaciones.

No se reemplaza la seguridad de Windows. No hay acceso remoto. Consultá los avisos de licencias del proyecto.