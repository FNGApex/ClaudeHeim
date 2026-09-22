# #17 - trader sell button with an item selected (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #17 sell button texture / state once a sellable item is selected
enter RETEP TestWorld
unequip
spawn Haldor 3 as trader
give Coins 500
give Ruby 3
ui store open trader
wait 1
shot 17_store_open
dump StoreGui.instance 17_store_tree
# the sell list is StoreGui.m_listRoot filled by FillList(); Auga's element names differ from vanilla,
# take the first element name from 17_store_tree.txt and put it here:
# Auga's store: one list (ItemElement(Clone) rows) + a diamond SellButton (Hotspot image has no sprite: the suspected white square) + BuyButton
click ItemElement(Clone)
wait 0.5
shot 17_store_item_selected
describe ui:Store/SellButton
dump ui:Store/SellButton 17_sellbutton_tree
describe ui:Store/SellButton/Hotspot
hoverui Store/SellButton
wait 0.5
shot 17_sellbutton_hover
# optional: sell it and confirm coins go up
call Player.m_localPlayer.m_inventory.CountItems $item_coins
#call StoreGui.instance.OnSellItem
wait 0.5
call Player.m_localPlayer.m_inventory.CountItems $item_coins
shot 17_after_sell
ui inventory close
despawn
expect noerrors
quit
