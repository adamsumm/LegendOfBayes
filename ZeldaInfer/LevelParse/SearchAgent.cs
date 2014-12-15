using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeldaInfer.LevelParse {
    public class SearchAgent : Priority_Queue.PriorityQueueNode {
        public SearchAgent parent;
        public int keysAcquired;
        public int keysSpent;
        public bool bigKey;
        public bool requiresBigKey;
        public int items;
        public int keyItems;
        public bool canAdd = true;
        public bool gotItem = false;
        public Room currentRoom;
        public int actualHash = -1;
        public bool hasVisited(Room room){
            if (currentRoom == room){
                return canAdd;
            }
            if (parent != null) {
                return parent.hasVisited(room);
            }
            else {
                return false;
            }
        }
        public bool switchSet(string sw) {
          
            if (currentRoom.type.Contains(sw)) {
                return canAdd;
            }
            if (parent != null) {
                return parent.switchSet(sw);
            }
            else {
                return false;
            }
        }
        public SearchAgent(SearchAgent parent,
                           int keysAcquired,
                           int keysSpent,
                           bool bigKey,
                           bool requiresBigKey,
                           int items,
                           int keyItems,
                           Room currentRoom) {
            this.keysAcquired = keysAcquired;
            this.keysSpent = keysSpent;
            this.bigKey = bigKey;
            this.parent = parent;
            this.requiresBigKey = requiresBigKey;
            this.items = items;
            this.keyItems = keyItems;
            if (!hasVisited(currentRoom)) {
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
                foreach (var type in currentRoom.type.Split(new char[] { ',' })) {
                    if (type.Contains("S")) {
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
            this.currentRoom = currentRoom;
        }

        public override int GetHashCode() {
            if (actualHash == -1) {
                unchecked // Overflow is fine, just wrap
                {
                    SortedSet<Room> rooms = new SortedSet<Room>(new RoomComparer());

                    int hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 486187739 + keysAcquired.GetHashCode();
                    hash = hash * 486187739 + keysSpent.GetHashCode();
                    hash = hash * 486187739 + bigKey.GetHashCode();
                    hash = hash * 486187739 + items.GetHashCode();
                    hash = hash * 486187739 + keyItems.GetHashCode();
                    hash = hash * 486187739 + currentRoom.GetHashCode();
                    SearchAgent agent = this;
                    while (agent != null) {
                        rooms.Add(agent.currentRoom);
                        agent = agent.parent;
                    }
                    foreach (var room in rooms) {
                        hash = hash * 486187739 + room.GetHashCode();

                    }
                    actualHash = hash;
                    return hash;
                }
            }
            else {
                return actualHash;
            }
          
        }
        public string pathToString() {
            string str = "";
            SearchAgent agent = this;
            while (agent != null) {
                str +=  "(" + agent.bigKey + "," + agent.keyItems + "," + agent.keysAcquired + "," + agent.keysSpent + "," + agent.currentRoom.type + ",";
                str += ") > \n ";
                agent = agent.parent;
            }
            /*
            foreach (var agent in agentPath) {
                str += "(" + agent.bigKey + "," + agent.keyItems + "," + agent.keysAcquired + "," + agent.keysSpent + "," + agent.currentRoom.type + ",";
                foreach (var sw in agent.switchesSet) {
                    str += sw + ":";
                }
                str += ") > \n ";
            }
             * */
            return str;
        }
        public List<SearchAgent> GetChildren() {
           
            List<SearchAgent> children = new List<SearchAgent>();

            if (currentRoom.neighbors.Count == 1) {
                children.Add(new SearchAgent(this, keysAcquired,keysSpent, 
                                             bigKey, requiresBigKey, 
                                             items, keyItems,
                                             currentRoom.neighbors[0]));
            }
            else {
                foreach (var door in currentRoom.doors) {
                    Room otherRoom = door.OtherRoom(currentRoom);
                    if ((door.lock1.Contains('s')) ||
                        (!bigKey && door.lock1.Contains('K')) ||
                        (keyItems == 0 && door.lock1.Contains("I")) ||
                        door.OtherLock(currentRoom).Contains("O") || 
                        (parent!= null && otherRoom == parent.currentRoom && !gotItem)){

                            continue;
                    }
                    if (door.lock1.Contains("S")) {
                        bool canContinue = true;
                       
                        foreach (var type in door.lock1.Split(new char[] {','})) {
                            if (type.Contains("S")) {
                                
                                if (!switchSet(type)) {
                                    canContinue = false;
                                    break;
                                }
                            }
                        }
                        if (!canContinue) {
                            continue;
                        }
                    }
                    int childKeysSpent = keysSpent;
                    if (door.lock1.Contains("k") && !hasVisited(otherRoom)) {
                        childKeysSpent++;
                    }
                    if (keysAcquired-childKeysSpent < 0) {
                        continue;
                    }
                    children.Add(new SearchAgent(this, keysAcquired, childKeysSpent, 
                                                 bigKey, requiresBigKey,
                                                 items, keyItems, 
                                                  otherRoom));

                }
            }
          
            return children;
        }
    }
}
