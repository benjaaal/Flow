# MacFlow — instrucciones de proyecto para Codex

## Objetivo y prioridades

MacFlow es una aplicacion de escritorio para Windows que busca una experiencia visual inspirada en macOS, sin sustituir ni comprometer los componentes esenciales de Windows.

Prioridades, en este orden:

1. Integridad y funcionamiento normal de Windows.
2. Estabilidad, cierres limpios y compatibilidad con dos monitores.
3. Rendimiento en una PC con 8 GB de RAM.
4. Fidelidad y calidad visual, contrastada con referencias reales.
5. Funciones nuevas: modo iPad y MacFlow Remote, solo tras estabilizar la base.

## Datos del proyecto que debes verificar

- Es un proyecto WPF para .NET Framework; el codigo conocido apunta a C# 5 y compilacion x64.
- La compilacion se realiza con `build.ps1` mediante `csc.exe`, no con `MacFlow.csproj`.
- `MacFlow-update.json` es un manifiesto de actualizacion, NO el codigo fuente.
- La clase `TaskbarLease` puede estar declarada dentro de otro archivo; buscar el simbolo antes de asumir que falta `TaskbarLease.cs`.
- `App.cs`, `Desktop.cs`, `ActivityIsland.cs`, `Native.cs`, `DockMotion.cs`, `PanelMotion.cs`, `IslandMotion.cs` y los componentes de saludo requieren especial cuidado.
- Verificar todas estas premisas contra los archivos reales antes de actuar; no inventar dependencias, rutas ni resultados.

## Reglas permanentes

- Ante errores o regresiones, usar el skill `macflow-quality` en `.agents/skills/macflow-quality/SKILL.md`.
- Empezar por un diagnostico de solo lectura: causa, evidencia, archivos y metodo propuesto.
- Antes de editar, inspeccionar `git status` si existe Git y preservar cambios previos. Crear una copia recuperable o checkpoint acordado para cambios importantes.
- Cambiar solo lo necesario para una incidencia. No mezclar reparacion, rediseño y funciones nuevas.
- No ocultar ni modificar la barra de tareas de Windows durante las pruebas de estabilizacion. No modificar permanentemente registro, Explorador ni configuracion del sistema.
- No elevar privilegios, desactivar protecciones, matar `explorer.exe` ni configurar inicio automatico sin autorizacion explicita.
- No ejecutar MacFlow, instaladores, `-RunTests`, scripts con descargas, ni publicar actualizaciones sin revisar efectos y pedir permiso.
- No actualizar `MacFlow-update.json`, el feed remoto ni las versiones publicadas salvo solicitud explicita.
- Mantener compatibilidad con la version real del compilador. No introducir caracteristicas posteriores a C# 5 si sigue siendo el objetivo.
- Evitar temporizadores agresivos, bucles de renderizacion ininterrumpidos y ventanas siempre visibles que capturen entrada indebidamente.
- Atender ventanas WPF cerradas: no reutilizar una instancia tras `Close()`; retirar timers, eventos y callbacks. Evitar cascadas de MessageBox por errores globales.
- Considerar multiples monitores: coordenadas negativas, DPI, diferentes resoluciones, cambio de principal y conexion/desconexion.
- Declarar siempre que pruebas fueron ejecutadas de verdad y cuales quedan pendientes de ejecutar en Windows con dos pantallas.
- Antes de habilitar MacFlow Remote: arquitectura separada, autorizacion explicita en PC, autenticacion, cifrado, indicador de sesion y capacidad de desconexion inmediata. No exponer un puerto de control a Internet directamente.

## Criterios de aceptacion para correcciones

- Windows funciona normalmente mientras MacFlow se ejecuta y tras cerrarlo.
- Dock, isla dinamica y paneles no bloquean ni desplazan la barra de tareas ni otras aplicaciones.
- No aparecen multiples cuadros de error ni operaciones sobre ventanas WPF cerradas.
- El comportamiento con dos monitores queda probado o claramente marcado como pendiente.
- No se observan regresiones nuevas en las funciones que ya estaban estables.
- Reportar: causa confirmada o hipotesis, cambios exactos, pruebas, resultados, limitaciones y forma de revertir.

## Referencias visuales

Tomar como referencias los videos y capturas proporcionados por Benja, y la documentacion oficial de diseño de Apple. No afirmar una fidelidad exacta sin comparacion visual y pruebas. No implementar nuevas animaciones mientras los errores criticos sigan abiertos.
