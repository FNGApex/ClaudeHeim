# ClaudeLab (PLAN_TERRAIN_TESTWORLD.md phase 4): build the test lab once on seed CLAUDELA16 with the local test character
# LABTEST, then save it as the golden copy (Manage-TestSaves.ps1 -SaveGolden). Every later run restores that copy
# (Run-ClaudeHeim.ps1 -Golden ClaudeLab:labtest), so all runs start on identical ground, objects and character.
# Pads sit 100+ m from spawn: the spawn stones are a no-build location.
newworld ClaudeLab CLAUDELA16
newchar LABTEST
enter LABTEST ClaudeLab
set Player.m_localPlayer.m_godMode true
learn all
give Hammer 1
give Hoe 1
give Cultivator 1
findbiome Meadows 100 400 labsite 48
teleport @labsite
wait 5
terrain clear @labsite 48

# Survival (8b): cultivated planting field 30x30 (their beds span ~26 m) + a station pad east of it
terrain pad survival_field @labsite 30
terrain paint @survival_field 15 cultivated square
terrain pad survival_stations @labsite+23,0 10
# Auga / general (c6): station pad west, build pad north, a 3 m ramp south - all clear of the field
terrain pad lab_stations @labsite-23,0 10
terrain pad lab_build @labsite+0,26 14
terrain slope @labsite+0,-20 +0 @labsite+0,-32 +3 4
mark lab_slope @labsite+0,-26

# every pad flat at its corners
expect height @survival_field
expect height @survival_field+14.5,14.5 @survival_field
expect height @survival_field-14.5,-14.5 @survival_field
expect height @survival_field+14.5,-14.5 @survival_field
expect height @survival_stations+4.5,4.5 @survival_stations
expect height @lab_stations-4.5,4.5 @lab_stations
expect height @lab_build+6.5,-6.5 @lab_build
marks
teleport @labsite
wait 3
# no tombstones in the golden copy, and morning so every lab run starts in daylight
tombstones clear 200
call EnvMan.instance.SkipToMorning
wait 3
shot lab_overview
expect noerrors
quit
