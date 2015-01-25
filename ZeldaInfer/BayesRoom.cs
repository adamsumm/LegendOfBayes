using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Distributions;

namespace ZeldaInfer {
    public class BayesRoom { 
        public int roomType;
        public int doorGoTo;
        public int numberOfNeighbors;
        public int  pureDepth;
        public BayesRoom parent;
        public List<BayesRoom> children = new List<BayesRoom>();
        public int xx;
        public int yy;
        public Discrete roomDist;
        public Discrete doorDist;
        public string location() {
            return "" + xx + "," + yy;
        }
        public BayesRoom(   int roomType,
                            int doorGoTo,
                            int numberOfNeighbors,
                            int  pureDepth,
                            BayesRoom parent,
                            int xx,
                            int yy,
                            Discrete roomDist,
                            Discrete doorDist) {
                                this.roomDist = roomDist;
                                this.roomType = roomType;
                                this.doorGoTo = doorGoTo;
                                this.numberOfNeighbors = numberOfNeighbors;
                                this.pureDepth = pureDepth;
                                this.parent = parent;
                                this.xx = xx;
                                this.yy = yy;
                                this.doorDist = doorDist;

        }  
    }
}
