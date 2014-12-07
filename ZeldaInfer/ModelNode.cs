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
    [Serializable]
    public enum DistributionType {
        Categorical,
        Numerical
    }
    [Serializable]
    public class ModelNode {
        [NonSerialized]
        public Range states;
        public string name;
        public DistributionType distributionType;
        public List<ModelNode> parents;
        public List<ModelNode> children;
        public DistributionsNode distributions;
        public ModelNode(string name, Range states,string domain)
        {
            this.name = name;
            this.states = states;
            parents = new List<ModelNode>();
            children = new List<ModelNode>();
            distributionType = (DistributionType) Enum.Parse(typeof(DistributionType), domain);
        }
        public void AddParent(ModelNode parent){
            parents.Add(parent);
        }
        public void AddChild(ModelNode child)
        {
            children.Add(child);
        }
        public static void AddLink(ModelNode parent, ModelNode child)
        {
            parent.AddChild(child);
            child.AddParent(parent);
        }
         
    }
}
