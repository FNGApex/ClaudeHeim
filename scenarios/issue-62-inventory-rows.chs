# #62 / #228 - extra inventory rows vs container panel overlap (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #62/#228 player panel growth with more rows; overlap with an open container
enter RETEP TestWorld
unequip
place piece_chest 3 as chest
fill chest Wood 10
ui inventory open
wait 1
get InventoryGui.instance.m_player.sizeDelta
shot 62_inv_4rows
call InventoryGui.instance.SetInventorySize 6
wait 0.5
get InventoryGui.instance.m_player.sizeDelta
shot 62_inv_6rows
call InventoryGui.instance.SetInventorySize 8
wait 0.5
get InventoryGui.instance.m_player.sizeDelta
shot 62_inv_8rows
describe InventoryGui.instance.m_player
ui inventory close
# now the container panel next to a real 6-row inventory (m_height is restored before quit; rows 5-6 stay empty)
set Player.m_localPlayer.m_inventory.m_height 6
ui container open chest
wait 1
get InventoryGui.instance.m_player.sizeDelta
get InventoryGui.instance.m_container.anchoredPosition
get InventoryGui.instance.m_container.sizeDelta
shot 62_container_with_6rows
dump InventoryGui.instance.m_containerGrid 62_container_grid.txt
describe InventoryGui.instance.m_containerGrid
ui inventory close
set Player.m_localPlayer.m_inventory.m_height 4
call InventoryGui.instance.SetInventorySize 4
despawn
expect noerrors
quit
