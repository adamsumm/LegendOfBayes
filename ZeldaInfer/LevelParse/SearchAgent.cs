using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeldaInfer.LevelParse {
    public class SearchAgent : Priority_Queue.PriorityQueueNode {
        public List<Room> pathSoFar;
        public int keysAcquired;
        public int keysSpent;
        public bool bigKey;
        public bool requiresBigKey;
        public int items;
        public int keyItems;
        public bool gotItem = false;
        public List<string> switchesSet;
        public SortedSet<Room> visited;
        public List<SearchAgent> agentPath;
        public Room currentRoom;
        public SearchAgent(List<Room> pathSoFar,
                           int keysAcquired,
                           int keysSpent,
                           bool bigKey,
                           bool requiresBigKey,
                           int items,
                           int keyItems,
                           List<string> switchesSet,
                           Room currentRoom,
                           List<SearchAgent> agentPath,
                           SortedSet<Room> visited) {
            this.pathSoFar = new List<Room>(pathSoFar);
            this.agentPath = new List<SearchAgent>(agentPath);
            this.agentPath.Add(this);
            this.keysAcquired = keysAcquired;
            this.keysSpent = keysSpent;
            this.bigKey = bigKey;

            this.requiresBigKey = requiresBigKey;
            this.items = items;
            this.keyItems = keyItems;
            this.switchesSet = new List<string>(switchesSet);
            this.visited = new SortedSet<Room>(visited, new RoomComparer());
            bool canAdd = true;
            if (!visited.Contains(currentRoom)) {
                if (currentRoom.type.Contains("k")) {
                    this.keysAcquired++;
                    this.gotItem = true;
                }
                if (currentRoom.type.Contains("i")) {
                    this.items++;
                    this.gotItem = true;
                }
                if (currentRoom.type.Contains("K")) {
                    this.bigKey = true;
                    this.gotItem = true;
                }
                foreach (var type in currentRoom.type.Split(new char[] { ' ' })) {
                    if (type.Contains("S")) {
                        this.switchesSet.Add(type);
                        this.gotItem = true;
                    }
                }
                if (currentRoom.type.Contains("I")) {
                    if (!this.requiresBigKey || this.bigKey) {
                        this.keyItems++;
                        this.gotItem = true;
                    }
                    else {
                        canAdd = false;
                    }
                }
            }
            if (canAdd) {
                this.visited.Add(currentRoom);
            }
            this.pathSoFar.Add(currentRoom);
            this.currentRoom = currentRoom;
        }
        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {  
 

                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 486187739 + keysAcquired.GetHashCode();
                hash = hash * 486187739 + keysSpent.GetHashCode();
                hash = hash * 486187739 + bigKey.GetHashCode();
                hash = hash * 486187739 + items.GetHashCode();
                hash = hash * 486187739 + keyItems.GetHashCode();
              //  Console.WriteLine(keyItems.GetHashCode());
                hash = hash * 486187739 + currentRoom.GetHashCode();
                foreach (var room in visited) {
            //    foreach (var room in pathSoFar) {
                    hash = hash * 486187739 + room.GetHashCode();
                }
                foreach (var switchSet in switchesSet) {
                    hash = hash * 486187739 + switchSet.GetHashCode();
                }
                return hash;
            }
        }
        public string pathToString() {
            string str = "";
            foreach (var agent in agentPath) {
                str += "(" + agent.bigKey + "," + agent.keyItems + "," + agent.keysAcquired + "," + agent.keysSpent + "," + agent.currentRoom.type + ") > \n ";
            }
            return str;
        }
        public List<SearchAgent> GetChildren() {
            Room cameFrom = null;
            if (pathSoFar.Count > 1) {
                cameFrom = pathSoFar[pathSoFar.Count - 2];
            }
            List<SearchAgent> children = new List<SearchAgent>();

            if (currentRoom.neighbors.Count == 1) {
                children.Add(new SearchAgent(pathSoFar, keysAcquired,keysSpent, 
                                             bigKey, requiresBigKey, 
                                             items, keyItems,
                                             switchesSet, currentRoom.neighbors[0],agentPath,visited));
            }
            else {
                foreach (var door in currentRoom.doors) {
                    Room otherRoom = door.OtherRoom(currentRoom);
                    if ((door.lock1.Contains('s')) ||
                        (!bigKey && door.lock1.Contains('K')) ||
                        (keyItems == 0 && door.lock1.Contains("I")) ||
                        door.OtherLock(currentRoom).Contains("1") || 
                        (otherRoom == cameFrom && !gotItem)){
                            continue;
                    }
                    if (door.lock1.Contains("S")) {
                        bool canContinue = false;
                        foreach (var type in door.lock1.Split(new char[] {','})) {
                            if (switchesSet.Contains(type)) {
                                canContinue = true;
                                break;
                            }
                        }
                        if (!canContinue) {
                            continue;
                        }
                    }
                    int childKeysSpent = keysSpent;
                    if (door.lock1.Contains("k") && !pathSoFar.Contains(otherRoom)) {
                        childKeysSpent++;
                    }
                    if (keysAcquired-childKeysSpent < 0) {
                        continue;
                    }
                    children.Add(new SearchAgent(pathSoFar, keysAcquired, childKeysSpent, 
                                                 bigKey, requiresBigKey,
                                                 items, keyItems, 
                                                 switchesSet, otherRoom, agentPath,visited));

                }
            }
            return children;
        }
    }
}
