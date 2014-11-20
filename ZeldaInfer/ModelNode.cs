using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;


namespace ZeldaInfer
{
    public class ModelNode
    {   
        public Range states;
        public string name;
        public List<ModelNode> parents;
        public List<ModelNode> children;
        public DistributionsNode distributions;
        public ModelNode(string name, Range states)
        {
            this.name = name;
            this.states = states;
            parents = new List<ModelNode>();
            children = new List<ModelNode>();
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
