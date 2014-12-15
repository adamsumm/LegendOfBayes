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
    public class NonSharedGraphicalModel {
        public Dictionary<string, ModelNodeNonShared> nodes = new Dictionary<string, ModelNodeNonShared>();
        public Dictionary<string, ModelNodeNonShared> independentNodes = new Dictionary<string, ModelNodeNonShared>();
        [NonSerialized]
        public Dictionary<string, Tuple<string[], Range>> rangeCategories = new Dictionary<string, Tuple<string[], Range>>();
        [NonSerialized]
        public Variable<int> NumberOfExamples;
        [NonSerialized]
        public Model sharedModel;
        [NonSerialized]
        public InferenceEngine Engine = new InferenceEngine(new VariationalMessagePassing());
        public NonSharedGraphicalModel() { }
        public NonSharedGraphicalModel(GraphicalModel original) {
            foreach (var node in original.nodes) {
                nodes[node.Key] = new ModelNodeNonShared(node.Value);
            }
            foreach (var node in original.independentNodes) {
                independentNodes[node.Key] = nodes[node.Key];
            }

            foreach (var node in original.nodes) {
                foreach (var parent in node.Value.parents) {
                    AddLink(nodes[parent.name], nodes[node.Key]);
                }
            }

        }
        public void AddNode(ModelNodeNonShared node) {
            nodes[node.name] = node;
            independentNodes[node.name] = node;
        }
        public void AddLink(ModelNodeNonShared parent, ModelNodeNonShared child) {
            if (independentNodes.ContainsKey(child.name)) {
                independentNodes.Remove(child.name);
            }
            ModelNodeNonShared.AddLink(parent, child);
        }
        public void CreateNetwork() {
            List<ModelNodeNonShared> completed = new List<ModelNodeNonShared>();
            NumberOfExamples = Variable.New<int>().Named("NofE");
            Range N = new Range(NumberOfExamples).Named("N");
            foreach (ModelNodeNonShared node in nodes.Values) {
                DistributionsNodeNonShared.CreateDistributionsNode(node, N);
            }
            foreach (ModelNodeNonShared node in independentNodes.Values) {
                node.distributions.AddParents();
                completed.Add(node);
            }
            while (completed.Count != nodes.Count) {
                foreach (ModelNodeNonShared node in nodes.Values) {
                    if (!completed.Contains(node)) {
                        bool allParentsDone = true;
                        foreach (ModelNodeNonShared parent in node.parents) {
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
                numElements = array.Length / (chunks - 1);
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
            int numberOfEntries = 0;
            HashSet<string> alreadyObserved = new HashSet<string>();
            List<KeyValuePair<string, ModelNodeNonShared>> toBeAdded = new List<KeyValuePair<string, ModelNodeNonShared>>();
            foreach (KeyValuePair<string, ModelNodeNonShared> kvPair in nodes) {
                if (observedData.ContainsKey(kvPair.Key)) {
                    if (kvPair.Value.distributionType == DistributionType.Categorical) {
                        // kvPair.Value.distributions.Observed.ObservedValue = observedData[kvPair.Key].Item1;
                        numberOfEntries = observedData[kvPair.Key].Item1.Length;
                    }
                    else if (kvPair.Value.distributionType == DistributionType.Numerical) {
                        //  kvPair.Value.distributions.ObservedNumerical.ObservedValue = observedData[kvPair.Key].Item2;
                        numberOfEntries = observedData[kvPair.Key].Item2.Length;
                    }
                }
            }
            NumberOfExamples.ObservedValue = numberOfEntries;
            foreach (KeyValuePair<string, ModelNodeNonShared> kvPair in nodes) {
                if (observedData.ContainsKey(kvPair.Key)) {
                    if (kvPair.Value.distributionType == DistributionType.Categorical) {
                        // kvPair.Value.distributions.Observed.ObservedValue = observedData[kvPair.Key].Item1;
                        numberOfEntries = observedData[kvPair.Key].Item1.Length;
                    }
                    else if (kvPair.Value.distributionType == DistributionType.Numerical) {
                        //  kvPair.Value.distributions.ObservedNumerical.ObservedValue = observedData[kvPair.Key].Item2;
                        numberOfEntries = observedData[kvPair.Key].Item2.Length;
                    }
                    bool quit = false;
                    foreach (var parent in kvPair.Value.parents) {
                        if (parent.name == "roomsOnPath") {
                            bool stophere = true;
                        }
                        if (parent.distributions.GetType() == typeof(LinearRegressionNonShared)) {
                            if (!alreadyObserved.Contains(parent.name)) {
                                toBeAdded.Add(kvPair);
                                quit = true;
                                break;
                            }
                        }

                    }
                    if (quit) {
                        continue;
                    }
                    alreadyObserved.Add(kvPair.Key);
                    kvPair.Value.distributions.SetObservedData(observedData[kvPair.Key]);
                }
            }
            int ii = 0;
            while (toBeAdded.Count > 0) {
                if (alreadyObserved.Contains(toBeAdded[ii].Key)){
                    toBeAdded.RemoveAt(ii);
                    continue;
                }
                bool quit = false;
                foreach (var parent in toBeAdded[ii].Value.parents) {
                    if (parent.distributions.GetType() == typeof(LinearRegressionNonShared)) {
                        if (!alreadyObserved.Contains(parent.name)) {
                            ii++;
                            quit = true;
                            ii = ii % toBeAdded.Count;
                            continue;
                        }
                    }
                }
                if (quit) {
                    continue;
                }
                alreadyObserved.Add(toBeAdded[ii].Key);
                toBeAdded[ii].Value.distributions.SetObservedData(observedData[toBeAdded[ii].Key]);
                toBeAdded.RemoveAt(ii);
                ii++;
                if (toBeAdded.Count == 0) {
                    break;
                }
                ii = ii % toBeAdded.Count;
            }
            foreach (ModelNodeNonShared node in nodes.Values) {
      //          node.distributions.Infer(Engine);
            }
            foreach (ModelNodeNonShared node in nodes.Values) {
      //          Console.WriteLine(node.distributions.PosteriorToString());
            }
        }
        public static Dictionary<string, Tuple<int[], double[]>> LoadData(string filename) {
            Dictionary<string, Tuple<int[], double[]>> data = new Dictionary<string, Tuple<int[], double[]>>();
            XDocument xdoc = XDocument.Load(filename);
            foreach (XElement element in xdoc.Root.Elements()) {
                string[] stringData = element.Value.Split(',');
                if (element.Attribute("domain").Value == "Numerical") {
                    data[element.Attribute("name").Value] = new Tuple<int[], double[]>(null, Array.ConvertAll(stringData, double.Parse));
                }
                else if (element.Attribute("domain").Value == "Categorical") {
                    data[element.Attribute("name").Value] = new Tuple<int[], double[]>(Array.ConvertAll(stringData, int.Parse), null);
                }
                else {
                    throw new ArgumentException("domain must be either 'Numerical' or 'Categorical'");
                }
            }
            return data;
        }
        public void LoadAfterSerialization(string filename, int batches) {
            XDocument xdoc = XDocument.Load(filename);
            List<Tuple<string, string>> edges = new List<Tuple<string, string>>();
            List<Tuple<string, string, string>> nodeParams = new List<Tuple<string, string, string>>();
            sharedModel = new Model(batches);
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
                nodes[node.Item1].distributionType = (DistributionType)Enum.Parse(typeof(DistributionType), node.Item3);
                nodes[node.Item1].states = rangeCategories[node.Item2].Item2;
            }

            foreach (var node in nodeParams) {
             //   nodes[node.Item1].distributions.LoadAfterSerialization(N, sharedModel);
            }
            List<ModelNodeNonShared> completed = new List<ModelNodeNonShared>();
            foreach (ModelNodeNonShared node in independentNodes.Values) {
                node.distributions.AddParents();
                completed.Add(node);
            }
            while (completed.Count != nodes.Count) {
                foreach (ModelNodeNonShared node in nodes.Values) {
                    if (!completed.Contains(node)) {
                        bool allParentsDone = true;
                        foreach (ModelNodeNonShared parent in node.parents) {
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
