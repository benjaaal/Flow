MacFlow 27.0.0 — Windows 11 x64, C# / WPF / .NET Framework 4.8

Renovación nativa de materiales, tipografía, proporciones, Dock, barra,
Spotlight, configuración, centro de control, isla y asistente.

Centro de control inspirado en las referencias: cápsulas Wi-Fi/Bluetooth,
controles circulares y reguladores verticales independientes. Música sólo
con sesión multimedia; carátula real, ampliación y controles de Windows.

Privacidad: reloj y música; deslizá desde el borde inferior para mostrar
el PIN de MacFlow. También Enter/Espacio para teclado.
La cortina cubre las pantallas secundarias. Ctrl+Windows abre Privacidad;
Ctrl+Windows+Alt recupera el escritorio. Es una cortina recuperable de
MacFlow: el bloqueo y las credenciales de Windows siguen perteneciendo a Windows.

Asistente: órdenes locales y preguntas en Gemini web dentro de Chrome,
sin claves ni API de IA. «Resumí las pestañas» recorre el texto accesible
de las pestañas abiertas y pide la síntesis en Gemini. Las páginas se envían
a Gemini al pedir ese resumen. Requiere Chrome, sesión de Google y una
interfaz accesible compatible; los errores se muestran sin inventar respuestas.
Cerrar ventanas y escribir requieren confirmación. Una cancelación evita
envíos tardíos. El foco y la dirección de Gemini se comprueban antes de escribir.

Hey Siri usa el modelo local incluido, con indicador de micrófono.
Se pausa al bloquear Windows y durante la búsqueda escrita. Se puede
desactivar en Configuración. Necesita permisos y micrófono funcional.

Se conserva la preferencia de Hola; trazo finito y movimiento reducido.
Recuperación de Windows independiente ante fallos y supervisor confirmado
antes de ocultar la barra. Dock y barra por monitor; widgets conservan
su monitor preferido al desconectarlo, y los paneles respetan el área útil.

Validación: pruebas aisladas de núcleo, instalador, modelos de voz y
ventanas nativas. La grabación física con dos monitores de DPI diferente,
micrófono, Gemini autenticado y arranque tras apagar/encender
requiere prueba interactiva. En este entorno algunas capturas del compositor
son negras; se verificaron también renders WPF de las ventanas reales.
No se afirma 60 FPS constantes ni reemplazo de las animaciones de apps ajenas.

No se instala automáticamente ni cambia autenticación, Explorer o Winlogon.
La publicación 26.5.0 se conserva. Los perfiles se respaldan antes de migrar.
