# Text viewers, messages and the enemy hud - run vanilla and with mods and compare the shots.
enter RETEP TestWorld
unequip
# the spawned creatures attack: keep the test character unharmed
set Player.m_localPlayer.m_godMode true
ui text rune "Test rune topic" "Rune text body. The quick brown fox jumps over the lazy dog."
wait 3
shot text_rune
ui text close
wait 1
# note: vanilla 1.0 itself never uses the raven style (Hugin talks through npctext); kept for completeness
ui text raven "Test raven topic" "Hugin text body. Craft a hammer to start building."
wait 2
shot text_raven
describe TextViewer.instance.m_ravenRoot
ui text close
message center "Center message check"
shot message_center
message topleft "Top-left message check"
shot message_topleft
spawn Boar 5 as boar
wait 2
lookat boar
shot enemyhud_boar
spawn Greydwarf_Elite 6 as brute
setc brute Character m_level 3
wait 2
lookat brute
shot enemyhud_star
despawn
set Player.m_localPlayer.m_godMode false
quit
