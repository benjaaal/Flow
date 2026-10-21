---
name: macflow-quality
description: Diagnostica y verifica correcciones de bugs, estabilidad, barra de tareas, animaciones y multimonitor en el proyecto WPF MacFlow. Usar cuando se investiguen errores, se cambie codigo de ventanas o se preparen pruebas de una correccion.
---

# MacFlow Quality — flujo de reparacion verificable

Aplicar las instrucciones de `AGENTS.md` del proyecto. La evidencia del repositorio prevalece sobre suposiciones o versiones anteriores del proyecto.

## 1. Orientacion y reproduccion

1. Ubicar la raiz real del codigo y examinar estado Git, `build.ps1`, fuentes relacionadas y herramientas disponibles; no asumir que hay `.csproj`.
2. Pedir o localizar pasos concretos para reproducir el error; distinguir hechos, sospechas y sintomas todavia no reproducidos.
3. Identificar la ultima version funcional, si existe, sin sobrescribir ni restaurar archivos automaticamente.
4. Para problemas de Windows: determinar si la incidencia desaparece al cerrar MacFlow. Este comportamiento ya fue reportado por el usuario, pero verificarlo de nuevo tras cambios.
5. Si falta el codigo completo o una herramienta esencial, detener conclusiones definitivas y listar lo faltante.

## 2. Diagnostico primero

- Buscar simbolos y llamadas relacionadas, no depender de nombres de archivo presuntos: `TaskbarLease`, `ShowWindow`, `SetWindowPos`, `Topmost`, `WindowInteropHelper`, `CompositionTarget.Rendering`, `DispatcherTimer`, `Close`, `Visibility`.
- Comprobar el ciclo de vida de ventanas WPF y la cancelacion de callbacks al cerrarlas.
- Revisar carga de CPU/GPU, temporizadores y eventos solo con mediciones disponibles; no atribuir lag sin evidencia.
- Revisar calculos por monitor (posicion, DPI, area de trabajo, barras de tareas, hotplug).
- Entregar un resumen con archivo/metodo, evidencia, severidad, hipotesis y reparacion minima sugerida. Esperar aprobacion antes de cambios de riesgo.

## 3. Correccion acotada

1. Hacer un checkpoint recuperable de los archivos afectados y preservar todo cambio de usuario.
2. Crear una reproduccion o prueba de regresion cuando sea viable.
3. Modificar el menor numero posible de componentes.
4. No agregar interfaces, efectos, servicios remotos ni funciones ajenas a la incidencia.
5. No instalar dependencias, ejecutar instaladores, iniciar MacFlow ni cambiar Windows sin autorizacion pertinente.

## 4. Verificacion

- Verificar sintaxis y compatibilidad con el compilador real (C# 5 si sigue aplicando).
- Revisar el contenido de `build.ps1` ANTES de cualquier ejecucion; podria crear instalador/manifiesto o descargar dependencias. La ausencia de `-RunTests` no implica que todos sus efectos sean inocuos.
- Si se autoriza compilacion, registrar comando, salida, advertencias y si se ejecuto o no el programa. No afirmar pruebas de interfaz en Windows si no se hicieron.
- Para validacion en dos pantallas: probar distintas resoluciones, DPI, posiciones izquierda/derecha, cambio del principal, modo pantalla completa y desconexion del monitor.
- Probar al menos varias aperturas/cierres de Dock, isla dinamica, paneles y MacFlow; anotar si se libera la barra de tareas y no quedan ventanas residuales.
- Comparar visualmente transiciones solo despues de que estabilidad y rendimiento esten aceptables.

## 5. Informe obligatorio

Responder de forma breve con:

1. **Causa:** confirmada o probable, con evidencia y ubicaciones.
2. **Cambios:** archivos y metodos precisos.
3. **Verificacion real:** pruebas corridas y resultados; pruebas no realizadas, separadas.
4. **Riesgos y revertir:** que puede fallar y como recuperar el estado anterior.
5. **Siguiente paso unico:** una correccion o prueba prioritaria; no una lista ilimitada de funciones nuevas.

Si la barra de tareas, las ventanas del sistema o los dos monitores siguen fallando, considerar la reparacion incompleta aunque la compilacion sea exitosa.
