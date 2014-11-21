from Queue import *
import pymc as mc


class Agent:
	def __init__(self,pathSoFar = [], keys = 0, items = 0, keyItem = 0, currentRoom = None, usedBombs = False, specialKey = False, requiresSpecialKey=False,switchesSet=[]):
		self.path = list(pathSoFar)
		self.visited = set(self.path)
		self.keys = keys
		self.specialKey = specialKey
		self.requiresSpecialKey=requiresSpecialKey
		self.switchesSet=switchesSet
		self.currentRoom = currentRoom
			
		self.items = items
			
		self.usedBombs = usedBombs
		self.keyItem = keyItem
		self.gotItem = False
		if (currentRoom.id not in self.path):
			if ('k' in self.currentRoom.type):
				self.keys+=1
				self.gotItem = True
			if ('i' in self.currentRoom.type):
				self.items+=1
				self.gotItem = True
			if ('K' in self.currentRoom.type):
				self.specialKey = True
			for type in self.currentRoom.type :
				if ('S' in type):
					self.switchesSet.append(type)
			if ('I' in self.currentRoom.type):
				if (!self.requiresSpecialKey  or self.specialKey):
					self.keyItem+=1
					self.gotItem = True
		self.path.append(currentRoom.id)
	def __key(self):
		return (str(self.visited),self.keys,self.items,self.keyItem,self.currentRoom)

	def __eq__(x, y):
		return x.__key() == y.__key()

	def __hash__(self):
		return hash(self.__key())
	
	def __str__(self):
		return self.currentRoom.__str__()
	def getChildren(self):
		
		cameFrom = None
		if len(self.path) > 1 :
			cameFrom = self.path[-2]
		
		children = []
		if (len(self.currentRoom.neighbors) == 1):
			#print self.currentRoom , " HERE?"
			children.append(Agent(self.path,self.keys,self.items,self.keyItem,self.currentRoom.neighbors[0]))
		else:
			for door in self.currentRoom.doors:
				room = door.other(self.currentRoom)
				if door.lock1 == 'i':
					continue
				if 'K' in  door.lock1 and not self.specialKey:
					continue
				if 'I' in  door.lock1 and self.keyItem == 0:
					continue
				if '1' in door.otherLock(self.currentRoom):
					continue
				if 'S' in door.lock1:
					canContinue = False
					for type in door.lock1.split():
						if 'S' in type:
							if type in switchesSet:
								canContinue = True
							#if any(type in s for s in switchesSet):
							#	canContinue = True
							#	break
					if not canContinue:
						continue
				if door.lock1 == 'b':
					self.usedBombs = True#pass #continue
				if room.id == cameFrom and not self.gotItem:
					continue
				keys = self.keys
				if door.lock1 == 'k'  and  room.id not in self.path:
					keys-=1
				if keys < 0 :
					continue
				children.append(Agent(self.path,keys,self.items,self.keyItem,room))
		
		return children
				
				
		
