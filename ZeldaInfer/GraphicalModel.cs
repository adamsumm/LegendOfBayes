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
    public class GraphicalModel {
        Dictionary<string, ModelNode> nodes = new Dictionary<string,ModelNode>();
        Dictionary<string, ModelNode> independentNodes = new Dictionary<string,ModelNode>();
        Dictionary<string, Tuple<string[], Range>> rangeCategories = new Dictionary<string,Tuple<string[],Range>>();
        public Variable<int> NumberOfExamples;
        public InferenceEngine Engine = new InferenceEngine();
        public GraphicalModel() {}
        public GraphicalModel(string filename) {
            XDocument xdoc = XDocument.Load(filename);
            List<Tuple<string, string>> edges = new List<Tuple<string, string>>();
            List<Tuple<string, string>> nodeParams = new List<Tuple<string, string>>();
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
                        nodeParams.Add(new Tuple<string, string>(element.Attribute("name").Value, element.Attribute("category").Value));
                        break;
                }
            }
            foreach (var node in nodeParams) {
                AddNode(new ModelNode(node.Item1, rangeCategories[node.Item2].Item2));
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
                DistributionsNode.CreateDistributionsNode(node, N);
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
        public void LearnParameters(Dictionary<string,int[]> observedData){
            int numberOfEntries = 0;
            foreach (KeyValuePair<string,ModelNode> kvPair in nodes){
                kvPair.Value.distributions.Observed.ObservedValue = observedData[kvPair.Key];
                numberOfEntries = observedData[kvPair.Key].Length;
            }
            NumberOfExamples.ObservedValue = numberOfEntries;
            foreach (ModelNode node in nodes.Values) {
                node.distributions.Infer(Engine);
            }
            foreach (ModelNode node in nodes.Values) {
                Console.WriteLine(node.distributions.PosteriorToString());
            }
        }
        public static Dictionary<string, int[]> LoadData(string filename) {
            Dictionary<string, int[]> data = new Dictionary<string, int[]>();
            XDocument xdoc = XDocument.Load(filename);
            foreach (XElement element in xdoc.Root.Elements()) {
                string[] stringData = element.Value.Split(',');
                data[element.Attribute("name").Value] = Array.ConvertAll(stringData, int.Parse);
            }
            return data;
        }
    }
}
