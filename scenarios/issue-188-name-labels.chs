# #188 - creature name label resolution (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #188 blurry name labels above creatures, at two distances
enter RETEP TestWorld
unequip
set Player.m_localPlayer.m_godMode true
spawn Greydwarf 4 as gd
setc gd Character m_speed 0
wait 1
lookat gd
wait 1
shot 188_name_near
dump comp:EnemyHud 188_enemyhud_tree.txt
goto gd 12
lookat gd
wait 1
shot 188_name_far
# a boss-style bar for the second label variant
spawn Eikthyr 8 as boss
wait 1
lookat boss
wait 1
shot 188_boss_name
despawn
set Player.m_localPlayer.m_godMode false
expect noerrors
quit
