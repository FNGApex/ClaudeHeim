# Bosses (Auga playthrough item 12, run with -Mods Auga -Golden ClaudeLab:labtest): boss health bar, guardian power
# on the HUD, raid event bar. On the lab_build pad, god mode throughout, everything despawned at the end.
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
teleport @lab_build
terrain clear @lab_build 7

# --- boss bar: Eikthyr in view
spawn Eikthyr 9 as boss
setc boss Character m_speed 0
setc boss Character m_runSpeed 0
wait 3
lookat boss
wait 1
shot boss_bar
dump comp:EnemyHud enemyhud_tree
despawn

# --- guardian power: the HUD icon and its cooldown slot
call Player.m_localPlayer.SetGuardianPower GP_Eikthyr
wait 1.5
get Player.m_localPlayer.m_guardianPower
shot guardian_power
ui inventory open
wait 1
shot guardian_power_inventory
ui inventory close

# --- raid event bar (the Eikthyr army event at the player); its creatures are the subject, so let them live
nomobs off
call RandEventSystem.instance.SetRandomEventByName army_eikthyr @Player.m_localPlayer.transform.position
wait 6
get RandEventSystem.instance.m_forcedEvent.m_name
shot raid_bar
call RandEventSystem.instance.ResetRandomEvent
wait 2

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
