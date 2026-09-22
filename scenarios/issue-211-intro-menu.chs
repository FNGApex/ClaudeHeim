# #211 - Escape during the intro (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #211 pause menu during the intro crawl must show Skip Intro
enter RETEP TestWorld
unequip
# the real intro needs a fresh character (Game.m_playerProfile.m_firstSpawn), which the RETEP-only rule excludes;
# fake the state instead: Menu.Show enables m_skipButton when Game.instance.InIntro(includeQueued: true)
set Game.instance.m_inIntro true
ui menu open
wait 1
shot 211_menu_in_intro
get Menu.m_instance.m_skipButton
expect active ui:AugaMenu(Clone)/MenuRoot/Menu/MenuEntries/SkipIntro
describe ui:AugaMenu(Clone)/MenuRoot/Menu/MenuEntries/SkipIntro
# do NOT click SkipIntro with a faked flag: Game.SkipIntro would respawn/teleport RETEP (UNVERIFIED consequences)
ui menu close
set Game.instance.m_inIntro false
ui menu open
wait 1
shot 211_menu_normal
expect inactive ui:AugaMenu(Clone)/MenuRoot/Menu/MenuEntries/SkipIntro
ui menu close
expect noerrors
quit
