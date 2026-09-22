enter RETEP TestWorld
ui menu open
wait 1
describe Menu.instance.m_root
describe Menu.instance.m_menuDialog
dump Menu.instance menu_tree
shot pausemenu
invoke AugaCompendiumController ShowCompendium
describe comp:AugaCompendiumController
shot compendium
invoke AugaCompendiumController HideCompendium
ui menu close
quit
