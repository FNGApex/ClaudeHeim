# #181 / #192 - CJK glyphs render as boxes (run with -Mods Auga)
# drafted by the peer session (SCENARIO_DRAFTS.md), reviewed before the run
# #181/#192 Japanese / Simplified Chinese glyphs in Auga-fonted surfaces
enter RETEP TestWorld
unequip
message center "テスト メッセージ 日本語"
wait 0.5
shot 181_center_ja
message center "简体中文测试 龙 鹰 骨"
wait 0.5
shot 181_center_zh
message topleft "木材 x1 テスト"
wait 0.5
shot 181_topleft
npctext "ハルドール" "いらっしゃいませ、簡体字：欢迎"
wait 1
shot 181_npctext
ui text rune "ルーンストーン" "北の果て、深い北。簡体字：深北。"
wait 1
shot 181_rune
ui text close
# creature name label (issue #192 mentions monster names)
set Player.m_localPlayer.m_godMode true
spawn Greydwarf 5 as gd
setc gd Character m_name "グレイドワーフ 灰矮人"
wait 1
lookat gd
wait 1
shot 181_enemy_name
dump comp:MessageHud 181_messagehud_fonts.txt
dump comp:EnemyHud 181_enemyhud_fonts.txt
despawn
set Player.m_localPlayer.m_godMode false
expect noerrors
quit
