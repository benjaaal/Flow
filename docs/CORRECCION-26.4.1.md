# MacFlow 26.4.1

Corrección del Dock a partir de la captura del usuario del 17 de octubre.

- Una sola entrada visible por identidad de aplicación, conservando el orden de
  favoritos. No se borran los favoritos guardados.
- Roblox Player y Studio conservan identidad entre sus carpetas de versiones y
  siguen siendo apps distintas. Codex conserva identidad entre versiones del
  paquete; no se fusiona con ChatGPT por compartir un logotipo.
- El mismo agrupamiento se aplica al Dock de cada monitor.
- Apertura y restauración usan el recorrido inverso de la misma malla: salen del
  icono del Dock y recuperan su forma. Los paneles superiores mantienen su gesto
  desde arriba. En apps ajenas se anima una representación sin ocultar ni modificar
  su ventana nativa; no se reemplaza el compositor de Windows.
- Iconos centrados verticalmente, con origen central de ampliación y espacio
  reservado para contener el zoom junto al impulso de apertura.

La suite nativa del Dock comprueba versiones antiguas/nuevas, favoritos repetidos,
distinción de aplicaciones y contención real de iconos con máximo zoom e impulso
en tamaños 32, 36, 44, 64 y 72, además del Dock lateral. Se mantienen las pruebas
del núcleo y del instalador. No se afirma que dos aplicaciones diferentes con
logotipos parecidos sean duplicados, ni se eliminan accesos por esa semejanza.
