# Checks for the Phase 5.5-5.9 fixes (run with -Mods Auga): compendium tabs + trophies, large-map hints and ping,
# crafting icon / multi-craft amount, HUD food time, skills, build-menu hints and category colours. AUGATEST / AugaTest.
newchar AUGATEST
newworld AugaTest
enter AUGATEST AugaTest
set Player.m_localPlayer.m_godMode true
unequip

# 5.8 HUD food time text (#59)
clearfood
eat CookedMeat
eat Honey
wait 1
shot hud_food_time

# 5.8 skills (#209)
ui inventory open
ui skills
wait 1
shot skills
ui inventory close

# 5.5 compendium: first open selects a tab (pause-texts-12), Tab key off (widgets-6), trophies (#153/#44)
give TrophyDeer 1
give TrophyGreydwarf 1
give TrophyEikthyr 1
give TrophyDraugr 1
wait 1
invoke AugaCompendiumController ShowCompendium
wait 1.5
shot compendium_first_open
dump comp:AugaCompendiumController compendium_tree
invoke AugaCompendiumController HideCompendium

# 5.6 large map: 1.0 key hints strip, ping wiring (minimap-2/-4)
ui map large
wait 1
shot map_large_hints
get Minimap.instance.m_selectedIconPing
get Minimap.instance.m_pingImageObject
get Minimap.instance.m_hints
call Minimap.instance.OnPressedPingIcon
get Minimap.instance.m_selectedType
ui map small

# 5.7 crafting: stale icon (#100), multi-craft amount in the title (crafting-10)
# a fresh character only knows recipes for materials it has held
give Hammer 1
give Wood 20
give Feathers 5
give Flint 5
wait 2
place piece_workbench 3 as wb
goto wb 1.6
interact wb
wait 1.5
recipe ArrowWood
wait 1
shot craft_arrowwood
recipe Club
wait 1
shot craft_club
ui inventory close

# 5.9 build menu: 1.0 hints (hud-build-3), category counts colour (hud-bars-5), station row (hud-build-6)
equip Hammer
ui build open
wait 1
shot build_menu_hints
ui build close
unequip
despawn

# round 2: NPC dialog templates hidden at load (text-chat-3), top-left message, pause backdrop, tamed Rename hover (#185)
expect inactive ui:NpcDialog
message topleft "ClaudeHeim top-left check"
wait 1
shot topleft_message
ui menu open
wait 1
shot pause_backdrop
ui menu close
spawn Boar 3 as boar
tame 10
setc boar Character m_speed 0
goto boar 1.8
lookat boar
wait 1
hover tame_hover
shot tame_hover
despawn
expect noerrors
quit
