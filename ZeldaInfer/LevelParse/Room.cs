using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeldaInfer.LevelParse {
    public class RoomComparer : IComparer<Room> {
        public int Compare(Room a, Room b) { return a.id.CompareTo(b.id); }
    }
    public class Room {
        public List<Door> doors = new List<Door>();
        public int depth = -1;
        public int pureDepth = -1;
        public int detour = -1;
        public int id;
        public int crossingCount = 0;
        public List<Room> neighbors = new List<Room>();
        public string type;
        public Room(string type, int id) {
            this.type = type;
            this.id = id;
        }public class MyComparer : IComparer<string>
{
    public int Compare(string a, string b)
    { return a.CompareTo(b); }
}
        public void Connect(Room other,string lock1,string lock2){
            if (!neighbors.Contains(other)) {
                neighbors.Add(other);
                doors.Add(new Door(lock1, lock2, this, other));
                other.Connect(this,lock2,lock1);
            }
        }
    }
}
