# Auga UI tour (port of Auga's old in-plugin PortTestDriver). Run with: -Mods Auga
shot mainmenu
enter RETEP TestWorld
shot hud

# Auga's adrenaline bar has a debug override so it can be seen without fighting
set AugaUnity.AugaHealthBar.DebugAdrenalineOverride 65
shot hud_adrenaline
set AugaUnity.AugaHealthBar.DebugAdrenalineOverride -1

ui inventory open
shot inventory
ui crafttab
shot inventory_crafttab
ui upgradetab
shot inventory_upgradetab
ui skills
shot inventory_skills
ui texts
shot inventory_texts
ui trophies
shot inventory_trophies
ui inventory close

ui map large
shot map_large
ui map small

ui menu open
describe Menu.instance.m_root
shot pausemenu
ui settings open
wait 1
dump Settings.instance settings_tree
tabs Settings.instance.m_tabHandler settings
ui settings close
ui menu close

ui textinput open "Port test"
shot textinput
ui textinput close
npctext "Port test" "Speech bubble check"
shot chat
message center "Port test center message"
shot message

# status effects under the minimap: do they list, and do the timers run
effect add Rested
effect add Wet
effect add CampFire
effect add Shelter
effect add Smoked
effect list
shot effects_applied
wait 6
shot effects_6s_later
ui inventory open
shot effects_in_inventory
ui inventory close
effect remove Wet
effect remove CampFire
effect remove Shelter
effect remove Smoked

# Auga's own right-panel tabs
ui inventory open
invoke AugaTabController@RightPanel SelectTab 0
shot augatab_0
invoke AugaTabController@RightPanel SelectTab 1
shot augatab_1
invoke AugaTabController@RightPanel SelectTab 2
shot augatab_2
invoke AugaTabController@RightPanel SelectTab 3
shot augatab_3
invoke AugaTabController@RightPanel SelectTab 0
ui inventory close

spawn piece_chest_wood 2.5 as chest
fill chest Wood 10
ui container open chest
shot chest
ui inventory close

spawn Haldor 4 as trader
ui store open trader
shot trader
ui store close

spawn Deer 5 as deer
shot enemyhud
despawn

ui text rune "Port test rune" "Rune text body. The quick brown fox jumps over the lazy dog."
wait 3
shot text_rune
ui text close
ui text raven "Port test raven" "Hugin text body. Craft a hammer to start building."
shot text_raven
ui text close

ui menu open
invoke AugaCompendiumController ShowCompendium
shot compendium
invoke AugaCompendiumController HideCompendium
ui menu close

message topleft "Port test top-left message"
shot topleft

give Hammer 1
equip Hammer
shot build_hud
ui build open
wait 1
dump Hud.instance.m_buildUi buildui_tree
tabs Hud.instance.m_buildUi build
ui build close
shot hud_after

expect noerrors
quit
