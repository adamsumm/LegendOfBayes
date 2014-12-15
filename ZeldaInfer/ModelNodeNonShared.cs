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
    public class ModelNodeNonShared {
        [NonSerialized]
        public Range states;
        public string name;
        public DistributionType distributionType;
        public List<ModelNodeNonShared> parents;
        public List<ModelNodeNonShared> children;
        public DistributionsNodeNonShared distributions;
        public ModelNode original;
        public ModelNodeNonShared(ModelNode copy)
        {
            original = copy;
            this.name = copy.name;
            this.states = copy.states;
            parents = new List<ModelNodeNonShared>();
            children = new List<ModelNodeNonShared>();
            distributionType = copy.distributionType;
        }
        public void AddParent(ModelNodeNonShared parent) {
            parents.Add(parent);
        }
        public void AddChild(ModelNodeNonShared child)
        {
            children.Add(child);
        }
        public static void AddLink(ModelNodeNonShared parent, ModelNodeNonShared child)
        {
            parent.AddChild(child);
            child.AddParent(parent);
        }
         
    }
}