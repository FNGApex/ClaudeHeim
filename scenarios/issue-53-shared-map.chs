# #53 - shared map data (cartography table) (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #53 other players' explored areas: fog texture green channel + SharedPanel toggle
enter RETEP TestWorld
unequip
ui map large
wait 1
shot 53_map_before
# ExploreOthers(int x, int y) is private, texture coords 0..m_textureSize-1 (2048 default); paint a block
invoke Minimap ExploreOthers 1020 1020
invoke Minimap ExploreOthers 1021 1020
invoke Minimap ExploreOthers 1020 1021
invoke Minimap ExploreOthers 1021 1021
invoke Minimap ExploreOthers 1030 1030
wait 1
shot 53_map_explored_others
# the vanilla fade: m_showSharedMapData drives m_sharedMapDataFade in Update
set Minimap.instance.m_showSharedMapData false
wait 2
shot 53_shared_off
set Minimap.instance.m_showSharedMapData true
wait 2
shot 53_shared_on
# Auga reuses one object as toggle + hint; vanilla SetActive(false)s the hint in Start/Reset
describe Minimap.instance.m_sharedMapHint
get Minimap.instance.m_sharedMapHint
get Minimap.instance.m_showSharedMapData
ui map small
expect noerrors
quit
