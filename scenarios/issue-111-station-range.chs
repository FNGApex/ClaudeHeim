# #111 - workbench UI stays open when walking away (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #111 crafting panel when the station goes out of range
enter RETEP TestWorld
unequip
place piece_workbench 2 as wb
setc wb CraftingStation m_craftRequireRoof false
goto wb 1.6
use
wait 1
expect value InventoryGui.instance.m_craftingStationName.text contains Workbench
shot 111_open
# explicit close first
ui inventory close
wait 0.5
shot 111_closed_by_key
get InventoryGui.instance.m_shownFrames
# reopen, then walk away
goto wb 1.6
use
wait 1
goto wb 8
wait 2
shot 111_walked_away
get InventoryGui.instance.m_shownFrames
get InventoryGui.instance.m_hiddenFrames
get InventoryGui.instance.m_craftingStationName.text
# vanilla does NOT hide here (see General facts); expect the panel to stay up with an empty / hand-crafting station name.
# If Auga's panel is expected to close, assert on Auga's own root instead:
describe InventoryGui.instance.m_player
despawn
expect noerrors
quit
