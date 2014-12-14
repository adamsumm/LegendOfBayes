using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearnRooms {
    public class SearchNode :  Priority_Queue.PriorityQueueNode {
        public int xx;
        public int yy;
        public SearchNode(int x, int y) {
            xx = x;
            yy = y;
        }
        public int dist(SearchNode other) {
            return Math.Abs(xx - other.xx) + Math.Abs(yy - other.yy);
        }
        public bool Equals(SearchNode p) {
            return p.xx == xx && p.yy == yy;
        }

        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {
                return ((xx * 486187739) + yy) * 486187739;
            }
        }
    }
}
