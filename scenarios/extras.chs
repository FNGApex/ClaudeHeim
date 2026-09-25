# Extras (Auga playthrough item 16, run with -Mods Auga -Golden ClaudeLab:labtest): achievement unlock popup (display
# only - never AchievementEvent, which unlocks on Steam), emotes, a feast table. Hugin's dialog is in auga-tour /
# texts-enemies, trophies in fixes55.
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
set Raven.m_tutorialsEnabled false
set Player.m_localPlayer.m_godMode true
unequip
teleport @flatfloor
terrain clear @flatfloor 12

# --- achievement popup
achievementpopup
wait 1.5
shot achievement_popup
dump comp:AchievementUnlockPopup achievement_popup_tree
wait 5

# --- emotes (the emote wheel is the radial menu, audit3.chs; here the player animation + HUD while emoting)
call Player.m_localPlayer.StartEmote wave
wait 1
shot emote_wave
call Player.m_localPlayer.StartEmote sit false
wait 2
shot emote_sit
call Player.m_localPlayer.StopEmote
wait 1

# --- feast: the Feast component sits on the food item itself; spawn it, hover + eat
spawn FeastMeadows @flatfloor+0,2 as feast
wait 2
describe feast
goto feast 1.6
hover feast_hover
shot feast_hover
use
wait 1
foods
shot feast_eaten
despawn

set Player.m_localPlayer.m_godMode false
expect noerrors
quit
