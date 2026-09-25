# Auga mod API: load the AugaAPI.dll stub the way a consumer mod would carry it, and check its calls reach the real Auga.
# Run with -Mods Auga after building Auga/AugaAPI/AugaAPI.csproj.
enter RETEP TestWorld
# Relative paths resolve against this scenario's folder; this one assumes Auga is cloned next to ClaudeHeim.
loadasm "..\..\Auga\Auga\bin\API\AugaAPI.dll"
call asm:AugaAPI|Auga.API.IsLoaded
call asm:AugaAPI|Auga.API.GetItemBackgroundSprite
call asm:AugaAPI|Auga.API.GetRegularFont
call asm:AugaAPI|Auga.API.GetRegularTmpFont
call asm:AugaAPI|Auga.API.GetHeaderTmpFont
ui inventory open
wait 1
call asm:AugaAPI|Auga.API.PlayerPanel_AddTab ClaudeHeimTab null "Claude Tab" null
call asm:AugaAPI|Auga.API.PlayerPanel_HasTab ClaudeHeimTab
invoke AugaTabController@RightPanel SelectTab 5
shot api_player_tab
call asm:AugaAPI|Auga.API.Workbench_AddWorkbenchTab ClaudeHeimBench null "Claude Bench" null
call asm:AugaAPI|Auga.API.Workbench_HasWorkbenchTab ClaudeHeimBench
invoke AugaTabController@RightPanel SelectTab 0
invoke AugaTabController@WorkbenchContent SelectTab 2
wait 2
shot api_inventory
# api-7: per-state label colours (Button_SetTextColors) on a TMP label - hover turns the label red
call asm:AugaAPI|Auga.API.SmallButton_Create @InventoryGui.instance.m_player ApiColorTest "Colour test"
call asm:AugaAPI|Auga.API.Button_SetTextColors @last 1,1,1,1 1,0,0,1 0,0,1,1 1,1,1,1 0.5,0.5,0.5,1 1,1,1,1
invoke UnityEngine.CanvasRenderer@ApiColorTest/Label GetColor
hoverui ApiColorTest
wait 0.6
invoke UnityEngine.CanvasRenderer@ApiColorTest/Label GetColor
expect value last contains "1.000, 0.000, 0.000"
shot api_button_hover
ui inventory close
expect noerrors
quit
