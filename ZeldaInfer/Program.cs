using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Drawing;


using MicrosoftResearch.Infer;
//using MicrosoftResearch.Infer.Collections;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Graphs;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Transforms;
using MicrosoftResearch.Infer.Utils;
using MicrosoftResearch.Infer.Views;

using ZeldaInfer.LevelParse;
namespace ZeldaInfer {
    class Program {

        static void RunAllLevels() {
            string[] levels = new string[]{
                "Levels/LA 1.xml","Levels/LA 2.xml","Levels/LA 3.xml","Levels/LA 4.xml","Levels/LA 5.xml",
                "Levels/LA 6.xml","Levels/LA 7.xml",
                "Levels/LA 8.xml","Levels/LoZ 1.xml",
                "Levels/LoZ 2.xml","Levels/LoZ 3.xml","Levels/LoZ 4.xml","Levels/LoZ 5.xml","Levels/LoZ 6.xml",
                "Levels/LoZ 7.xml","Levels/LoZ 8.xml","Levels/LoZ 9.xml","Levels/LoZ2 1.xml",
                "Levels/LoZ2 2.xml", "Levels/LoZ2 3.xml","Levels/LoZ2 4.xml","Levels/LoZ2 5.xml","Levels/LoZ2 6.xml",
                "Levels/LoZ2 7.xml","Levels/LoZ2 8.xml",
                "Levels/LoZ2 9.xml",
                "Levels/LttP 1.xml",
                "Levels/LttP 10.xml",
                "Levels/LttP 11.xml",
                "Levels/LttP 12.xml",
                "Levels/LttP 2.xml","Levels/LttP 3.xml",
                "Levels/LttP 4.xml","Levels/LttP 5.xml","Levels/LttP 6.xml","Levels/LttP 7.xml",
                "Levels/LttP 8.xml","Levels/LttP 9.xml",
			};

            foreach (var level in levels) {
                Console.WriteLine(level);
                Dungeon dungeon = new Dungeon(level);
                SearchAgent path = dungeon.getOptimalPath(level.Contains("LttP"));

                dungeon.setPureDepth();
                Console.WriteLine(path.pathToString());
                dungeon.UpdateRooms(path);
                string output = level;
                output = Regex.Replace(output, @"Levels", "Summaries");
                output = Regex.Replace(output, " ", "");
                dungeon.WriteStats(output, path);
            }
        }
        static void CreateGraphicalModelFiles(string[] summaries, string inputFile, string networkFilename, string dataFilename) {
            string[] allSummaries = new string[]{
                "Summaries/LA1.xml","Summaries/LA2.xml","Summaries/LA3.xml",
                "Summaries/LA4.xml","Summaries/LA5.xml","Summaries/LA6.xml","Summaries/LA7.xml",
                "Summaries/LA8.xml","Summaries/LoZ1.xml","Summaries/LoZ2.xml",
                "Summaries/LoZ21.xml","Summaries/LoZ22.xml","Summaries/LoZ23.xml","Summaries/LoZ24.xml",
                "Summaries/LoZ25.xml","Summaries/LoZ26.xml","Summaries/LoZ27.xml",
                "Summaries/LoZ28.xml","Summaries/LoZ29.xml","Summaries/LoZ3.xml",
                "Summaries/LoZ4.xml","Summaries/LoZ5.xml","Summaries/LoZ6.xml","Summaries/LoZ7.xml",
                "Summaries/LoZ8.xml","Summaries/LoZ9.xml","Summaries/LttP1.xml",
                "Summaries/LttP10.xml","Summaries/LttP11.xml","Summaries/LttP12.xml","Summaries/LttP2.xml",
                "Summaries/LttP3.xml","Summaries/LttP4.xml","Summaries/LttP5.xml",
                "Summaries/LttP6.xml","Summaries/LttP7.xml","Summaries/LttP8.xml",
                "Summaries/LttP9.xml",
            };

            HashSet<string> wholeLevelParameters = new HashSet<string>(){
                "roomsInLevel",  "enemyRoomsInLevel",  "puzzleRoomsInLevel",
                "itemRoomsInLevel",  "doorsInLevel",  "passableDoorsInLevel",
                "lockedDoorsInLevel", "bombLockedDoorsInLevel",  "bigKeyDoorsInLevel",
                "oneWayDoorsInLevel",  "itemLockedDoorsInLevel",  "puzzleLockedDoorsInLevel",
                "softLockedDoorsInLevel",  "lookAheadsInLevel",  "totalCrossings",
                "maximumCrossings",  "maximumDistanceToPath",  "pathLength",
                "roomsOnPath",  "enemyRoomsOnPath",  "puzzleRoomsOnPath",
                "itemRoomsOnPath",  "doorsOnPath",  "lockedDoorsOnPath",
                "bombLockedDoorsOnPath",  "bigKeyLockedDoorsOnPath",  "itemLockedDoorsOnPath",
                "softLockedDoorsOnPath",  "puzzleLockedDoorsOnPath",  "lookAheadsOnPath",
                "oneWayDoorsOnPath",  "distanceToDungeonKey",  "distanceToSpecialItem",
            };
            Dictionary<string, string> summaryDictionary = new Dictionary<string, string>();
            int totalCount = 0;
            string str = "";
            Dictionary<string, bool> isCategorical = new Dictionary<string, bool>();
            foreach (var summary in allSummaries) {
                XDocument summaryDoc = XDocument.Load(summary);
                Dictionary<string, string> levelParams = new Dictionary<string, string>();
                Dictionary<string, string> roomParams = new Dictionary<string, string>();
                int copies = 0;
                foreach (XElement element in summaryDoc.Root.Descendants()) {
                    if (Regex.IsMatch(element.Value, @"[a-z]")) {
                        isCategorical[element.Name.LocalName] = true;
                    }
                    else {
                        isCategorical[element.Name.LocalName] = false;

                    }
                    if (wholeLevelParameters.Contains(element.Name.LocalName)) {

                        if (element.Name.LocalName == "pathLength") {
                            str += element.Value + ",";
                        }
                        levelParams[element.Name.LocalName] = element.Value;
                    }
                    else {
                        roomParams[element.Name.LocalName] = element.Value;
                        copies = element.Value.Count(f => f == ';') + 1;
                    }
                }
                totalCount += copies;
                foreach (var pair in levelParams) {
                    if (summaryDictionary.ContainsKey(pair.Key)) {
                        summaryDictionary[pair.Key] += string.Concat(Enumerable.Repeat(pair.Value + ";", copies));
                    }
                    else {
                        summaryDictionary[pair.Key] = string.Concat(Enumerable.Repeat(pair.Value + ";", copies));
                    }
                }
                foreach (var pair in roomParams) {
                    if (summaryDictionary.ContainsKey(pair.Key)) {
                        summaryDictionary[pair.Key] += pair.Value + ";";
                    }
                    else {
                        summaryDictionary[pair.Key] = pair.Value + ";";
                    }
                }
            }

            Console.WriteLine("depth = " + str);
            XDocument categoriesDoc = new XDocument(new XElement("root"));
            Console.WriteLine(totalCount);
            Dictionary<string, List<string>> categories = new Dictionary<string, List<string>>();
            foreach (var category in summaryDictionary) {

                categories[category.Key] = new List<string>(new SortedSet<string>(category.Value.Split(';')));
                categories[category.Key].Remove("");
                categoriesDoc.Root.Add(new XElement(category.Key, new XAttribute("count", categories[category.Key].Count), string.Join(";", categories[category.Key].ToArray())));

            }
            categories["doorCameFromCategory"] = categories["doorTypesFromCategory"];
            categories["doorGoToCategory"] = categories["doorTypesFromCategory"];
            categories["roomCameFromCategory"] = categories["roomTypeCategory"];
            categories["roomGoToCategory"] = categories["roomTypeCategory"];
            foreach (var category in summaryDictionary) {
                categoriesDoc.Root.Add(new XElement(category.Key, new XAttribute("count", categories[category.Key].Count), string.Join(";", categories[category.Key].ToArray())));

            }
            categoriesDoc.Save("categories.xml");
            summaryDictionary = new Dictionary<string, string>();
            totalCount = 0;
            str = "";
            isCategorical = new Dictionary<string, bool>();
            foreach (var summary in summaries) {
                XDocument summaryDoc = XDocument.Load(summary);
                Dictionary<string, string> levelParams = new Dictionary<string, string>();
                Dictionary<string, string> roomParams = new Dictionary<string, string>();
                int copies = 0;
                foreach (XElement element in summaryDoc.Root.Descendants()) {
                    if (Regex.IsMatch(element.Value, @"[a-z]")) {
                        isCategorical[element.Name.LocalName] = true;
                    }
                    else {
                        isCategorical[element.Name.LocalName] = false;

                    }
                    if (wholeLevelParameters.Contains(element.Name.LocalName)) {

                        if (element.Name.LocalName == "pathLength") {
                            str += element.Value + ",";
                        }
                        levelParams[element.Name.LocalName] = element.Value;
                    }
                    else {
                        roomParams[element.Name.LocalName] = element.Value;
                        copies = element.Value.Count(f => f == ';') + 1;
                    }
                }
                totalCount += copies;
                foreach (var pair in levelParams) {
                    if (summaryDictionary.ContainsKey(pair.Key)) {
                        summaryDictionary[pair.Key] += string.Concat(Enumerable.Repeat(pair.Value + ";", copies));
                    }
                    else {
                        summaryDictionary[pair.Key] = string.Concat(Enumerable.Repeat(pair.Value + ";", copies));
                    }
                }
                foreach (var pair in roomParams) {
                    if (summaryDictionary.ContainsKey(pair.Key)) {
                        summaryDictionary[pair.Key] += pair.Value + ";";
                    }
                    else {
                        summaryDictionary[pair.Key] = pair.Value + ";";
                    }
                }
            }
            XDocument xdoc = XDocument.Load(inputFile);
            Dictionary<string, string> nodes = new Dictionary<string, string>();
            List<Tuple<string, string>> edges = new List<Tuple<string, string>>();
            foreach (XElement element in xdoc.Root.Descendants()) {
                string val = "";
                if (element.Attribute("value") != null) {
                    val = element.Attribute("value").Value.Split('&')[0];
                    val = Regex.Replace(val, " ", ",");
                }
                if (element.Attribute("style") != null) {
                    string style = element.Attribute("style").Value;
                    if (style.Contains("ellipse")) {
                        nodes[element.Attribute("id").Value] = val;
                    }
                    else if (style.Contains("edgeStyle")) {
                        edges.Add(new Tuple<string, string>(element.Attribute("source").Value, element.Attribute("target").Value));
                    }
                }
            }
            XDocument dungeonDoc = new XDocument(new XElement("root"));
            foreach (var category in categories) {
                string domain = "Numerical";
                if (isCategorical[category.Key]) {
                    domain = "Categorical";
                }
                dungeonDoc.Root.Add(new XElement("Category", new XAttribute("name", category.Key + "Category"), new XAttribute("categories", string.Join(",",
                    Enumerable.Range(0, category.Value.Count))), new XAttribute("domain", domain)));
            }
            foreach (var node in nodes.Values) {
                string domain = "Numerical";
                if (isCategorical[node]) {
                    domain = "Categorical";
                }
                dungeonDoc.Root.Add(new XElement("Node", new XAttribute("name", node), new XAttribute("category", node + "Category"), new XAttribute("domain", domain)));
            }
            foreach (var edge in edges) {
                dungeonDoc.Root.Add(new XElement("Edge", new XAttribute("parent", nodes[edge.Item1]), new XAttribute("child", nodes[edge.Item2])));
            }
            dungeonDoc.Save(networkFilename);
            XDocument dataDoc = new XDocument(new XElement("root"));
            foreach (var param in summaryDictionary) {
                string domain = "Numerical";
                if (isCategorical[param.Key]) {
                    domain = "Categorical";
                    dataDoc.Root.Add(new XElement("Data", new XAttribute("domain", domain), new XAttribute("name", param.Key), string.Join(",", param.Value.Substring(0, param.Value.Length - 1).Split(';').Select(p => Math.Max(0, categories[param.Key].IndexOf(p))))));

                }
                else {
                    dataDoc.Root.Add(new XElement("Data", new XAttribute("domain", domain), new XAttribute("name", param.Key), string.Join(",", param.Value.Substring(0, param.Value.Length - 1).Split(';'))));

                }
                //  Console.WriteLine(param.Key + " = [" + string.Join(",",param.Value.Substring(0,param.Value.Length-1).Split(';')) + "]");
            }
            dataDoc.Save(dataFilename);

            XDocument categoryDoc = new XDocument(new XElement("root"));
            foreach (var category in categories) {
                categoryDoc.Root.Add(new XElement("Category", new XAttribute("name", category.Key), string.Join(",", category.Value.ToArray())));

            }
            categoryDoc.Save("categories.xml");
        }
        /*
        static Tuple<GraphicalModel, Dictionary<string, Tuple<int[],double[]>>> CreateGraphicalModel(string modelFile, string dataFile)
        {

            GraphicalModel model = new GraphicalModel(modelFile, 13);
            model.CreateNetwork();
            Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
            model.LearnParameters(observedData);
            BinaryFormatter serializer = new BinaryFormatter();

            using (FileStream stream = new FileStream(modelFile.Substring(0,modelFile.LastIndexOf("."))+"bin", FileMode.Create)) {
                serializer.Serialize(stream, model);
            }

            return new Tuple<GraphicalModel,Dictionary<string,Tuple<int[],double[]>>>(model,observedData);
        }
        */
        static Tuple<GraphicalModel, Dictionary<string, Tuple<int[], double[]>>> CreateGraphicalModel(string modelFile, string dataFile) {

            GraphicalModel model = new GraphicalModel(modelFile, 4);
            model.CreateNetwork();

            Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
            model.LearnParameters(observedData);
            BinaryFormatter serializer = new BinaryFormatter();

            using (FileStream stream = new FileStream(modelFile.Substring(0, modelFile.LastIndexOf(".")) + ".bin", FileMode.Create)) {
                serializer.Serialize(stream, model);
            }

            return new Tuple<GraphicalModel, Dictionary<string, Tuple<int[], double[]>>>(model, observedData);
        }

