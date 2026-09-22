# S5-9 - map pin interactions (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# map pins: add, checked toggle, ping, remove
enter RETEP TestWorld
unequip
ui map large
wait 1
shot s9_map_open
# AddPin(Vector3 pos, PinType type, string name, bool save, bool isChecked, long ownerID, PlatformUserID author)
call Minimap.instance.AddPin @Player.m_localPlayer.transform.position 0 "TestPin" false false 0
wait 0.5
shot s9_pin_added
dump Minimap.instance.m_pinRootLarge s9_pins_large.txt
# toggling the checked (X) state: vanilla flips closestPin.m_checked inside the left-click handler; no public toggle.
get last.m_name
set last.m_checked true
wait 0.5
shot s9_pin_checked
get last.m_checked
call Minimap.instance.ShowPointOnMap @Player.m_localPlayer.transform.position
wait 0.5
shot s9_point_shown
ui map small
ui map large
wait 1
shot s9_reopened
ui map small
expect noerrors
quit
