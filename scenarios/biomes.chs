# Later-biome HUD bits (Auga playthrough item 15, run with -Mods Auga -Golden ClaudeLab:labtest): bed hover, rested
# status, eitr bar from food, a staff in the hands, floating damage numbers. On the lab stone floor (flatfloor).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
teleport @flatfloor
terrain clear @flatfloor 12

# --- bed hover and the rested status effect
place bed @flatfloor+0,2.5 as bed
goto bed 1.6
hover bed_hover
shot bed_hover
effect add Rested
wait 1
effect list
shot rested_status
despawn

# --- eitr: a Mistlands food fills the eitr bar; a staff in the hands
clearfood
eat YggdrasilPorridge
eat MushroomBlue
wait 2
foods
get Player.m_localPlayer.m_eitr
shot eitr_bar
give StaffFireball 1
equip StaffFireball
wait 1.5
shot staff_equipped
unequip

# --- floating damage numbers (player and enemy colours)
spawn Boar 3 as boar
setc boar Character m_speed 0
wait 1
lookat boar
call DamageText.instance.ShowText Normal @Player.m_localPlayer.transform.position 25 true
call DamageText.instance.ShowText Weak @Player.m_localPlayer.transform.position 60 false
wait 0.3
shot damage_text
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
