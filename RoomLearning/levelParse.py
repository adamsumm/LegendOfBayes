
import cv2
import json
import numpy as np
import matplotlib.pyplot as plt
import json




def clamp(val,minimum,maximum):
	return max(min(val, maximum), minimum)

def findSubImageLocations(image,subImages,confidence):
	allLocations = [ np.array([]) , np.array([])];
	for subImage in subImages:
		result = cv2.matchTemplate(image,subImage,cv2.TM_CCOEFF_NORMED)
		
		print (result>confidence)
		match_indices = np.arange(result.size)[(result>confidence).flatten()]
		locations =  np.unravel_index(match_indices,result.shape)
		allLocations[0] = np.concatenate((allLocations[0],locations[0]))
		allLocations[1] = np.concatenate((allLocations[1],locations[1]))
	return allLocations
	
def prefixPostfix(prefix,str,postfix):
	return prefix + str + postfix

def parseLevel(level,tiles):
	blockTiles = ['mapTile132.png', 'mapTile138.png', 'mapTile172.png', 'mapTile173.png', 'mapTile182.png', 'mapTile382.png', 'mapTile383.png', 'mapTile384.png', 'mapTile385.png', 'mapTile461.png', 'mapTile462.png', 'mapTile463.png', 'mapTile464.png', 'mapTile469.png', 'mapTile470.png', 'mapTile551.png', 'mapTile552.png', 'mapTile561.png', 'mapTile566.png', 'mapTile571.png', 'mapTile578.png', 'mapTile581.png', 'mapTile582.png', 'mapTile615.png', 'mapTile616.png', 'mapTile772.png', 'mapTile773.png', 'mapTile783.png', 'mapTile787.png', 'mapTile788.png', 'mapTile789.png', 'mapTile790.png', 'mapTile791.png', 'mapTile793.png', 'mapTile794.png', 'mapTile795.png', 'mapTile796.png', 'mapTile797.png', 'mapTile798.png', 'mapTile814.png', 'mapTile909.png',];
	floorTiles = ['mapTile102.png', 'mapTile106.png', 'mapTile405.png', 'mapTile406.png', 'mapTile467.png', 'mapTile468.png', 'mapTile472.png', 'mapTile547.png', 'mapTile549.png', 'mapTile786.png', 'mapTile799.png', 'mapTile800.png', 'mapTile801.png', 'mapTile802.png', 'mapTile803.png', 'mapTile810.png', 'mapTile910.png', 'mapTile99.png', ]
	enemyTiles = ['enemyA.png', 'enemyB.png', 'enemyC.png', 'enemyD.png', 'enemyE.png', 'enemyF.png', 'enemyG.png', 'enemyH.png', 'enemyI.png', 'enemyJ.png', 'enemyK.png', 'enemyL.png', 'enemyM.png', 'enemyN.png', 'enemyTile00.png', 'enemyTile01.png', 'enemyTile02.png', 'enemyTile03.png', 'enemyTile04.png', 'enemyTile05.png', 'enemyTile06.png', 'enemyTile07.png', 'enemyTile08.png', 'enemyTile09.png', 'enemyTile100.png', 'enemyTile101.png', 'enemyTile105.png', 'enemyTile106.png', 'enemyTile107.png', 'enemyTile108.png', 'enemyTile109.png', 'enemyTile110.png', 'enemyTile111.png', 'enemyTile112.png', 'enemyTile113.png', 'enemyTile114.png', 'enemyTile115.png', 'enemyTile116.png', 'enemyTile120.png', 'enemyTile121.png', 'enemyTile122.png', 'enemyTile123.png', 'enemyTile124.png', 'enemyTile125.png', 'enemyTile126.png', 'enemyTile127.png', 'enemyTile128.png', 'enemyTile129.png', 'enemyTile132.png', 'enemyTile135.png', 'enemyTile136.png', 'enemyTile137.png', 'enemyTile138.png', 'enemyTile139.png', 'enemyTile14.png', 'enemyTile140.png', 'enemyTile141.png', 'enemyTile145.png', 'enemyTile146.png', 'enemyTile15.png', 'enemyTile150.png', 'enemyTile151.png', 'enemyTile153.png', 'enemyTile16.png', 'enemyTile161.png', 'enemyTile162.png', 'enemyTile163.png', 'enemyTile164.png', 'enemyTile166.png', 'enemyTile17.png', 'enemyTile175.png', 'enemyTile176.png', 'enemyTile177.png', 'enemyTile179.png', 'enemyTile18.png', 'enemyTile19.png', 'enemyTile20.png', 'enemyTile21.png', 'enemyTile22.png', 'enemyTile23.png', 'enemyTile24.png', 'enemyTile29.png', 'enemyTile30.png', 'enemyTile31.png', 'enemyTile32.png', 'enemyTile33.png', 'enemyTile34.png', 'enemyTile35.png', 'enemyTile36.png', 'enemyTile37.png', 'enemyTile38.png', 'enemyTile39.png', 'enemyTile40.png', 'enemyTile41.png', 'enemyTile42.png', 'enemyTile43.png', 'enemyTile44.png', 'enemyTile45.png', 'enemyTile46.png', 'enemyTile47.png', 'enemyTile48.png', 'enemyTile49.png', 'enemyTile50.png', 'enemyTile51.png', 'enemyTile52.png', 'enemyTile53.png', 'enemyTile54.png', 'enemyTile55.png', 'enemyTile56.png', 'enemyTile57.png', 'enemyTile58.png', 'enemyTile59.png', 'enemyTile60.png', 'enemyTile61.png', 'enemyTile62.png', 'enemyTile63.png', 'enemyTile64.png', 'enemyTile65.png', 'enemyTile66.png', 'enemyTile67.png', 'enemyTile68.png', 'enemyTile69.png', 'enemyTile70.png', 'enemyTile71.png', 'enemyTile72.png', 'enemyTile73.png', 'enemyTile74.png', 'enemyTile75.png', 'enemyTile76.png', 'enemyTile77.png', 'enemyTile78.png', 'enemyTile79.png', 'enemyTile80.png', 'enemyTile81.png', 'enemyTile82.png', 'enemyTile83.png', 'enemyTile84.png', 'enemyTile85.png', 'enemyTile86.png', 'enemyTile87.png', 'enemyTile88.png', 'enemyTile89.png', 'enemyTile90.png', 'enemyTile91.png', 'enemyTile92.png', 'enemyTile93.png', 'enemyTile94.png', 'enemyTile95.png', 'enemyTile96.png', 'enemyTile97.png', 'enemyTile98.png', 'enemyTile99.png', ]
	bossTiles = ['boss1.png', 'boss10.png', 'boss11.png', 'boss12.png', 'boss13.png', 'boss14.png', 'boss15.png', 'boss16.png', 'boss17.png', 'boss18.png', 'boss19.png', 'boss2.png', 'boss20.png', 'boss21.png', 'boss3.png', 'boss4.png', 'boss5.png', 'boss6.png', 'boss7.png', 'boss8.png', 'boss9.png', 'bossA00.png', 'bossA01.png', 'bossA02.png', 'bossA03.png', 'bossA04.png', 'bossA05.png', 'bossA06.png', 'bossA07.png', 'bossA08.png', 'bossA09.png', 'bossA10.png', 'bossA11.png', 'bossB00.png', 'bossB01.png', 'bossB02.png', 'bossB03.png', 'bossB04.png', 'bossB05.png', 'bossB06.png', 'bossB07.png', 'bossB08.png', 'bossB09.png', 'bossB10.png', 'bossB11.png', ]
	itemTiles = ['itemTile00.png', 'itemTile01.png', 'itemTile02.png', 'itemTile05.png', 'itemTile09.png', 'itemTile10.png', 'itemTile11.png', 'itemTile12.png', 'itemTile13.png', 'itemTile14.png', 'itemTile19.png', 'itemTile20.png', 'itemTile21.png', 'itemTile22.png', 'itemTile26.png', 'itemTile30.png', 'itemTile31.png', 'itemTile34.png', 'itemTile35.png', 'itemTile36.png', 'itemTile37.png', 'itemTile39.png', ]
	keyTile = ['itemTile17.png']
	keyItems = ['itemTile03.png', 'itemTile04.png', 'itemTile06.png', 'itemTile07.png', 'itemTile08.png', 'itemTile18.png', 'itemTile23.png', 'itemTile24.png', 'itemTile25.png', 'itemTile27.png', 'itemTile28.png', 'itemTile29.png', 'itemTile32.png', 'itemTile33.png', ]
	teleportTile = ['mapTile199.png', 'mapTile460.png', 'mapTile612.png', 'mapTile623.png', 'mapTile776.png', 'mapTile811.png', ]
	trapTile = ['enemyTile174.png']
	waterTile = ['mapTile135.png', 'mapTile556.png', 'mapTile792.png', 'mapTile861.png', ]
	
	prefix = 'Tiles/LoZ'
	
	tilesets = {'Blocks' : blockTiles, 'Bosses' : bossTiles, 'Enemy' : enemyTiles, 'Ground' : floorTiles, 'Items' : itemTiles, 'Key' : keyTile, 'KeyItems' : keyItems, 'Teleport' : teleportTile, 'Traps' : trapTile, 'Water':waterTile}

	level = cv2.imread(level, cv2.IMREAD_GRAYSCALE)
	for k,v in tilesets.iteritems():
		levelMap = np.zeros((level.shape[0]/16,level.shape[1]/16));
		v = map(lambda str: prefix + '/' + k +'/' + str ,v)
		print prefix
		v = [cv2.imread(v_, cv2.IMREAD_GRAYSCALE) for v_ in v]#map( cv2.imread,v)
		v = findSubImageLocations(level,v,0.85)
		
		for ii in range(0,v[0].size):
			levelMap[clamp(round(v[0][ii]/16),0,levelMap.shape[0]-1),clamp(round(v[1][ii]/16),0,levelMap.shape[1]-1)] = 1
		
		plt.imshow(levelMap);
		plt.show()
	

levels = [	'Levels\LoZ\Raw\zelda-dungeon1.png']
tiles = {}
for levelFile in levels:
	parseLevel(levelFile,tiles)