        static double evaluate(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> data) {
            int dataPointCount = 0;
            foreach (var datatype in data) {
                if (datatype.Value.Item1 != null) {
                    dataPointCount = datatype.Value.Item1.Length;
                    break;
                }
                else {
                    dataPointCount = datatype.Value.Item2.Length;
                    break;
                }
            }

            double loglikelihood = 0;
            int N = dataPointCount, d = 0;

            foreach (ModelNode eachNode in model.nodes.Values) {
                int parentStates = 1;
                int numericalParentCount = 1;
                foreach (var parent in eachNode.parents) {
                    if (parent.distributionType == DistributionType.Categorical) {
                        parentStates *= parent.states.SizeAsInt;
                    }
                    else {
                        numericalParentCount += 1;
                    }
                }
                if (eachNode.distributionType == DistributionType.Categorical) {
                    d += eachNode.states.SizeAsInt * parentStates * numericalParentCount;
                }
                else {
                    d += parentStates * numericalParentCount;
                }
            }

            for (int ii = 0; ii < dataPointCount; ii++) {
                foreach (ModelNode eachNode in model.nodes.Values) {

                    loglikelihood += eachNode.distributions.getLogLikelihood(data, ii);

                }
            }

            return loglikelihood - d / 2.0 * Math.Log(N);
        }

