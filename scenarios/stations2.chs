# Remaining stations (Auga playthrough item 13, run with -Mods Auga -Golden ClaudeLab:labtest): every crafting station
# UI not covered by stations.chs, and the hover of every processing station. One at a time on the lab stone floor (flatfloor).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip

# --- cauldron
teleport @flatfloor
terrain clear @flatfloor 12
place piece_cauldron @flatfloor+0,2.5 as cauldron
setc cauldron CraftingStation m_craftRequireRoof false
goto cauldron 1.8
hover cauldron_hover
shot cauldron_hover
interact cauldron
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot cauldron_ui
ui inventory close
despawn

# --- stonecutter
teleport @flatfloor
terrain clear @flatfloor 12
place piece_stonecutter @flatfloor+0,2.5 as stonecutter
setc stonecutter CraftingStation m_craftRequireRoof false
goto stonecutter 1.8
hover stonecutter_hover
shot stonecutter_hover
interact stonecutter
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot stonecutter_ui
ui inventory close
despawn

# --- artisan
teleport @flatfloor
terrain clear @flatfloor 12
place piece_artisanstation @flatfloor+0,2.5 as artisan
setc artisan CraftingStation m_craftRequireRoof false
goto artisan 1.8
hover artisan_hover
shot artisan_hover
interact artisan
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot artisan_ui
ui inventory close
despawn

# --- blackforge
teleport @flatfloor
terrain clear @flatfloor 12
place blackforge @flatfloor+0,2.5 as blackforge
setc blackforge CraftingStation m_craftRequireRoof false
goto blackforge 1.2
hover blackforge_hover
shot blackforge_hover
interact blackforge
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot blackforge_ui
ui inventory close
despawn

# --- galdr
teleport @flatfloor
terrain clear @flatfloor 12
place piece_magetable @flatfloor+0,2.5 as galdr
setc galdr CraftingStation m_craftRequireRoof false
goto galdr 1.8
hover galdr_hover
shot galdr_hover
interact galdr
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot galdr_ui
ui inventory close
despawn

# --- preptable
teleport @flatfloor
terrain clear @flatfloor 12
place piece_preptable @flatfloor+0,2.5 as preptable
setc preptable CraftingStation m_craftRequireRoof false
goto preptable 1.8
hover preptable_hover
shot preptable_hover
interact preptable
wait 1.2
get InventoryGui.instance.m_craftingStationName.text
shot preptable_ui
ui inventory close
despawn

# --- blastfurnace
teleport @flatfloor
terrain clear @flatfloor 12
place blastfurnace @flatfloor+0,2.5 as blastfurnace
goto blastfurnace 1.8
hover blastfurnace_hover
shot blastfurnace_hover
despawn

# --- windmill
teleport @flatfloor
terrain clear @flatfloor 12
place windmill @flatfloor+0,2.5 as windmill
goto windmill 1.8
hover windmill_hover
shot windmill_hover
despawn

# --- spinningwheel
teleport @flatfloor
terrain clear @flatfloor 12
place piece_spinningwheel @flatfloor+0,2.5 as spinningwheel
goto spinningwheel 1.8
hover spinningwheel_hover
shot spinningwheel_hover
despawn

# --- eitrrefinery
teleport @flatfloor
terrain clear @flatfloor 12
place eitrrefinery @flatfloor+0,2.5 as eitrrefinery
goto eitrrefinery add_ore 1.5
hover eitrrefinery_hover
# the add_ore switch sits inside the casing: the camera ray hits the casing, so read the switch text directly
invoke Switch@add_ore GetHoverText
shot eitrrefinery_hover
despawn

# --- fermenter
teleport @flatfloor
terrain clear @flatfloor 12
place fermenter @flatfloor+0,2.5 as fermenter
goto fermenter 1.8
hover fermenter_hover
shot fermenter_hover
despawn

# --- beehive
teleport @flatfloor
terrain clear @flatfloor 12
place piece_beehive @flatfloor+0,2.5 as beehive
goto beehive 1.8
hover beehive_hover
shot beehive_hover
despawn

# --- cookingstation
teleport @flatfloor
terrain clear @flatfloor 12
place piece_cookingstation @flatfloor+0,2.5 as cookingstation
goto cookingstation 1.8
hover cookingstation_hover
shot cookingstation_hover
despawn

# --- oven
teleport @flatfloor
terrain clear @flatfloor 12
place piece_oven @flatfloor+0,2.5 as oven
goto oven add_food 1.5
hover oven_hover
shot oven_hover
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
