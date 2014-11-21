from dungeonNetwork import *
import time


dungeon = Dungeon.constructFromFile("LoZ 1.xml")
now = time.time()
path = dungeon.getBestPath(2,None,[],0)
for room in path.path:
	print dungeon.rooms[room]
print path.keys
print path.keyItem

print (time.time()-now)