        static Tuple<GraphicalModel, Dictionary<string, Tuple<int[], double[]>>> ModelNetworkSerialized(string binary, string network, string data) {
            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(binary, FileMode.Open, FileAccess.Read, FileShare.Read);
            GraphicalModel model = (GraphicalModel)formatter.Deserialize(stream);
            Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(data);

            model.LoadAfterSerialization(network, 1);
            stream.Close();
            return new Tuple<GraphicalModel, Dictionary<string, Tuple<int[], double[]>>>(model, observedData);
        }
        static Dictionary<string, string[]> loadCategories(string filename) {
            Dictionary<string, string[]> output = new Dictionary<string, string[]>();
            XDocument xdoc = XDocument.Load(filename);
            foreach (var element in xdoc.Root.Descendants()) {
                string name = element.Attribute("name").Value;
                string[] categories = element.Value.Split(',');
                output[name] = categories;
            }
            return output;
        }
        static BayesRoom createRoom(int xx, int yy, BayesRoom parentRoom, Dictionary<string, Tuple<int[], double[]>> presets, NonSharedGraphicalModel model) {
            int pureDepth = parentRoom.pureDepth + 1;
            Dictionary<string, Tuple<int[], double[]>> conditionedOn = new Dictionary<string, Tuple<int[], double[]>>(presets);
            conditionedOn["pureDepth"] = new Tuple<int[], double[]>(null, new double[] { pureDepth });
            conditionedOn["roomCameFrom"] = new Tuple<int[], double[]>(new int[] { parentRoom.roomType }, null);
            conditionedOn["doorCameFrom"] = new Tuple<int[], double[]>(new int[] { parentRoom.doorGoTo }, null);
            List<string> toBeInferred = new List<string>();
            toBeInferred.Add("roomType");
            //  toBeInferred.Add("crossings");
            toBeInferred.Add("numberOfNeighbors");
            //   toBeInferred.Add("distanceOfRoomToOptimalPath");
            toBeInferred.Add("doorGoTo");
            var inferred = model.Infer(conditionedOn, toBeInferred);
            Discrete roomDist = (Discrete)inferred["roomType"].Item2;
            Gaussian gN = (Gaussian)inferred["numberOfNeighbors"].Item2;
            //    Gaussian gC = (Gaussian)inferred["crossings"].Item2;
            //     Gaussian gD = (Gaussian)inferred["distanceOfRoomToOptimalPath"].Item2;
            int roomType = roomDist.Sample();
            Discrete doorDist = (Discrete)inferred["doorGoTo"].Item2;
            int doorGoTo = doorDist.Sample();
            int numberOfNeighbors = Math.Min(4, Math.Max(1, (int)gN.Sample()));
            //    int crossings = (int)gC.Sample();
            //      int distanceOfRoomToOptimalPath = (int)gD.Sample();
            BayesRoom room = new BayesRoom(roomType, doorGoTo, numberOfNeighbors, pureDepth, parentRoom, xx, yy, roomDist, doorDist);

            return room;

        }
        static bool updateRoom(BayesRoom room, Dictionary<string, Tuple<int[], double[]>> presets, NonSharedGraphicalModel model) {
            if (room.parent == null) {
                return false;
            }
            Dictionary<string, Tuple<int[], double[]>> conditionedOn = new Dictionary<string, Tuple<int[], double[]>>(presets);
            conditionedOn["pureDepth"] = new Tuple<int[], double[]>(null, new double[] { room.pureDepth });
            conditionedOn["roomCameFrom"] = new Tuple<int[], double[]>(new int[] { room.parent.roomType }, null);
            conditionedOn["doorCameFrom"] = new Tuple<int[], double[]>(new int[] { room.parent.doorGoTo }, null);
            conditionedOn["numberOfNeighbors"] = new Tuple<int[], double[]>(null, new double[] { room.numberOfNeighbors });

            if (room.children.Count > 0) {
                conditionedOn["roomGoTo"] = new Tuple<int[], double[]>(new int[] { room.children.GetRandom().roomType }, null);
            }
            List<string> toBeInferred = new List<string>();
            toBeInferred.Add("roomType");
            //  toBeInferred.Add("crossings");
            //     toBeInferred.Add("distanceOfRoomToOptimalPath");
            toBeInferred.Add("doorGoTo");
            var inferred = model.Infer(conditionedOn, toBeInferred);
            Discrete roomDist = (Discrete)inferred["roomType"].Item2;
            //     Gaussian gN = (Gaussian)inferred["numberOfNeighbors"].Item2;
            //      Gaussian gC = (Gaussian)inferred["crossings"].Item2;
            //      Gaussian gD = (Gaussian)inferred["distanceOfRoomToOptimalPath"].Item2;
            int roomType = roomDist.Sample();
            Discrete doorDist = (Discrete)inferred["doorGoTo"].Item2;
            int doorGoTo = doorDist.Sample();

            bool changed = room.roomType != roomType || room.doorGoTo == doorGoTo;
            room.roomType = roomType;
            room.doorGoTo = doorGoTo;
            // BayesRoom room = new BayesRoom(roomCameFrom, doorCameFrom, numberOfNeighbors, pureDepth, parentRoom, xx, yy, roomDist, doorDist);

            return changed;

        }
        static public int getNeighbors(string location, Dictionary<string, BayesRoom> rooms) {
            int neighborCount = 0;
            string[] xy = location.Split(',');
            int x = int.Parse(xy[0]);
            int y = int.Parse(xy[1]);
            int[][] directions = new int[][]{
                new int[] {0,1},
                new int[]{1,0},
                new int[]{-1,0},
                new int[]{0,-1}
            };
            foreach (var dir in directions) {
                if (rooms.ContainsKey("" + (dir[0] + x) + "," + (dir[1] + y))) {
                    neighborCount++;
                }
            }
            return neighborCount;
        }
        static List<string> CreateDungeon(Dictionary<string, Tuple<int[], double[]>> presets, NonSharedGraphicalModel model) {
            int numberOfRoomsInLevel = 0;
            int pathLength = 0;
            if (presets.ContainsKey("roomsInLevel")) {
                numberOfRoomsInLevel = (int)presets["roomsInLevel"].Item2[0];
            }
            if (presets.ContainsKey("pathLength")) {
                pathLength = (int)presets["pathLength"].Item2[0];
            }
            List<string> toBeInferred = new List<string>();
            if (numberOfRoomsInLevel == 0 && presets.Count > 0) {
                toBeInferred.Add("roomsInLevel");
                var output = model.Infer(presets, toBeInferred);
                Gaussian g = (Gaussian)output["roomsInLevel"].Item2;
                numberOfRoomsInLevel = (int)g.Sample();
            }
            else {
                model.CreateNetwork(1);
                numberOfRoomsInLevel = (int)(model.nodes["roomsInLevel"].distributions as NoParentNodeNumericalNonShared).meanPrior.ObservedValue.Sample();
            }
            if (pathLength == 0 && presets.Count > 0) {
                toBeInferred.Add("pathLength");
                var output = model.Infer(presets, toBeInferred);
                Gaussian g = (Gaussian)output["pathLength"].Item2;
                pathLength = (int)g.Sample();
            }
            else {
                model.CreateNetwork(1);
                pathLength = (numberOfRoomsInLevel * 2) / 4;
            }
            Dictionary<string, string[]> categories = loadCategories("Categories.xml");
            Dictionary<string, BayesRoom> rooms = new Dictionary<string, BayesRoom>();
            int[][] directions = new int[][]{
                new int[] {0,1},
                new int[]{1,0},
                new int[]{-1,0},
                new int[]{0,-1}
            };
            List<int[]> dirs = new List<int[]>(directions);



            Dictionary<string, Tuple<int[], double[]>> modPresets = new Dictionary<string, Tuple<int[], double[]>>(presets);
            modPresets["roomsInLevel"] = new Tuple<int[], double[]>(null, new double[] { numberOfRoomsInLevel });
            modPresets["pathLength"] = new Tuple<int[], double[]>(null, new double[] { pathLength });
            BayesRoom room = new BayesRoom(categories["roomCameFrom"].IndexOf("s"), categories["doorCameFrom"].IndexOf("_"), 2, 0, null, 0, -1, null, null);
            // rooms[room.location()] = room;
            Dictionary<string, int> childCount = new Dictionary<string, int>();
            List<BayesRoom> openSet = new List<BayesRoom>();
            List<BayesRoom> closedSet = new List<BayesRoom>();
            openSet.Add(room);
            Random rand = new Random();
            for (int ii = 0; ii < numberOfRoomsInLevel || openSet.Count == 0; ii++) {

                int index = rand.Next(openSet.Count);
                room = openSet[index];
                if (!childCount.ContainsKey(room.location())) {
                    childCount[room.location()] = 1;
                }

                int currentNeighbors = getNeighbors(room.location(), rooms);
                if (room.pureDepth == 0) {
                    openSet.RemoveAt(index);
                }
                if (childCount[room.location()] > room.numberOfNeighbors) {
                    openSet.RemoveAt(index);
                    closedSet.Add(room);
                    ii--;

                }
                else if (currentNeighbors == 4) {
                    openSet.RemoveAt(index);
                    closedSet.Add(room);
                    ii--;
                }
                else {
                    dirs.Shuffle(rand);
                    string[] xy = room.location().Split(',');
                    int xx = int.Parse(xy[0]);
                    int yy = int.Parse(xy[1]);
                    bool placedRoom = false;
                    foreach (var dir in dirs) {
                        if (dir[1] + yy >= 0) {
                            if (!rooms.ContainsKey("" + (dir[0] + xx) + "," + (dir[1] + yy))) {
                                var newRoom = createRoom((dir[0] + xx), (dir[1] + yy), room, presets, model);
                                if (newRoom.pureDepth == 1) {
                                    newRoom.roomType = categories["roomType"].IndexOf("_");
                                }
                                childCount[room.location()]++;
                                room.children.Add(newRoom);
                                rooms[newRoom.location()] = newRoom;
                                openSet.Add(newRoom);
                                placedRoom = true;
                                break;
                            }
                        }
                    }
                    if (!placedRoom) {
                        openSet.RemoveAt(index);
                        closedSet.Add(room);
                        ii--;
                    }
                }
            }
            validateDungeon(rooms, categories, modPresets, model);
            List<string> roomStrings = new List<string>();
            foreach (var rr in rooms.Values) {
                int pureDepth = rr.pureDepth;
                string roomType = categories["roomType"][rr.roomType];
                string doorGoTo = categories["doorCameFrom"][rr.doorGoTo];
                string location = rr.location();
                roomStrings.Add(pureDepth + " " + roomType + " " + doorGoTo + " " + location + " " + rr.parent.location());
                Console.WriteLine(pureDepth + " " + roomType + " " + doorGoTo + " " + location + " " + rr.parent.location());
            }
            return roomStrings;
            /*
            foreach (var rr in closedSet) {
                int pureDepth = rr.pureDepth;
                string roomType = categories["roomType"][rr.roomType];
                string doorGoTo = categories["doorCameFrom"][rr.doorGoTo];
                string location = rr.location();
                Console.WriteLine(pureDepth + " " + roomType + " " + doorGoTo + " " + location + " " +
                rr.roomDist.GetLogProb(categories["roomType"].IndexOf("t")) + " " +
                rr.roomDist.GetLogProb(categories["roomType"].IndexOf("I")));

            }
            foreach (var rr in openSet) {
                int pureDepth = rr.pureDepth;
                string roomType = categories["roomType"][rr.roomType];
                string doorGoTo = categories["doorCameFrom"][rr.doorGoTo];
                string location = rr.location();

                Console.WriteLine(pureDepth + " " + roomType + " " + doorGoTo + " " + location + " " +
                rr.roomDist.GetLogProb(categories["roomType"].IndexOf("t")) + " " +
                rr.roomDist.GetLogProb(categories["roomType"].IndexOf("I")));

            }
             * 
             * */

            /*
            int depth = previousDepth + 1;
            Dictionary<string, Tuple<int[], double[]>> conditionedOn = new Dictionary<string, Tuple<int[], double[]>>(modPresets);
            conditionedOn["depth"] = new Tuple<int[], double[]>(null, new double[] { depth });
            conditionedOn["roomCameFrom"] = new Tuple<int[], double[]>(new int[] { roomCameFrom },null);
            conditionedOn["doorCameFrom"] = new Tuple<int[], double[]>(new int[] { doorCameFrom },null);

            toBeInferred.Clear();
            toBeInferred.Add("roomType");
            toBeInferred.Add("crossings");
            toBeInferred.Add("numberOfNeighbors");
            toBeInferred.Add("distanceOfRoomToOptimalPath");
            toBeInferred.Add("doorGoTo");
            var inferred = model.Infer(conditionedOn, toBeInferred);
            Discrete categorical = (Discrete)inferred["roomType"].Item2;
            Gaussian gN = (Gaussian)inferred["numberOfNeighbors"].Item2;
            Gaussian gC = (Gaussian)inferred["crossings"].Item2;
            Gaussian gD = (Gaussian)inferred["distanceOfRoomToOptimalPath"].Item2;
            roomCameFrom = categorical.Sample();
            categorical = (Discrete)inferred["doorGoTo"].Item2;
            doorCameFrom = categorical.Sample();
            int numberOfNeighbors = Math.Min(4,Math.Max(1,(int)gN.Sample()));
            int crossings = (int)gC.Sample();
            int distanceOfRoomToOptimalPath = (int)gD.Sample();
            Dictionary<string, string> roomDefinition = new Dictionary<string, string>();
            roomDefinition["roomCameFrom"] = "" + roomCameFrom;
            roomDefinition["doorCameFrom"] = "" + doorCameFrom;
            roomDefinition["numberOfNeighbors"] = "" + numberOfNeighbors;
            roomDefinition["depth"] = "" + depth;
            roomDefinition["parent"] = ""+px+","+py;
            rooms["" + x + "," + y] = roomDefinition;
            if (depth == 0){
                for (int jj = 0; jj < numberOfNeighbors; jj++){

                }
            }
            else {
                for (int jj = 1; jj < numberOfNeighbors; jj++) {

                }
            }
           // Console.WriteLine(depth + " - " + roomCameFrom + " - " + numberOfNeighbors);
            level += depth + " " + categories["roomType"][roomCameFrom] + " " + categories["doorCameFrom"][doorCameFrom] + " " + numberOfNeighbors + " " + crossings + " " + distanceOfRoomToOptimalPath + "\n";
            roomCameFrom = Math.Max(0,Array.IndexOf(categories["roomCameFrom"], categories["roomType"][roomCameFrom]));
            previousDepth = depth;*/

        }
        static double dotProduct(double[] arr1, double[] arr2) {
            double val = 0;
            for (int ii = 0; ii < arr1.Length; ii++) {
                val += arr1[ii] * arr2[ii];
            }
            return val;
        }
        static double[] normalize(double[] arr1) {
            double[] arr2 = new double[arr1.Length];
            double invmag = 1.0 / Math.Sqrt(dotProduct(arr1, arr1));
            for (int ii = 0; ii < arr1.Length; ii++) {
                arr2[ii] = arr1[ii] * invmag;
            }
            return arr2;
        }
        static double angleBetween(double[] arr1, double[] arr2) {
            double dotProd = dotProduct(normalize(arr1), normalize(arr2));

            return Math.Acos(dotProd);
        }
        static bool tooClose(double threshold, Dictionary<string, double[]> coeffs1, Dictionary<string, double[]> coeffs2) {
            foreach (var coeff in coeffs1) {
                double angle = angleBetween(coeff.Value, coeffs2[coeff.Key]);
                if (angle > threshold) {
                    return false;
                }
            }
            return true;
        }
        static List<Bitmap> ConstructDungeonImage(List<string> rooms) {
            string clusterFile = "noclusters.xml";
            string componentsFile = "components.xml";
            XDocument clusterDoc = XDocument.Load(clusterFile);
            Dictionary<string, List<List<Dictionary<string, double[]>>>> clusters = new Dictionary<string, List<List<Dictionary<string, double[]>>>>();
            Dictionary<string, List<List<LearnRooms.Room>>> clusteredRooms = new Dictionary<string, List<List<LearnRooms.Room>>>();
            int duplicates = 0;
            foreach (XElement cluster in clusterDoc.Root.Elements()) {
                HashSet<string> seenRooms = new HashSet<string>();

                if (!clusters.ContainsKey(cluster.Attribute("name").Value)) {
                    clusters[cluster.Attribute("name").Value] = new List<List<Dictionary<string, double[]>>>();
                    clusteredRooms[cluster.Attribute("name").Value] = new List<List<LearnRooms.Room>>();
                }
                List<Dictionary<string, double[]>> clusterCoeffs = new List<Dictionary<string, double[]>>();
                List<LearnRooms.Room> clusterRooms = new List<LearnRooms.Room>();
                foreach (XElement room in cluster.Elements()) {
                    string value = room.Value;
                    if (!seenRooms.Contains(value)) {
                        Dictionary<string, double[]> coeffs = new Dictionary<string, double[]>();
                        foreach (var coeff in room.Elements()) {
                            coeffs[coeff.Attribute("name").Value] = coeff.Value.Split(',').Select(i => double.Parse(i)).ToArray();
                        }
                        bool passes = true;
                        foreach (var coeff in clusterCoeffs) {
                            if (tooClose((3.14159 / 180.0) * 10.0, coeffs, coeff)) {
                                passes = false;
                                break;
                            }
                        }
                        if (passes) {
                            LearnRooms.Room bmRoom = new LearnRooms.Room(12, 10);
                            var reach = room.Attribute("reachability");
                            bmRoom.connections = int.Parse(room.Attribute("reachability").Value);
                            bmRoom.coefficients = coeffs;
                            clusterRooms.Add(bmRoom);
                            clusterCoeffs.Add(coeffs);
                            seenRooms.Add(value);
                        }
                    }
                }
                clusteredRooms[cluster.Attribute("name").Value].Add(clusterRooms);
                clusters[cluster.Attribute("name").Value].Add(clusterCoeffs);
            }
            XDocument componentDoc = XDocument.Load(componentsFile);
            Dictionary<string, double[,]> components = new Dictionary<string, double[,]>();
            foreach (XElement cluster in componentDoc.Root.Elements()) {
                int dim1 = int.Parse(cluster.Attribute("dim1").Value);
                int dim2 = int.Parse(cluster.Attribute("dim2").Value);
                double[,] component = new double[dim1, dim2];
                string[] rows = cluster.Value.Split(';');
                for (int ii = 0; ii < dim1; ii++) {
                    double[] column = rows[ii].Split(',').Select(i => double.Parse(i)).ToArray();
                    for (int jj = 0; jj < dim2; jj++) {
                        component[ii, jj] = column[jj];
                    }
                }
                components[cluster.Name.ToString()] = component;
            }
            foreach (var roomCluster in clusteredRooms) {

                for (int ii = 0; ii < roomCluster.Value.Count; ii++) {
                    for (int jj = 0; jj < roomCluster.Value[ii].Count; jj++) {
                        roomCluster.Value[ii][jj] = roomCluster.Value[ii][jj].reconstruct(components, 0);
                    }
                    //   roomCluster.Value[ii].toBitmap().Save(roomCluster.Key + ii + ".png");
                }
            }
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            //  roomStrings.Add(pureDepth + " " + roomType + " " + doorGoTo + " " + location + " " + rr.parent.location());
            foreach (var room in rooms) {
                string[] roomComponents = room.Split(' ');
                string type = roomComponents[1];
                string doorType = roomComponents[2];
                string location = roomComponents[3];
                string parenLocation = roomComponents[4];
                string[] xy = location.Split(',');
                int xx = int.Parse(xy[0]);
                int yy = int.Parse(xy[1]);
                if (xx < minX) {
                    minX = xx;
                }
                if (xx > maxX) {
                    maxX = xx;
                }
                if (yy < minY) {
                    minY = yy;
                }
                if (yy > maxY) {
                    maxY = yy;
                }
            }
            List<Bitmap> dungeons = new List<Bitmap>();
            for (int dungeonCount = 0; dungeonCount < 1; dungeonCount++) {
                Bitmap dungeonMap = new Bitmap((maxX - minX + 1) * 16, (maxY - minY + 1) * 14);
                dungeonMap.Fill(Color.Black);
                Random rand = new Random();
                List<Tuple<string, string>> connections = new List<Tuple<string, string>>();
                foreach (var room in rooms) {
                    string[] roomComponents = room.Split(' ');
                    string location = roomComponents[3];
                    string parentLocation = roomComponents[4];
                    connections.Add(new Tuple<string, string>(location, parentLocation));
                }
                Dictionary<string, HashSet<string>> connDict = new Dictionary<string, HashSet<string>>();
                foreach (var connection in connections) {
                    if (!connDict.ContainsKey(connection.Item1)) {
                        connDict[connection.Item1] = new HashSet<string>();
                    }
                    if (!connDict.ContainsKey(connection.Item2)) {
                        connDict[connection.Item2] = new HashSet<string>();
                    }
                    connDict[connection.Item1].Add(connection.Item2);
                    connDict[connection.Item2].Add(connection.Item1);
                }
                foreach (var room in rooms) {
                    string[] roomComponents = room.Split(' ');
                    string type = roomComponents[1];
                    string doorType = roomComponents[2];
                    string location = roomComponents[3];
                    string parentLocation = roomComponents[4];
                    string[] xy = location.Split(',');
                    int xx = int.Parse(xy[0]);
                    int yy = int.Parse(xy[1]);
                    xy = parentLocation.Split(',');
                    int px = int.Parse(xy[0]);
                    int py = int.Parse(xy[1]);

                    if (!clusteredRooms.ContainsKey(type)) {
                        type = "_";
                        Console.WriteLine("Changed (" + room + ") because type was missing");
                    }

                    bool[][] requiredConnections = new bool[4][];
                    for (int ii = 0; ii < 4; ii++) {
                        requiredConnections[ii] = new bool[4];
                    }
                    foreach (var conn in connDict[location]) {
                        xy = conn.Split(',');
                        int ox = int.Parse(xy[0]);
                        int oy = int.Parse(xy[1]);
                        if (ox < xx) {
                            for (int ii = 0; ii < 4; ii++) {
                                requiredConnections[0][ii] = true;
                                requiredConnections[ii][0] = true;
                            }
                        }
                        if (ox > xx) {
                            for (int ii = 0; ii < 4; ii++) {
                                requiredConnections[1][ii] = true;
                                requiredConnections[ii][1] = true;
                            }
                        }
                        if (oy < yy) {
                            for (int ii = 0; ii < 4; ii++) {
                                requiredConnections[2][ii] = true;
                                requiredConnections[ii][2] = true;
                            }
                        }
                        if (oy > yy) {
                            for (int ii = 0; ii < 4; ii++) {
                                requiredConnections[3][ii] = true;
                                requiredConnections[ii][3] = true;
                            }
                        }
                    }
                    int bitmask = 0;
                    int bitcount = 0;
                    foreach (var row in requiredConnections) {
                        foreach (var d in row) {
                            if (d){
                                bitmask += (int)Math.Pow(2, bitcount);
                            }
                            bitcount++;
                        }
                    }

                    int interpmask = 0;
                    LearnRooms.Room interpRoom = null;
                    while ((interpmask & bitmask) != bitmask) {
                        int clusterId = rand.Next(clusteredRooms[type].Count);
                        interpRoom = clusteredRooms[type][clusterId][0];
                        clusteredRooms[type][clusterId].Shuffle(rand);
                        if (clusteredRooms[type][clusterId].Count > 1) {
                            int id1 = -1;
                            int id2 = -1;
                            for (int ii = 0; ii < clusteredRooms[type][clusterId].Count; ii++) {
                                if ((bitmask & clusteredRooms[type][clusterId][ii].connections) == bitmask) {
                                    if (id1 == -1) {
                                        id1 = ii;
                                    }
                                    else if (id2 == -1) {
                                        id2 = ii;
                                        break;
                                    }
                                }
                            }
                            if (id2 < 0) {
                                id2 = 0;
                            }
                            if (id1 < 0) {
                                id1 = 0;
                            }
                            interpRoom = LearnRooms.Room.Interpolate(clusteredRooms[type][clusterId][id1], clusteredRooms[type][clusterId][id2], (float)rand.NextDouble());
                            interpRoom = interpRoom.reconstruct(components, 0);
                            bool[][] reachability = interpRoom.GetReachability(rand);
                            interpmask = interpRoom.connections;
                            if (type.Contains("k")) {
                                if (!interpRoom.containsObject("keys")) {
                                    interpmask = 0;
                                }
                            }
                            if (type.Contains("I")) {
                                if (!interpRoom.containsObject("keyItems")) {
                                    interpmask = 0;
                                }
                            }
                            if (type.Contains("e")) {
                                if (!interpRoom.containsObject("enemies")) {
                                    interpmask = 0;
                                }
                            }
                            if (type.Contains("p")) {
                                if (!(interpRoom.containsObject("puzzles") || interpRoom.containsObject("traps") || interpRoom.containsObject("water"))) {
                                    interpmask = 0;
                                }
                            }
                            if (type.Contains("i")) {
                                if (!(interpRoom.containsObject("items"))) {
                                    interpmask = 0;
                                }
                            }

                        }
                    }
                    Stamp(dungeonMap, interpRoom.reconstruct(components, 0).toBitmap(), (xx - minX) * 16 + 2, (yy - minY) * 14 + 2);
                    Color doorColor = Color.Gray;
                    if (doorType == "k") {
                        doorColor = Color.Gold;
                    }
                    else if (doorType == "K") {
                        doorColor = Color.Crimson;
                    }
                    else if (doorType == "l") {
                        doorColor = Color.DarkCyan;
                    }
                    else if (doorType == "b") {
                        doorColor = Color.YellowGreen;
                    }
                    if (py < minY) {

                        dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14, Color.Beige);
                        dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14, Color.Beige);
                        dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14 + 1, Color.Beige);
                        dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14 + 1, Color.Beige);
                    }
                    if (px >= minX && py >= minY) {

                        if (px == xx) {
                            if (py > yy) {
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (py - minY) * 14, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (py - minY) * 14, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (py - minY) * 14 - 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (py - minY) * 14 - 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (py - minY) * 14 -2, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (py - minY) * 14 -2, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (py - minY) * 14 + 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (py - minY) * 14 +1, doorColor);
                            }
                            else {
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14 - 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14 - 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14-2, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14-2 , doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 8, (yy - minY) * 14 + 1, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 7, (yy - minY) * 14 + 1, doorColor);

                            }
                        }
                        if (py == yy) {
                            if (px > xx) {
                                dungeonMap.SetPixel((px - minX) * 16, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16 - 1, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16 - 1, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16-2, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16-2, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16 + 1, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((px - minX) * 16 + 1, (py - minY) * 14 + 7, doorColor);
                            }
                            else {
                                dungeonMap.SetPixel((xx - minX) * 16, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 - 1, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 - 1, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16-2, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16-2, (py - minY) * 14 + 7, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 1, (py - minY) * 14 + 6, doorColor);
                                dungeonMap.SetPixel((xx - minX) * 16 + 1, (py - minY) * 14 + 7, doorColor);

                            }

                        }
                    }
                }


                dungeons.Add(dungeonMap);
                dungeonMap.Save("dungeon" + dungeonCount + ".png");
            }

            return dungeons;
        }
        static void Stamp(Bitmap canvas, Bitmap stamp, int xx, int yy) {
            for (int ii = 0; ii < stamp.Width; ii++) {
                for (int jj = 0; jj < stamp.Height; jj++) {
                    if (stamp.GetPixel(ii, jj).A > 0) {
                        canvas.SetPixel(ii + xx, jj + yy, stamp.GetPixel(ii, jj));
                    }
                }
            }
        }
        static void validateDungeon(Dictionary<string, BayesRoom> dungeon, Dictionary<string, string[]> categories, Dictionary<string, Tuple<int[], double[]>> presets, NonSharedGraphicalModel model) {
            List<BayesRoom> ignoreList = new List<BayesRoom>();
            BayesRoom endRoom = correctDungeonRoom(categories["roomType"].IndexOf("t"), dungeon, ignoreList);

            BayesRoom bossRoom = endRoom.parent;
            bossRoom.roomType = categories["roomType"].IndexOf("b");
            List<BayesRoom> cascadedChanges = new List<BayesRoom>();
            cascadedChanges.Add(bossRoom.parent);
            while (cascadedChanges.Count > 0) {
                BayesRoom room = cascadedChanges[0];
                cascadedChanges.RemoveAt(0);
                if (updateRoom(room, presets, model)) {
                    cascadedChanges.Add(room.parent);
                }
            }
            ignoreList.Add(endRoom);
            ignoreList.Add(bossRoom);
            BayesRoom itemRoom = correctDungeonRoom(categories["roomType"].IndexOf("I"), dungeon, ignoreList);
            ignoreList.Add(itemRoom);
            //KEYS?
            int keysPlaced = 0;
            int keysNeeded = 0;

            foreach (var room in dungeon.Values) {
                if (categories["roomType"][room.roomType].Contains('k')) {
                    keysPlaced++;

                    ignoreList.Add(room);
                }
                if (categories["doorCameFrom"][room.doorGoTo].Contains('k')) {
                    keysNeeded++;
                }
            }
            Console.WriteLine(keysPlaced + " vs " + keysNeeded);
            List<int> keyRoomTypes = new List<int>();
            foreach (var type in categories["roomType"]) {
                if (type.Contains('k')) {
                    keyRoomTypes.Add(categories["roomType"].IndexOf(type));
                }
            }

            while (keysPlaced < keysNeeded) {
                BayesRoom keyRoom = correctDungeonRoom(keyRoomTypes, dungeon, ignoreList, false);
                ignoreList.Add(keyRoom);
                keysPlaced++;
            }
            while (keysPlaced > keysNeeded) {
                correctDungeonDoor(categories["doorCameFrom"].IndexOf("k"), dungeon, new List<BayesRoom>(), false);
                keysNeeded++;
            }


        }
        static Bitmap CreateLTTPDungeon(Bitmap dungeon) {
            dungeon.RotateFlip(RotateFlipType.RotateNoneFlipY);
            int[,] distFromOpen = new int[dungeon.Width, dungeon.Height];
            int[,] distFrom2 = new int[dungeon.Width, dungeon.Height];
            distFromOpen.Fill(int.MaxValue);
            distFrom2.Fill(int.MaxValue);

            Color[] openColors = new Color[] {  Color.White, Color.Red, Color.DarkRed, Color.Green, 
                                                Color.Orange, Color.Yellow, Color.Purple, Color.Blue, Color.Turquoise};
            //Dictionary<LearnRooms.SearchNode, int> openSet = new Dictionary<LearnRooms.SearchNode, int>();
            int changed = 0;
            var colorComparer = new EqualityComparison<Color>((c1, c2)
                        => c1.R == c2.R && c2.G == c1.G && c1.B == c2.B);
            int[,] tilemap = new int[dungeon.Width, dungeon.Height];
            int[,] watermap = new int[dungeon.Width, dungeon.Height];
            for (int xx = 0; xx < dungeon.Width; xx++) {
                for (int yy = 0; yy < dungeon.Height; yy++) {
                    if (openColors.IndexOf(dungeon.GetPixel(xx, yy), colorComparer) == -1) {
                            tilemap[xx, yy] = 1;
                    }
                    else {
                        distFromOpen[xx, yy] = 0;
                        changed++;
                    }
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Blue)) {
                        watermap[xx, yy] = 1;
                    }

                }
            }
            int lastChanged = changed+1;
            while (lastChanged != changed) {
                lastChanged = changed;
                for (int xx = 1; xx < dungeon.Width - 1; xx++) {
                    for (int yy = 1; yy < dungeon.Height - 1; yy++) {
                        if (tilemap[xx,yy] == 1){
                            int minNeighbor = int.MaxValue;
                            for (int ii = -1; ii <= 1; ii++) {
                                for (int jj = -1; jj <= 1; jj++) {
                                    if (distFromOpen[xx + ii, yy + jj] < minNeighbor) {
                                        minNeighbor = distFromOpen[xx + ii, yy + jj];
                                    }
                                }
                            }
                            if (minNeighbor < distFromOpen[xx, yy]-1) {
                                changed++;
                                distFromOpen[xx, yy] = minNeighbor + 1;
                            }
                        }
                    }
                }
            }
            changed = 0;
            lastChanged =1;
            while (lastChanged != changed) {
                lastChanged = changed;
                for (int xx = 0; xx < dungeon.Width; xx++) {
                    for (int yy = 0; yy < dungeon.Height; yy++) {
                        if (distFromOpen[xx, yy] < 2) {
                            int minNeighbor = int.MaxValue;
                            for (int ii = -1; ii <= 1; ii++) {
                                for (int jj = -1; jj <= 1; jj++) {
                                    if (distFrom2[xx +ii, yy+jj] < minNeighbor) {
                                        minNeighbor = distFrom2[xx +ii, yy+jj];
                                    }
                                }
                            }
                            if (minNeighbor < distFrom2[xx, yy] - 1) {
                                changed++;

                                distFrom2[xx, yy] = minNeighbor + 1;
                            }
                        }
                        else if (distFrom2[xx,yy] != 0){
                            distFrom2[xx, yy] = 0;
                            changed++;
                        }
                    }
                }
            }
            Bitmap dist2bm = new Bitmap(dungeon.Width, dungeon.Height);
            Bitmap distbm = new Bitmap(dungeon.Width, dungeon.Height);
            for (int xx = 0; xx < dungeon.Width; xx++) {
                for (int yy = 0; yy < dungeon.Height; yy++) {
                    dist2bm.SetPixel(xx, yy, Color.FromArgb(255, distFrom2[xx, yy] * 15, distFrom2[xx, yy] * 15, distFrom2[xx, yy] * 15));
                    distbm.SetPixel(xx, yy, Color.FromArgb(255, Math.Min(255,distFromOpen[xx, yy] * 9), Math.Min(255,distFromOpen[xx, yy] * 9), Math.Min(255,distFromOpen[xx, yy] * 9)));
                }
            }
            dist2bm.Save("dist2bm.png");
            distbm.Save("distbm.png");
            
            var rules = GenerateRules();
            Bitmap tileset = (Bitmap)Bitmap.FromFile("Tiles/outer.png");
            Bitmap LttpDungeon = new Bitmap(dungeon.Width * tileset.Height, dungeon.Height * tileset.Height);
            Bitmap floorTiles = (Bitmap)Bitmap.FromFile("Tiles/floortile.png");
            Bitmap pitTiles = (Bitmap)Bitmap.FromFile("Tiles/pit.png");
            Bitmap[] tiles = new Bitmap[tileset.Width / tileset.Height];
                       //tiles = new Bitmap[tileset.Width / tileset.Height];
            for (int ii = 0; ii < tiles.Length; ii++) {
                tiles[ii] = new Bitmap(tileset.Height, tileset.Height);
                for (int xx = 0; xx < tileset.Height; xx++) {
                    for (int yy = 0; yy < tileset.Height; yy++) {
                        tiles[ii].SetPixel(xx, yy, tileset.GetPixel(ii * tileset.Height + xx, yy));
                    }
                }
            }
            for (int xx = 0; xx < dungeon.Width; xx++) {
                for (int yy = 0; yy < dungeon.Height; yy++) {
                    Stamp(LttpDungeon, tiles[13], xx * tiles[13].Width, yy * tiles[13].Height);
                }
            }
            for (int xx = 1; xx < dungeon.Width - 1; xx++) {
                for (int yy = 1; yy < dungeon.Height - 1; yy++) {                  

                    if (tilemap[xx, yy] == 1) {
                        int tile = GetIndex(rules.Item3, rules.Item1, rules.Item2, tilemap, xx, yy);
                        Stamp(LttpDungeon, tiles[tile], xx * tiles[tile].Width, yy * tiles[tile].Height);
                    }
                }
            }
            ///////////////////////
            for (int ii = 0; ii < tiles.Length; ii++) {
                tiles[ii] = new Bitmap(floorTiles.Height, floorTiles.Height);
                for (int xx = 0; xx < floorTiles.Height; xx++) {
                    for (int yy = 0; yy < floorTiles.Height; yy++) {
                        tiles[ii].SetPixel(xx, yy, floorTiles.GetPixel(ii * floorTiles.Height + xx, yy));
                    }
                }
            }
            int[] ignoreTiles = new int[]{1,30,31,32,33,34,15,7,10,25,26,11,21,20};
            for (int xx = 1; xx < dungeon.Width - 1; xx++) {
                for (int yy = 1; yy < dungeon.Height - 1; yy++) {
                    if (tilemap[xx, yy] == 1 && distFrom2[xx, yy] >= 2) {
                        int count = 0;
                        for (int ii = -1; ii <= 1; ii++) {
                            for (int jj = -1; jj <= 1; jj++) {
                                if (tilemap[xx + ii, yy + jj] > 0) {
                                    count++;
                                }
                            }
                        }
                        if (count < 4 || ignoreTiles.IndexOf(GetIndex(rules.Item3, rules.Item1, rules.Item2, tilemap, xx, yy)) != -1) {
                            tilemap[xx, yy] = -1;
                        }
                    }
                }
            }



            int[][] urCorner = new int[][]{
                new int[]{-1,0,1},
                new int[]{-1,0,0},
                new int[]{-1,-1,-1},
            };
            int[][] ulCorner = new int[][]{
                new int[]{1,0,-1},
                new int[]{0,0,-1},
                new int[]{-1,-1,-1},
            };
            int[][] lrCorner = new int[][]{
                new int[]{-1,-1,-1},
                new int[]{-1,0,0},
                new int[]{-1,0,1},
            };
            int[][] llCorner = new int[][]{
                new int[]{-1,-1,-1},
                new int[]{0,0,-1},
                new int[]{1,0,-1},
            };
            for (int xx = 1; xx < dungeon.Width - 1; xx++) {
                for (int yy = 1; yy < dungeon.Height - 1; yy++) {

                    if (tilemap[xx, yy] == 0) {
                        int tile = GetIndex(rules.Item3, rules.Item1, rules.Item2, tilemap, xx, yy);
                        Stamp(LttpDungeon, tiles[tile], xx * tiles[tile].Width, yy * tiles[tile].Height);
                    }
                    bool ur = true;
                    bool lr = true;
                    bool ul = true;
                    bool ll = true;
                    for (int ii = -1; ii <= 1; ii++) {
                        for (int jj = -1; jj <= 1; jj++) {
                            ur = ur && (urCorner[jj+1][ii+1] == -1 || (urCorner[jj+1][ii+1] == tilemap[xx + ii, yy + jj]));
                            lr = lr && (lrCorner[jj+1][ii+1] == -1 || (lrCorner[jj + 1][ii + 1] == tilemap[xx + ii, yy + jj]));
                            ul = ul && (ulCorner[jj + 1][ii + 1] == -1 || (ulCorner[jj + 1][ii + 1] == tilemap[xx + ii, yy + jj]));
                            ll = ll && (llCorner[jj+1][ii+1] == -1 || (llCorner[jj + 1][ii + 1] == tilemap[xx + ii, yy + jj]));
                        }
                    }
                    if (ur) {
                        Stamp(LttpDungeon, tiles[40], xx * tiles[40].Width, yy * tiles[40].Height);
                    }
                    if (lr) {
                        Stamp(LttpDungeon, tiles[42], xx * tiles[42].Width, yy * tiles[42].Height);
                    }
                    if (ul) {
                        Stamp(LttpDungeon, tiles[41], xx * tiles[41].Width, yy * tiles[41].Height);
                    }
                    if (ll) {
                        Stamp(LttpDungeon, tiles[44], xx * tiles[44].Width, yy * tiles[44].Height);
                    }
                }
            }

            List<Bitmap> enemySprites = new List<Bitmap>();
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Popo_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Popo_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Popo_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Popo_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Popo_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/StalfosBlue_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/StalfosBlue_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/StalfosBlue_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/BiriBlue_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/BiriBlue_ALttP.png"));
            enemySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Gibdo_ALttP.png"));

            List<Bitmap> trapSprites = new List<Bitmap>();
            trapSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Blade_Trap_ALttP.png"));
            List<Bitmap> itemSprites = new List<Bitmap>();
            itemSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/pot.png"));
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add(itemSprites[0]);
            itemSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/ALttP_Treasure_Chest.gif"));
           
            List<Bitmap> keySprites = new List<Bitmap>();
            keySprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/SmallKey.png"));

            List<Bitmap> puzzleSprites = new List<Bitmap>();
            puzzleSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/switch.png"));

            List<Bitmap> teleporterSprites = new List<Bitmap>();
            teleporterSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/Warp_Tile.gif"));

            for (int ii = 0; ii < tiles.Length; ii++) {
                tiles[ii] = new Bitmap(pitTiles.Height, pitTiles.Height);
                for (int xx = 0; xx < pitTiles.Height; xx++) {
                    for (int yy = 0; yy < pitTiles.Height; yy++) {
                        tiles[ii].SetPixel(xx, yy, pitTiles.GetPixel(ii * pitTiles.Height + xx, yy));
                    }
                }
            }
            Random rng = new Random();


            Color[] doorColors = new Color[] {  Color.Gray, Color.Gold, Color.Crimson, Color.DarkCyan, 
                                                Color.YellowGreen,Color.Beige};
         
            List<Bitmap> doorSprites = new List<Bitmap>();
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/doorTop.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/doorBottom.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/doorLeft.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/doorRight.png"));
           
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/keyUp.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/keyDown.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/keyLeft.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/keyRight.png"));

            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bossTop.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bossBottom.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bossLeft.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bossRight.png"));

            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/lockTop.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/lockBottom.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/lockLeft.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/lockRight.png"));

            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bombTop.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bombBottom.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bombLeft.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/bombRight.png"));

            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/outside.png"));
            doorSprites.Add((Bitmap)Bitmap.FromFile("Tiles/sprites/outside.png"));
           
            for (int xx = 1; xx < dungeon.Width - 1; xx++) {
                for (int yy = 1; yy < dungeon.Height - 1; yy++) {
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Red)) {
                        enemySprites.Shuffle(rng);
                        Stamp(LttpDungeon, enemySprites[0], xx * tiles[0].Width, (yy+1)* tiles[0].Height-enemySprites[0].Height);
                    }

                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.DarkRed)) {
                        trapSprites.Shuffle(rng);
                        Stamp(LttpDungeon, trapSprites[0], xx * tiles[0].Width, (yy + 1) * tiles[0].Height - trapSprites[0].Height);
                    }
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Green)) {
                        itemSprites.Shuffle(rng);
                        Stamp(LttpDungeon, itemSprites[0], xx * tiles[0].Width, (yy + 1) * tiles[0].Height - trapSprites[0].Height);
                    }
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Orange)) {
                        keySprites.Shuffle(rng);
                        Stamp(LttpDungeon, keySprites[0], xx * tiles[0].Width, (yy + 1) * tiles[0].Height - trapSprites[0].Height);
                    }
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Turquoise)) {
                        puzzleSprites.Shuffle(rng);
                        Stamp(LttpDungeon, puzzleSprites[0], xx * tiles[0].Width, (yy + 1) * tiles[0].Height - trapSprites[0].Height);
                    }
                    if (colorComparer.Equals(dungeon.GetPixel(xx, yy), Color.Purple)) {
                        teleporterSprites.Shuffle(rng);
                        Stamp(LttpDungeon, teleporterSprites[0], xx * tiles[0].Width, (yy + 1) * tiles[0].Height - trapSprites[0].Height);
                    }

                    if (watermap[xx, yy] == 1) {
                        Stamp(LttpDungeon, tiles[GetIndex(rules.Item3, rules.Item1, rules.Item2, watermap, xx, yy)], xx * tiles[0].Width, yy * tiles[0].Height);
                    }

                    int doorIndex = doorColors.IndexOf(dungeon.GetPixel(xx, yy), colorComparer);

                    if (doorIndex != -1) {
                        //Right Door
                        if (colorComparer.Equals(dungeon.GetPixel(xx - 1, yy), Color.White) && colorComparer.Equals(dungeon.GetPixel(xx, yy+1), Color.Black)) {
                            Stamp(LttpDungeon, doorSprites[3 + doorIndex*4], xx * tiles[0].Width, yy * tiles[0].Height);
                        }
                        //Left Door
                        if (colorComparer.Equals(dungeon.GetPixel(xx + 1, yy), Color.White) && colorComparer.Equals(dungeon.GetPixel(xx, yy + 1), Color.Black)) {
                            Stamp(LttpDungeon, doorSprites[2 + doorIndex * 4], xx * tiles[0].Width - 8, yy * tiles[0].Height);
                        }
                        //Bottom Door
                        if (colorComparer.Equals(dungeon.GetPixel(xx, yy+1), Color.White) && colorComparer.Equals(dungeon.GetPixel(xx-1, yy), Color.Black)) {
                            Stamp(LttpDungeon, doorSprites[0 + doorIndex * 4], xx * tiles[0].Width, yy * tiles[0].Height - 8);
                        }
                        //Top Door
                        if (colorComparer.Equals(dungeon.GetPixel(xx, yy-1), Color.White) && colorComparer.Equals(dungeon.GetPixel(xx-1, yy ), Color.Black)) {
                            Stamp(LttpDungeon, doorSprites[1 + doorIndex * 4], xx * tiles[0].Width, yy * tiles[0].Height);
                        }
                    }
                    /*
                     
                     * */
                }
            }


            LttpDungeon.Save("lttp.png");

            return LttpDungeon;
        }
        static int GetIndex(List<int[]> tileOffsets,List<int[]> positiveRules,List<int[]> negativeRules,int[,] tilemap, int ii, int jj) {
            if (positiveRules == null) {
                GenerateRules();
            }
            int index = 0;
            for (int ruleCount = 0; ruleCount < positiveRules.Count; ruleCount++) {
                bool allRulesPassed = true;

                for (int ruleNumber = 0; ruleNumber < positiveRules[ruleCount].Length && allRulesPassed; ruleNumber++) {
                    int xValue = tileOffsets[positiveRules[ruleCount][ruleNumber] - 1][0] + ii;
                    int yValue = tileOffsets[positiveRules[ruleCount][ruleNumber] - 1][1] + jj;
                    if (!(yValue >= tilemap.GetLength(1) ||
                        xValue >= tilemap.GetLength(0) ||
                        yValue < 0 ||
                        xValue < 0)) {
                        allRulesPassed = allRulesPassed && tilemap[xValue, yValue] > 0;
                    }
                }
                for (int ruleNumber = 0; ruleNumber < negativeRules[ruleCount].Length && allRulesPassed; ruleNumber++) {
                    int xValue = tileOffsets[negativeRules[ruleCount][ruleNumber] - 1][0] + ii;
                    int yValue = tileOffsets[negativeRules[ruleCount][ruleNumber] - 1][1] + jj;
                    if (!(yValue >= tilemap.GetLength(1) ||
                        xValue >= tilemap.GetLength(0) ||
                        yValue < 0 ||
                        xValue < 0)) {
                        allRulesPassed = allRulesPassed && tilemap[xValue, yValue] <= 0;
                    }
                }
                if (allRulesPassed) {
                    index = ruleCount + 1;
                    break;
                }
            }
            return index;
        }
        static Tuple<List<int[]>, List<int[]>, List<int[]>> GenerateRules() {
            var tileOffsets = new List<int[]>();

            tileOffsets.Add(new int[]{-1, -1});
            tileOffsets.Add(new int[]{0, -1});
            tileOffsets.Add(new int[]{1, -1});
            tileOffsets.Add(new int[]{-1, 0});
            tileOffsets.Add(new int[]{1, 0});
            tileOffsets.Add(new int[]{-1, 1});
            tileOffsets.Add(new int[]{0, 1});
            tileOffsets.Add(new int[] { 1, 1 });
		    var positiveRules = new List<int[]>();
		    var negativeRules = new List<int[]>();
		    positiveRules.Add(new int[]{7});
		    negativeRules.Add(new int[]{2, 4, 5});

		    //rule
		    positiveRules.Add(new int[]{5,7,8});
		    negativeRules.Add(new int[]{2,4});
		    //rule
		    positiveRules.Add(new int[]{4,5,6,7,8});
		    negativeRules.Add(new int[]{2});
		    //rule
		    positiveRules.Add(new int[]{4,6,7});
		    negativeRules.Add(new int[]{2, 5});

		    //rule
		    positiveRules.Add(new int[]{5,7});
		    negativeRules.Add(new int[]{2,4,8});
		    //rule
		    positiveRules.Add(new int[]{4,5,7});
		    negativeRules.Add(new int[]{2,6,8});
		    //rule
		    positiveRules.Add(new int[]{4,7});
		    negativeRules.Add(new int[]{2,5,6});

		    //rule
		    positiveRules.Add(new int[]{1,2,3,4,5,6,7});
		    negativeRules.Add(new int[]{8});
		    //rule
		    positiveRules.Add(new int[]{1,2,3,4,5,7});
		    negativeRules.Add(new int[]{6,8});
		    //rule
		    positiveRules.Add(new int[]{1,2,3,4,5,7,8});
		    negativeRules.Add(new int[]{6});

		    //rule
		    positiveRules.Add(new int[]{2,7});
		    negativeRules.Add(new int[]{4, 5});

		    //rule
		    positiveRules.Add(new int[]{2,3,5,7,8});
		    negativeRules.Add(new int[]{4});
		    //rule
		    positiveRules.Add(new int[]{1,2,3,4,5,6,7,8});
		    negativeRules.Add(new int[]{});
		    //rule
		    positiveRules.Add(new int[]{1,2,4,6,7});
		    negativeRules.Add(new int[]{5});

		    //rule
		    positiveRules.Add(new int[]{2,5,7});
		    negativeRules.Add(new int[]{4,3,8});
		    //rule
		    positiveRules.Add(new int[]{});
		    negativeRules.Add(new int[]{2,4,5,7});
		    //rule
		    positiveRules.Add(new int[]{2,4,7});
		    negativeRules.Add(new int[]{1, 5, 6});

		    //rule
		    positiveRules.Add(new int[]{1,2,4,5,6,7});
		    negativeRules.Add(new int[]{3,8});
		    //rule
		    positiveRules.Add(new int[]{2,4,5,7});
		    negativeRules.Add(new int[]{1,3,6,8});
		    //rule
		    positiveRules.Add(new int[]{2,3,4,5,7,8});
		    negativeRules.Add(new int[]{1, 6});


		    //rule
		    positiveRules.Add(new int[]{2});
		    negativeRules.Add(new int[]{4, 5,7});

		    //rule
		    positiveRules.Add(new int[]{2,3,5});
		    negativeRules.Add(new int[]{4,7});
		    //rule
		    positiveRules.Add(new int[]{1,2,3,4,5});
		    negativeRules.Add(new int[]{7});
		    //rule
		    positiveRules.Add(new int[]{1,2,4});
		    negativeRules.Add(new int[]{5, 7});

		    //rule
		    positiveRules.Add(new int[]{2,5});
		    negativeRules.Add(new int[]{3,4,7});
		    //rule
		    positiveRules.Add(new int[]{2,4,5});
		    negativeRules.Add(new int[]{1,3,7});
		    //rule
		    positiveRules.Add(new int[]{2,4});
		    negativeRules.Add(new int[]{1,5,7});

		    //rule
		    positiveRules.Add(new int[]{1,2,4,5,6,7,8});
		    negativeRules.Add(new int[]{3});
		    //rule
		    positiveRules.Add(new int[]{2,4,5,6,7,8});
		    negativeRules.Add(new int[]{1,3});
		    //rule
		    positiveRules.Add(new int[]{2,3,4,5,6,7,8});
		    negativeRules.Add(new int[]{1});

		    //rule
		    positiveRules.Add(new int[]{5});
		    negativeRules.Add(new int[]{2, 4, 7});			
		    //rule
		    positiveRules.Add(new int[]{4,5});
		    negativeRules.Add(new int[]{2,7});
		    //rule
		    positiveRules.Add(new int[]{4});
		    negativeRules.Add(new int[]{2, 5, 7});

		    //rule
		    positiveRules.Add(new int[]{4,5,6,7});
		    negativeRules.Add(new int[]{2,8});	
		    //rule
		    positiveRules.Add(new int[]{1,2,4,7});
		    negativeRules.Add(new int[]{5,6});	
		    //rule
		    positiveRules.Add(new int[]{2,3,5,7});
		    negativeRules.Add(new int[]{4,8});	
		    //rule
		    positiveRules.Add(new int[]{4,5,7,8});
		    negativeRules.Add(new int[]{2, 6});	

		    //rule
		    positiveRules.Add(new int[]{1,2,4,5,7});
		    negativeRules.Add(new int[]{3,6,8});	
		    //rule
		    positiveRules.Add(new int[]{2,3,4,5,7});
		    negativeRules.Add(new int[]{1, 6, 8});	

		    //rule
		    positiveRules.Add(new int[]{1,2,4,5,7,8});
		    negativeRules.Add(new int[]{3,6});	
		    //rule
		    positiveRules.Add(new int[]{2,3,4,5,6,7});
		    negativeRules.Add(new int[]{1, 8});	

		    //rule
		    positiveRules.Add(new int[]{2,5,7,8});
		    negativeRules.Add(new int[]{3,4});	
		    //rule
		    positiveRules.Add(new int[]{2,3,4,5});
		    negativeRules.Add(new int[]{1,7});	
		    //rule
		    positiveRules.Add(new int[]{1,2,4,5});
		    negativeRules.Add(new int[]{3,7});	
		    //rule
		    positiveRules.Add(new int[]{2,4,6,7});
		    negativeRules.Add(new int[]{1,5});	
		    //rule
		    positiveRules.Add(new int[]{2,4,5,6,7});
		    negativeRules.Add(new int[]{1,3,8});	
		    //rule
		    positiveRules.Add(new int[]{2,4,5,7,8});
		    negativeRules.Add(new int[]{1, 3, 6});
            return new Tuple<List<int[]>, List<int[]>, List<int[]>>(positiveRules, negativeRules, tileOffsets);
        }
        static BayesRoom correctDungeonRoom(int roomType, Dictionary<string, BayesRoom> dungeon, List<BayesRoom> ignore, bool unique = true) {
            double best = double.NegativeInfinity;
            BayesRoom bestRoom = null;
            foreach (var room in dungeon.Values) {
                if (!ignore.Contains(room)) {
                    if (room.roomType == roomType && unique) {
                        best = double.PositiveInfinity;
                        bestRoom = room;
                    }
                    else if (room.roomType != roomType && room.roomDist.GetLogProb(roomType) > best) {
                        best = room.roomDist.GetLogProb(roomType);
                        bestRoom = room;
                    }
                }
            }
            bestRoom.roomType = roomType;
            return bestRoom;
        }
        static BayesRoom correctDungeonRoom(List<int> roomTypes, Dictionary<string, BayesRoom> dungeon, List<BayesRoom> ignore, bool unique = true) {
            double best = double.NegativeInfinity;
            BayesRoom bestRoom = null;
            int bestType = 0;
            foreach (var roomType in roomTypes) {
                foreach (var room in dungeon.Values) {
                    if (!ignore.Contains(room)) {
                        if (room.roomType == roomType && unique) {
                            best = double.PositiveInfinity;
                            bestType = roomType;
                            bestRoom = room;
                        }
                        else if (room.roomType != roomType && room.roomDist.GetLogProb(roomType) > best) {
                            best = room.roomDist.GetLogProb(roomType);
                            bestType = roomType;
                            bestRoom = room;
                        }
                    }
                }
            }
            bestRoom.roomType = bestType;
            return bestRoom;
        }
        static BayesRoom correctDungeonDoor(int doorType, Dictionary<string, BayesRoom> dungeon, List<BayesRoom> ignore, bool unique = true) {
            double best = double.NegativeInfinity;
            BayesRoom bestRoom = null;
            foreach (var room in dungeon.Values) {
                if (!ignore.Contains(room)) {
                    if (room.doorGoTo == doorType && unique) {
                        best = double.PositiveInfinity;
                        bestRoom = room;
                    }
                    else if (room.doorGoTo != doorType && room.doorDist.GetLogProb(doorType) > best) {
                        best = room.doorDist.GetLogProb(doorType);
                        bestRoom = room;
                    }
                }
            }
            bestRoom.doorGoTo = doorType;
            return bestRoom;
        }
        static void Main(string[] args) {
            //      RunAllLevels();
            // CreateGraphicalModelFiles();
            //GraphicalModel model = CreateGraphicalModel();
            //  InferTest.Test2();

            /*
            List<string> predicted = new List<string>();
            predicted.Add("roomsInLevel");
            predicted.Add("neighborTypes");
             * */
            string[] allSummaries = new string[]{
                    "Summaries/LA1.xml","Summaries/LA2.xml","Summaries/LA3.xml",
                    "Summaries/LA4.xml","Summaries/LA5.xml","Summaries/LA6.xml","Summaries/LA7.xml",
                    "Summaries/LA8.xml","Summaries/LoZ1.xml","Summaries/LoZ2.xml",
                    "Summaries/LoZ21.xml","Summaries/LoZ22.xml","Summaries/LoZ23.xml","Summaries/LoZ24.xml",
                    "Summaries/LoZ25.xml","Summaries/LoZ26.xml","Summaries/LoZ27.xml",
                    "Summaries/LoZ28.xml","Summaries/LoZ29.xml","Summaries/LoZ3.xml",
                    "Summaries/LoZ4.xml","Summaries/LoZ5.xml","Summaries/LoZ6.xml","Summaries/LoZ7.xml",
                    "Summaries/LoZ8.xml","Summaries/LoZ9.xml","Summaries/LttP1.xml",
                    "Summaries/LttP10.xml","Summaries/LttP11.xml","Summaries/LttP12.xml","Summaries/LttP2.xml",
                    "Summaries/LttP3.xml","Summaries/LttP4.xml","Summaries/LttP5.xml",
                    "Summaries/LttP6.xml","Summaries/LttP7.xml","Summaries/LttP8.xml",
                    "Summaries/LttP9.xml",
                };
            if (true) {

                //    RunAllLevels();
            }
            //  string downloadedFilename = "Generator.xml";
            string variantName = "SESVGen.xml"; //CHANGE THIS
            string downloadedFilename = "SuperExtremeSparseVariantGenerator.xml";
            //  CreateGraphicalModelFiles(allSummaries, downloadedFilename, variantName, "ZeldaData.xml");
            //   var output = CreateGraphicalModel(variantName, "ZeldaData.xml");
            var modelData = ModelNetworkSerialized("SESVGen.bin", variantName, "ZeldaData.xml");
            //  var rooms = CreateDungeon(new Dictionary<string, Tuple<int[], double[]>>(), new NonSharedGraphicalModel(modelData.Item1));
            string[] rooms = new string[]{"1 _ _ 0,0 0,-1",
                "2 p _ 1,0 0,0",
                "3 e l 1,1 1,0",
                "3 e _ 2,0 1,0",
                "4 p _ 0,1 1,1",
                "4 p l 2,1 2,0",
                "4 ei _ 1,2 1,1",
                "5 e _ 3,1 2,1",
                "5 e _ -1,1 0,1",
                "5 e k 0,2 1,2",
                "5 e _ 2,2 2,1",
                "6 e _ -2,1 -1,1",
                "6 ek I 3,2 3,1",
                "7 kp k 4,2 3,2",
                "6 ei _ 4,1 3,1",
                "6 e _ -1,2 0,2",
                "6 m l 0,3 0,2",
                "7 ei _ 0,4 0,3",
                "6 m _ 2,3 2,2",
                "7 ei _ 2,4 2,3",
                "8 ekp l 2,5 2,4",
                "7 _ I -1,3 -1,2",
                "7 ek k -2,2 -1,2",
                "7 p k -3,1 -2,1",
                "5 e b 1,3 1,2",
                "9 I _ 3,5 2,5",
                "7 ekp l 3,3 3,2",
                "8 e _ -2,3 -2,2",
                "8 e l -3,2 -2,2",
                "8 e k -1,4 0,4",
                "8 p l 3,4 3,3",
                "9 b _ 4,4 3,4",
                "9 ek _ -3,3 -2,3",
                "9 ek _ -2,4 -2,3",
                "10 e k 3,6 3,5",
                "10 eip l -3,4 -2,4",
                "10 t k 5,4 4,4",
                "7 e _ 5,1 4,1",
                "6 p l 1,4 1,3",
                "7 e _ 1,5 1,4"};
            var dungeons = ConstructDungeonImage(new List<string>(rooms));
            CreateLTTPDungeon(dungeons[0]);
            /*  
              string[] summaries = null;
              summaries = new string[]{
                  "Summaries/LA1.xml"
              };
               */
            /*
            string[] allSummaries = new string[]{
                "Summaries/LA1.xml","Summaries/LA2.xml","Summaries/LA3.xml",
                "Summaries/LA4.xml","Summaries/LA6.xml","Summaries/LA7.xml",
                "Summaries/LA8.xml","Summaries/LoZ1.xml","Summaries/LoZ2.xml",
                "Summaries/LoZ21.xml","Summaries/LoZ22.xml","Summaries/LoZ24.xml",
                "Summaries/LoZ25.xml","Summaries/LoZ26.xml","Summaries/LoZ27.xml",
                "Summaries/LoZ28.xml","Summaries/LoZ29.xml","Summaries/LoZ3.xml",
                "Summaries/LoZ4.xml","Summaries/LoZ5.xml","Summaries/LoZ7.xml",
                "Summaries/LoZ8.xml","Summaries/LoZ9.xml","Summaries/LttP1.xml",
                "Summaries/LttP10.xml","Summaries/LttP11.xml","Summaries/LttP2.xml",
                "Summaries/LttP3.xml","Summaries/LttP4.xml","Summaries/LttP5.xml",
                "Summaries/LttP6.xml","Summaries/LttP7.xml","Summaries/LttP8.xml",
                "Summaries/LttP9.xml",
            };
            */
            /*
            string[] allSummaries = new string[]{
                "Summaries/TestSet/LA5.xml"  ,  "Summaries/TestSet/LoZ6.xml",
                "Summaries/TestSet/LoZ23.xml" , "Summaries/TestSet/LttP12.xml"
            };
             * */
            //  string downloadedFilename = "SuperSimple.xml";
            //     string variantName = "SS.xml"; //CHANGE THIS

            /*
            string evalData = "test2.xml";
            string dataFile = "testSmall.xml";
            Dictionary<string, List<double>> errors = new Dictionary<string, List<double>>();
            errors["Chosen"] = new List<double>();
            errors["ESV"] = new List<double>();
            errors["NaiveBayes"] = new List<double>();
            errors["SV"] = new List<double>();
            foreach (var summ in allSummaries) {
                string[] summaries = new string[] { summ };
                CreateGraphicalModelFiles(summaries, downloadedFilename, variantName, dataFile); //FILE CONVERSION

                //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
                //  CreateGraphicalModelFiles(downloadedFilename, variantName); //FILE CONVERSION
                var output = ModelNetworkSerialized("Chosen.bin", "Chosen.xml", evalData);
            //    var output = CreateGraphicalModel("ESV.xml", evalData); 
                Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
                foreach (var kv in observedData) {
                    if (kv.Value.Item1 != null) {
                        for (int ii = 0; ii < kv.Value.Item1.Length; ii++) {
                            kv.Value.Item1[ii] = kv.Value.Item1[ii] < 0 ? 0 : kv.Value.Item1[ii];
                        }
                    }
                }
               // TestC(output.Item1, observedData);
          //      errors["Chosen"].Add(Infer(output.Item1, observedData, predicted));
                output = ModelNetworkSerialized("ESV.bin", "ESV.xml", evalData);
           //     errors["ESV"].Add(Infer(output.Item1, observedData, predicted));
          //      Test(output.Item1, observedData);
                output = ModelNetworkSerialized("NaiveBayes.bin", "NaiveBayes.xml", evalData);
         //       errors["NaiveBayes"].Add(Infer(output.Item1, observedData, predicted));
              //  Test(output.Item1, observedData); 
                output = ModelNetworkSerialized("SV.bin", "SV.xml", evalData);
         //       errors["SV"].Add(Infer(output.Item1, observedData, predicted));
              //  Test(output.Item1, observedData);
            }
            foreach (var v in errors) {
                double sse = 0;
                foreach (var e in v.Value) {
                    sse += e;
                }
                Console.WriteLine(v + " " + sse);
            }
            
               
  */

            //   var output = ModelNetworkSerialized("Chosen.bin", "Chosen.xml", evalData);
            //   Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
            //   Test(output.Item1);
            //     double  evaluationMetric = evaluate(output.Item1, output.Item2);
            //      Console.WriteLine(evaluationMetric);

            //        output = ModelNetworkSerialized("NaiveBayes.bin", "NaiveBayes.xml", evalData);
            //    evaluationMetric = evaluate(output.Item1, output.Item2);
            //    Console.WriteLine(evaluationMetric);

            //       output = ModelNetworkSerialized("SV.bin", "SV.xml", evalData);
            //    evaluationMetric = evaluate(output.Item1, output.Item2);
            //    Console.WriteLine(evaluationMetric);

            //    output = ModelNetworkSerialized("ESV.bin", "ESV.xml", evalData);
            //    evaluationMetric = evaluate(output.Item1, output.Item2);
            //    Console.WriteLine(evaluationMetric);
            //  CreateGraphicalModel();
            //  var output = ModelNetworkSprinklerFile();
            //     var output = CreateGraphicalModel(variantName, dataFile);
            //  var  output = ModelNetworkSerialized("SuperSimple.bin", "SuperSimple.xml", dataFile);
            //   Infer(output.Item1, output.Item2, predicted);
            //    ModelNetworkSprinklerSerialized();

            //CHANGE THIS
            //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
            //  CreateGraphicalModelFiles(downloadedFilename, variantName); //FILE CONVERSION
            //  CreateGraphicalModel();
            //    Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData("dungeonNetworkData.xml");
            //       string downloadedFilename = "Random.xml"; //CHANGE THIS
            //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
            //  CreateGraphicalModelFiles(downloadedFilename, variantName); //FILE CONVERSION


            //  downloadedFilename = "BayesNetwork.xml"; //CHANGE THIS
            //  variantName = "dungeonNetwork.xml"; //CHANGE THIS
            // CreateGraphicalModelFiles(downloadedFilename, variantName); //FILE CONVERSION
            //   var output = CreateGraphicalModel("SuperSimple.xml", "dungeonNetworkData.xml"); // LEARNING HAPPENS

            //      double evaluationMetric = evaluate(output.Item1, output.Item2);

            //   Console.WriteLine(evaluationMetric);
            //   output = ModelNetworkSerialized();
            //   evaluationMetric = evaluate(output.Item1, output.Item2);

            //       Console.WriteLine(evaluationMetric);
            Console.WriteLine("ALL DONE :)");
            //Console.Read();
        }
    }
}
