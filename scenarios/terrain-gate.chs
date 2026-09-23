# Terrain safety gate: in the user's protected cloud TestWorld every terrain edit must be refused; reads still work.
enter RETEP TestWorld
terrain info
terrain height @player
expectfail terrain level @player 3
expectfail terrain pad gate @player+10,0 6
expectfail terrain nuke @player 5
expectfail terrain clear @player 5
expectfail terrain paint @player 3 dirt
expect noerrors
quit
