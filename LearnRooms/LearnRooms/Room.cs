using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Priority_Queue;
using SimpleLottery;
namespace LearnRooms {
    public class Room {
        public Dictionary<string, double[,]> objects = new Dictionary<string, double[,]>();
        public SearchNode[,] nodes;
        public string roomType;
        public int width;
        public int height;
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
                        other.objects[obj.Key][width-ii-1,jj] = obj.Value[ii, jj];
                    }
                }
            }
            return other;
        }
        public Room reconstruct(Dictionary<string, double[,]> components,double randomness) {

            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            Room other = new Room(width, height);
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

        public void setType() {

            int width = objects["blocks"].GetLength(0);
            int height = objects["blocks"].GetLength(1);
            SortedSet<string> types = new SortedSet<string>();
            nodes = new SearchNode[width, height];
            roomType = "";
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    if (0.9 <objects["enemies"][ii, jj]) {
                        types.Add("e");
                    }
                    if (0.9 <objects["keys"][ii, jj]) {
                        types.Add("k");
                    }
                    if (0.9 <objects["keyItems"][ii, jj]) {
                        types.Add("I");
                    }
                    if (0.9 <objects["items"][ii, jj]) {
                        types.Add("i");
                    }
                    if (0.9 <objects["puzzles"][ii, jj]) {
                        types.Add("p");
                    }
                    if (0.9 <objects["traps"][ii, jj]) {
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
            
        }
        public List<SearchNode> GetNeighbors(SearchNode node) {
            List<SearchNode> neighbors = new List<SearchNode>();
            if (node.xx > 0) {
                neighbors.Add(nodes[node.xx - 1, node.yy]);
            }
            if (node.yy > 0) {
                neighbors.Add(nodes[node.xx, node.yy - 1]);
            }
            if (node.xx+1 < objects["blocks"].GetLength(0) ) {
                neighbors.Add(nodes[node.xx + 1, node.yy]);
            }
            if (node.yy + 1 < objects["blocks"].GetLength(1)) {
                neighbors.Add(nodes[node.xx, node.yy + 1]);
            }
            neighbors.Shuffle();
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
            Room output = new Room(room1.width,room1.height);
            Console.WriteLine(t);
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
                  //  interp[ii] = coeff1[ii] * (1 - t) + coeff2[ii] * t;
                    interp[ii] = coeff1[ii] * Math.Sin((1 - t) * angleBetween) / s + coeff2[ii] * Math.Sin(t * angleBetween) / s;
                }
                output.coefficients[layer] = interp;
            }
            return output;
        }
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
                2 * Math.Abs( objects["traps"][b.xx, b.yy]) +
                5 * Math.Abs( objects["keys"][b.xx, b.yy]) +
               5 * Math.Abs(objects["keyItems"][b.xx, b.yy]) +
               2 * Math.Abs( objects["items"][b.xx, b.yy]) +
               5 * Math.Abs( objects["puzzles"][b.xx, b.yy]) +
                2 * Math.Abs( objects["water"][b.xx, b.yy]) +
                5 * Math.Abs( objects["teleporters"][b.xx, b.yy]) +
                5 * Math.Abs( objects["enemies"][b.xx, b.yy]));
        }
        public List<SearchNode> getPath(SearchNode start, SearchNode end) {
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
                foreach (var neighbor in GetNeighbors(current)) {
                    if (!closeSet.Contains(neighbor.GetHashCode())) {
                        double tentativeGScore = gScore[current.GetHashCode()] + getScore(current, neighbor);
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
        public void changeWidth(int newWidth) {
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
            List<SearchNode> optimalPath = getPath(nodes[width / 2, 0], nodes[width / 2, height-1]);
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
                        newEnemies[ii,jj] =   objects["enemies"][ii,jj];
                        newKeys[ii,jj] =   objects["keys"][ii,jj];
                        newKeyItems[ii,jj] =   objects["keyItems"][ii,jj];
                        newItems[ii,jj] =   objects["items"][ii,jj];
                        newPuzzles[ii,jj] =   objects["puzzles"][ii,jj];
                        newTraps[ii,jj] =   objects["traps"][ii,jj];
                        newWater[ii,jj] =   objects["water"][ii,jj];
                        newTeleporters[ii,jj] =   objects["teleporters"][ii,jj];
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
                            newEnemies[ii+xx,jj] =   objects["enemies"][duplicatedPoints[jj].xx,jj];
                            newKeys[ii, jj] = objects["keys"][duplicatedPoints[jj].xx, jj];
                            newKeyItems[ii+xx,jj] =   objects["keyItems"][duplicatedPoints[jj].xx,jj];
                            newItems[ii+xx,jj] =   objects["items"][duplicatedPoints[jj].xx,jj];
                            newPuzzles[ii+xx,jj] =   objects["puzzles"][duplicatedPoints[jj].xx,jj];
                            newTraps[ii+xx,jj] =   objects["traps"][duplicatedPoints[jj].xx,jj];
                            newWater[ii+xx,jj] =   objects["water"][duplicatedPoints[jj].xx,jj];
                            newTeleporters[ii+xx,jj] =   objects["teleporters"][duplicatedPoints[jj].xx,jj];
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
        public void changeHeight(int newHeight) {
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
            List<SearchNode> optimalPath = getPath(nodes[0,height/2], nodes[width-1, height/2]);
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
                        newBlock[ii, jj+heightDiff] = objects["blocks"][ii, jj];
                        newEnemies[ii, jj+heightDiff] = objects["enemies"][ii, jj];
                        newKeys[ii, jj+heightDiff] = objects["keys"][ii, jj];
                        newKeyItems[ii, jj+heightDiff] = objects["keyItems"][ii, jj];
                        newItems[ii, jj+heightDiff] = objects["items"][ii, jj];
                        newPuzzles[ii, jj+heightDiff] = objects["puzzles"][ii, jj];
                        newTraps[ii, jj+heightDiff] = objects["traps"][ii, jj];
                        newWater[ii, jj+heightDiff] = objects["water"][ii, jj];
                        newTeleporters[ii, jj+heightDiff] = objects["teleporters"][ii, jj];

                    }
                    else {
                        for (int yy = 0; yy <= heightDiff; yy++) {
                            newBlock[ii, jj+yy] = objects["blocks"][ii, duplicatedPoints[ii].yy];
                            newEnemies[ii, jj+yy] = objects["enemies"][ii, duplicatedPoints[ii].yy];
                            newKeys[ii, jj+yy] = objects["keys"][ii, duplicatedPoints[ii].yy];
                            newKeyItems[ii, jj+yy] = objects["keyItems"][ii, duplicatedPoints[ii].yy];
                            newItems[ii, jj+yy] = objects["items"][ii, duplicatedPoints[ii].yy];
                            newPuzzles[ii, jj+yy] = objects["puzzles"][ii, duplicatedPoints[ii].yy];
                            newTraps[ii, jj+yy] = objects["traps"][ii, duplicatedPoints[ii].yy];
                            newWater[ii, jj+yy] = objects["water"][ii, duplicatedPoints[ii].yy];
                            newTeleporters[ii, jj+yy] = objects["teleporters"][ii, duplicatedPoints[ii].yy];
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
        public void changeSize(int width, int height) {
            for (int newHeight = objects["blocks"].GetLength(1) + 1; newHeight <= height; newHeight++) {
                changeHeight(newHeight);
            }
            for (int newWidth = objects["blocks"].GetLength(0) + 1; newWidth <= width; newWidth++) {
                changeWidth(newWidth);
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
                    if (objects["blocks"][ii,jj] != 0 || objects["enemies"][ii,jj] != 0 ||
                        objects["keys"][ii,jj] != 0 || objects["keyItems"][ii,jj] != 0 ||
                        objects["items"][ii,jj] != 0 || objects["puzzles"][ii,jj] != 0 ||
                        objects["traps"][ii,jj] != 0 || objects["water"][ii,jj] != 0 ||
                        objects["teleporters"][ii, jj] != 0) {
                            return false;
                    }
                }
            }
            return true;
        }
    }
}
