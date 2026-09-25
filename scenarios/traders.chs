# Traders (Auga playthrough item 14, run with -Mods Auga -Golden ClaudeLab:labtest): Hildir and the Bog Witch through
# the store UI (Auga store), hover, buy the first item. Haldor's coin flow is s5-13-trader-coins.chs.
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
teleport @flatfloor
terrain clear @flatfloor 12
give Coins 5000

# --- Hildir
spawn Hildir @flatfloor+0,3 as hildir
wait 3
goto hildir 2
lookat hildir
wait 1
hover hildir_hover
shot hildir_hover
ui store open hildir
wait 1
dump StoreGui.instance hildir_store_tree
shot hildir_store
click ItemElement(Clone)
wait 0.3
call StoreGui.instance.OnBuyItem
wait 0.5
call Player.m_localPlayer.m_inventory.CountItems $item_coins
shot hildir_after_buy
ui inventory close
despawn

# --- Bog Witch
terrain clear @flatfloor 12
teleport @flatfloor
spawn BogWitch @flatfloor+0,3 as witch
wait 3
goto witch 2
lookat witch
wait 1
hover witch_hover
shot witch_hover
ui store open witch
wait 1
dump StoreGui.instance witch_store_tree
shot witch_store
click ItemElement(Clone)
wait 0.3
call StoreGui.instance.OnBuyItem
wait 0.5
call Player.m_localPlayer.m_inventory.CountItems $item_coins
shot witch_after_buy
ui inventory close
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
