﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/*
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Collections;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Graphs;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Transforms;
using MicrosoftResearch.Infer.Utils;
using MicrosoftResearch.Infer.Views;
*/
using ZeldaInfer.LevelParse;
namespace ZeldaInfer {
	class Program {
		
		static Tuple<GraphicalModel,Dictionary<string, Tuple<int[],double[]>>> ModelNetworkSprinklerFile() {

			GraphicalModel model = new GraphicalModel("WetRainSprinkler.xml",5);
			model.CreateNetwork();
			Dictionary<string, Tuple<int[],double[]>> observedData = GraphicalModel.LoadData("WetRainSprinklerData3.xml");
            model.LearnParameters(observedData);
            BinaryFormatter serializer = new BinaryFormatter();

            using (FileStream stream = new FileStream("temp.bin", FileMode.Create)) {
                serializer.Serialize(stream, model);
            }
            return new Tuple<GraphicalModel,Dictionary<string,Tuple<int[],double[]>>>(model,observedData);
		}

        static void ModelNetworkSprinklerSerialized() {
            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("temp.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
            GraphicalModel model = (GraphicalModel)formatter.Deserialize(stream);
       //     model.LoadAfterSerialization("WetRainSprinkler3.xml");
            stream.Close();

        }
		 
		static void RunAllLevels() {
			string[] levels = new string[]{
                "Levels/LA 1.xml","Levels/LA 2.xml","Levels/LA 3.xml","Levels/LA 4.xml",
                "Levels/LA 6.xml","Levels/LA 7.xml",
                "Levels/LA 8.xml","Levels/LoZ 1.xml",
                "Levels/LoZ 2.xml","Levels/LoZ 3.xml","Levels/LoZ 4.xml","Levels/LoZ 5.xml",
                "Levels/LoZ 7.xml","Levels/LoZ 8.xml","Levels/LoZ 9.xml","Levels/LoZ2 1.xml",
                "Levels/LoZ2 2.xml","Levels/LoZ2 4.xml","Levels/LoZ2 5.xml","Levels/LoZ2 6.xml",
                "Levels/LoZ2 7.xml","Levels/LoZ2 8.xml",
                "Levels/LoZ2 9.xml",
                "Levels/LttP 1.xml",
                "Levels/LttP 10.xml",
                "Levels/LttP 11.xml",
                "Levels/LttP 2.xml","Levels/LttP 3.xml",
                "Levels/LttP 4.xml","Levels/LttP 5.xml","Levels/LttP 6.xml","Levels/LttP 7.xml",
                "Levels/LttP 8.xml","Levels/LttP 9.xml",
			};

			foreach (var level in levels) {
				Console.WriteLine(level);
				Dungeon dungeon = new Dungeon(level);
				SearchAgent path = dungeon.getOptimalPath(level.Contains("LttP"));
				Console.WriteLine(path.pathToString());
				dungeon.UpdateRooms(path);
				string output = level;
				output = Regex.Replace(output, @"Levels", "Summaries");
				output = Regex.Replace(output, " ", "");
				dungeon.WriteStats(output, path);
			}
		}
		static void CreateGraphicalModelFiles(string[] summaries, string inputFile, string networkFilename,string dataFilename) {
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
                        copies = element.Value.Count(f => f == ';')+1;
                    }
                    /*
                    if (summaryDictionary.ContainsKey(element.Name.LocalName)) {

                    }
                    else {
                        summaryDictionary[element.Name.LocalName] = element.Value + "|";
                    }
                     * */
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
           
            //    Console.WriteLine(str);
            }
            categoriesDoc.Save("categories.xml");
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
                    Enumerable.Range(0, category.Value.Count))),new XAttribute("domain",domain)));
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
                }
                dataDoc.Root.Add(new XElement("Data",new XAttribute("domain",domain), new XAttribute("name", param.Key),string.Join(",",param.Value.Substring(0,param.Value.Length-1).Split(';').Select(p => categories[param.Key].IndexOf(p)))  ));
              //  Console.WriteLine(param.Key + " = [" + string.Join(",",param.Value.Substring(0,param.Value.Length-1).Split(';')) + "]");
            }
            dataDoc.Save(dataFilename);
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

            GraphicalModel model = new GraphicalModel(modelFile, 13);
            model.CreateNetwork();
           
            Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
            model.LearnParameters(observedData);
            BinaryFormatter serializer = new BinaryFormatter();

            using (FileStream stream = new FileStream(modelFile.Substring(0, modelFile.LastIndexOf(".")) + "bin", FileMode.Create)) {
                serializer.Serialize(stream, model);
            }

            return new Tuple<GraphicalModel, Dictionary<string, Tuple<int[], double[]>>>(model, observedData);
        }

        static double evaluate(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> data)
        {
            int dataPointCount = 0;
            foreach (var datatype in data)
            {
                if (datatype.Value.Item1 != null)
                {
                    dataPointCount = datatype.Value.Item1.Length;
                    break;
                }
                else
                {
                    dataPointCount = datatype.Value.Item2.Length;
                    break;
                }
            }

            double loglikelihood = 0;
            int N = dataPointCount, d = 0;

            foreach (ModelNode eachNode in model.nodes.Values) 
            {
                int parentStates = 1;
                int numericalParentCount = 1;
                foreach (var parent in eachNode.parents){
                    if (parent.distributionType == DistributionType.Categorical)
                    {
                        parentStates *= parent.states.SizeAsInt;
                    }
                    else
                    {
                        numericalParentCount += 1;
                    }
                }
                if (eachNode.distributionType == DistributionType.Categorical)
                {
                    d += eachNode.states.SizeAsInt * parentStates * numericalParentCount;
                }
                else
                {
                    d += parentStates * numericalParentCount;
                }
            }

            for (int ii = 0; ii < dataPointCount; ii++)
            {
                foreach (ModelNode eachNode in model.nodes.Values)
                {

                    loglikelihood += eachNode.distributions.getLogLikelihood(data, ii);

                }
            }

            return loglikelihood - d/2.0 * Math.Log(N);
        }

        static Tuple<GraphicalModel,Dictionary<string, Tuple<int[],double[]>>> ModelNetworkSerialized(string binary, string network, string data) {
            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(binary, FileMode.Open, FileAccess.Read, FileShare.Read);
            GraphicalModel model = (GraphicalModel)formatter.Deserialize(stream);
            Dictionary<string, Tuple<int[],double[]>> observedData = GraphicalModel.LoadData(data);

            model.LoadAfterSerialization(network,1);
            stream.Close();
            return new Tuple<GraphicalModel,Dictionary<string,Tuple<int[],double[]>>>(model,observedData);
        }
        static void Infer(GraphicalModel model,Dictionary<string,Tuple<int[],double[]>> data, List<string> inferred){
            foreach (var varName in inferred) {
                data.Remove(varName);
            }
            model.LearnParameters(data);
            foreach (var varName in inferred) {
                var prediction = model.nodes[varName].distributions.getPredicted();
                if (model.nodes[varName].distributionType == DistributionType.Categorical) {
                    Console.WriteLine("Predicted Value is " + prediction.Item1);
                }
                else {
                    Console.WriteLine("Predicted Value is " + prediction.Item2);
                }
            }
            
        }
		static void Main(string[] args) {
      //      RunAllLevels();
          // CreateGraphicalModelFiles();
            //GraphicalModel model = CreateGraphicalModel();
          //  InferTest.Test2();

            List<string> predicted = new List<string>();
            predicted.Add("roomsInLevel");

            string[] summaries = new string[]{
                "Summaries/LttP1.xml"
            };
            string downloadedFilenmae = "Random.xml"; 
            string variantName = "RandomNetwork.xml"; //CHANGE THIS
            string dataFile = "LA1Test.xml";
            CreateGraphicalModelFiles(summaries, downloadedFilenmae, variantName, dataFile); //FILE CONVERSION
            //  CreateGraphicalModel();
          //  var output = ModelNetworkSprinklerFile();
            var output = ModelNetworkSerialized("learnedDungeonNetworkData.bin", "dungeonNetwork.xml", dataFile);
            Infer(output.Item1, output.Item2, predicted);
        //    ModelNetworkSprinklerSerialized();

 //CHANGE THIS
            //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
            //  CreateGraphicalModelFiles(downloadedFilenmae, variantName); //FILE CONVERSION
          //  CreateGraphicalModel();
        //    Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData("dungeonNetworkData.xml");
     //       string downloadedFilenmae = "Random.xml"; //CHANGE THIS
     //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
          //  CreateGraphicalModelFiles(downloadedFilenmae, variantName); //FILE CONVERSION


          //  downloadedFilenmae = "BayesNetwork.xml"; //CHANGE THIS
          //  variantName = "dungeonNetwork.xml"; //CHANGE THIS
           // CreateGraphicalModelFiles(downloadedFilenmae, variantName); //FILE CONVERSION
         //   var output = CreateGraphicalModel("SuperSimple.xml", "dungeonNetworkData.xml"); // LEARNING HAPPENS

      //      double evaluationMetric = evaluate(output.Item1, output.Item2);

         //   Console.WriteLine(evaluationMetric);
         //   output = ModelNetworkSerialized();
         //   evaluationMetric = evaluate(output.Item1, output.Item2);

     //       Console.WriteLine(evaluationMetric);
			Console.WriteLine("ALL DONE :)");
			Console.Read();
		}
	}
}
