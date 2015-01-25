
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;

namespace ZeldaInfer {
    public abstract class DistributionsNodeNonShared {
        public virtual int ParentCount() { return -1; }
        public ModelNodeNonShared node;
        public VariableArray<int> Observed;
        public VariableArray<double> ObservedNumerical;
        public Range N;
        public virtual void AddParents(int numberOfEntries) { }
        public virtual string PosteriorToString() { return ""; }
        public abstract void Infer(InferenceEngine engine);
        public abstract void SetPriorToPosterior();
        public abstract void SetObservedData(Tuple<int[],double[]> observedValues);
        public static void CreateDistributionsNode(ModelNodeNonShared node, Range N) {
            if (node.distributionType == DistributionType.Categorical) {
                if (node.parents.Count == 0) {
                    node.distributions = new NoParentNodeCategoricalNonShared(node);
                }
                else {
                    int numCategorical = 0;
                    foreach (var parent in node.parents) {
                        numCategorical += parent.distributionType == DistributionType.Categorical ? 1 : 0;
                    }
                    if (numCategorical == node.parents.Count) {
                        if (node.parents.Count == 1) {
                            node.distributions = new OneCategoricalParentNodeCategoricalNonShared(node);
                        }
                        else if (node.parents.Count == 2) {
                         //   node.distributions = new TwoCategoricalParentNodeCategorical(node);
                        }
                        else if (node.parents.Count == 3) {
                            throw new ArgumentException("Only up to 2 Categorical Parents Allowed");
                           // node.distributions = new ThreeCategoricalParentNodeCategorical(node);
                        }
                    }
                    else {
                        if (numCategorical == 0) {
                            node.distributions = new SoftmaxCategoricalNonShared(node);
                        }
                        else {
                            node.distributions = new SoftmaxCategoricalOneParentNonShared(node);
                        }
                    }
                }
            }
            else {
                if (node.parents.Count == 0) {
                    node.distributions = new NoParentNodeNumericalNonShared(node);
                }
                else {
                    int numCategorical = 0;
                    foreach (var parent in node.parents) {
                        numCategorical += parent.distributionType == DistributionType.Categorical ? 1 : 0;
                    }
                    if (numCategorical == node.parents.Count) {
                        if (node.parents.Count == 1) {
                            node.distributions = new OneCategoricalParentNodeNumericalNonShared(node);
                        }
                        else if (node.parents.Count == 2) {
                            throw new ArgumentException("Only up to 2 Categorical Parents Allowed");
                        }
                        else if (node.parents.Count == 3) {
                            throw new ArgumentException("Only up to 2 Categorical Parents Allowed");
                        }
                    }
                    else if (numCategorical == 0) {
                        node.distributions = new LinearRegressionNonShared(node);

                    }
                    else if (numCategorical == 1) {
                        node.distributions = new LinearRegressionOneParentNonShared(node);
                    }
                    else {
                        throw new ArgumentException("Mixed Categorical and Numerical Parents not allowed");
                    }
                }
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
        /// Helper method to add a child from one parent
        /// </summary>
        /// <param name="parent">Parent (a variable array over a range of examples)</param>
        /// <param name="cpt">Conditional probability table</param>
        /// <returns></returns>
        public static VariableArray<double> AddChildFromOneParent(
            VariableArray<int> parent,
            VariableArray<double> cpt) {
            var n = parent.Range;
            var child = Variable.Array<double>(n);

            using (Variable.ForEach(n))
            using (Variable.Switch(parent[n]))
                child[n] = cpt[parent[n]];
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
        public static VariableArray<double> AddChildFromTwoParents(
            VariableArray<int> parent1,
            VariableArray<int> parent2,
            VariableArray<VariableArray<double>, double[][]> cpt) {
            var n = parent1.Range;
            var child = Variable.Array<double>(n);
            using (Variable.ForEach(n))
            using (Variable.Switch(parent1[n]))
            using (Variable.Switch(parent2[n]))
                child[n] = cpt[parent2[n]][parent1[n]];
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

    public class NoParentNodeCategoricalNonShared : DistributionsNodeNonShared {
        public Variable<Vector> Probability;
        public Variable<Dirichlet> ProbPrior;
        public Dirichlet ProbPosterior;
        override public int ParentCount() {
            return 0;
        }
        public NoParentNodeCategoricalNonShared(ModelNodeNonShared node) {
            this.node = node;
            ProbPrior = Variable.New<Dirichlet>().Named("Prob" + node.name + "Prior");
            ProbPrior.ObservedValue = (node.original.distributions as NoParentNodeCategorical).ProbPrior;
            Probability = Variable<Vector>.Random(ProbPrior).Named("Prob" + node.name);
            Probability.SetValueRange(node.states);
        }
        public override void AddParents(int numberOfEntries) {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed[N] = Variable.Discrete(Probability).ForEach(N);
        }
        public override void Infer(InferenceEngine engine) {
            ProbPosterior = engine.Infer<Dirichlet>(Probability);
        }
        public override void SetPriorToPosterior() {
            ProbPrior.ObservedValue = ProbPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt-1; ii++) {
                str += node.name + " P(" + ii + ") = " + ProbPosterior.GetMean()[ii] + "\n";
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    public class NoParentNodeNumericalNonShared : DistributionsNodeNonShared {
        public Variable<Gaussian> meanPrior;
        public Variable<Gamma> precPrior;
        public Variable<double> mean;
        public Variable<double> prec;
        public Variable<double> val;

        public Gaussian meanPosterior;
        public Gamma precPosterior;
        override public int ParentCount() {
            return 0;
        }
        public NoParentNodeNumericalNonShared(ModelNodeNonShared node) {
            this.node = node;
            meanPrior = (node.original.distributions as NoParentNodeNumerical).meanPrior;
            precPrior = (node.original.distributions as NoParentNodeNumerical).precPrior;
            meanPrior.Named(node.name + "MeanPrior");
            precPrior.Named(node.name + "PrecPrior");
            mean = Variable<double>.Random(meanPrior).Named(node.name + "Mean");
            prec = Variable<double>.Random(precPrior).Named(node.name + "Prec");
        }
        public override void AddParents(int numberOfEntries) {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);
            ObservedNumerical[N] = Variable.GaussianFromMeanAndPrecision(mean, prec).ForEach(N);
        }
        public override void Infer(InferenceEngine engine) {
            meanPosterior = engine.Infer<Gaussian>(mean);
            precPosterior = engine.Infer<Gamma>(prec);
        }
        public override void SetPriorToPosterior() {
            meanPrior.ObservedValue = meanPosterior;
            precPrior.ObservedValue = precPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }

    }
    public class OneCategoricalParentNodeCategoricalNonShared : DistributionsNodeNonShared {
        public VariableArray<Vector> CPT;
        public VariableArray<Dirichlet> CPTPrior;
        public Dirichlet[] CPTPosterior;
        public OneCategoricalParentNodeCategoricalNonShared(ModelNodeNonShared node) {
            this.node = node;
            Range parentStates = node.parents[0].states;
            CPTPrior = Variable.Array<Dirichlet>(parentStates).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = (node.original.distributions as OneCategoricalParentNodeCategorical).CPTPrior;
            CPT = Variable.Array<Vector>(parentStates).Named("Prob" + node.name);
            CPT[parentStates] = Variable<Vector>.Random(CPTPrior[parentStates]); // Softmax over feature vector of size 1
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[]>(CPT);
            
        }
        public override void SetPriorToPosterior() {
            CPTPrior.ObservedValue = CPTPosterior;
        }

        public override void AddParents(int numberOfEntries) {
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
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    public class SoftmaxCategoricalOneParentNonShared : DistributionsNodeNonShared {
        public  VariableArray<VariableArray<VectorGaussian>,VectorGaussian[][]> Bprior;
        public VariableArray<VariableArray<Gaussian>, Gaussian[][]> mPrior;
        public VariableArray<VariableArray<Vector>, Vector[][]> B;
        public VariableArray<VariableArray<double>, double[][]> m;
        public VariableArray<Vector> x;
        VectorGaussian[][] bPost;
        Gaussian[][] mPost;
        override public int ParentCount() {
            return 0;
        }
        public SoftmaxCategoricalOneParentNonShared(ModelNodeNonShared node) {
            this.node = node;
            Range parentRange = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentRange = parent.states;
                }
            }
            Bprior = Variable.Array(Variable.Array<VectorGaussian>(parentRange), node.states).Named(node.name + "CoefficientsPrior");
            Bprior.ObservedValue = (node.original.distributions as SoftmaxOneParentCategorical).Bprior;

            B = Variable.Array(Variable.Array<Vector>(parentRange), node.states).Named(node.name + "B");
            B[node.states][parentRange] = Variable<Vector>.Random(Bprior[node.states][parentRange]);

            mPrior = Variable.Array(Variable.Array<Gaussian>(parentRange), node.states).Named(node.name + "mPrior");
            mPrior.ObservedValue = (node.original.distributions as SoftmaxOneParentCategorical).mPrior;

            m = Variable.Array(Variable.Array<double>(parentRange), node.states).Named(node.name + "m");
            m[node.states][parentRange] = Variable<double>.Random(mPrior[node.states][parentRange]);


            Variable.ConstrainEqualRandom(B[node.states.SizeAsInt - 1][parentRange], VectorGaussian.PointMass(Vector.Zero(node.parents.Count-1)));
            Variable.ConstrainEqualRandom(m[node.states.SizeAsInt - 1][parentRange], Gaussian.PointMass(0));


        }
        public override void AddParents(int numberOfEntries) {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed.SetValueRange(node.states);
            Range parentRange = new Range(node.parents.Count - 1);

            Range parentStates = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentStates = parent.states;
                }
            }
            x = Variable.Array<Vector>(N).Named("x" + node.name);
            for (int ii = 0; ii < numberOfEntries; ii++) {
                VariableArray<double> row = Variable<double>.Array(parentRange).Named(node.name + "row" + ii);
                int offset = 0;
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    if (node.parents[jj].distributionType == DistributionType.Categorical) {
                        offset = -1;
                    }
                    else {
                        row[jj + offset] = Variable.GaussianFromMeanAndPrecision((node.parents[jj].distributions.ObservedNumerical[ii]),1);
                    }
                }
                x[ii] = Variable.Vector(row);
            }

            var g = Variable.Array(Variable.Array(Variable.Array<double>(node.states), N), parentStates).Named("g" + node.name);
            g[parentStates][N][node.states] = Variable.InnerProduct(B[node.states][parentStates], (x[N])) + m[node.states][parentStates];
            var p = Variable.Array(Variable.Array<Vector>(N), parentStates).Named("p" + node.name);
            p[parentStates][N] = Variable.Softmax(g[parentStates][N]);
            if (node.name == "doorGoTo") {
                bool stophere = true;
            }
            VariableArray<int> parentObserved = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentObserved = parent.distributions.Observed;
                }
            }
            using (Variable.ForEach(N))
            using (Variable.Switch(parentObserved[N]))
                Observed[N] = Variable.Discrete(p[parentObserved[N]][N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian[][]>(B);
            mPost = engine.Infer<Gaussian[][]>(m);
        }
        public override void SetPriorToPosterior() {
            Bprior.ObservedValue = bPost;
            mPrior.ObservedValue = mPost;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    public class SoftmaxCategoricalNonShared : DistributionsNodeNonShared {
        public VariableArray<VectorGaussian> Bprior;
        public VariableArray<Gaussian> mPrior;
        public VariableArray<Vector> B;
        public VariableArray<double> m;
        public VariableArray<Vector> x;
        VectorGaussian[] bPost;
        Gaussian[] mPost;
        override public int ParentCount() {
            return 0;
        }
        public SoftmaxCategoricalNonShared(ModelNodeNonShared node) {
            this.node = node;
            Bprior = Variable.Array<VectorGaussian>(node.states).Named(node.name + "CoefficientsPrior");
            Bprior.ObservedValue = (node.original.distributions as SoftmaxCategorical).Bprior;
            B = Variable.Array<Vector>(node.states).Named(node.name + "Coefficients");
            // The weight vector for each class.
            B[node.states] = Variable<Vector>.Random(Bprior[node.states]);
            mPrior = Variable.Array<Gaussian>(node.states).Named(node.name + "BiasPrior");
            mPrior.ObservedValue = (node.original.distributions as SoftmaxCategorical).mPrior; 
            m = Variable.Array<double>(node.states).Named(node.name + "Bias");
            m[node.states] = Variable<double>.Random(mPrior[node.states]);
            Variable.ConstrainEqualRandom(B[node.states.SizeAsInt - 1], VectorGaussian.PointMass(Vector.Zero(node.parents.Count)));
            Variable.ConstrainEqualRandom(m[node.states.SizeAsInt - 1], Gaussian.PointMass(0));


        }
        public override void AddParents(int numberOfEntries) {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed.SetValueRange(node.states);
            Range parentRange = new Range(node.parents.Count);

            x = Variable.Array<Vector>(N).Named("x" + node.name);
            for (int ii = 0; ii < numberOfEntries; ii++) {
                VariableArray<double> row = Variable<double>.Array(parentRange);
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    row[jj] = Variable.GaussianFromMeanAndPrecision((node.parents[jj].distributions.ObservedNumerical[ii]),1);
                }
                x[ii] = Variable.Vector(row);
            }

            var g = Variable.Array(Variable.Array<double>(node.states), N).Named("g" + node.name);
            g[N][node.states] = Variable.InnerProduct(B[node.states], (x[N])) + m[node.states];
            var p = Variable.Array<Vector>(N).Named("p" + node.name);
            p[N] = Variable.Softmax(g[N]);
            using (Variable.ForEach(N))
                Observed[N] = Variable.Discrete(p[N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian[]>(B);
            mPost = engine.Infer<Gaussian[]>(m);
        }
        public override void SetPriorToPosterior() {
            Bprior.ObservedValue = bPost;
            mPrior.ObservedValue = mPost;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    public class LinearRegressionNonShared : DistributionsNodeNonShared {
        public VariableArray<Vector> x;
        public Variable<Vector> B;
        public Variable<VectorGaussian> Bprior;
        public VectorGaussian Bpost;
        public LinearRegressionNonShared(ModelNodeNonShared node) {
            this.node = node;
            Bprior = Variable.New<VectorGaussian>().Named(node.name + "CoefficientsPrior");
            Bprior.ObservedValue = (node.original.distributions as LinearRegression).Bprior;
            B = Variable<Vector>.Random(Bprior).Named(node.name + "B");
        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            Bpost = engine.Infer<VectorGaussian>(B);

        }
        public override void SetPriorToPosterior() {
            Bprior.ObservedValue = Bpost;
        }

        public override void AddParents(int numberOfEntries) {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);

            Range parentRange = new Range(node.parents.Count + 1);
            x = Variable.Array<Vector>(N).Named("x" + node.name);
            for (int ii = 0; ii < numberOfEntries; ii++) {
                VariableArray<double> row = Variable<double>.Array(parentRange);
                row[0] = Variable.GaussianFromMeanAndPrecision(1, 1000);
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    row[jj + 1] = Variable.GaussianFromMeanAndPrecision((node.parents[jj].distributions.ObservedNumerical[ii]), 1);
                }
                x[ii] = Variable.Vector(row);
            }

            ObservedNumerical[N] = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct(x[N], B), 1.0);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }
    }
    public class LinearRegressionOneParentNonShared : DistributionsNodeNonShared {
        public VariableArray<Vector> x;
        public VariableArray<Vector> B;
        public VariableArray<VectorGaussian> Bprior;
        public VectorGaussian[] Bpost;
        public LinearRegressionOneParentNonShared(ModelNodeNonShared node) {
            this.node = node;
            
            Range parentRange = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentRange = parent.states;
                }
            }
            Bprior = Variable<VectorGaussian>.Array(parentRange).Named(node.name + "B Prior");
            Bprior.ObservedValue = (node.original.distributions as LinearRegressionOneParent).Bprior;
            
            B = Variable<Vector>.Array(parentRange).Named(node.name + "B");
            B[parentRange] = Variable<Vector>.Random(Bprior[parentRange]);
        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            Bpost = engine.Infer<VectorGaussian[]>(B);

        }
        public override void SetPriorToPosterior() {
            Bprior.ObservedValue = Bpost;
        }

        public override void AddParents(int numberOfEntries) {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);
            Range parentRange = new Range(node.parents.Count);
            x = Variable.Array<Vector>(N).Named("x" + node.name);
            for (int ii = 0; ii < numberOfEntries; ii++) {
                VariableArray<double> row = Variable<double>.Array(parentRange);
                row[0] = Variable.GaussianFromMeanAndPrecision(1, 1000);
                int offset = 0;
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    if (node.parents[jj].distributionType == DistributionType.Categorical) {
                        offset = -1;
                    }
                    else {
                        row[jj + 1 + offset] = Variable.GaussianFromMeanAndPrecision((node.parents[jj].distributions.ObservedNumerical[ii]),1);
                    }
                }
                x[ii] = Variable.Vector(row);
            }
            VariableArray<int> parentObserved = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentObserved = parent.distributions.Observed;
                }
            }
            using (Variable.ForEach(N))
            using (Variable.Switch(parentObserved[N]))
                ObservedNumerical[N] = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct(B[parentObserved[N]], x[N]), 1.0);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
           
