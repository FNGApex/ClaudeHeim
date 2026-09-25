# S5-43 - GUI scale slider (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# GUI scale 60% / 100% / 150%: HUD, inventory, crafting, map, no overflow; #13 blank stats below 100%
# Runs on the lab (terrain plan phase 5): -Golden ClaudeLab:labtest, pieces at fixed spots on the lab stone floor (flatfloor, lab-flat.chs).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
teleport @flatfloor
terrain clear @flatfloor 12
unequip
give HelmetBronze 1
give Wood 30
place piece_workbench @flatfloor+0,2 as wb
setc wb CraftingStation m_craftRequireRoof false
# 60 %
call GuiScaler.SetScale 0.6
wait 1
shot s43_hud_60
ui inventory open
wait 1
shot s43_inventory_60
get InventoryGui.instance.m_weight.text
get InventoryGui.instance.m_armor.text
expect value InventoryGui.instance.m_weight.text contains "/"
ui inventory close
goto wb 1.6
use
wait 1
shot s43_crafting_60
ui inventory close
ui map large
wait 1
shot s43_map_60
ui map small
# 150 %
call GuiScaler.SetScale 1.5
wait 1
shot s43_hud_150
ui inventory open
wait 1
shot s43_inventory_150
describe InventoryGui.instance.m_player
ui inventory close
goto wb 1.6
use
wait 1
shot s43_crafting_150
ui inventory close
ui map large
wait 1
shot s43_map_150
ui map small
# the settings tab itself (Accessibility) at 150 %
ui settings open
ui settingstab 5                                # UNVERIFIED index of the Accessibility tab
wait 1
shot s43_settings_accessibility_150
dump comp:Valheim.SettingsGui.AccessibilitySettings s43_accessibility_tree.txt
ui settings close
# back to 100 %
call GuiScaler.SetScale 1
wait 1
shot s43_hud_100
despawn
expect noerrors
quit
