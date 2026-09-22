# S5-11 - hammer vs hoe build menu (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# hammer: categories + favorites; hoe / cultivator: flat list
enter RETEP TestWorld
unequip
give Hammer 1
give Hoe 1
give Cultivator 1
equip Hammer
wait 0.5
ui build open
wait 1
shot s11_hammer_menu
dump Hud.instance.m_buildUi s11_hammer_buildui.txt
ui buildtab 0
shot s11_hammer_tab0
ui buildtab 1
shot s11_hammer_tab1
ui buildtab 2
shot s11_hammer_tab2
ui build close
equip Hoe
wait 0.5
ui build open
wait 1
shot s11_hoe_menu
dump Hud.instance.m_buildUi s11_hoe_buildui.txt
ui build close
equip Cultivator
wait 0.5
ui build open
wait 1
shot s11_cultivator_menu
ui build close
# back to the hammer: does it remember the last tab?
equip Hammer
wait 0.5
ui build open
wait 1
shot s11_hammer_again
ui build close
unequip
expect noerrors
quit
