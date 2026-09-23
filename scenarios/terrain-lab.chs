# TerrainLab (phases 1-3 of PLAN_TERRAIN_TESTWORLD.md) in the local test world AugaTest: every op, then exact heights.
newchar AUGATEST
newworld AugaTest
enter AUGATEST AugaTest
set Player.m_localPlayer.m_godMode true
terrain info
# the spawn stones are a no-build location (the hoe refuses there too): build the lab on open Meadows further out
findbiome Meadows 90 400 labsite
teleport @labsite
wait 3
mark start

# flat pad 8x8 m, 0.5 m above the ground at its centre; corners and centre exact
terrain pad padA @start+14,0 8 +0.5
expect height @padA
expect height @padA+3.5,3.5 @padA
expect height @padA-3.5,-3.5 @padA
shot pad

# raise a disc 1 m, then check; snapshot/restore puts it back exactly
terrain snapshot snapA @padA 12
terrain raise @padA 2 1
expect height @padA +1
terrain restore snapA
wait 1
expect height @padA

# slope 0 -> +3 m over 12 m, width 4: middle is +1.5
terrain slope @padA+8,-6 +0 @padA+20,-6 +3 4
terrain height @padA+14,-6
shot slope

# paint, level, the +-8 m limit (vertices refused, nothing thrown)
terrain paint @padA-10,0 3 cultivated
terrain paint @padA-10,6 3 paved
terrain level @padA+0,12 3 @padA
expect height @padA+0,12 @padA 0.1
terrain lower @padA+0,-14 2 20
terrain height @padA+0,-14
shot paint_level

# clear vs nuke: spawn things, nuke the area (not the player)
spawn Boar 4 as boar1
place piece_workbench 5 as wb1
wait 1
terrain nuke @player 8
wait 1
terrain clear @player 20
marks
findbiome BlackForest 0 1500 blackforest
terrain reset @padA 10
terrain height @padA
expect noerrors
quit
