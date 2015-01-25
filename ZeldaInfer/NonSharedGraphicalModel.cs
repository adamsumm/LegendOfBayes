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
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Graphs;
using MicrosoftResearch.Infer.Utils;
using MicrosoftResearch.Infer.Views;

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
        [NonSerialized]
        public Range N;
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
            NumberOfExamples = Variable.New<int>().Named("NofE");
            N = new Range(NumberOfExamples).Named("N");
          //  CreateNetwork();
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
        public void CreateNetwork(int numberOfEntries) {
            List<ModelNodeNonShared> completed = new List<ModelNodeNonShared>();
            foreach (ModelNodeNonShared node in nodes.Values) {
                DistributionsNodeNonShared.CreateDistributionsNode(node, N);
            }
            foreach (ModelNodeNonShared node in independentNodes.Values) {
                node.distributions.AddParents(numberOfEntries);
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
                            node.distributions.AddParents(numberOfEntries);
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
                Array.Copy(array, currentSpot, outArray[ii], 0, nextSpot - currentSpot);
                currentSpot = nextSpot;
                nextSpot = Math.Min(array.Length, currentSpot + numElements);
            }

            return outArray;
        }
        public void LearnParameters(Dictionary<string, Tuple<int[], double[]>> observedData) {
            int numberOfEntries = 1;
            HashSet<string> alreadyObserved = new HashSet<string>();
            List<KeyValuePair<string, ModelNodeNonShared>> toBeAdded = new List<KeyValuePair<string, ModelNodeNonShared>>();
            foreach (KeyValuePair<string, ModelNodeNonShared> kvPair in nodes) {
                if (observedData.ContainsKey(kvPair.Key)) {
                    if (kvPair.Value.distributionType == DistributionType.Categorical) {
                        numberOfEntries = observedData[kvPair.Key].Item1.Length;
                        break;
                    }
                    else if (kvPair.Value.distributionType == DistributionType.Numerical) {
                        numberOfEntries = observedData[kvPair.Key].Item2.Length;
                        break;
                    }
                }
            }
            NumberOfExamples.ObservedValue = numberOfEntries;
            CreateNetwork(numberOfEntries);
            foreach (KeyValuePair<string, ModelNodeNonShared> kvPair in nodes) {
                if (observedData.ContainsKey(kvPair.Key)) {
                    if (kvPair.Value.distributionType == DistributionType.Categorical) {
                        numberOfEntries = observedData[kvPair.Key].Item1.Length;
                    }
                    else if (kvPair.Value.distributionType == DistributionType.Numerical) {
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
            HashSet<string> toBeAddedKeys = new HashSet<string>();
            foreach (var tb in toBeAdded){
                toBeAddedKeys.Add(tb.Key);
            }
            while (toBeAdded.Count > 0) {
                if (alreadyObserved.Contains(toBeAdded[ii].Key)){
                    toBeAdded.RemoveAt(ii);
                    continue;
                }
                bool quit = false;
                foreach (var parent in toBeAdded[ii].Value.parents) {
                    if (parent.distributions.GetType() == typeof(LinearRegressionNonShared)) {
                        if (!alreadyObserved.Contains(parent.name) && toBeAddedKeys.Contains(parent.name)) {
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

        public Dictionary<string, Tuple<DistributionType,Object>> Infer(Dictionary<string, Tuple<int[], double[]>> data, List<string> inferred) {
            int dataCount = 0;
            Dictionary<string, Tuple<DistributionType, Object>> inferredDistributions = new Dictionary<string, Tuple<DistributionType, Object>>();
            Dictionary<string, Tuple<int[], double[]>> dataCopy = new Dictionary<string, Tuple<int[], double[]>>(data);
            foreach (var kv in data) {
                if (kv.Value.Item1 != null) {
                    dataCount = kv.Value.Item1.Length;
                    break;
                }
                else {
                    dataCount = kv.Value.Item2.Length;
                    break;
                }
            }

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
                Engine.Compiler.GivePriorityTo(typeof(GaussianProductOp_SHG09));
                LearnParameters(dataCopy);
                foreach (var inferredVar in inferred) {
                    ModelNodeNonShared node = nodes[inferredVar];
                    if (node.distributionType == DistributionType.Categorical) {
                        var output = Engine.Infer<Discrete[]>((nodes[inferredVar].distributions).Observed);

                        inferredDistributions[inferredVar] = new Tuple<DistributionType, Object>(DistributionType.Categorical,Engine.Infer<Discrete[]>((nodes[inferredVar].distributions).Observed)[0]);
                    }
                    else {
                        inferredDistributions[inferredVar] = new Tuple<DistributionType, Object>(DistributionType.Numerical, Engine.Infer<Gaussian[]>((nodes[inferredVar].distributions).ObservedNumerical)[0]);
               
                    }
                }
            }
            return inferredDistributions;
        }
    }
}
