using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;


namespace ZeldaInfer {
    public class GraphicalModel {
        Dictionary<string, ModelNode> nodes;
        Dictionary<string, ModelNode> independentNodes;
        public Variable<int> NumberOfExamples;
        public InferenceEngine Engine = new InferenceEngine();
        public GraphicalModel() {
            nodes = new Dictionary<string, ModelNode>();
            independentNodes = new Dictionary<string, ModelNode>();
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
                /*
                switch (node.distributions.ParentCount()){
                    case 0:
                        Engine.Infer<Dirichlet>((node.distributions as NoParentNode).Probability);
                        break;
                    case 1:
                        Engine.Infer<Dirichlet[]>((node.distributions as OneParentNode).CPT);
                        break;
                    case 2:
                        Engine.Infer<Dirichlet[][]>((node.distributions as TwoParentNodes).CPT);
                        break;
                    case 3:
                        Engine.Infer<Dirichlet[,][]>((node.distributions as ThreeParentNodes).CPT);
                        break;
                    case 4:
                        Engine.Infer<Dirichlet[,,][]>((node.distributions as FourParentNodes).CPT);
                        break;
                    default:
                        throw new ArgumentException("Nodes can only have up to 4 parents");
                }    
                 */
            }
            foreach (ModelNode node in nodes.Values) {
                Console.WriteLine(node.distributions.PosteriorToString());
            }
        }
        /*
            int[] cloudy,
            int[] sprinkler,
            int[] rain,
            int[] wetgrass,
            Dirichlet probCloudyPrior,
            Dirichlet[] cptSprinklerPrior,
            Dirichlet[] cptRainPrior,
            Dirichlet[][] cptWetGrassPrior
            ) {
            NumberOfExamples.ObservedValue = cloudy.Length;
            Cloudy.ObservedValue = cloudy;
            Sprinkler.ObservedValue = sprinkler;
            Rain.ObservedValue = rain;
            WetGrass.ObservedValue = wetgrass;
            ProbCloudyPrior.ObservedValue = probCloudyPrior;
            CPTSprinklerPrior.ObservedValue = cptSprinklerPrior;
            CPTRainPrior.ObservedValue = cptRainPrior;
            CPTWetGrassPrior.ObservedValue = cptWetGrassPrior;
            // Inference
            ProbCloudyPosterior = Engine.Infer<Dirichlet>(ProbCloudy);
            CPTSprinklerPosterior = Engine.Infer<Dirichlet[]>(CPTSprinkler);
            CPTRainPosterior = Engine.Infer<Dirichlet[]>(CPTRain);
            CPTWetGrassPosterior = Engine.Infer<Dirichlet[][]>(CPTWetGrass);
        }
         * */
    }
}
