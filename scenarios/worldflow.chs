# World flow (run with -Mods Auga, or -Vanilla to compare): the start-game world panel, the world modifiers window,
# the crossplay toggle, and the loading screen of a first world load. Uses local test saves: AUGATEST / AugaTest, and
# AugaGen, which is only generated on its first load (wipe it with Manage-TestSaves.ps1 to see generation again).
newchar AUGATEST
newworld AugaTest
newworld AugaGen
call FejdStartup.instance.OnStartGame
wait 1.5
call FejdStartup.instance.SetSelectedProfile augatest
call FejdStartup.instance.OnCharacterStart
wait 2
shot worldpanel
dump FejdStartup.instance.m_startGamePanel startgame_tree
get FejdStartup.instance.m_serverOptionsButton
get FejdStartup.instance.m_crossplayServerToggle
expect active FejdStartup.instance.m_serverOptionsButton

# open server on: public + crossplay become interactable
set FejdStartup.instance.m_openServerToggle.isOn true
wait 1
shot worldpanel_server
get FejdStartup.instance.m_crossplayServerToggle.interactable
set FejdStartup.instance.m_openServerToggle.isOn false

# world modifiers for the selected test world (cancel: nothing is written)
selectworld AugaTest
get FejdStartup.instance.m_world.m_name
expect value FejdStartup.instance.m_world.m_name contains "Auga"
call FejdStartup.instance.OnServerOptions
wait 1.5
shot world_modifiers
expect active FejdStartup.instance.m_serverOptions
call FejdStartup.instance.OnServerOptionsCancel
wait 1

# first load of a fresh world: the loading screen must show generation progress
call FejdStartup.instance.OnStartGameBack
wait 1
enter AUGATEST AugaGen nowait
wait 2
shot loading_2s
wait 4
shot loading_6s
wait 6
shot loading_12s
waitfor player 240
wait 5
shot ingame
expect noerrors
quit
