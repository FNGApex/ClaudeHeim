# Sailing (Auga playthrough item 11, run with -Mods Auga -Golden ClaudeLab:labtest): Karve on deep water off the lab's
# nearest beach, ship storage, take the rudder -> ship HUD (speed, wind, rudder), sail, then a cart on the beach.
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
findshore 100 600 harbor
teleport @harbor
wait 3
expect biome @harbor Meadows

# --- Karve on the water next to the beach
spawn Karve @harbor_sea as ship
wait 4
dump ship karve_tree
lookat ship
shot karve_from_beach

# --- board: stand on the deck, take the rudder through the game's own Interact (needs "standing on this ship")
moveto ship 1.2
wait 2
invoke ShipControlls Interact @Player.m_localPlayer false false
wait 2
get Player.m_localPlayer.m_doodadController
expect active Hud.instance.m_shipHudRoot
shot ship_hud_stopped
dump Hud.instance.m_shipHudRoot ship_hud_tree

# --- sails: half sail, then full sail; the HUD speed indicator and the wind arrow move
invoke Ship Forward
wait 1
invoke Ship Forward
wait 8
invoke Ship GetSpeedSetting
invoke Ship GetSpeed
shot ship_hud_sailing
invoke Ship Stop
wait 5
shot ship_hud_stop_again

# --- ship storage (Karve chest) through the container UI
ui container open ship
wait 1
shot ship_storage
ui inventory close

# --- off the ship, back on the beach: a cart
call Player.m_localPlayer.StopDoodadControl
teleport @harbor
wait 2
spawn Cart 3 as cart
wait 2
goto cart 1.8
lookat cart
wait 1
hover cart_hover
shot cart_hover
interact cart
wait 1
shot cart_attached

despawn
set Player.m_localPlayer.m_godMode false
expect noerrors
quit
