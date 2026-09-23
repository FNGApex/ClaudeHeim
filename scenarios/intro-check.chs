# First spawn of a brand-new character: the intro text crawl ("When Oden heard...") and the Valkyrie arrival.
# Needs a character that has never spawned: INTROTEST is wiped after each use (Manage-TestSaves.ps1 -Wipe -Name introtest).
newchar INTROTEST
newworld AugaTest
enter INTROTEST AugaTest nowait
waitfor player 240
wait 3
shot intro_3s
wait 8
shot intro_11s
wait 15
shot intro_26s
wait 20
shot intro_46s
expect noerrors
quit
