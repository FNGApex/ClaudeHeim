# Place crafting stations through the real placement call, open their UI, hover processing-station input slots.
# Works vanilla or with any mod set (e.g. -Mods Auga; ValheimSurvival adds the chest-input hover list).
enter RETEP TestWorld
# nothing in the hands: while a build tool is out the game hovers nothing
unequip

# --- workbench
place piece_workbench 2 as bench
# a bench in the open refuses to work without a roof; this test is about the UI, not shelter
setc bench CraftingStation m_craftRequireRoof false
goto bench 1.6
hover bench_hover
expect hover contains "Workbench"
shot workbench_hover
use
wait 1
expect value InventoryGui.instance.m_craftingStationName.text contains Workbench
dump InventoryGui.instance inventory_at_workbench
shot workbench_ui
ui upgradetab
shot workbench_upgrade
ui inventory close
despawn

# --- forge
place forge 2 as forge
setc forge CraftingStation m_craftRequireRoof false
setc forge CraftingStation m_useDistance 4
goto forge 1.8
hover forge_hover
shot forge_hover
interact forge
wait 1
expect value InventoryGui.instance.m_craftingStationName.text contains Forge
shot forge_ui
ui inventory close
despawn

# --- smelter: the two input slots
place piece_chest_wood 3 as chest
fill chest Coal 20
fill chest CopperOre 10
fill chest TinOre 10
place smelter 6 as smelter
dump smelter smelter_tree
goto smelter add_ore 1.5
hover smelter_ore_hover
expect hover contains "Smelter"
shot smelter_ore_hover
goto smelter add_wood 1.5
hover smelter_fuel_hover
shot smelter_fuel_hover
despawn

# --- charcoal kiln
place charcoal_kiln 3.5 as kiln
goto kiln add_ore 1.5
hover kiln_hover
shot kiln_hover
despawn

expect noerrors
quit
