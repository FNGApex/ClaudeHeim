# #108 - crafting subtitles at another resolution (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #108 recipe subtitles disappear on ultrawide; runner starts 1600x900
enter RETEP TestWorld
unequip
place piece_workbench 2 as wb
setc wb CraftingStation m_craftRequireRoof false
give Wood 20
call UnityEngine.Screen.SetResolution 2560 1080 false
wait 3
shot 108_ultrawide_hud
goto wb 1.6
use
wait 1
ui crafttab
wait 0.5
shot 108_craft_ultrawide
dump comp:AugaUnity.AugaCraftingPanel 108_craft_ultrawide.txt
ui inventory close
call UnityEngine.Screen.SetResolution 1600 900 false
wait 3
goto wb 1.6
use
wait 1
ui crafttab
wait 0.5
shot 108_craft_1600
ui inventory close
despawn
expect noerrors
quit
