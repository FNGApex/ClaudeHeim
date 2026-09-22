# AUDIT2 round 3 verification (run with -Mods Auga): radial key hints, rune close hint, pause-menu button rebinding,
# settings tabs skin, unlock message, minimap filter icons, mount hud parts, NPC dialog rect, store guard.
enter RETEP TestWorld
unequip
set Player.m_localPlayer.m_godMode true

# hud-1 / hud-2: animator + food refs (diagnostics line in LogOutput.log; the flash needs a fight, so just check the refs)
get Hud.instance.m_adrenalineAnimator
get Hud.instance.m_foodTime
get Hud.instance.m_foodIcon
expect value Hud.instance.m_adrenalineAnimator contains "AdrenalineBar"

# hud-6 / misc-1: radial-menu key hints
get KeyHints.instance.m_radialHints
get KeyHints.instance.m_radialKeyHints
expect value KeyHints.instance.m_radialHints contains "RadialHints"
describe KeyHints.instance.m_radialHints
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1.5
expect active KeyHints.instance.m_radialHints
dump KeyHints.instance.m_radialHints radial_hints_tree
shot radial_open
call Hud.instance.m_radialMenu.QueuedClose
wait 1.5
expect inactive KeyHints.instance.m_radialHints
shot radial_closed
# text-3 regression: the close hint must not show while no text is open
expect inactive TextViewer.instance.m_closeText

# text-3: rune close hint
ui text rune "Runestone topic" "Rune text body. The quick brown fox jumps over the lazy dog."
wait 3
get TextViewer.instance.m_closeText
expect value TextViewer.instance.m_closeText contains "CloseText"
expect active TextViewer.instance.m_closeText
describe TextViewer.instance.m_closeText
dump TextViewer.instance.m_closeText closetext_tree
dump TextViewer.instance.transform textviewer_tree
shot text_rune_closehint
ui text close
wait 1.5
expect inactive TextViewer.instance.m_closeText
shot text_rune_closed

# text-5 / text-6: unlock message + biome banner (prefab-side fixes; instances show in the shots)
call MessageHud.instance.QueueUnlockMsg null "$msg_newstation" "$piece_workbench"
wait 1.5
shot unlock_message
call MessageHud.instance.ShowBiomeFoundMsg "$biome_meadows" true
wait 0.8
shot biome_found_early
dump MessageHud.instance.m_biomeMsgInstance biome_tree
wait 2
shot biome_found_late

# text-7: small NPC dialog rect
npctext "Haldor" "Welcome, friend. Come see what I have for sale today."
wait 1
shot npc_small_dialog
get Chat.instance.m_npcTextBase

# menus-2 / menus-3: pause menu button fields rebound to Auga entries
ui menu open
wait 1
get Menu.instance.m_continueButton
get Menu.instance.m_settingsButton
get Menu.instance.m_logoutButton
get Menu.instance.m_quitButton
expect value Menu.instance.m_settingsButton contains "Settings"
expect value Menu.instance.m_quitButton contains "Exit"
expect value Menu.instance.m_continueButton contains "CloseButton"
shot pausemenu

# menus-4: settings tabs (non-default tabs now get the button fonts)
ui settings open
wait 1
tabs Settings.instance.m_tabHandler settings
ui settings close
ui menu close

# hud-5: minimap filter icons (boss head on IconBoss, skull on IconDeath)
ui map large
wait 1
get Minimap.instance.m_selectedIconBoss
shot map_large_icons
ui map small

# misc-3: mount hud parts
get EnemyHud.instance.m_baseHudMount
dump EnemyHud.instance.m_baseHudMount mount_hud_tree

# misc-4 / store: shop still Auga's
spawn Haldor 4 as haldor
wait 2
ui store open haldor
wait 1
shot store
get StoreGui.instance
ui inventory close
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
