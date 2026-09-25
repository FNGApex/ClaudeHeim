# megaflatten + floor command test (run with -Golden ClaudeLab:labtest): its own 100 m site 400+ m out (the golden lab keeps
# flatland/flatfloor from lab-flat.chs and every other mark untouched), a stone floor on it, pieces placed on the tiles.
enter LABTEST ClaudeLab
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
findflatland 100 400 1500 flattest
teleport @flattest
terrain height @flattest
terrain megaflatten flattest @flattest 100 auto
wait 3

# flat everywhere inside the circle: heightmap grid and the collider the player / pieces stand on
expect height @flattest @flattest
expect height @flattest+60,0 @flattest
expect height @flattest-60,0 @flattest
expect height @flattest+0,60 @flattest
expect height @flattest+0,-60 @flattest
expect height @flattest+68,68 @flattest
expect ground @flattest @flattest 0.05
expect ground @flattest+40,-40 @flattest 0.05
expect ground @flattest-65,50 @flattest 0.05
terrain height @flattest+95,0
shot megaflat_center

# stone floor 20 x 20 m, pieces on it
floor testfloor @flattest 20
wait 2
place piece_workbench @testfloor+0,4 as bench
place piece_chest_wood @testfloor-4,4 as chest
place forge @testfloor+5,4 as forge
place piece_cauldron @testfloor-6,-4 as cauldron
wait 2
goto bench 2
shot floor_bench
lookat chest
shot floor_chest
expect noerrors
quit
