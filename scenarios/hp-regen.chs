# HP bar under per-frame healing (regen mods such as SmoothRegen). GuiBar re-arms its change delay on every rise, so a
# bar with smooth fill and a delay never moves. Works vanilla and with mods (run with -Mods Auga for Auga's health bar).
enter RETEP TestWorld
unequip
clearfood
eat CookedMeat
eat Honey
call Player.m_localPlayer.SetHealth 10
wait 3
shot hp_low
regen 6 5
shot hp_regen
expect noerrors
quit
