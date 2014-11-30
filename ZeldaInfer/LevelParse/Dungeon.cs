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
               {"block0" , "O"}
            };
            List<Tuple<string,string,string,string> > doors = new List<Tuple<string,string,string,string>>();
            foreach (XElement element in xdoc.Root.Descendants()) {
                string val = "";
                if (element.Attribute("value") != null) {
                    val = element.Attribute("value").Value.Split('&')[0];
                    val = Regex.Replace(val, " ", ",");
                }
                if (element.Attribute("style") != null) {
                    string style = element.Attribute("style").Value;
                    if (style.Contains("ellipse")) {
                        Room room = new Room(val, Convert.ToInt32(element.Attribute("id").Value.GetHashCode()));
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
                                            arrowTypes[sa + sf]+"," + val,
                                            arrowTypes[ea + ef]+"," + val));
                    }
                }
            }
            foreach (var door in doors){
			    Room room1 = rooms[Convert.ToInt32(door.Item1.GetHashCode())];
                Room room2 = rooms[Convert.ToInt32(door.Item2.GetHashCode())];
                room1.Connect(room2, door.Item3, door.Item4);
            }
		    
        }
        
        public SearchAgent getOptimalPath(bool requiresBigKey) {
            int requiredKeyItems = 0;
            foreach (var room in rooms.Values) {
                requiredKeyItems += room.type.Contains("I") ? 1 : 0;
            }
            HeapPriorityQueue<SearchAgent> openSet = new HeapPriorityQueue<SearchAgent>(8000000);
            HashSet<SearchAgent> closedSet = new HashSet<SearchAgent>();
            Dictionary<int,double> gScore = new Dictionary<int,double>();
            SearchAgent current = new SearchAgent(null, 0, 0, false, requiresBigKey, 0, 0,  start);
            gScore[current.GetHashCode()] = 0;
            openSet.Enqueue(current, 0);
            int ii = 0;
            while (openSet.Count != 0) {
                ii++;
                current = openSet.Dequeue();
                closedSet.Add(current);
                if (current.currentRoom == end) {
                    if (current.keyItems >= requiredKeyItems) {
                        return current;
                    }
                }
                if (current.currentRoom.type.Contains("S1")) {
                    bool stophere = true;
                }
                {
                    List<SearchAgent> children = current.GetChildren();
                    foreach (var child in children) {
                        if (!closedSet.Contains(child)) {
                            double discounts = 0;
                            if (current.keyItems < child.keyItems){
                                discounts -= 0.1;
                            }
                            if (current.keysAcquired < child.keysAcquired) {
                                discounts -= 0.1;
                            }
                            if (!current.bigKey && child.bigKey) {
                                discounts -= 0.1;
                            }
                            double tentativeGScore = gScore[current.GetHashCode()] + 1 - discounts;// +rand.NextDouble() * 0.1 + 1;
                            if (!gScore.ContainsKey(child.GetHashCode())) {
                              
                                gScore[child.GetHashCode()] = tentativeGScore;
                                openSet.Enqueue(child, tentativeGScore);
                            }
                            else if (gScore[child.GetHashCode()] > tentativeGScore) {
                              //  gScore[child.GetHashCode()] = tentativeGScore;
                             //   openSet.UpdatePriority(child, tentativeGScore);
                            }
                        }
                    }
                }
            }
            Console.WriteLine(current.pathToString());
         //   List<SearchAgent> children2 = current.GetChildren();
         //   int hash3 = children2[0].GetHashCode();
            return null;
        }
        public void UpdateRooms(SearchAgent endAgent) {

            List<Room> optimalPath = new List<Room>();
            SearchAgent agent = endAgent;
            while (agent != null) {
                optimalPath.Add(agent.currentRoom);
                agent = agent.parent;
            }
            optimalPath.Reverse();

            List<Room> notVisited = new List<Room>();
		
		    foreach(var room in rooms.Values){
			    if (optimalPath.Contains(room)){
				    room.detour = 0;
				    room.depth = optimalPath.IndexOf(room);
                    room.crossingCount = optimalPath.Where(x => x.Equals(room)).Count();
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
			    if (door.OtherLock(currentRoom).Contains("O")){
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

                    if (!visited.Contains(room)) {
                        if (door.OtherLock(currentRoom).Contains("O")) {
                            continue;
                        }
                        if (door.lock1.Contains("s")) {
                            continue;
                        }
                        if (goals.Contains(room)) {
                            return new Tuple<int, Room>(depth + 1, room);
                        }
                        toVisit.Add(new Tuple<int, Room>(depth + 1, room));
                        visited.Add(room);
                    }
                }			
            }
		    return null;
        }
        protected static string cleanString(string str) {
            str = Regex.Replace(str, @"\d+", "");
            str = Regex.Replace(str, " ", "");
            str = Regex.Replace(str, ",", "");
            str = String.Join("", str.Distinct());
            char[] c = str.ToCharArray();
            Array.Sort(c);
            return new String(c);
        }
        public void WriteStats(string filename,SearchAgent endAgent) {
            Dictionary<string, int> dungeonStatistics = new Dictionary<string,int>() {
                {"roomsInLevel",0},
                {"enemyRoomsInLevel",0},
                {"puzzleRoomsInLevel",0},
                {"itemRoomsInLevel",0},
                {"doorsInLevel",0},
                {"passableDoorsInLevel",0},
                {"lockedDoorsInLevel",0},
                {"bombLockedDoorsInLevel",0},
                {"bigKeyDoorsInLevel",0},
                {"oneWayDoorsInLevel",0},
                {"itemLockedDoorsInLevel",0},
                {"puzzleLockedDoorsInLevel",0},
                {"softLockedDoorsInLevel",0},
                {"lookAheadsInLevel",0},
                {"totalCrossings",0},
                {"maximumCrossings",0},
                {"maximumDistanceToPath",0},
                {"pathLength",0},
                {"roomsOnPath",0},
                {"enemyRoomsOnPath",0},
                {"puzzleRoomsOnPath",0},
                {"itemRoomsOnPath",0},
                {"doorsOnPath",0},
                {"lockedDoorsOnPath",0},
                {"bombLockedDoorsOnPath",0},
                {"bigKeyLockedDoorsOnPath",0},
                {"itemLockedDoorsOnPath",0},
                {"softLockedDoorsOnPath",0},
                {"puzzleLockedDoorsOnPath",0},
                {"lookAheadsOnPath",0},
                {"oneWayDoorsOnPath",0},
                {"distanceToDungeonKey",0},
                {"distanceToSpecialItem",0},
            };
            Dictionary<string, List<string>> roomStatistics = new Dictionary<string, List<string>>(){
                {"doorTypesFrom", new List<string>()},
                {"doorTypesTo", new List<string>()},
                {"crossings", new List<string>()},
                {"depth", new List<string>()},
                {"distanceOfRoomToOptimalPath", new List<string>()},
                {"neighborTypes", new List<string>()},
                {"numberOfNeighbors", new List<string>()},
                {"roomType", new List<string>()},
                {"enemyNeighbors", new List<string>()},
                {"puzzleNeighbors", new List<string>()},
                {"itemNeighbors", new List<string>()},
                {"doorsToNeighbors", new List<string>()},
                {"passableToNeighbors", new List<string>()},
                {"lockedToNeighbors", new List<string>()},
                {"bombLockedToNeighbors", new List<string>()},
                {"itemLockedToNeighbors", new List<string>()},
                {"puzzleLockedToNeighbors", new List<string>()},
                {"softLockedToNeighbors", new List<string>()},
                {"bigKeyLockedToNeighbors", new List<string>()},
                {"oneWayLockedToNeighbors", new List<string>()},
                {"lookAheadToNeighbors", new List<string>()},

                {"passableFromNeighbors", new List<string>()},
                {"lockedFromNeighbors", new List<string>()},
                {"bombLockedFromNeighbors", new List<string>()},
                {"itemLockedFromNeighbors", new List<string>()},
                {"puzzleLockedFromNeighbors", new List<string>()},
                {"softLockedFromNeighbors", new List<string>()},
                {"bigKeyLockedFromNeighbors", new List<string>()},
                {"oneWayLockedFromNeighbors", new List<string>()},
                {"lookAheadFromNeighbors", new List<string>()},
            };
            foreach (var room in rooms.Values) {
                string type = cleanString(room.type == "," || room.type == "" ? "_" : room.type);
                roomStatistics["roomType"].Add(type);
                roomStatistics["numberOfNeighbors"].Add("" + room.neighbors.Count);
                type = "";
                string[] neighborTypes = new string[room.neighbors.Count];
                int ii = 0;
                int enemyCount = 0;
                int puzzleCount = 0;
                int itemCount = 0;
                foreach (var neighbor in room.neighbors){
                    neighborTypes[ii] = cleanString(neighbor.type == "," || neighbor.type == "" ? "_" : neighbor.type);
                    ii++;
                    enemyCount += ((neighbor.type.Contains('e')) || (neighbor.type.Contains('m')) || (neighbor.type.Contains('b')))? 1 : 0;
                    puzzleCount += neighbor.type.Contains('p')? 1 : 0;
                    itemCount += ((neighbor.type.Contains('i') ) || (neighbor.type.Contains('I') ) || 
                                                          (neighbor.type.Contains('k')) || (neighbor.type.Contains('K') ))? 1 : 0;
                } 
                 roomStatistics["enemyNeighbors"].Add(""+enemyCount);
                 roomStatistics["puzzleNeighbors"].Add("" + puzzleCount);
                 roomStatistics["puzzleNeighbors"].Add("" + itemCount);
                Dictionary<string,int> doorStats = new Dictionary<string,int>(){
                    {"doorsToNeighbors",0},
                    {"passableToNeighbors",0},
                    {"lockedToNeighbors",0},
                    {"bombLockedToNeighbors",0},
                    {"itemLockedToNeighbors",0},
                    {"puzzleLockedToNeighbors",0},
                    {"softLockedToNeighbors",0},
                    {"bigKeyLockedToNeighbors",0},
                    {"oneWayLockedToNeighbors",0},
                    {"lookAheadToNeighbors",0},

                    {"passableFromNeighbors",0},
                    {"lockedFromNeighbors",0},
                    {"bombLockedFromNeighbors",0},
                    {"itemLockedFromNeighbors",0},
                    {"puzzleLockedFromNeighbors",0},
                    {"softLockedFromNeighbors",0},
                    {"bigKeyLockedFromNeighbors",0},
                    {"oneWayLockedFromNeighbors",0},
                    {"lookAheadFromNeighbors",0},
                };
                ii = 0;
                string[] doorToTypes = new string[room.neighbors.Count];
                string[] doorFromTypes = new string[room.neighbors.Count];
                foreach (var door in room.doors) {
                    string doorLock = door.OtherLock(room);
                    doorStats["doorsToNeighbors"] += 1;
                    doorStats["passableToNeighbors"] += doorLock == "," || doorLock == "" ? 1 : 0;
                    doorStats["lockedToNeighbors"] += doorLock.Contains("k") ? 1 : 0;
                    doorStats["bombLockedToNeighbors"] += doorLock.Contains("b") ? 1 : 0;
                    doorStats["itemLockedToNeighbors"] += doorLock.Contains("I") ? 1 : 0;
                    doorStats["puzzleLockedToNeighbors"] += doorLock.Contains("S") ? 1 : 0;
                    doorStats["softLockedToNeighbors"] += doorLock.Contains("l") ? 1 : 0;
                    doorStats["bigKeyLockedToNeighbors"] += doorLock.Contains("K") ? 1 : 0;
                    doorStats["oneWayLockedToNeighbors"] += doorLock.Contains("O") ? 1 : 0;
                    doorStats["lookAheadToNeighbors"] += doorLock.Contains("s") ? 1 : 0;
                    doorLock = door.OtherLock(door.OtherRoom(room));
                    doorStats["passableFromNeighbors"] += doorLock == "," || doorLock == "" ? 1 : 0;
                    doorStats["lockedFromNeighbors"] += doorLock.Contains("k") ? 1 : 0;
                    doorStats["bombLockedFromNeighbors"] += doorLock.Contains("b") ? 1 : 0;
                    doorStats["itemLockedFromNeighbors"] += doorLock.Contains("I") ? 1 : 0;
                    doorStats["puzzleLockedFromNeighbors"] += doorLock.Contains("S") ? 1 : 0;
                    doorStats["softLockedFromNeighbors"] += doorLock.Contains("l") ? 1 : 0;
                    doorStats["bigKeyLockedFromNeighbors"] += doorLock.Contains("K") ? 1 : 0;
                    doorStats["oneWayLockedFromNeighbors"] += doorLock.Contains("O") ? 1 : 0;
                    doorStats["lookAheadFromNeighbors"] += doorLock.Contains("s") ? 1 : 0;

                    doorToTypes[ii] = cleanString(door.OtherLock(room) == "," || door.OtherLock(room) == "" ? "_" : door.OtherLock(room));
                    doorFromTypes[ii] = cleanString(door.OtherLock(door.OtherRoom(room)) == "," || door.OtherLock(door.OtherRoom(room)) == "" ? "_" : door.OtherLock(door.OtherRoom(room)));
                    if (doorToTypes[ii].Contains(",")) {
                        string t = doorToTypes[ii];
                    }
                    ii++;
                }
                foreach (var statPair in doorStats) {
                    roomStatistics[statPair.Key].Add(""+statPair.Value);
                }

                Array.Sort(neighborTypes);
                Array.Sort(doorToTypes);
                Array.Sort(doorFromTypes);
                roomStatistics["doorTypesTo"].Add(String.Join(";", doorToTypes));
                roomStatistics["doorTypesFrom"].Add(String.Join(";", doorFromTypes));
                roomStatistics["neighborTypes"].Add(String.Join(";",neighborTypes));
              //  roomStatistics["neighborTypes"].Add(cleanString(type));
                roomStatistics["distanceOfRoomToOptimalPath"].Add("" + room.detour);
                roomStatistics["depth"].Add("" + room.depth);
                roomStatistics["crossings"].Add("" + room.crossingCount);
                /*
                type = "";
                foreach (var door in room.doors){
                    type += door.OtherLock(door.OtherRoom(room)) == "" ? "_" : door.OtherLock(door.OtherRoom(room)); 
                }
                roomStatistics["doorTypesFrom"].Add(cleanString(type));
                 */

                dungeonStatistics["roomsInLevel"] += 1;
                dungeonStatistics["enemyRoomsInLevel"] += ((room.type.Contains('e')) || (room.type.Contains('m')) || (room.type.Contains('b')))? 1 : 0;
                dungeonStatistics["puzzleRoomsInLevel"] += room.type.Contains('p')? 1 : 0;
                dungeonStatistics["itemRoomsInLevel"] += ((room.type.Contains('i') ) || (room.type.Contains('I') ) || 
                                                          (room.type.Contains('k')) || (room.type.Contains('K') ))? 1 : 0;
                foreach (var door in room.doors){
                    string doorLock = door.OtherLock(room); 
                    dungeonStatistics["doorsInLevel"] += 1;
                    dungeonStatistics["passableDoorsInLevel"] += doorLock == "" ? 1 : 0;
                    dungeonStatistics["lockedDoorsInLevel"] += doorLock.Contains("k") ? 1 : 0;
                    dungeonStatistics["bombLockedDoorsInLevel"] += doorLock.Contains("b") ? 1 : 0;
                    dungeonStatistics["itemLockedDoorsInLevel"] += doorLock.Contains("I") ? 1 : 0;
                    dungeonStatistics["puzzleLockedDoorsInLevel"] += doorLock.Contains("S") ? 1 : 0;
                    dungeonStatistics["softLockedDoorsInLevel"] += doorLock.Contains("l") ? 1 : 0;
                    dungeonStatistics["bigKeyDoorsInLevel"] += doorLock.Contains("K") ? 1 : 0;
                    dungeonStatistics["oneWayDoorsInLevel"] += doorLock.Contains("O") ? 1 : 0;
                    dungeonStatistics["lookAheadsInLevel"] += doorLock.Contains("s") ? 1 : 0;
                }               
                dungeonStatistics["totalCrossings"] += room.crossingCount;
                dungeonStatistics["maximumCrossings"] = Math.Max(room.crossingCount,dungeonStatistics["maximumCrossings"]);
                dungeonStatistics["maximumDistanceToPath"] = Math.Max(room.detour, dungeonStatistics["maximumDistanceToPath"]); 
            }

            //PATH STATISTICS

            List<Room> path = new List<Room>();
            List<SearchAgent> agentPath = new List<SearchAgent>();
            SortedSet<Room> roomsOnPath = new SortedSet<Room>(new RoomComparer());
            SearchAgent agent = endAgent;
            int roomCount = 0;
            while (agent != null){
                roomsOnPath.Add(agent.currentRoom);
                path.Add(agent.currentRoom);
                agentPath.Add(agent);
                agent = agent.parent;
                roomCount++;
            }
            path.Reverse();
            agentPath.Reverse();
            dungeonStatistics["pathLength"] = roomCount;
            dungeonStatistics["roomsOnPath"] = roomsOnPath.Count;
            foreach (var room in roomsOnPath){

                dungeonStatistics["enemyRoomsOnPath"] += ((room.type.Contains('e')) || (room.type.Contains('m')) || (room.type.Contains('b')))? 1 : 0;
                dungeonStatistics["puzzleRoomsOnPath"] += room.type.Contains('p')? 1 : 0;
                dungeonStatistics["itemRoomsOnPath"] += ((room.type.Contains('i') ) || (room.type.Contains('I') ) || 
                                                          (room.type.Contains('k')) || (room.type.Contains('K') ))? 1 : 0;
                foreach (var door in room.doors){
                    string doorLock = door.OtherLock(room); 
                    dungeonStatistics["doorsOnPath"] += 1;
                    dungeonStatistics["lockedDoorsOnPath"] += doorLock.Contains("k") ? 1 : 0;
                    dungeonStatistics["bombLockedDoorsOnPath"] += doorLock.Contains("b") ? 1 : 0;
                    dungeonStatistics["itemLockedDoorsOnPath"] += doorLock.Contains("I") ? 1 : 0;
                    dungeonStatistics["puzzleLockedDoorsOnPath"] += doorLock.Contains("S") ? 1 : 0;
                    dungeonStatistics["softLockedDoorsOnPath"] += doorLock.Contains("l") ? 1 : 0;
                    dungeonStatistics["bigKeyLockedDoorsOnPath"] += doorLock.Contains("K") ? 1 : 0;
                    dungeonStatistics["oneWayDoorsOnPath"] += doorLock.Contains("O") ? 1 : 0;
                    dungeonStatistics["lookAheadsOnPath"] += doorLock.Contains("s") ? 1 : 0;
                }   
            }   
            dungeonStatistics["distanceToDungeonKey"] = path.FindIndex(a => a.type.Contains("K"));
            dungeonStatistics["distanceToSpecialItem"] = agentPath.FindIndex(a => a.keyItems > 0);
            XDocument statisticsDoc = new XDocument(new XElement("root"));
            foreach (var stat in dungeonStatistics) {
                statisticsDoc.Root.Add(new XElement(stat.Key, stat.Value));
            }
            foreach (var stat in roomStatistics) {
                statisticsDoc.Root.Add(new XElement(stat.Key, string.Join(";", stat.Value)));
            }
            statisticsDoc.Save(filename);
           
        }
    }
}
