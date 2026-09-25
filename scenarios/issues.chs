# Reproduction pass for the upstream issues that may still apply to the 1.0 port (Auga/ISSUES_1.0.md). Run with -Mods Auga.
# Runs on the lab (terrain plan phase 5): -Golden ClaudeLab:labtest, pieces at fixed spots on the lab stone floor (flatfloor, lab-flat.chs).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
teleport @flatfloor
terrain clear @flatfloor 12
unequip
set Player.m_localPlayer.m_godMode true

# #13 armor / weight numbers in the player panel; #235 armour item rendering; #127 double trash button
give HelmetBronze 1
give ArmorBronzeChest 1
give SwordBronze 1
give Wood 30
ui inventory open
wait 1
get InventoryGui.instance.m_weight.text
get InventoryGui.instance.m_armor.text
expect value InventoryGui.instance.m_weight.text contains "/"
dump InventoryGui.instance.m_player player_panel_tree
shot inventory_stats

# #233 skills stacked / #209 skills tab
ui skills
wait 1
shot skills
dump InventoryGui.instance.m_skillsDialog skills_tree
ui inventory close

# #44 / #153 / #186 compendium (Auga's own, from the pause menu)
ui menu open
wait 1
invoke AugaCompendiumController ShowCompendium
wait 1.5
shot compendium
dump comp:AugaCompendiumController compendium_tree
invoke AugaCompendiumController HideCompendium
ui menu close

# #84 upgrade level cut off in a container's top row
place piece_chest @flatfloor+0,3 as chest
fill chest SwordBronze 1 3
fill chest AxeBronze 1 2
fill chest HelmetBronze 1 4
ui container open chest
wait 1
shot container_quality
dump InventoryGui.instance.m_containerGrid container_grid_tree
ui inventory close

# #100 wrong item after a max-upgraded one; #214 crafting-station level requirement
place piece_workbench @flatfloor-3,2 as bench
setc bench CraftingStation m_craftRequireRoof false
goto bench 1.6
use
wait 1
expect value InventoryGui.instance.m_craftingStationName.text contains Workbench
ui upgradetab
wait 0.5
shot upgrade_tab
ui crafttab
wait 0.5
recipe Hammer
shot craft_hammer
recipe AxeStone
shot craft_axe
get InventoryGui.instance.m_recipeName.text
expect value InventoryGui.instance.m_recipeName.text contains "axe"
ui inventory close

# #152 friendly creatures get a hostile bar
spawn Boar 4 as boar
wait 1
tame 20
wait 1
lookat boar
wait 1
shot hud_tamed_boar
dump comp:EnemyHud enemyhud_tree

# #212 top-left pickup notification fade
message topleft "Wood x1"
wait 3.5
message topleft "Wood x1"
wait 1
shot pickup_fade
describe comp:AugaTopLeftMessageController

# #99 map icon clicks log errors
ui map large
wait 1
click hudroot/MiniMap/large/IconPanel/Icon0
click hudroot/MiniMap/large/IconPanel/Icon1
click hudroot/MiniMap/large/IconPanel/Icon2
click hudroot/MiniMap/large/IconPanel/Icon3
click hudroot/MiniMap/large/IconPanel/Icon4
click hudroot/MiniMap/large/IconBoss
click hudroot/MiniMap/large/IconDeath
shot map_icons_clicked
ui map small

# #185 rename hover on a tamed animal (Auga key chips)
lookat boar
wait 1
hover hover_tamed.txt
shot hover_tamed

despawn
set Player.m_localPlayer.m_godMode false
expect noerrors
quit
