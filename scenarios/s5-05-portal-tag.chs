# S5-5 - portal tag dialog (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# portal tag dialog: 10-char limit, case, blank
# Runs on the lab (terrain plan phase 5): -Golden ClaudeLab:labtest, pieces at fixed spots on the lab stone floor (flatfloor, lab-flat.chs).
enter LABTEST ClaudeLab
# no wild creatures near the test (a Greyling attacked during a lab run); spawned/placed subjects are kept
nomobs on
teleport @flatfloor
terrain clear @flatfloor 12
unequip
place portal_wood @flatfloor+0,3 as portal
goto portal 1.5
lookat portal
wait 1
hover s5_portal_hover.txt
expect hover contains "Portal"                  # UNVERIFIED wording of the localised hover
interact portal
wait 0.5
shot s5_tag_dialog
get TextInput.instance.m_inputField.characterLimit
expect value TextInput.instance.m_inputField.characterLimit contains "10"
type "AbCdEfGhIjKLM"
expect value TextInput.instance.m_inputField.text contains "AbCdEfGhIj"
wait 0.3
shot s5_tag_overlong
get TextInput.instance.m_inputField.text
call TextInput.instance.OnEnter
wait 0.5
invoke TeleportWorld GetText
hover
shot s5_tag_set
# blank tag
interact portal
wait 0.3
type ""
call TextInput.instance.OnEnter
wait 0.5
invoke TeleportWorld GetText
# cancel path
interact portal
wait 0.3
call TextInput.instance.OnCancel
expect inactive TextInput.instance.m_panel
despawn
expect noerrors
quit
