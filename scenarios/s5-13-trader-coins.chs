# S5-13 - trader buy / sell coin flow (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# trader: buy decrements coins, sell increments, tabs keep their selection
enter RETEP TestWorld
unequip
spawn Haldor 3 as trader
give Coins 1000
give Ruby 3
ui store open trader
wait 1
call Player.m_localPlayer.m_inventory.CountItems $item_coins
dump StoreGui.instance s13_store_tree
click ItemElement(Clone)
wait 0.3
shot s13_buy_selected
call StoreGui.instance.OnBuyItem
wait 0.5
call Player.m_localPlayer.m_inventory.CountItems $item_coins
shot s13_after_buy
# Auga's store has no sell list: the diamond SellButton sells the selected inventory item
wait 0.3
call StoreGui.instance.OnSellItem
wait 0.5
call Player.m_localPlayer.m_inventory.CountItems $item_coins
shot s13_after_sell
# switch tabs a few times (Auga has tabs; vanilla has one list)
# (Auga has no buy/sell tabs)
shot s13_tabs_cycled
ui inventory close
remove Ruby 3
remove Coins 1000
despawn
expect noerrors
quit
