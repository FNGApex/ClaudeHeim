# EpicLoot under Auga (run with -Mods Auga,EpicLoot for stock EpicLoot, -Mods Auga,EpicLootFork for our fork, or
# -Mods EpicLoot -Vanilla to compare): magic item tooltip in the inventory, the enchanting table UI, the crafting panel.
# -Golden ClaudeLab:labtest, pieces on the lab stone floor. devcommands is on for EpicLoot's magicitem command
# (lab world + test character only; the next golden restore resets both).
enter LABTEST ClaudeLab
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
teleport @flatfloor
terrain clear @flatfloor 12
console devcommands
wait 1

# magic items: one per rarity, then their tooltips
console magicitem Magic SwordBronze 1
console magicitem Rare AxeBronze 1
console magicitem Legendary SpearBronze 1
# magicitem drops the items at the feet (loot beams); pick them up like walking over them
wait 2
pickup 5
ui inventory open
wait 1
shot inventory_magic
hoveritem SwordBronze
wait 1
shot tooltip_magic
hoveritem SpearBronze
wait 1
shot tooltip_legendary
ui inventory close

# the enchanting table (EpicLoot's own UI)
place piece_enchantingtable @flatfloor+0,3 as table
goto table 1.8
hover table_hover
shot table_hover
interact table
wait 1.5
shot enchanting_ui
dump comp:EpicLoot_UnityLib.EnchantingTableUI enchanting_tree
ui inventory close
wait 1
despawn

# crafting panel at a workbench
place piece_workbench @flatfloor+3,3 as bench
setc bench CraftingStation m_craftRequireRoof false
goto bench 1.6
use
wait 1.5
shot workbench
ui inventory close
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
