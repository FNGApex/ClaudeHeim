# S5-4 - split dialog bounds (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# split dialog: default = half rounded up, slider limits, 51-stack
enter RETEP TestWorld
unequip
# Wood stacks at 50, so the 51-stack case needs Coins (stack 999)
remove Coins 9999
give Coins 51
ui inventory open
wait 1
split Coins open
wait 0.5
shot s4_split_51
get InventoryGui.instance.m_splitDialog.m_splitSlider.value
get InventoryGui.instance.m_splitDialog.m_splitSlider.minValue
get InventoryGui.instance.m_splitDialog.m_splitSlider.maxValue
get InventoryGui.instance.m_splitDialog.m_splitAmount.text
expect value InventoryGui.instance.m_splitDialog.m_splitAmount.text contains "26/51"
# push the slider to both ends
set InventoryGui.instance.m_splitDialog.m_splitSlider.value 1
wait 0.3
shot s4_split_min
set InventoryGui.instance.m_splitDialog.m_splitSlider.value 50
wait 0.3
shot s4_split_max
get InventoryGui.instance.m_splitDialog.m_splitAmount.text
dump comp:SplitDialog s4_split_tree.txt
split Coins close
remove Coins 9999
# a 2-stack: min 1 max 1
give Raspberry 2
split Raspberry open
wait 0.3
shot s4_split_2
get InventoryGui.instance.m_splitDialog.m_splitAmount.text
split Raspberry close
ui inventory close
expect noerrors
quit
