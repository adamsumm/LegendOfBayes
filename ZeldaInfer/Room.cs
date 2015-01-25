using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Drawing;
using Priority_Queue;


namespace LearnRooms {
    public class SearchNode : Priority_Queue.PriorityQueueNode {
        public int xx;
        public int yy;
        public int depth = -1;
        public SearchNode(int x, int y, int depth = -1) {
            xx = x;
            yy = y;
            this.depth = depth;
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
    public class Room {
        public Dictionary<string, double[,]> objects = new Dictionary<string, double[,]>();
        public SearchNode[,] nodes;
        public string roomType;
        public int width;
        public int height;
        public int connections;
        public Dictionary<string, double[]> coefficients = new Dictionary<string, double[]>();
        public Room(int width, int height) {
            this.width = width;
            this.height = height;
            objects["blocks"] = new double[width, height];
            objects["enemies"] = new double[width, height];
            objects["keys"] = new double[width, height];
            objects["keyItems"] = new double[width, height];
            objects["items"] = new double[width, height];
            objects["puzzles"] = new double[width, height];
            objects["traps"] = new double[width, height];
            objects["water"] = new double[width, height];
            objects["teleporters"] = new double[width, height];
            nodes = new SearchNode[width, height];
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    nodes[ii, jj] = new SearchNode(ii, jj);
                }
            }
        }
        public Room FlipUpDown() {
            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            Room other = new Room(width, height);
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    foreach (var obj in objects) {
                        other.objects[obj.Key][ii, height - jj - 1] = obj.Value[ii, jj];
                    }
                }
            }
            return other;
        }
        public Room FlipLeftRight() {
            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            Room other = new Room(width, height);
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    foreach (var obj in objects) {
                        other.objects[obj.Key][width - ii - 1, jj] = obj.Value[ii, jj];
                    }
                }
            }
            return other;
        }
        public Room reconstruct(Dictionary<string, double[,]> components, double randomness) {

            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            Room other = new Room(width, height);
            other.coefficients = coefficients;
            int counter = 0;
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    foreach (var obj in objects) {
                        for (int kk = 0; kk < coefficients[obj.Key].Length; kk++) {
                            other.objects[obj.Key][ii, jj] += components[obj.Key][counter, kk] * coefficients[obj.Key][kk];
                        }
                    }
                    counter++;
                }
            }
            return other;
        }
        public void setCoefficients(string layer, double[,] weight, int row, int dimension) {
            coefficients[layer] = new double[dimension];
            for (int ii = 0; ii < dimension; ii++) {
                coefficients[layer][ii] = weight[row, ii];
            }
        }
        public XElement toXML() {
            Random rand = new Random();
            bool[][] reachability = GetReachability(rand);
            string rstring = "";
            int bitmask = 0;
            int ii = 0;
            foreach (var row in reachability) {
                foreach (var d in row) {
                    bitmask += (int)Math.Pow(2, ii);
                    ii++;
                }
                rstring += string.Join(",", row) + ";";
            }
            XElement room = new XElement("Room", new XAttribute("reachability", bitmask));
            foreach (var layer in coefficients) {
                room.Add(new XElement("Coefficients", new XAttribute("name", layer.Key), String.Join(",", layer.Value.Select(p => p.ToString()).ToArray())));
            }
            return room;
        }

        public void setType() {

            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            SortedSet<string> types = new SortedSet<string>();
            nodes = new SearchNode[width, height];
            roomType = "";
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    if (0.9 < objects["enemies"][ii, jj]) {
                        types.Add("e");
                    }
                    if (0.9 < objects["keys"][ii, jj]) {
                        types.Add("k");
                    }
                    if (0.9 < objects["keyItems"][ii, jj]) {
                        types.Add("I");
                    }
                    if (0.9 < objects["items"][ii, jj]) {
                        types.Add("i");
                    }
                    if (0.9 < objects["puzzles"][ii, jj]) {
                        types.Add("p");
                    }
                    if (0.9 < objects["traps"][ii, jj]) {
                        types.Add("p");
                    }
                    if (0 < objects["water"][ii, jj]) {
                        types.Add("p");
                    }
                    if (0 < objects["teleporters"][ii, jj]) {
                        types.Add("t");
                    }
                }
            }
            foreach (var t in types) {
                roomType += t;
            }
            if (roomType == "") {
                roomType = "_";
            }

        }
        public List<SearchNode> GetNeighbors(SearchNode node,Random rng) {
            List<SearchNode> neighbors = new List<SearchNode>();
            if (node.xx > 0) {
                neighbors.Add(nodes[node.xx - 1, node.yy]);
            }
            if (node.yy > 0) {
                neighbors.Add(nodes[node.xx, node.yy - 1]);
            }
            if (node.xx + 1 < objects["blocks"].GetLength(0)) {
                neighbors.Add(nodes[node.xx + 1, node.yy]);
            }
            if (node.yy + 1 < objects["blocks"].GetLength(1)) {
                neighbors.Add(nodes[node.xx, node.yy + 1]);
            }
            neighbors.Shuffle(rng);
            return neighbors;
        }
        static double AngleBetween(double[] vec1, double[] vec2) {
            double v1 = 0;
            double v2 = 0;
            double v12 = 0;
            for (int ii = 0; ii < vec1.Length; ii++) {
                v1 += vec1[ii] * vec1[ii];
                v2 += vec2[ii] * vec2[ii];
                v12 += vec1[ii] * vec2[ii];
            }
            return Math.Acos(v12 / (Math.Sqrt(v1) * Math.Sqrt(v2))); ;
        }
        public static Room Interpolate(Room room1, Room room2, float t) {
            Room output = new Room(room1.width, room1.height);
            foreach (var layer in room1.coefficients.Keys) {
                double[] coeff1 = room1.coefficients[layer];
                double[] coeff2 = room2.coefficients[layer];
                double angleBetween = AngleBetween(coeff1, coeff2);
                if (double.IsNaN(angleBetween)) {
                    angleBetween = 1;
                }
                double[] interp = new double[coeff1.Length];
                double s = Math.Sin(angleBetween);
                for (int ii = 0; ii < interp.Length; ii++) {
                      interp[ii] = coeff1[ii] * (1 - t) + coeff2[ii] * t;
                   // interp[ii] = coeff1[ii] * Math.Sin((1 - t) * angleBetween) / s + coeff2[ii] * Math.Sin(t * angleBetween) / s;
                }
                output.coefficients[layer] = interp;
            }
            return output;
        }
        public bool[][] GetReachability(Random rand) {
            nodes = new SearchNode[width, height];
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    nodes[ii, jj] = new SearchNode(ii, jj);
                }
            }
            bool[] doors = new bool[4];
            for (int ii = 0; ii < 4; ii++) {
                doors[ii] = true;
            }
            foreach (var type in objects.Values) {
                doors[0] = doors[0] && type[0, 5] < 0.5f && type[0, 6] < 0.5f;
                doors[1] = doors[1] && type[width - 1, 5] < 0.5f && type[width - 1, 6] < 0.5f;
                doors[2] = doors[2] && type[4, height - 1] < 0.5f && type[5, height - 1] < 0.5f;
                doors[3] = doors[3] && type[4, 0] < 0.5f && type[5, 0] < 0.5f;
            }
            int[][] doorLocs = new int[4][];
            doorLocs[0] = new int[] { 0, 5 };
            doorLocs[1] = new int[] { width - 1, 5 };
            doorLocs[2] = new int[] { 4, height - 1 };
            doorLocs[3] = new int[] { 4, 0 };
            bool[][] reachabilityTable = new bool[4][];
            for (int ii = 0; ii < 4; ii++) {
                reachabilityTable[ii] = new bool[4];
            }
            for (int ii = 0; ii < 4; ii++) {
                for (int jj = ii; jj < 4; jj++) {
                    reachabilityTable[ii][jj] = doors[ii] && doors[jj];
                    reachabilityTable[jj][ii] = doors[ii] && doors[jj];
                    if (reachabilityTable[ii][jj]) {
                        List<SearchNode> path = getPath(nodes[doorLocs[ii][0], doorLocs[ii][1]], nodes[doorLocs[jj][0], doorLocs[jj][1]], getBlocked,rand);
                        foreach (var node in path) {
                            if (objects["blocks"][node.xx, node.yy] > 0.5f) {
                                reachabilityTable[ii][jj] = false;
                                reachabilityTable[jj][ii] = false;
                                break;
                            }
                        }
                    }
                }
            }
            int bitcount = 0;
            int bitmask = 0;
            foreach (var row in reachabilityTable) {
                foreach (var d in row) {
                    if (d) {
                        bitmask += (int)Math.Pow(2, bitcount);
                    }
                    bitcount++;
                }
            }
            connections = bitmask;
            return reachabilityTable;
        }
        public bool containsObject(string objectType) {
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    if (objects[objectType][ii, jj] > 0.5f) {
                        return true;
                    }
                }
            }
            return false;
        }
        public delegate float scoringFunction(SearchNode a, SearchNode b);
        public float getScore(SearchNode a, SearchNode b) {
            /*
            return 1f + (float)(0.1*Math.Abs(objects["blocks"][a.xx, a.yy] - objects["blocks"][b.xx, b.yy])) + (float)(
                2 * Math.Abs(objects["traps"][a.xx, a.yy] - objects["traps"][b.xx, b.yy]) +
                5 * Math.Abs(objects["keys"][a.xx, a.yy] - objects["keys"][b.xx, b.yy]) +
               5 * Math.Abs(objects["keyItems"][a.xx, a.yy] - objects["keyItems"][b.xx, b.yy]) +
               2 * Math.Abs(objects["items"][a.xx, a.yy] - objects["items"][b.xx, b.yy]) +
               5 * Math.Abs(objects["puzzles"][a.xx, a.yy] - objects["puzzles"][b.xx, b.yy]) +
                2 * Math.Abs(objects["water"][a.xx, a.yy] - objects["water"][b.xx, b.yy]) +
                5 * Math.Abs(objects["teleporters"][a.xx, a.yy] - objects["teleporters"][b.xx, b.yy]) +
                5 * Math.Abs(objects["enemies"][a.xx, a.yy] - objects["enemies"][b.xx, b.yy])

                );
             * */
            return 1 + (float)(1.5 * Math.Abs(objects["blocks"][a.xx, a.yy] - objects["blocks"][b.xx, b.yy]) +
                2 * Math.Abs(objects["traps"][b.xx, b.yy]) +
                5 * Math.Abs(objects["keys"][b.xx, b.yy]) +
               5 * Math.Abs(objects["keyItems"][b.xx, b.yy]) +
               2 * Math.Abs(objects["items"][b.xx, b.yy]) +
               5 * Math.Abs(objects["puzzles"][b.xx, b.yy]) +
                2 * Math.Abs(objects["water"][b.xx, b.yy]) +
                5 * Math.Abs(objects["teleporters"][b.xx, b.yy]) +
                5 * Math.Abs(objects["enemies"][b.xx, b.yy]));
        }
        public float getBlocked(SearchNode a, SearchNode b) {
            /*
            return 1f + (float)(0.1*Math.Abs(objects["blocks"][a.xx, a.yy] - objects["blocks"][b.xx, b.yy])) + (float)(
                2 * Math.Abs(objects["traps"][a.xx, a.yy] - objects["traps"][b.xx, b.yy]) +
                5 * Math.Abs(objects["keys"][a.xx, a.yy] - objects["keys"][b.xx, b.yy]) +
               5 * Math.Abs(objects["keyItems"][a.xx, a.yy] - objects["keyItems"][b.xx, b.yy]) +
               2 * Math.Abs(objects["items"][a.xx, a.yy] - objects["items"][b.xx, b.yy]) +
               5 * Math.Abs(objects["puzzles"][a.xx, a.yy] - objects["puzzles"][b.xx, b.yy]) +
                2 * Math.Abs(objects["water"][a.xx, a.yy] - objects["water"][b.xx, b.yy]) +
                5 * Math.Abs(objects["teleporters"][a.xx, a.yy] - objects["teleporters"][b.xx, b.yy]) +
                5 * Math.Abs(objects["enemies"][a.xx, a.yy] - objects["enemies"][b.xx, b.yy])

                );
             * */
            return 1000 * ((float)objects["blocks"][b.xx, b.yy]);
        }
        public List<SearchNode> getPath(SearchNode start, SearchNode end, scoringFunction func,Random rng) {
            List<SearchNode> optimalPath = new List<SearchNode>();
            HashSet<int> closeSet = new HashSet<int>();
            HeapPriorityQueue<SearchNode> openSet = new HeapPriorityQueue<SearchNode>(80000);
            Dictionary<int, double> gScore = new Dictionary<int, double>();
            Dictionary<int, double> fScore = new Dictionary<int, double>();
            Dictionary<int, SearchNode> nodes = new Dictionary<int, SearchNode>();
            Dictionary<SearchNode, SearchNode> cameFrom = new Dictionary<SearchNode, SearchNode>();
            gScore[start.GetHashCode()] = 0;
            fScore[start.GetHashCode()] = start.dist(end);
            openSet.Enqueue(start, 0);
            while (openSet.Count > 0) {
                SearchNode current = openSet.Dequeue();
                if (current == end) {
                    optimalPath.Add(current);
                    while (cameFrom.ContainsKey(current)) {
                        current = cameFrom[current];
                        optimalPath.Add(current);
                    }
                    return optimalPath;
                }
                closeSet.Add(current.GetHashCode());
                foreach (var neighbor in GetNeighbors(current,rng)) {
                    if (!closeSet.Contains(neighbor.GetHashCode())) {
                        double tentativeGScore = gScore[current.GetHashCode()] + func(current, neighbor);
                        if (!gScore.ContainsKey(neighbor.GetHashCode())) {
                            cameFrom[neighbor] = current;
                            gScore[neighbor.GetHashCode()] = tentativeGScore;
                            fScore[neighbor.GetHashCode()] = tentativeGScore + neighbor.dist(end);

                            openSet.Enqueue(neighbor, fScore[neighbor.GetHashCode()]);
                        }
                        else if (gScore[neighbor.GetHashCode()] > tentativeGScore) {
                            cameFrom[neighbor] = current;
                            gScore[neighbor.GetHashCode()] = tentativeGScore;
                            fScore[neighbor.GetHashCode()] = tentativeGScore + neighbor.dist(end);

                            openSet.UpdatePriority(neighbor, fScore[neighbor.GetHashCode()]);
                        }
                    }
                }
            }

            return optimalPath;
        }
        public void changeWidth(int newWidth, Random rng) {
            this.width = newWidth;
            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            double[,] newBlock = new double[newWidth, height];
            double[,] newEnemies = new double[newWidth, height];
            double[,] newKeys = new double[newWidth, height];
            double[,] newKeyItems = new double[newWidth, height];
            double[,] newItems = new double[newWidth, height];
            double[,] newPuzzles = new double[newWidth, height];
            double[,] newTraps = new double[newWidth, height];
            double[,] newWater = new double[newWidth, height];
            double[,] newTeleporters = new double[newWidth, height];
            int widthDiff = newWidth - width;
            List<SearchNode> optimalPath = getPath(nodes[width / 2, 0], nodes[width / 2, height - 1], getScore, rng);
            SearchNode[] duplicatedPoints = new SearchNode[height];
            foreach (SearchNode node in optimalPath) {
                if (duplicatedPoints[node.yy] == null) {
                    duplicatedPoints[node.yy] = node;
                }
            }
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    if (ii < duplicatedPoints[jj].xx) {
                        newBlock[ii, jj] = objects["blocks"][ii, jj];
                        newEnemies[ii, jj] = objects["enemies"][ii, jj];
                        newKeys[ii, jj] = objects["keys"][ii, jj];
                        newKeyItems[ii, jj] = objects["keyItems"][ii, jj];
                        newItems[ii, jj] = objects["items"][ii, jj];
                        newPuzzles[ii, jj] = objects["puzzles"][ii, jj];
                        newTraps[ii, jj] = objects["traps"][ii, jj];
                        newWater[ii, jj] = objects["water"][ii, jj];
                        newTeleporters[ii, jj] = objects["teleporters"][ii, jj];
                    }
                    else if (ii > duplicatedPoints[jj].xx) {
                        newBlock[ii + widthDiff, jj] = objects["blocks"][ii, jj];
                        newEnemies[ii + widthDiff, jj] = objects["enemies"][ii, jj];
                        newKeys[ii + widthDiff, jj] = objects["keys"][ii, jj];
                        newKeyItems[ii + widthDiff, jj] = objects["keyItems"][ii, jj];
                        newItems[ii + widthDiff, jj] = objects["items"][ii, jj];
                        newPuzzles[ii + widthDiff, jj] = objects["puzzles"][ii, jj];
                        newTraps[ii + widthDiff, jj] = objects["traps"][ii, jj];
                        newWater[ii + widthDiff, jj] = objects["water"][ii, jj];
                        newTeleporters[ii + widthDiff, jj] = objects["teleporters"][ii, jj];

                    }
                    else {
                        for (int xx = 0; xx <= widthDiff; xx++) {
                            newBlock[ii + xx, jj] = objects["blocks"][duplicatedPoints[jj].xx, jj];
                            newEnemies[ii + xx, jj] = objects["enemies"][duplicatedPoints[jj].xx, jj];
                            newKeys[ii, jj] = objects["keys"][duplicatedPoints[jj].xx, jj];
                            newKeyItems[ii + xx, jj] = objects["keyItems"][duplicatedPoints[jj].xx, jj];
                            newItems[ii + xx, jj] = objects["items"][duplicatedPoints[jj].xx, jj];
                            newPuzzles[ii + xx, jj] = objects["puzzles"][duplicatedPoints[jj].xx, jj];
                            newTraps[ii + xx, jj] = objects["traps"][duplicatedPoints[jj].xx, jj];
                            newWater[ii + xx, jj] = objects["water"][duplicatedPoints[jj].xx, jj];
                            newTeleporters[ii + xx, jj] = objects["teleporters"][duplicatedPoints[jj].xx, jj];
                        }
                    }
                }
            }
            objects["blocks"] = newBlock;
            objects["water"] = newWater;
            objects["teleporters"] = newTeleporters;
            objects["puzzles"] = newPuzzles;
            objects["traps"] = newTraps;
            objects["keyItems"] = newKeyItems;
            objects["items"] = newItems;
            objects["keys"] = newKeys;
            objects["enemies"] = newEnemies;
            nodes = new SearchNode[newWidth, height];
            for (int ii = 0; ii < newWidth; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    nodes[ii, jj] = new SearchNode(ii, jj);
                }
            }
        }
        public void changeHeight(int newHeight,Random rng) {
            this.height = newHeight;
            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            double[,] newBlock = new double[width, newHeight];
            double[,] newEnemies = new double[width, newHeight];
            double[,] newKeys = new double[width, newHeight];
            double[,] newKeyItems = new double[width, newHeight];
            double[,] newItems = new double[width, newHeight];
            double[,] newPuzzles = new double[width, newHeight];
            double[,] newTraps = new double[width, newHeight];
            double[,] newWater = new double[width, newHeight];
            double[,] newTeleporters = new double[width, newHeight];
            int heightDiff = newHeight - height;
            List<SearchNode> optimalPath = getPath(nodes[0, height / 2], nodes[width - 1, height / 2], getScore,rng);
            SearchNode[] duplicatedPoints = new SearchNode[width];
            foreach (SearchNode node in optimalPath) {
                if (duplicatedPoints[node.xx] == null) {
                    duplicatedPoints[node.xx] = node;
                }
            }
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    if (jj < duplicatedPoints[ii].yy) {
                        newBlock[ii, jj] = objects["blocks"][ii, jj];
                        newEnemies[ii, jj] = objects["enemies"][ii, jj];
                        newKeys[ii, jj] = objects["keys"][ii, jj];
                        newKeyItems[ii, jj] = objects["keyItems"][ii, jj];
                        newItems[ii, jj] = objects["items"][ii, jj];
                        newPuzzles[ii, jj] = objects["puzzles"][ii, jj];
                        newTraps[ii, jj] = objects["traps"][ii, jj];
                        newWater[ii, jj] = objects["water"][ii, jj];
                        newTeleporters[ii, jj] = objects["teleporters"][ii, jj];
                    }
                    else if (jj > duplicatedPoints[ii].yy) {
                        newBlock[ii, jj + heightDiff] = objects["blocks"][ii, jj];
                        newEnemies[ii, jj + heightDiff] = objects["enemies"][ii, jj];
                        newKeys[ii, jj + heightDiff] = objects["keys"][ii, jj];
                        newKeyItems[ii, jj + heightDiff] = objects["keyItems"][ii, jj];
                        newItems[ii, jj + heightDiff] = objects["items"][ii, jj];
                        newPuzzles[ii, jj + heightDiff] = objects["puzzles"][ii, jj];
                        newTraps[ii, jj + heightDiff] = objects["traps"][ii, jj];
                        newWater[ii, jj + heightDiff] = objects["water"][ii, jj];
                        newTeleporters[ii, jj + heightDiff] = objects["teleporters"][ii, jj];

                    }
                    else {
                        for (int yy = 0; yy <= heightDiff; yy++) {
                            newBlock[ii, jj + yy] = objects["blocks"][ii, duplicatedPoints[ii].yy];
                            newEnemies[ii, jj + yy] = objects["enemies"][ii, duplicatedPoints[ii].yy];
                            newKeys[ii, jj + yy] = objects["keys"][ii, duplicatedPoints[ii].yy];
                            newKeyItems[ii, jj + yy] = objects["keyItems"][ii, duplicatedPoints[ii].yy];
                            newItems[ii, jj + yy] = objects["items"][ii, duplicatedPoints[ii].yy];
                            newPuzzles[ii, jj + yy] = objects["puzzles"][ii, duplicatedPoints[ii].yy];
                            newTraps[ii, jj + yy] = objects["traps"][ii, duplicatedPoints[ii].yy];
                            newWater[ii, jj + yy] = objects["water"][ii, duplicatedPoints[ii].yy];
                            newTeleporters[ii, jj + yy] = objects["teleporters"][ii, duplicatedPoints[ii].yy];
                        }
                    }
                }
            }
            objects["blocks"] = newBlock;
            objects["water"] = newWater;
            objects["teleporters"] = newTeleporters;
            objects["puzzles"] = newPuzzles;
            objects["traps"] = newTraps;
            objects["keyItems"] = newKeyItems;
            objects["items"] = newItems;
            objects["keys"] = newKeys;
            objects["enemies"] = newEnemies;
            nodes = new SearchNode[width, newHeight];
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < newHeight; jj++) {
                    nodes[ii, jj] = new SearchNode(ii, jj);
                }
            }
        }
        public void changeSize(int width, int height,Random rng) {
            for (int newHeight = objects["blocks"].GetLength(1) + 1; newHeight <= height; newHeight++) {
                changeHeight(newHeight,rng);
            }
            for (int newWidth = objects["blocks"].GetLength(0) + 1; newWidth <= width; newWidth++) {
                changeWidth(newWidth, rng);
            }
        }
        public Bitmap toBitmap() {
            Bitmap bitmap = new Bitmap(objects["blocks"].GetLength(0), objects["blocks"].GetLength(1));
            for (int ii = 0; ii < objects["blocks"].GetLength(0); ii++) {
                for (int jj = 0; jj < objects["blocks"].GetLength(1); jj++) {
                    if (objects["blocks"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Black);
                    }
                    else if (objects["enemies"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Red);
                    }
                    else if (objects["traps"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.DarkRed);
                    }
                    else if (objects["items"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Green);
                    }
                    else if (objects["keys"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Orange);
                    }
                    else if (objects["keyItems"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Yellow);
                    }
                    else if (objects["teleporters"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Purple);
                    }
                    else if (objects["water"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Blue);
                    }
                    else if (objects["puzzles"][ii, jj] > 0.5) {
                        bitmap.SetPixel(ii, jj, Color.Turquoise);
                    }
                    else {
                        bitmap.SetPixel(ii, jj, Color.White);
                    }
                }
            }
            return bitmap;
        }
        public bool isAllZero() {
            for (int ii = 0; ii < objects["blocks"].GetLength(0); ii++) {
                for (int jj = 0; jj < objects["blocks"].GetLength(1); jj++) {
                    if (objects["blocks"][ii, jj] != 0 || objects["enemies"][ii, jj] != 0 ||
                        objects["keys"][ii, jj] != 0 || objects["keyItems"][ii, jj] != 0 ||
                        objects["items"][ii, jj] != 0 || objects["puzzles"][ii, jj] != 0 ||
                        objects["traps"][ii, jj] != 0 || objects["water"][ii, jj] != 0 ||
                        objects["teleporters"][ii, jj] != 0) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
