# Every UI canvas and what is under it - run once with -Vanilla and once with -Mods Auga, then diff.
dump ui ui_mainmenu
enter RETEP TestWorld
dump ui ui_ingame
ui inventory open
wait 1
dump ui ui_ingame_inventory
ui inventory close
quit
