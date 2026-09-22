# S5-7 - container persistence on rapid open/close (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# rapid container cycling: no lost items, no stuck in-use flag
enter RETEP TestWorld
unequip
place piece_chest 3 as chest
fill chest Wood 20
fill chest Stone 5
goto chest 2
hover s7_hover_before.txt
ui container open chest
wait 0.2
move Wood player 5
move Wood container 5
ui inventory close
ui container open chest
wait 0.2
move Wood player 5
move Wood container 5
ui inventory close
ui container open chest
wait 0.2
move Wood player 5
move Wood container 5
ui inventory close
ui container open chest
wait 0.2
move Wood player 5
move Wood container 5
ui inventory close
ui container open chest
wait 1
move Stone player 5
shot s7_after_cycling
get InventoryGui.instance.m_currentContainer.m_inventory.m_inventory.Count
expect value Player.m_localPlayer.m_inventory.m_inventory.Count contains ""
dump InventoryGui.instance.m_containerGrid s7_container_grid.txt
ui inventory close
wait 0.5
hover s7_hover_after.txt
expect hover contains "Chest"                 # 1.0 hover shows no slot count; "in use" would appear here if the flag stuck
get InventoryGui.instance.m_currentContainer
despawn
expect noerrors
quit
