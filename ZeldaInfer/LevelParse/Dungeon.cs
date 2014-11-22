using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Priority_Queue;
namespace ZeldaInfer.LevelParse {
    public class Dungeon {
        public Room start;
        public Room end;
        public Dictionary<int, Room> rooms;
        public Dungeon(Room start, Room end, Dictionary<int, Room> rooms) {
            this.start = start;
            this.end = end;
            this.rooms = rooms;
        }
        public Dungeon(string filename) {
            rooms = new Dictionary<int, Room>();
            XDocument xdoc = XDocument.Load(filename);
            
		    Regex startArrow = new Regex(@"startArrow\=(\w+)");
		    Regex startFill = new Regex(@"startFill\=(\w+)");
		    Regex endArrow = new Regex(@"endArrow\=(\w+)");
		    Regex endFill = new Regex(@"endFill\=(\w+)");
            var arrowTypes = new Dictionary<string, string>() {
               { "oval1" , "k"},
               {"diamondThin1" , "b"}, 
               {"diamond1" , "l"}, 
               {"none0" , ""}, 
               {"" , ""},
               {"oval0" , "s"},
               {"diamond0" , "K"}, 
               {"diamondThin0" , "I"},
               {"block0" , "1"}
            };
            List<Tuple<string,string,string,string> > doors = new List<Tuple<string,string,string,string>>();
            foreach (XElement element in xdoc.Root.Descendants()) {
                string val = "";
                if (element.Attribute("value") != null) {
                    val = element.Attribute("value").Value;
                }
                if (element.Attribute("style") != null) {
                    string style = element.Attribute("style").Value;
                    if (style.Contains("ellipse")) {
                        Room room = new Room(val, Convert.ToInt32(element.Attribute("id").Value));
                        if (room.type.Contains("s")) {
                            start = room;
                        }
                        if (room.type.Contains("t")) {
                            end = room;
                        }
                        rooms[room.id] = room;
                    }
                    else if (style.Contains("edgeStyle")) {
                        MatchCollection matches;
                        matches = startArrow.Matches(style);
                        string sa = "none";
                        if (matches.Count != 0) {
                            sa = matches[0].Groups[1].Value;
                        }
                        matches = startFill.Matches(style);
                        string sf = "0";
                        if (matches.Count != 0) {
                            sf = matches[0].Groups[1].Value;
                        }
                        matches = endArrow.Matches(style);
                        string ea = "none";
                        if (matches.Count != 0) {
                            ea = matches[0].Groups[1].Value;
                        }
                        matches = endFill.Matches(style);
                        string ef = "0";
                        if (matches.Count != 0) {
                            ef = matches[0].Groups[1].Value;
                        }
                        doors.Add(new Tuple<string, string, string, string>(
                                            element.Attribute("source").Value,
                                            element.Attribute("target").Value,
                                            arrowTypes[sa + sf] + val,
                                            arrowTypes[ea + ef] + val));
                    }
                }
            }
            foreach (var door in doors){
			    Room room1 = rooms[Convert.ToInt32(door.Item1)];
			    Room room2 = rooms[Convert.ToInt32(door.Item2)];
                room1.Connect(room2, door.Item3, door.Item4);
            }
		    
        }

        public SearchAgent getOptimalPath(bool requiresBigKey,int requiredKeyItems) {

            HeapPriorityQueue<SearchAgent> openSet = new HeapPriorityQueue<SearchAgent>(80000);
            HashSet<SearchAgent> closedSet = new HashSet<SearchAgent>();
            Dictionary<int,double> gScore = new Dictionary<int,double>();
            SearchAgent current = new SearchAgent(new List<Room>(), 0,0,false, requiresBigKey, 0, 0, new List<string>(), start,new List<SearchAgent>());
            gScore[current.GetHashCode()] = 0;
            openSet.Enqueue(current, 0);

            while (openSet.Count != 0) {
                current = openSet.Dequeue();
                closedSet.Add(current);
                if (current.currentRoom == end) {
                    if (current.keyItems >= requiredKeyItems) {
                        return current;
                    }
                }
                else {
                    List<SearchAgent> children = current.GetChildren();
                    foreach (var child in children) {
                        if (!closedSet.Contains(child)) {
                            int hash = current.GetHashCode();
                            int hash2 = child.GetHashCode();
                            double tentativeGScore = gScore[current.GetHashCode()] + 1;
                            if (!gScore.ContainsKey(child.GetHashCode())) {
                                gScore[child.GetHashCode()] = tentativeGScore;
                                openSet.Enqueue(child, tentativeGScore);
                            }
                            else if (gScore[child.GetHashCode()] > tentativeGScore) {
                                gScore[child.GetHashCode()] = tentativeGScore;
                                openSet.UpdatePriority(child, tentativeGScore);
                            }
                        }
                    }
                }
            }
            Console.WriteLine(current.pathToString());
            return null;
        }
        public void UpdateRooms(List<Room> optimalPath) {
            List<Room> notVisited = new List<Room>();
		
		    foreach(var room in rooms.Values){
			    if (optimalPath.Contains(room)){
				    room.detour = 0;
				    room.depth = optimalPath.IndexOf(room);
                }
			    else {
                    notVisited.Add(room);
                }
            }
					
		    while (notVisited.Count > 0){
			    Room roomToVisit = notVisited[0];
                notVisited.RemoveAt(0);
			    Tuple<int,Room> detourRoomPair= FloodFill(roomToVisit,optimalPath);
                roomToVisit.detour = detourRoomPair.Item1;
                roomToVisit.depth = detourRoomPair.Item2.depth + roomToVisit.detour;
            }
        }
        public Tuple<int, Room> FloodFill(Room currentRoom, List<Room> goals) {
            
		List<Room> visited = new List<Room>();
		List<Tuple<int, Room>> toVisit = new List<Tuple<int, Room>>();
		foreach (var door in currentRoom.doors){
			Room room = door.OtherRoom(currentRoom);
			if (door.OtherLock(currentRoom).Contains("1")){
				continue;
            }
			if (door.lock1.Contains("s")){
				continue;
            }
			if (goals.Contains(room)){
				return new Tuple<int,Room>(1,room);
            }
			toVisit.Add(new Tuple<int,Room>(1,room));
			visited.Add(room);
        }
		while (toVisit.Count > 0){
			Tuple<int,Room> depthRoomTuple = toVisit[0];
            toVisit.RemoveAt(0);
			currentRoom = depthRoomTuple.Item2;
			int depth = depthRoomTuple.Item1;
			foreach (var door in currentRoom.doors){
			    Room room = door.OtherRoom(currentRoom);
			    if (door.OtherLock(currentRoom).Contains("1")){
				    continue;
                }
			    if (door.lock1.Contains("s")){
				    continue;
                }
			    if (goals.Contains(room)){
				    return new Tuple<int,Room>(depth+1,room);
                }
			    toVisit.Add(new Tuple<int,Room>(depth+1,room));
			    visited.Add(room);
            }			
        }
		return null;
        }
    }
}
