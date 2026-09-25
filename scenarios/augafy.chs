# "Everything becomes Augafied" checks (run with -Mods Auga): styled crosshair hover text, Auga split dialog.
# Runs on the lab (terrain plan phase 5): -Golden ClaudeLab:labtest, pieces on the lab stone floor (flatfloor, lab-flat.chs).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
teleport @flatfloor
terrain clear @flatfloor 12
unequip
set Player.m_localPlayer.m_godMode true

# hud-3: Auga key-chip hover rows instead of vanilla "[E] Use"
place piece_chest @flatfloor+0,2 as chest
wait 1
goto chest 2
lookat chest
wait 1
hover hover_chest.txt
expect hover contains "Open"
describe Hud.instance.m_hoverName
dump Hud.instance.m_hoverName.transform.parent.parent hover_tree
shot hover_chest
spawn Boar 4 as boar
wait 1
lookat boar
wait 1
shot hover_boar

# inventory-11: the split dialog is Auga's panel with a live SplitDialog component
give Wood 20
ui inventory open
wait 1
split Wood
expect value InventoryGui.instance.m_splitDialog contains "SplitDialog"
expect active InventoryGui.instance.m_splitDialog
describe InventoryGui.instance.m_splitDialog
dump InventoryGui.instance.m_splitDialog split_tree
shot split_dialog
split Wood close
expect inactive InventoryGui.instance.m_splitDialog
ui inventory close

# hud-8: the selected-piece readout next to the crosshair uses Auga panel art
give Hammer 1
equip Hammer
wait 1.5
shot build_selected_info
dump Hud.instance.m_buildHud.transform selected_info_tree
unequip

despawn
set Player.m_localPlayer.m_godMode false
expect noerrors
quit
