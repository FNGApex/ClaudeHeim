# Achievements (run with -Mods Auga, or -Vanilla to compare): the 1.0 achievements panel from the inventory, and the
# player statistics page ($inventory_stats) in the texts list / compendium. Local test saves AUGATEST / AugaTest.
newchar AUGATEST
newworld AugaTest
enter AUGATEST AugaTest
ui inventory open
wait 1
shot inventory
describe ui:TabButton_Achievements
click TabButton_Achievements
wait 1.5
shot achievements
expect active InventoryGui.instance.m_achievementsPanel
call InventoryGui.instance.OnCloseAchievements
wait 1
ui texts
wait 1.5
shot texts_list
get InventoryGui.instance.m_textsDialog.m_texts.Count
ui inventory close
invoke AugaCompendiumController ShowCompendium
wait 1.5
shot compendium
invoke AugaCompendiumController HideCompendium
expect noerrors
quit
