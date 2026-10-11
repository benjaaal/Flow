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

Descargas oficiales: https://github.com/colanabenjamin-spec/Flow/releases. El canal se genera después de una compilación y pruebas exitosas en GitHub Actions.

## Licencia

Código bajo MIT: [LICENSE.txt](LICENSE.txt). Atribuciones en [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Las capacidades y limitaciones de Windows se explican en [README.txt](README.txt).
