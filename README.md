# MacFlow 27.2.2

Escritorio nativo para Windows 11 x64, construido con C# 5, WPF y .NET Framework 4.8. Continúa el proyecto MacFlow existente: Dock, barra superior, isla dinámica, Centro de control, Configuración, Spotlight, widgets, multimedia, notificaciones y asistente local.

Esta versión comparte una física de movimiento con continuidad de velocidad, mantiene proporciones al minimizar hacia el Dock y renueva materiales, interfaces y bienvenida. La pantalla visual usa el fondo de escritorio o una foto, reloj de vidrio y reproductor compacto. Desde la esquina superior izquierda se despliega el reloj con notificaciones sin bloquear Windows.

[Cambios y límites](docs/RELEASE-27.2.2.md) · [Arquitectura y referencias](docs/RENOVACION-27.2.2.md)

Compilar desde esta carpeta:

```powershell
./build.ps1 -OutputDirectory ../artifacts -UpdateFeedUrl https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json
```

`-Offline` exige la caché de dependencias verificadas. `-CoreOnly` compila la interfaz sin los motores nativos. `-NativeRuntimeDirectory <portable anterior>` permite reutilizar un runtime íntegro sólo si sus workers no cambiaron y sus modelos coinciden con los hashes fijados.

Pruebas aisladas: `MacFlow.exe --self-test <carpeta>`, `--modern-smoke <carpeta>`, `--renewal-27-test <carpeta>` y `--curtain-27-test <carpeta>`. Las pruebas visuales abren ventanas propias y perfiles temporales. No adquieren la barra de Windows ni cambian inicio automático, fondo o conexiones. `MacFlow-Setup-27.2.2.exe --setup-test <carpeta>` verifica instalación y reversión en una carpeta de prueba.

El bloqueo seguro y la autenticación de Windows siguen a cargo del sistema. La cortina visual no los reemplaza. El lector de avisos externos necesita identidad de paquete compatible y permiso del usuario; el EXE clásico conserva el historial propio. Los controles multimedia dependen de las sesiones que publique el reproductor. El asistente conserva su integración existente con Gemini en Chrome, que requiere sesión del usuario.

No incluye acceso remoto ni modo iPad. Ver LICENSE.txt y los avisos de terceros.