class Dungeon:
	def __init__(self,start = None,goal = None, rooms = {}):
		self.start = start
		self.goal = goal
		self.rooms = rooms
	def getBestPath(self,requireKeyItem=1,start = None,goals = [],bombCost = 10):
		openSet = PriorityQueue()
		closedSet = set()
		gScore = {}
		fScore = {}
		if start == None:
			start = self.start
		current = Agent(currentRoom = start)
		gScore[current]  = 0
		fScore[current] = 0
		cameFrom = {}
		openSet.put(current,0)
		if len(goals) == 0:
			goals.append(self.goal)
			
		while not openSet.empty():
			current = openSet.get()
			closedSet.add(current)
			if current.currentRoom in goals:
				if (current.keyItem >= requireKeyItem):
					return current
			children = current.getChildren()
			for child in children:
				if child not in closedSet:
					tentativeGScore = gScore[current] + 1 
					if child.usedBombs:
						tentativeGScore += bombCost
					if child not in gScore or gScore[child] > tentativeGScore :
						cameFrom[child] = current
						gScore[child] = tentativeGScore
						fScore[child] = tentativeGScore #+ self.estimateHeuristic(child)
						openSet.put(child,fScore[child])
	def updateRooms(self,optimalPath):
		notVisited = []
		
		for roomID in self.rooms:
			if roomID in optimalPath.path:
				self.rooms[roomID].detour = 0
				self.rooms[roomID].depth = optimalPath.path.index(roomID)
			else:
				notVisited.append(self.rooms[roomID])
				
				
		while len(notVisited) > 0:
			roomToVisit = notVisited.pop()
			print roomToVisit
			path = self.getBestPath(0,roomToVisit,map(lambda id: self.rooms[id],optimalPath.path)).path
			roomToVisit.detour = len(path)-1
			roomToVisit.depth = self.rooms[path[-1]].depth+len(path)-1
			
	@staticmethod				
	def constructFromFile(filename):		
		import xml.etree.ElementTree as ET
		import re
		tree = ET.parse(filename)

		root = tree.getroot()
		rooms = {}
		doors = []
		
		startArrow = re.compile("startArrow\=(\w+)")
		startFill = re.compile("startFill\=(\w+)")
		endArrow = re.compile("endArrow\=(\w+)")
		endFill = re.compile("endFill\=(\w+)")
		arrowTypes = {"oval1" : "k",
							"diamondThin1" : "b", 
							"diamond1" : "l", 
							"none0" : "", 
							"" : "",
							"oval0" : "s",
							"diamond0" : "K", 
							"diamondThin0" : "I",
							"block0" : "1"}
		startRoom = None
		
		goalRoom = None
		for child in root:
			for subChild in child:
				if ("style" in subChild.attrib):
					if ("ellipse" in subChild.attrib["style"]):
						room = Room(subChild.attrib["value"],subChild.attrib["id"])
						rooms[subChild.attrib["id"]] = room
						if "s" in room.type:
							startRoom = room
							
						
						if "t" in room.type:
							goalRoom = room
					if ("edgeStyle" in subChild.attrib["style"]):
						style = subChild.attrib["style"]
						sa = startArrow.search(style)
						if not sa is None:
							sa = sa.group(1)
						else :
							sa = "none"
							
						sf = startFill.search(style)
						if not sf is None:
							sf = sf.group(1)
						else :
							sf = "0"
						ea = endArrow.search(style)
						if not ea is None:
							ea = ea.group(1)
						else :
							ea = "none"
							
						ef = endFill.search(style)
						if not ef is None:
							ef = ef.group(1)
						else:
							ef = "0"
						doors.append((subChild.attrib["source"],subChild.attrib["target"],arrowTypes[sa+sf] + subChild.attrib["value"],arrowTypes[ea+ef] + subChild.attrib["value"],"rounded=0" in style))
		
		for door in doors:
			room1 = rooms[door[0]]
			room2 = rooms[door[1]]
			#print "Connecting " , room1 , " to ", room2
			room1.connect(room2,door[4],door[2],door[3])
		return Dungeon(startRoom,goalRoom,rooms)
				#print subChild.tag, subChild.attrib
class Room:
	def __init__(self,type = "",id = 0,depth = 0):
		self.doors = []
		self.depth = depth
		self.detour = 0
		self.id = id
		self.numberOfCrossingsRequired = 0
		self.neighbors = []
		
		self.type = type
	def connect(self,other,type="",lock1= "",lock2=""):
		#print "CONNECTING ",self,other
		if (other not in self.neighbors):
			self.neighbors.append(other)
			#print self.neighbors
			self.doors.append(Door(type,lock1,lock2,self,other))
			other.connect(self,type,lock2,lock1)
	def __str__(self):
		return self.type + "-" + self.id
class Door:
	def __init__(self,type = "", lock1="",lock2="",room1=None,room2=None):
		self.type = type
		self.lock1 = lock1
		self.lock2 = lock2
		self.room1 = room1
		self.room2 = room2
	def otherLock(self,room):
		if (self.room1 == room):
			return self.lock2
		if (self.room2 == room):
			return self.lock1
		return None
		
	def other(self,room):
		if (self.room1 == room):
			return self.room2
		if (self.room2 == room):
			return self.room1
		return None
		