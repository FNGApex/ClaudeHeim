# Augafied main menu (run with -Mods Auga): every FejdStartup screen through the game's own handlers, then enter.
# 1.0 plays the intro cinematic on the first start; the menu comes up after it
waitfor FejdStartup.instance.m_mainMenu.activeSelf 300
wait 2
shot mainmenu
dump ui mainmenu_ui
describe FejdStartup.instance.m_mainMenu
get FejdStartup.instance.m_menuList
get FejdStartup.instance.m_versionLabel.text
call FejdStartup.instance.OnStartGame
wait 1.5
shot charselect
describe FejdStartup.instance.m_characterSelectScreen
dump FejdStartup.instance.m_characterSelectScreen charselect_tree
call FejdStartup.instance.OnCharacterNew
wait 1
shot newcharacter
call FejdStartup.instance.OnNewCharacterCancel
wait 1
call FejdStartup.instance.OnSelelectCharacterBack
wait 1
call FejdStartup.instance.OnCredits
wait 1.5
shot credits
call FejdStartup.instance.OnCreditsBack
wait 1
shot mainmenu_again
expect noerrors
enter RETEP TestWorld
shot hud_after_auga_menu
expect noerrors
quit
