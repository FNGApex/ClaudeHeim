# Survival loop: eating and dying / respawning. Works vanilla and with mods (run with -Mods Auga to check Auga's HUD).
enter RETEP TestWorld
unequip
clearfood
foods
shot hud_nofood

# three different foods fill the three slots; each shows on the HUD
eat Raspberry
shot food_one
eat CookedMeat
eat Honey
foods
expect value Player.m_localPlayer.m_foods.Count contains "3"
shot food_three

# refusal rules: the same food again is refused while it is still fresh; a fourth food is refused when all three are fresh
eat Raspberry fail
give Mushroom 1
eat Mushroom fail
expect value Player.m_localPlayer.m_foods.Count contains "3"

# the food tooltips in the inventory
ui inventory open
wait 1
shot inventory_with_food
ui inventory close

# die and come back: tombstone + death pin + food loss are vanilla behaviour, respawn restores the snapshotted items
die
wait 2
shot death_screen
respawn 120
shot respawned
waitfor player
expect value Player.m_localPlayer.m_foods.Count contains "0"
get Player.m_localPlayer.m_godMode
foods
wait 3
shot respawned_hud
ui map large
wait 1
shot map_after_death
ui map small

# the HUD must still work for the new player: eat again
eat Raspberry
expect value Player.m_localPlayer.m_foods.Count contains "1"
shot food_after_respawn

expect noerrors
quit