            ObservedNumerical.ObservedValue = observedValues.Item2;
            
        }
    }
    public class OneCategoricalParentNodeNumericalNonShared : DistributionsNodeNonShared {
        
        VariableArray<Gaussian> meanPrior;
        VariableArray<Gamma> precisionPrior;  
        VariableArray<double> mean;
        VariableArray<double> precision;
        VariableArray<double> val;
        public Gaussian[] meanPosterior;
        public Gamma[] precisionPosterior;
        public Gaussian[] valPosterior;
        override public int ParentCount() {
            return 0;
        }
        public OneCategoricalParentNodeNumericalNonShared(ModelNodeNonShared node) {
            this.node = node;
            Range parentStates = node.parents[0].states;
            meanPrior = Variable.Array<Gaussian>(parentStates).Named(node.name + "mean" + "Prior");
            meanPrior.ObservedValue = (node.original.distributions as OneCategoricalParentNodeNumerical).meanPrior; 
            precisionPrior = Variable.Array<Gamma>(parentStates).Named(node.name + "prec" + "Prior");
            precisionPrior.ObservedValue = (node.original.distributions as OneCategoricalParentNodeNumerical).precisionPrior;
            mean = Variable.Array<double>(parentStates).Named(node.name + "mean");
            mean[parentStates] = Variable<double>.Random(meanPrior[parentStates]);

            precision = Variable.Array<double>(parentStates).Named(node.name + "prec");
            precision[parentStates] = Variable<double>.Random(precisionPrior[parentStates]);

            val = Variable.Array<double>(parentStates).Named(node.name + "val");
            val[parentStates] = Variable.GaussianFromMeanAndVariance(mean[parentStates], precision[parentStates]);
        }
        public override void AddParents(int numberOfEntries) {
            ObservedNumerical = DistributionsNode.AddChildFromOneParent(node.parents[0].distributions.Observed, val).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            meanPosterior = engine.Infer<Gaussian[]>(mean);
            precisionPosterior = engine.Infer<Gamma[]>(precision);
        }
        public override void SetPriorToPosterior() {
            meanPrior.ObservedValue = meanPosterior;
            precisionPrior.ObservedValue = precisionPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
        
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }
    }
}
