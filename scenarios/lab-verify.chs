# Lab check straight after the golden restore (run with -Golden ClaudeLab:labtest), NO terrain commands: is
# survival_field flat on the heightmap grid AND on the collider (what planting and the player use), up to its edges?
enter LABTEST ClaudeLab
teleport @survival_field
wait 5
tombstones list 100
get EnvMan.instance.m_smoothDayFraction
terrain height @survival_field
terrain height @survival_field+7,0
terrain height @survival_field-7.7,0
terrain height @survival_field+14.5,14.5
terrain height @survival_field-14.9,-14.9
expect height @survival_field
expect ground @survival_field @survival_field 0.05
expect ground @survival_field+7,0 @survival_field 0.05
expect ground @survival_field-7.7,0 @survival_field 0.05
expect ground @survival_field+14.5,14.5 @survival_field 0.05
expect ground @survival_field-14.5,-14.5 @survival_field 0.05
expect ground @survival_field+14.9,-14.9 @survival_field 0.05
expect ground @survival_field-14.9,14.9 @survival_field 0.05
expect ground @survival_stations+4.9,4.9 @survival_stations 0.05
expect ground @lab_stations-4.9,-4.9 @lab_stations 0.05
expect ground @lab_build+6.9,6.9 @lab_build 0.05
shot field

tombstones list 300
get Player.m_localPlayer.m_health
quit
