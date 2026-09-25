# One-time lab extension (run with -Golden ClaudeLab:labtest, then save the world + marks as the new golden copy): a
# megaflattened 100 m site "flatland" 220+ m from the lab pads and a 24 m stone floor "flatfloor" on it, where the building
# scenarios place their pieces. Run it ONCE on the old golden copy: a second run would lay a second floor on the first.
enter LABTEST ClaudeLab
nomobs on
set Player.m_localPlayer.m_godMode true
unequip
marks
findflatland 100 220 1500 flatland
teleport @flatland
terrain megaflatten flatland @flatland 100 auto
wait 3
expect height @flatland+60,0 @flatland
expect height @flatland-60,0 @flatland
expect height @flatland+0,60 @flatland
expect height @flatland+0,-60 @flatland
expect ground @flatland+40,-40 @flatland 0.05
floor flatfloor @flatland 24
wait 2
marks
shot flatfloor
# only the world + marks go into the golden copy; labtest.fch (position, inventory) stays the old one
call EnvMan.instance.SkipToMorning
wait 3
expect noerrors
quit
