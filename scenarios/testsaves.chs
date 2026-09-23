# Test saves: create (or reuse) a local test character and world, enter the fresh world and look at the first spawn.
# Both are local saves (never Steam Cloud) and get recorded in the registry (Manage-TestSaves.ps1).
newchar AUGATEST
newworld AugaTest
enter AUGATEST AugaTest
wait 5
shot first_spawn
wait 20
shot after_valkyrie
expect noerrors
quit
