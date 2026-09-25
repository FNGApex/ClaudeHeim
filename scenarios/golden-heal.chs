# Golden-copy maintenance (run with -Golden ClaudeLab:labtest): LABTEST back to full health, nothing else changed - no
# movement, no items, no terrain. After the run copy ONLY characters_local/labtest.fch into TestSavesGolden/ClaudeLab/character
# (the golden world stays as it is).
enter LABTEST ClaudeLab
get Player.m_localPlayer.transform.position
call Player.m_localPlayer.GetHealth
call Player.m_localPlayer.GetMaxHealth
call Player.m_localPlayer.Heal 1000 false
wait 1
call Player.m_localPlayer.GetHealth
get Player.m_localPlayer.transform.position
expect noerrors
quit
