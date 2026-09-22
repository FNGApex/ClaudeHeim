# S5-38 - radial menu size (revised draft from the peer session)
# open once so RadialDataInitializer has run and RadialData.SO is non-null
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1
call Hud.instance.m_radialMenu.Back
wait 0.5
get RadialData.SO.RadialSize
# Small (1)
set RadialData.SO.RadialSize 1
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1
shot s38_radial_small
dump comp:RadialOverlapPreventer s38_overlap_small.txt
call Hud.instance.m_radialMenu.Back
wait 0.5
# SmallEdge (2)
set RadialData.SO.RadialSize 2
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1
shot s38_radial_smalledge
call Hud.instance.m_radialMenu.Back
wait 0.5
# Big (0) again, and at GUI scale 60 %
set RadialData.SO.RadialSize 0
call GuiScaler.SetScale 0.6
call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null
wait 1
shot s38_radial_big_scale60
call Hud.instance.m_radialMenu.Back
call GuiScaler.SetScale 1
# restore from prefs (Init re-reads PlatformPrefs("RadialSize"))
call RadialData.Init @RadialData.SO
