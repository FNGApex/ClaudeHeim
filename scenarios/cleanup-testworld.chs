# Housekeeping for TestWorld: remove RETEP's leftover tombstone(s) from the death tests (near -23, 298).
enter RETEP TestWorld
get Player.m_localPlayer.transform.position
teleport -23 298
tombstones list 80
tombstones clear 80
wait 1
tombstones list 80
teleport -4 5
quit
