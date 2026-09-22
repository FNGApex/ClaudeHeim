# S5-5 - portal tag dialog (vanilla and -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# portal tag dialog: 10-char limit, case, blank
enter RETEP TestWorld
unequip
place portal_wood 3 as portal
goto portal 2
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
