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

        static void Test(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> observedData) {
           // Variable.GaussianFromMeanAndPrecision(38.5, .005);

        /*    Variable<double> meanPrior =  Variable<double>.Random((model.nodes["roomsInLevel"].distributions as NoParentNodeNumerical).meanPosterior).Named("a");
            Variable<double> precPrior = Variable<double>.Random((model.nodes["roomsInLevel"].distributions as NoParentNodeNumerical).precPosterior).Named("b");

/*
            Gaussian roomSize = Gaussian.FromMeanAndPrecision((model.nodes["roomsInLevel"].distributions as NoParentNodeNumerical).meanPosterior.GetMean(),
                                                              (model.nodes["roomsInLevel"].distributions as NoParentNodeNumerical).precPosterior.GetMean());
          
 * */
            Variable<double> Amean = Variable.GaussianFromMeanAndPrecision(38.5, 0.1);
            Variable<double> Aprec = Variable.GammaFromMeanAndVariance(0.005, 0.001);
            Variable<double> A = Variable.GaussianFromMeanAndPrecision(Amean, Aprec);//Variable<double>.GaussianFromMeanAndPrecision(meanPrior, precPrior).Named("c");

            /*     
             *  Vector mean = Vector.FromArray(new double[]{0,0});
             *  PositiveDefiniteMatrix prec = new PositiveDefiniteMatrix(2,2);
                 (model.nodes["puzzleRoomsInLevel"].distributions as LinearRegression).bPost.GetMeanAndPrecision(mean, prec);
                 Console.WriteLine(mean[0] + " " + prec[0, 0]);
                 Variable<double> bias = Variable<double>.GaussianFromMeanAndPrecision(mean[0],prec[0,0]);
                 Variable<double> coeff = Variable<double>.GaussianFromMeanAndPrecision(mean[1],prec[1,1]);
             */
            Variable<double> bias = Variable.GaussianFromMeanAndPrecision(0.45, 100);
            Variable<double> coeff = Variable.GaussianFromMeanAndPrecision(0.45, 100);
            Variable<double> puzzles = bias + coeff * A;
            puzzles.ObservedValue = 12;
            InferenceEngine engine = new InferenceEngine(); //new InferenceEngine(new GibbsSampling());
            Console.WriteLine(engine.Infer(Amean));
            Console.WriteLine(engine.Infer(A));
           // Gaussian roomPost = engine.Infer(room);
          //  Console.WriteLine(observedData["roomsInLevel"].Item2[0] + " - " + engine.Infer(room));
        }

        static void TestA(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> observedData) {
         
            Variable<double> Amean = Variable.GaussianFromMeanAndPrecision(38.5, 0.1);
            Variable<double> Aprec = Variable.GammaFromMeanAndVariance(0.005, 0.001);
            Variable<double> A = Variable.GaussianFromMeanAndPrecision(Amean, Aprec);
            Variable<double> bias = Variable.GaussianFromMeanAndPrecision(0.45, 1);
            Variable<double> coeff = Variable.GaussianFromMeanAndPrecision(0.45, 1);
            Variable<double> B = bias + coeff * A;
            B.ObservedValue = 12;
            InferenceEngine engine = new InferenceEngine();
            Console.WriteLine(engine.Infer(A));
            Console.WriteLine(engine.Infer(Amean));
        }

        static void TestC(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> observedData) {
            Range N = new Range(1);
            Variable<double> Amean = Variable.GaussianFromMeanAndPrecision(38.5, 0.1).Named("Amean");
            Variable<double> Aprec = Variable.GammaFromMeanAndVariance(0.005, 0.001).Named("Aprec");
            VariableArray<double> A = Variable.Array<double>(N).Named("A");
            A[N] = Variable.GaussianFromMeanAndPrecision(Amean, Aprec).ForEach(N);
            var x = Variable.Array<Vector>(N).Named("x");
            for (int ii = 0; ii < N.SizeAsInt; ii++) {
                VariableArray<double> row = Variable<double>.Array(new Range(2)).Named("row");
                row[0] = Variable.GaussianFromMeanAndPrecision(1, 10000);
                row[1] = A[0];
                x[ii] = Variable.Vector(row);
            }
            PositiveDefiniteMatrix prec = new PositiveDefiniteMatrix(new double[,] { { 100, 0 }, { 0, 100 } });
            Variable<Vector> coeffs = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(new double[] { 0.45, 0.45 }), prec).Named("Coeffs");
            
            
            VariableArray<double> B = Variable.Array<double>(N).Named("B");
            B[N] = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct( x[N],coeffs), 1.0).Named("Bprior");

            B.ObservedValue = new double[] { 24 };

            var x2 = Variable.Array<Vector>(N).Named("x2");
            for (int ii = 0; ii < N.SizeAsInt; ii++) {
                VariableArray<double> row2 = Variable<double>.Array(new Range(2)).Named("row2");
                row2[0] = Variable.GaussianFromMeanAndPrecision(1, 10000);
                row2[1] = B[0];
                x2[ii] = Variable.Vector(row2);
            }
            PositiveDefiniteMatrix prec2 = new PositiveDefiniteMatrix(new double[,] { { 100, 0 }, { 0, 100 } });
            Variable<Vector> coeffs2 = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(new double[] { 0.45, 0.45 }), prec2).Named("Coeffs2");


            VariableArray<double> C = Variable.Array<double>(N).Named("C");
            C[N] = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct(x2[N], coeffs2), 1.0).Named("Cprior");
            C.ObservedValue = new double[] { 12 };

            InferenceEngine engine = new InferenceEngine(new VariationalMessagePassing());
            Console.WriteLine(engine.Infer(A));
            Console.WriteLine(engine.Infer(Amean));
        }
        static void TestB(GraphicalModel model, Dictionary<string, Tuple<int[], double[]>> observedData) {

            Variable<double> Amean = Variable.GaussianFromMeanAndPrecision(38.5, 0.1);
            Variable<double> Aprec = Variable.GammaFromMeanAndVariance(0.005, 0.001);
            Variable<double> A = Variable.GaussianFromMeanAndPrecision(Amean, Aprec); 
            VariableArray<double> x = Variable<double>.Array(new Range(2));
            x[0] = Variable<double>.Random(Gaussian.PointMass(1));
            x[1] = A;
           // Variable<double> bias = Variable.GaussianFromMeanAndPrecision(0.45, 100);
          //  Variable<double> coeff = Variable.GaussianFromMeanAndPrecision(0.45, 100);
            PositiveDefiniteMatrix prec = new PositiveDefiniteMatrix(new double[,]{{100,0},{0,100}});
            Variable<Vector> coeffs = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(new double[]{0.45,0.45}),PositiveDefiniteMatrix.Identity(2));
            Variable<double> B = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct(Variable.Vector(x),coeffs),1);
            B.ObservedValue = 12;
            InferenceEngine engine = new InferenceEngine(new VariationalMessagePassing());
            Console.WriteLine(engine.Infer(x));
            Console.WriteLine(engine.Infer(A));
            Console.WriteLine(engine.Infer(Amean));
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
                if (category.Key == "roomType") {
                    bool stopHere = true;
                }
                categories[category.Key] = new List<string>(new SortedSet<string>(category.Value.Split(';')));
                categories[category.Key].Remove("");
                categoriesDoc.Root.Add(new XElement(category.Key, new XAttribute("count", categories[category.Key].Count), string.Join(";", categories[category.Key].ToArray())));

                //    Console.WriteLine(str);
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
                    dataDoc.Root.Add(new XElement("Data", new XAttribute("domain", domain), new XAttribute("name", param.Key), string.Join(",", param.Value.Substring(0, param.Value.Length - 1).Split(';').Select(p => categories[param.Key].IndexOf(p)))));
             
                }
                else {
                    dataDoc.Root.Add(new XElement("Data", new XAttribute("domain", domain), new XAttribute("name", param.Key), string.Join(",", param.Value.Substring(0, param.Value.Length - 1).Split(';'))));
             
                }
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

      //      GraphicalModel model = new GraphicalModel(modelFile, 13);
            GraphicalModel model = new GraphicalModel(modelFile, 13);
            model.CreateNetwork();
           
            Dictionary<string, Tuple<int[], double[]>> observedData = GraphicalModel.LoadData(dataFile);
            model.LearnParameters(observedData);
            BinaryFormatter serializer = new BinaryFormatter();

            using (FileStream stream = new FileStream(modelFile.Substring(0, modelFile.LastIndexOf(".")) + ".bin", FileMode.Create)) {
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
        static double Infer(GraphicalModel model,Dictionary<string,Tuple<int[],double[]>> data, List<string> inferred){
            int dataCount = 0;
            Dictionary<string, Tuple<int[], double[]>> dataCopy = new Dictionary<string, Tuple<int[], double[]>>(data);
            foreach (var kv in data) {
                Tuple<int[], double[]> tup = null;
                if (kv.Value.Item1 != null){
                    dataCount = kv.Value.Item1.Length;
                }
                else {
                    dataCount = kv.Value.Item2.Length;
                }
                
            }
            double trueValue = data["roomsInLevel"].Item2[0];
            for (int ii = 0; ii < 1; ii++) {
                foreach (var varName in inferred) {
                    dataCopy.Remove(varName);
                }
                Dictionary<string, Tuple<int[], double[]>> singlePoint = new Dictionary<string, Tuple<int[], double[]>>();
                foreach (var kv in data) {
                    Tuple<int[], double[]> tup = null;
                    if (kv.Value.Item1 != null) {
                        tup = new Tuple<int[], double[]>(new int[] { kv.Value.Item1[ii] }, null);
                    }
                    else {
                        tup = new Tuple<int[], double[]>(null, new double[] { kv.Value.Item2[ii] });
                    }
                    singlePoint[kv.Key] = tup;
                }
                //model.LearnParameters(data);
                NonSharedGraphicalModel nsGM = new NonSharedGraphicalModel(model);
                nsGM.CreateNetwork();
                nsGM.LearnParameters(dataCopy);
                var output = nsGM.Engine.Infer<Gaussian[]>((nsGM.nodes["roomsInLevel"].distributions as NoParentNodeNumericalNonShared).ObservedNumerical);
                double mean = 0;
                foreach (var g in output) {
                    mean += g.GetMean() / ((double)output.Length);
                }
                return Math.Pow((trueValue - mean), 2.0);
               // Console.WriteLine(trueValue + " - " + mean);
          //      Console.WriteLine(nsGM.Engine.Infer((nsGM.nodes["puzzleRoomsInLevel"].distributions as LinearRegressionNonShared).x));
                /*%
                Console.WriteLine(nsGM.Engine.Infer((
                    Variable<double>.GaussianFromMeanAndPrecision((nsGM.nodes["roomsInLevel"].distributions as NoParentNodeNumericalNonShared).mean, (nsGM.nodes["roomsInLevel"].distributions as NoParentNodeNumericalNonShared).prec))));
                 * 
                 * */
            }
            return -1;
           // foreach (var varName in inferred) {
                /*
                var prediction = model.nodes[varName].distributions.getPredicted();
                if (model.nodes[varName].distributionType == DistributionType.Categorical) {
                    Console.WriteLine("Predicted Value is " + prediction.Item1);
                }
                else {
                    Console.WriteLine("Predicted Value is " + prediction.Item2);
                }
                 * */
         //   }
            
        }
		static void Main(string[] args) {
      //      RunAllLevels();
          // CreateGraphicalModelFiles();
            //GraphicalModel model = CreateGraphicalModel();
          //  InferTest.Test2();

            List<string> predicted = new List<string>();
            predicted.Add("roomsInLevel");
            predicted.Add("neighborTypes");
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
            string[] allSummaries = new string[]{
                "Summaries/TestSet/LA5.xml"  ,  "Summaries/TestSet/LoZ6.xml",
                "Summaries/TestSet/LoZ23.xml" , "Summaries/TestSet/LttP12.xml"
            };
            string downloadedFilenmae = "BayesNetwork.xml";
            string variantName = "TEST.xml"; //CHANGE THIS
          //  string downloadedFilenmae = "SuperSimple.xml";
       //     string variantName = "SS.xml"; //CHANGE THIS

            string evalData = "test2.xml";
            string dataFile = "testSmall.xml";
            Dictionary<string, List<double>> errors = new Dictionary<string, List<double>>();
            errors["Chosen"] = new List<double>();
            errors["ESV"] = new List<double>();
            errors["NaiveBayes"] = new List<double>();
            errors["SV"] = new List<double>();
            foreach (var summ in allSummaries) {
                string[] summaries = new string[] { summ };
                CreateGraphicalModelFiles(summaries, downloadedFilenmae, variantName, dataFile); //FILE CONVERSION

                //       string variantName = "RandomNetwork.xml"; //CHANGE THIS
                //  CreateGraphicalModelFiles(downloadedFilenmae, variantName); //FILE CONVERSION
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
                errors["Chosen"].Add(Infer(output.Item1, observedData, predicted));
                output = ModelNetworkSerialized("ESV.bin", "ESV.xml", evalData);
                errors["ESV"].Add(Infer(output.Item1, observedData, predicted));
          //      Test(output.Item1, observedData);
                output = ModelNetworkSerialized("NaiveBayes.bin", "NaiveBayes.xml", evalData);
                errors["NaiveBayes"].Add(Infer(output.Item1, observedData, predicted));
              //  Test(output.Item1, observedData); 
                output = ModelNetworkSerialized("SV.bin", "SV.xml", evalData);
                errors["SV"].Add(Infer(output.Item1, observedData, predicted));
              //  Test(output.Item1, observedData);
            }
            foreach (var v in errors) {
                double sse = 0;
                foreach (var e in v.Value) {
                    sse += e;
                }
                Console.WriteLine(v + " " + sse);
            }
            
               
  

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
