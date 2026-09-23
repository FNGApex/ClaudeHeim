# Golden-copy check (run with -Golden ClaudeLab:labtest): the lab and LABTEST start identical on every run.
enter LABTEST ClaudeLab
get Player.m_localPlayer.transform.position
marks
expect height @survival_field
expect height @survival_field+14.5,-14.5 @survival_field
expect height @survival_stations-4.5,4.5 @survival_stations
expect height @lab_stations+4.5,-4.5 @lab_stations
expect height @lab_build-6.5,6.5 @lab_build
expect biome @survival_field Meadows
get Player.m_localPlayer.m_knownRecipes.Count
# change the lab on purpose: the next golden restore must undo it
expect height @lab_build
terrain raise @lab_build 3 2
place piece_workbench 3 as scratch
expect noerrors
quit
