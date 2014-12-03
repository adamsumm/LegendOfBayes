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
    public abstract class DistributionsNode {
        public virtual int ParentCount() { return -1; }
        public ModelNode node;
        public VariableArray<int> Observed;
        public Range N;
        public virtual void AddParents() { }
        public virtual string PosteriorToString() { return ""; }
        public abstract void Infer(InferenceEngine engine);
        public static void CreateDistributionsNode(ModelNode node, Range N) {

            if (node.parents.Count == 0) {
                node.distributions = new NoParentNode(node);
            }
            else if (node.parents.Count == 1) {
                node.distributions = new OneParentNode(node);
            }
            else if (node.parents.Count == 2) {
                node.distributions = new TwoParentNodes(node);
            }
            else if (node.parents.Count == 3) {
                node.distributions = new ThreeParentNodes(node);
            }
            else if (node.parents.Count == 4) {
                node.distributions = new FourParentNodes(node);
            }
            else {
                //OH SHIT
                throw new ArgumentException("Can't have more than 4 parents");
            }
            node.distributions.N = N;
        }
        /// <summary>
        /// Helper method to add a child from one parent
        /// </summary>
        /// <param name="parent">Parent (a variable array over a range of examples)</param>
        /// <param name="cpt">Conditional probability table</param>
        /// <returns></returns>
        public static VariableArray<int> AddChildFromOneParent(
            VariableArray<int> parent,
            VariableArray<Vector> cpt) {
            var n = parent.Range;
            var child = Variable.Array<int>(n);
          
            using (Variable.ForEach(n))
            using (Variable.Switch(parent[n]))
                child[n] = Variable.Discrete(cpt[parent[n]]);
            return child;
        }

        /// <summary>
        /// Helper method to add a child from two parents
        /// </summary>
        /// <param name="parent1">First parent (a variable array over a range of examples)</param>
        /// <param name="parent2">Second parent (a variable array over the same range)</param>
        /// <param name="cpt">Conditional probability table</param>
        /// <returns></returns>
        public static VariableArray<int> AddChildFromTwoParents(
            VariableArray<int> parent1,
            VariableArray<int> parent2,
            VariableArray<VariableArray<Vector>, Vector[][]> cpt) {
            var n = parent1.Range;
            var child = Variable.Array<int>(n);
            using (Variable.ForEach(n))
            using (Variable.Switch(parent1[n]))
            using (Variable.Switch(parent2[n]))
                child[n] = Variable.Discrete(cpt[parent2[n]][parent1[n]]);
            return child;
        }
        /// <summary>
        /// Helper method to add a child from two parents
        /// </summary>
        /// <param name="parent1">First parent (a variable array over a range of examples)</param>
        /// <param name="parent2">Second parent (a variable array over the same range)</param>
        /// <param name="cpt">Conditional probability table</param>
        /// <returns></returns>
        public static VariableArray<int> AddChildFromThreeParents(
            VariableArray<int> parent1,
            VariableArray<int> parent2,
            VariableArray<int> parent3,
            VariableArray2D<VariableArray<Vector>, Vector[,][]> cpt) {
            var n = parent1.Range;
            var child = Variable.Array<int>(n);
            using (Variable.ForEach(n))
            using (Variable.Switch(parent1[n]))
            using (Variable.Switch(parent2[n]))
            using (Variable.Switch(parent3[n]))
                child[n] = Variable.Discrete(cpt[parent2[n], parent3[n]][parent1[n]]);
            return child;
        }
        /// <summary>
        /// Helper method to add a child from two parents
        /// </summary>
        /// <param name="parent1">First parent (a variable array over a range of examples)</param>
        /// <param name="parent2">Second parent (a variable array over the same range)</param>
        /// <param name="cpt">Conditional probability table</param>
        /// <returns></returns>
        public static VariableArray<int> AddChildFromFourParents(
            VariableArray<int> parent1,
            VariableArray<int> parent2,
            VariableArray<int> parent3,
            VariableArray<int> parent4,
            VariableArray3D<VariableArray<Vector>, Vector[, ,][]> cpt) {
            var n = parent1.Range;
            var child = Variable.Array<int>(n);
            using (Variable.ForEach(n))
            using (Variable.Switch(parent1[n]))
            using (Variable.Switch(parent2[n]))
            using (Variable.Switch(parent3[n]))
                child[n] = Variable.Discrete(cpt[parent2[n], parent3[n], parent4[n]][parent1[n]]);
            return child;
        }
    }

    public class NoParentNode : DistributionsNode {
        public Variable<Vector> Probability;
        public Variable<Dirichlet> ProbPrior;
        public Dirichlet ProbPosterior;
        override public int ParentCount() {
            return 0;
        }
        public NoParentNode(ModelNode node) {
            this.node = node;
            ProbPrior = Variable.New<Dirichlet>().Named("Prob" + node.name + "Prior");
            ProbPrior.ObservedValue = Dirichlet.Uniform(node.states.SizeAsInt);
            Probability = Variable<Vector>.Random(ProbPrior).Named("Prob" + node.name);
            Probability.SetValueRange(node.states);
        }
        public override void AddParents() {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed[N] = Variable.Discrete(Probability).ForEach(N);
        }
        public override void Infer(InferenceEngine engine) {
            ProbPosterior = engine.Infer<Dirichlet>(Probability);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                str += node.name + " P(" + ii + ") = " + ProbPosterior.GetMean()[ii] + "\n";
            }
            return str;
        }

    }
    public class OneParentNode : DistributionsNode {
        public VariableArray<Vector> CPT;
        public VariableArray<Dirichlet> CPTPrior;
        public Dirichlet[] CPTPosterior;
        public OneParentNode(ModelNode node) {
            this.node = node;
            Range parentStates = node.parents[0].states;
            CPTPrior = Variable.Array<Dirichlet>(parentStates).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parentStates.SizeAsInt).ToArray();
            CPT = Variable.Array<Vector>(parentStates).Named("Prob" + node.name);
            CPT[parentStates] = Variable<Vector>.Random(CPTPrior[parentStates]);
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[]>(CPT);
        }

        public override void AddParents() {
            Observed = DistributionsNode.AddChildFromOneParent(node.parents[0].distributions.Observed, CPT).Named(node.name);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                    str += node.name + " P(" + ii + "|" + p1 + ") = " + CPTPosterior[p1].GetMean()[ii] + "\n";
                }
            }
            return str;
        }
    }
    public class TwoParentNodes : DistributionsNode {
        public VariableArray<VariableArray<Vector>, Vector[][]> CPT;
        public VariableArray<VariableArray<Dirichlet>, Dirichlet[][]> CPTPrior;
        public Dirichlet[][] CPTPosterior;
        public TwoParentNodes(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parent1States.SizeAsInt).ToArray(), parent2States.SizeAsInt).ToArray(); ;
            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States).Named("Prob" + node.name);
            CPT[parent2States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States][parent1States]);
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 2;
        }
        public override void AddParents() {
            Observed = DistributionsNode.AddChildFromTwoParents(node.parents[0].distributions.Observed,
                node.parents[1].distributions.Observed, CPT).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[][]>(CPT);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int p2 = 0; p2 < node.parents[1].states.SizeAsInt; p2++) {
                    for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                        str += node.name + " P(" + ii + "|" + p2 + "," + p1 + ") = " + CPTPosterior[p2][p1].GetMean()[ii] + "\n";
                    }
                }
            }
            return str;
        }
    }
    public class ThreeParentNodes : DistributionsNode {
        public VariableArray2D<VariableArray<Vector>, Vector[,][]> CPT;
        public VariableArray2D<VariableArray<Dirichlet>, Dirichlet[,][]> CPTPrior;
        public Dirichlet[,][] CPTPosterior;
        public ThreeParentNodes(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            Range parent3States = node.parents[2].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States, parent3States).Named("Prob" + node.name + "Prior");
            Dirichlet[,][] priorObserved = new Dirichlet[parent2States.SizeAsInt, parent3States.SizeAsInt][];
            for (int p2 = 0; p2 < parent2States.SizeAsInt; p2++) {
                for (int p3 = 0; p3 < parent3States.SizeAsInt; p3++) {
                    priorObserved[p2, p3] = Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parent1States.SizeAsInt).ToArray();
                }
            }
            CPTPrior.ObservedValue = priorObserved;
            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States, parent3States).Named("Prob" + node.name);
            CPT[parent2States, parent3States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States, parent3States][parent1States]);
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 3;
        }
        public override void AddParents() {
            Observed = DistributionsNode.AddChildFromThreeParents(node.parents[0].distributions.Observed,
                node.parents[1].distributions.Observed, node.parents[2].distributions.Observed, CPT).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[,][]>(CPT);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int p2 = 0; p2 < node.parents[1].states.SizeAsInt; p2++) {
                    for (int p3 = 0; p3 < node.parents[2].states.SizeAsInt; p3++) {
                        for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                            str += node.name + " P(" + ii + "|" + p2 + "," + p3 + "," + p1 + ") = " + CPTPosterior[p2, p3][p1].GetMean()[ii] + "\n";
                        }
                    }
                }
            }
            return str;
        }
    }
    public class FourParentNodes : DistributionsNode {
        public VariableArray3D<VariableArray<Vector>, Vector[, ,][]> CPT;
        public VariableArray3D<VariableArray<Dirichlet>, Dirichlet[, ,][]> CPTPrior;
        public Dirichlet[, ,][] CPTPosterior;
        public FourParentNodes(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            Range parent3States = node.parents[2].states;
            Range parent4States = node.parents[3].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States, parent3States, parent4States).Named("Prob" + node.name + "Prior");
            Dirichlet[, ,][] priorObserved = new Dirichlet[parent2States.SizeAsInt, parent3States.SizeAsInt, parent4States.SizeAsInt][];
            for (int p2 = 0; p2 < parent2States.SizeAsInt; p2++) {
                for (int p3 = 0; p3 < parent3States.SizeAsInt; p3++) {
                    for (int p4 = 0; p4 < parent3States.SizeAsInt; p4++) {
                        priorObserved[p2, p3, p4] = Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parent1States.SizeAsInt).ToArray();
                    }
                }
            }
            CPTPrior.ObservedValue = priorObserved;

            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States, parent3States, parent4States).Named("Prob" + node.name);
            CPT[parent2States, parent3States, parent4States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States, parent3States, parent4States][parent1States]);
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 4;
        }
        public override void AddParents() {
            Observed = DistributionsNode.AddChildFromFourParents(node.parents[0].distributions.Observed,
                node.parents[1].distributions.Observed, node.parents[2].distributions.Observed,
                node.parents[3].distributions.Observed, CPT).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[,,][]>(CPT);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int p2 = 0; p2 < node.parents[1].states.SizeAsInt; p2++) {
                    for (int p3 = 0; p3 < node.parents[2].states.SizeAsInt; p3++) {
                        for (int p4 = 0; p4 < node.parents[3].states.SizeAsInt; p4++) {
                            for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                                str += node.name + " P(" + ii + "|" + p2 + "," + p3 + "," + p4 + "," + p1 + ") = " + CPTPosterior[p2, p3, p4][p1].GetMean()[ii] + "\n";
                            }
                        }
                    }
                }
            }
            return str;
        }
    }
}
