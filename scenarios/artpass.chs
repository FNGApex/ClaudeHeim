# Auga art pass (5.18 + 5.19; run with -Mods Auga -Golden ClaudeLab:labtest, and -Vanilla to compare): every panel that
# still showed vanilla art. Main menu: world modifiers + the generic popup (the modifiers disclaimer: shown once while
# the pref is 0; the game sets it back to 1 itself). In world: achievements + details + unlock toast, trophies, pause
# player list, radial menu, hovered-piece author window, large-map pin filters (hud-5).
call FejdStartup.instance.OnStartGame
wait 1.5
call FejdStartup.instance.SetSelectedProfile labtest
call FejdStartup.instance.OnCharacterStart
wait 2
selectworld ClaudeLab
call PlatformPrefs.SetInt ServerOptionsDisclaimer 0
call FejdStartup.instance.OnServerOptions
wait 1.5
shot popup_disclaimer
dump UnifiedPopup.instance popup_tree
call UnifiedPopup.Pop
wait 1
shot world_modifiers
call FejdStartup.instance.OnServerOptionsCancel
wait 1
call FejdStartup.instance.OnStartGameBack
wait 1

enter LABTEST ClaudeLab
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip

# achievements panel, a detail page, the unlock toast
ui inventory open
wait 1
click TabButton_Achievements
wait 1.5
shot achievements
dump InventoryGui.instance.m_achievementsPanel achievements_tree
click AchElement(Clone)
wait 1
shot achievement_details
call InventoryGui.instance.m_achievementsPanel.OnCloseAchievementDetails
call InventoryGui.instance.OnCloseAchievements
wait 0.5
call InventoryGui.instance.OnOpenTrophies
wait 1
shot trophies
call InventoryGui.instance.OnCloseTrophies
ui inventory close
achievementpopup
wait 2
shot achievement_toast
wait 5

# pause menu player list (hidden button in single player; the panel itself works)
ui menu open
wait 1
call Menu.instance.OnCurrentPlayers
wait 1.5
shot player_list
# close it the way its Back button does (it saves the block list on disable - do that while Steam is still up)
invoke Valheim.UI.SessionPlayerList Close
wait 0.5
ui menu close
wait 1

# radial menu
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1.5
shot radial
call Hud.instance.m_radialMenu.QueuedClose
wait 1.5

# hovered-piece author window: needs the game option, a player-built piece and the hammer out
teleport @flatfloor
set Hud.s_showBuildPieceAuthor true
place piece_chest_wood @flatfloor+0,2.5 as chest
give Hammer 1
equip Hammer
wait 1
lookat chest
wait 1.5
shot piece_author
unequip
despawn

# large map: death / boss filter icons on the right buttons (hud-5 prefab fix)
ui map large
wait 1.5
shot map_filters
dump Minimap.instance.m_largeRoot map_large_tree
ui map small

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
