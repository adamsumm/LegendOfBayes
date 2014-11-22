using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeldaInfer.LevelParse {
    public class Door {
        public string lock1;
        public string lock2;
        public Room room1;
        public Room room2;
        public Door(string lock1,string lock2,Room room1,Room room2){
            this.lock1 = lock1;
            this.lock2 = lock2;
            this.room1 = room1;
            this.room2 = room2;
        }
        public string OtherLock(Room room) {
            if (room1 == room) {
                return lock2;
            }
            else {
                return lock2;
            }
        }
        public Room OtherRoom(Room room) {
            if (room1 == room) {
                return room2;
            }
            else {
                return room1;
            }
        }
    }
}
