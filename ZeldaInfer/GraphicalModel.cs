using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;


namespace ZeldaInfer {
    [Serializable]
    public class GraphicalModel {
        public Dictionary<string, ModelNode> nodes = new Dictionary<string, ModelNode>();
        public Dictionary<string, ModelNode> independentNodes = new Dictionary<string, ModelNode>();
        [NonSerialized]
        public Dictionary<string, Tuple<string[], Range>> rangeCategories = new Dictionary<string, Tuple<string[], Range>>();
        [NonSerialized]
        public Variable<int> NumberOfExamples;
        [NonSerialized]
        public Model sharedModel;
        [NonSerialized]
        public InferenceEngine Engine = new InferenceEngine(new VariationalMessagePassing());
        public GraphicalModel() {}
        public GraphicalModel(string filename, int numberOfChunks) {
            sharedModel = new Model(numberOfChunks);
            XDocument xdoc = XDocument.Load(filename);
            List<Tuple<string, string>> edges = new List<Tuple<string, string>>();
            List<Tuple<string, string, string>> nodeParams = new List<Tuple<string, string, string>>();
            foreach (var element in xdoc.Descendants()){
                switch (element.Name.ToString()) {
                    case "Category":
                        string[] categeories = element.Attribute("categories").Value.Split(new char[1]{','});
                        string name = element.Attribute("name").Value;
                        rangeCategories[name] = new Tuple<string[], Range>(categeories, new Range(categeories.Length).Named(name));
                        break;
                    case "Edge":
                        edges.Add(new Tuple<string, string>(element.Attribute("parent").Value, element.Attribute("child").Value));
                        break;
                    case "Node":
                        nodeParams.Add(new Tuple<string, string, string>(element.Attribute("name").Value, element.Attribute("category").Value, element.Attribute("domain").Value));
                        break;
                }
            }
            foreach (var node in nodeParams) {
                AddNode(new ModelNode(node.Item1, rangeCategories[node.Item2].Item2,node.Item3));
            }
            foreach (var edge in edges) {
                AddLink(nodes[edge.Item1], nodes[edge.Item2]);
            }
        }
        public void AddNode(ModelNode node) {
            nodes[node.name] = node;
            independentNodes[node.name] = node;
        }
        public void AddLink(ModelNode parent, ModelNode child) {
            if (independentNodes.ContainsKey(child.name)) {
                independentNodes.Remove(child.name);
            }
            ModelNode.AddLink(parent, child);
        }
        public void CreateNetwork() {
            List<ModelNode> completed = new List<ModelNode>();
            NumberOfExamples = Variable.New<int>().Named("NofE");
            Range N = new Range(NumberOfExamples).Named("N");
            foreach (ModelNode node in nodes.Values) {
                DistributionsNode.CreateDistributionsNode(node, N,sharedModel);
            }
            foreach (ModelNode node in independentNodes.Values) {
                node.distributions.AddParents();
                completed.Add(node);
            }
            while (completed.Count != nodes.Count) {
                foreach (ModelNode node in nodes.Values) {
                    if (!completed.Contains(node)) {
                        bool allParentsDone = true;
                        foreach (ModelNode parent in node.parents) {
                            if (!completed.Contains(parent)) {
                                allParentsDone = false;
                                break;
                            }
                        }
                        if (allParentsDone) {
                            node.distributions.AddParents();
                            completed.Add(node);
                        }
                    }
                }
            }
        }
        public static T[][] splitArray<T>(T[] array, int chunks) {
            int numElements = array.Length / chunks;
            if (numElements * chunks < array.Length) {
                numElements = array.Length / (chunks-1);
            }
            T[][] outArray = new T[chunks][];
            int currentSpot = 0;
            int nextSpot = numElements;
            for (int ii = 0; ii < chunks; ii++) {
                int diff = nextSpot - currentSpot;
                outArray[ii] = new T[nextSpot - currentSpot];
                Array.Copy(array, currentSpot, outArray[ii], 0, nextSpot - currentSpot);//array.TakeWhile((___, index) => ((index >= currentSpot) && (index < nextSpot))).ToArray();
                currentSpot = nextSpot;
                nextSpot = Math.Min(array.Length, currentSpot + numElements);
            }

            return outArray;
        }
        public void LearnParameters(Dictionary<string, Tuple<int[], double[]>> observedData) {
           // int numberOfEntries = 0;
            Dictionary<string, Tuple<int[][], double[][]>> chunkedData = new Dictionary<string, Tuple<int[][], double[][]>>();
            foreach (KeyValuePair<string,ModelNode> kvPair in nodes){                
                if (kvPair.Value.distributionType == DistributionType.Categorical) {
                    chunkedData[kvPair.Key] = new Tuple<int[][],double[][]>(splitArray<int>(observedData[kvPair.Key].Item1, sharedModel.BatchCount),null);
                  //   numberOfEntries = observedData[kvPair.Key].Item1.Length;
                }
                else if (kvPair.Value.distributionType == DistributionType.Numerical) {
                    chunkedData[kvPair.Key] = new Tuple<int[][], double[][]>(null, splitArray<double>(observedData[kvPair.Key].Item2, sharedModel.BatchCount));
                 //   numberOfEntries = observedData[kvPair.Key].Item2.Length;
                }
                
             //   kvPair.Value.distributions.SetObservedData(observedData[kvPair.Key]);
            }
          //  NumberOfExamples.ObservedValue = numberOfEntries;
            double count = 0;
            double total = nodes.Count;
            /*
            foreach (ModelNode node in nodes.Values) {
                //node.distributions.Infer(Engine);
                count += 1;
                Console.WriteLine(count / total);
            }
            */
            for (int ii = 0; ii < sharedModel.BatchCount; ii++) {
                Console.WriteLine((ii + 1) + "/" + sharedModel.BatchCount + " : " + ((double)(ii + 1)) / ((double)sharedModel.BatchCount));
                int numberOfEntries = 0;
                foreach (KeyValuePair<string, ModelNode> kvPair in nodes) {
                    if (kvPair.Value.distributionType == DistributionType.Categorical) {
                        NumberOfExamples.ObservedValue = chunkedData[kvPair.Key].Item1[ii].Length;
                        kvPair.Value.distributions.SetObservedData(new Tuple<int[], double[]>(chunkedData[kvPair.Key].Item1[ii], null));
                        //   numberOfEntries = observedData[kvPair.Key].Item1.Length;
                    }
                    else if (kvPair.Value.distributionType == DistributionType.Numerical) {
                        NumberOfExamples.ObservedValue = chunkedData[kvPair.Key].Item2[ii].Length;
                        kvPair.Value.distributions.SetObservedData(new Tuple<int[], double[]>(null,chunkedData[kvPair.Key].Item2[ii]));
                        //   numberOfEntries = observedData[kvPair.Key].Item2.Length;
                    }

                   // kvPair.Value.distributions.SetObservedData(observedData[kvPair.Key]);
                }
                sharedModel.InferShared(Engine, ii);

            }
            foreach (ModelNode node in nodes.Values) {
                node.distributions.SetPriorToPosterior();
            }
            foreach (ModelNode node in nodes.Values) {
                Console.WriteLine(node.distributions.PosteriorToString());
            }
        }
        public static Dictionary<string, Tuple<int[],double[]>> LoadData(string filename) {
            Dictionary<string, Tuple<int[], double[]>> data = new Dictionary<string, Tuple<int[], double[]>>();
            XDocument xdoc = XDocument.Load(filename);
            foreach (XElement element in xdoc.Root.Elements()) {
                string[] stringData = element.Value.Split(',');
                if (element.Attribute("domain").Value == "Numerical") {
                    data[element.Attribute("name").Value] = new Tuple<int[], double[]>(null,Array.ConvertAll(stringData, double.Parse));
                }
                else if (element.Attribute("domain").Value == "Categorical") {
                    data[element.Attribute("name").Value] = new Tuple<int[],double[]>(Array.ConvertAll(stringData, int.Parse),null);
                }
                else {
                    throw new ArgumentException("domain must be either 'Numerical' or 'Categorical'");
                }
            }
            return data;
        }
        public void LoadAfterSerialization(string filename) {
            XDocument xdoc = XDocument.Load(filename);
            List<Tuple<string, string>> edges = new List<Tuple<string, string>>();
            List<Tuple<string, string, string>> nodeParams = new List<Tuple<string, string, string>>();

            Engine = new InferenceEngine(new VariationalMessagePassing());
       //     Engine.Compiler.UseParallelForLoops = true;
            rangeCategories = new Dictionary<string, Tuple<string[], Range>>();
            foreach (var element in xdoc.Descendants()) {
                switch (element.Name.ToString()) {
                    case "Category":
                        string[] categeories = element.Attribute("categories").Value.Split(new char[1] { ',' });
                        string name = element.Attribute("name").Value;
                        rangeCategories[name] = new Tuple<string[], Range>(categeories, new Range(categeories.Length).Named(name));
                        break;
                    case "Edge":
                        edges.Add(new Tuple<string, string>(element.Attribute("parent").Value, element.Attribute("child").Value));
                        break;
                    case "Node":
                        nodeParams.Add(new Tuple<string, string, string>(element.Attribute("name").Value, element.Attribute("category").Value, element.Attribute("domain").Value));
                        break;
                }
            }

            NumberOfExamples = Variable.New<int>().Named("NofE");
            Range N = new Range(NumberOfExamples).Named("N");

            foreach (var node in nodeParams) {

                nodes[node.Item1].states = rangeCategories[node.Item2].Item2;
                nodes[node.Item1].distributions.LoadAfterSerialization(N);
            }
            List<ModelNode> completed = new List<ModelNode>();
            foreach (ModelNode node in independentNodes.Values) {
                node.distributions.AddParents();
                completed.Add(node);
            }
            while (completed.Count != nodes.Count) {
                foreach (ModelNode node in nodes.Values) {
                    if (!completed.Contains(node)) {
                        bool allParentsDone = true;
                        foreach (ModelNode parent in node.parents) {
                            if (!completed.Contains(parent)) {
                                allParentsDone = false;
                                break;
                            }
                        }
                        if (allParentsDone) {
                            node.distributions.AddParents();
                            completed.Add(node);
                        }
                    }
                }
            }
        }
    }
}
