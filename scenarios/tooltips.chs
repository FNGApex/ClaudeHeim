# Item tooltips (run with -Mods Auga, or -Vanilla to compare): hover inventory items of each kind and screenshot the
# tooltip. Covers 1.0's trinkets and equipment modifiers, set effects, stacks (value/weight per item + total), food,
# a consumable with a status effect and a weapon with an attack status effect. Local AUGATEST / AugaTest.
newchar AUGATEST
newworld AugaTest
enter AUGATEST AugaTest
clearinv
give TrinketBronzeHealth 1
give ArmorFenringChest 1
give ArmorTrollLeatherChest 1
give Coins 50
give Wood 20
give CookedMeat 1
give MeadHealthMinor 1
give AxeBerzerkrNature 1
ui inventory open
wait 1
hoveritem TrinketBronzeHealth
shot tt_trinket
hoveritem ArmorFenringChest
shot tt_fenring_chest
hoveritem ArmorTrollLeatherChest
shot tt_trollleather_chest
hoveritem Coins
shot tt_coins_stack
hoveritem Wood
shot tt_wood_stack
hoveritem CookedMeat
shot tt_food
hoveritem MeadHealthMinor
shot tt_mead
hoveritem AxeBerzerkrNature
shot tt_axe_nature
ui inventory close
clearinv
expect noerrors
quit
