using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Collections;

namespace ZeldaInfer {
    using GammaArray = DistributionStructArray<Gamma, double>;
    using DirichletArray = DistributionRefArray<Dirichlet, Vector>;
    using GaussianArray = DistributionStructArray<Gaussian, double>;
    using VectorGaussianArray = DistributionRefArray<VectorGaussian, Vector>;
    using GammaArray2D = DistributionRefArray<DistributionStructArray<Gamma, double>,double[]>;
    using DirichletArray2D = DistributionRefArray<DistributionRefArray<Dirichlet, Vector>, Vector[]>;
    using GaussianArray2D = DistributionRefArray<DistributionStructArray<Gaussian, double>,double[]>;
    using VectorGaussianArray2D = DistributionRefArray<DistributionRefArray<VectorGaussian, Vector>,Vector[]>;
    [Serializable]
    public abstract class DistributionsNode {
        public virtual int ParentCount() { return -1; }
        [NonSerialized]
        public Model sharedModel;

        public ModelNode node;
        [NonSerialized]
        public VariableArray<int> Observed;
        [NonSerialized]
        public VariableArray<double> ObservedNumerical;
        [NonSerialized]
        public Range N;
        public virtual void AddParents() { }
        public virtual string PosteriorToString() { return ""; }
        public abstract void Infer(InferenceEngine engine);
        public abstract void SetPriorToPosterior();
        public abstract void SetObservedData(Tuple<int[],double[]> observedValues);
        public virtual void LoadAfterSerialization(Range N) {
            this.N = N;
        }
        public static void CreateDistributionsNode(ModelNode node, Range N, Model sharedModel) {
            if (node.distributionType == DistributionType.Categorical) {
                if (node.parents.Count == 0) {
                    node.distributions = new NoParentNodeCategorical(node, sharedModel);
                }
                else {
                    int numCategorical = 0;
                    foreach (var parent in node.parents) {
                        numCategorical += parent.distributionType == DistributionType.Categorical ? 1 : 0;
                    }
                    if (numCategorical == node.parents.Count) {
                        if (node.parents.Count == 1) {
                            node.distributions = new OneCategoricalParentNodeCategorical(node, sharedModel);
                        }
                        else {
                            throw new ArgumentException("Only up to 1 Categorical Parents Allowed");
                        }
                        /*
                        else if (node.parents.Count == 2) {
                            node.distributions = new TwoCategoricalParentNodeCategorical(node);
                        }
                        else if (node.parents.Count == 3) {
                            throw new ArgumentException("Only up to 2 Categorical Parents Allowed");
                           // node.distributions = new ThreeCategoricalParentNodeCategorical(node);
                        }*/
                    }
                    else {
                        if (numCategorical == 0) {
                            node.distributions = new SoftmaxCategorical(node, sharedModel);
                        }
                        else if (numCategorical == 1) {
                         //   throw new ArgumentException("Continuous to Categorical only covered for basic Softmax");
                            node.distributions = new SoftmaxOneParentCategorical(node, sharedModel);
                        }
                        else if (numCategorical == 2) {
                            throw new ArgumentException("Continuous to Categorical only covered for basic Softmax");
                        //    node.distributions = new TwoCategoricalParentNodeSoftmaxCategorical(node);
                        }
                        else if (numCategorical == 3) {
                            throw new ArgumentException("Continuous to Categorical only covered for basic Softmax");
                        //    node.distributions = new ThreeCategoricalParentNodeSoftmaxCategorical(node);
                        }
                        else {
                            throw new ArgumentException("Continuous to Categorical only covered for basic Softmax");
                        }

                    }
                }
            }
            else {
                if (node.parents.Count == 0) {
                    node.distributions = new NoParentNodeNumerical(node, sharedModel);
                }
                else {
                    int numCategorical = 0;
                    foreach (var parent in node.parents) {
                        numCategorical += parent.distributionType == DistributionType.Categorical ? 1 : 0;
                    }
                    if (numCategorical == node.parents.Count) {
                        if (node.parents.Count == 1) {
                            node.distributions = new OneCategoricalParentNodeNumerical(node, sharedModel);
                        }
                        else {
                            throw new ArgumentException("Only up to 1 Categorical Parent Allowed");
                        }/*
                        else if (node.parents.Count == 2) {
                            node.distributions = new TwoCategoricalParentNodeNumerical(node);
                        }
                        else if (node.parents.Count == 3) {
                            throw new ArgumentException("Only up to 2 Categorical Parents Allowed");
                       //     node.distributions = new ThreeCategoricalParentNodeNumerical(node);
                        }
                          * */
                    }
                    else if (numCategorical == 0){
                        //Bayes Linear Regression
                        node.distributions = new LinearRegression(node, sharedModel);
                    }
                    else {
                        if (numCategorical == 1) {
                            node.distributions = new LinearRegressionOneParent(node, sharedModel);
                        }
                        else {
                            //Ooh boy don't know what to do here
                            throw new ArgumentException(node.name + " - Mixed Categorical and Numerical Parents not allowed");
                        }
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
    [Serializable]
    public class NoParentNodeCategorical : DistributionsNode {
        [NonSerialized]
        public SharedVariable<Vector> SharedProbability;
        [NonSerialized]
        public Variable<Vector> Probability;
        [NonSerialized]
        public Dirichlet ProbPrior;
        [NonSerialized]
        public Dirichlet ProbPosterior;
        override public int ParentCount() {
            return 0;
        }
        public NoParentNodeCategorical(ModelNode node,Model sharedModel) {
            this.node = node;
           // ProbPrior = Variable.New<Dirichlet>().Named("Prob" + node.name + "Prior");
            ProbPrior = Dirichlet.Uniform(node.states.SizeAsInt);
            SharedProbability = SharedVariable<Vector>.Random(ProbPrior).Named("SharedProb" + node.name);
                //.Random(ProbPrior).Named("SharedProb" + node.name);
            Probability = SharedProbability.GetCopyFor(sharedModel);//Variable<Vector>.Random(ProbPrior).Named("Prob" + node.name);
            Probability.SetValueRange(node.states);
        }
        public override void AddParents() {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed[N] = Variable.Discrete(Probability).ForEach(N);
        }
        public override void Infer(InferenceEngine engine) {
            ProbPosterior = engine.Infer<Dirichlet>(Probability);
        }
        public override void SetPriorToPosterior() {
            ProbPosterior = SharedProbability.Marginal<Dirichlet>();
            ProbPrior = ProbPosterior;
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
          //  ProbPrior = Variable.New<Dirichlet>().Named("Prob" + node.name + "Prior");
          //  ProbPrior.ObservedValue = ProbPosterior;
            Probability = Variable<Vector>.Random(ProbPrior).Named("Prob" + node.name);
            Probability.SetValueRange(node.states);
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
    [Serializable]
    public class NoParentNodeNumerical : DistributionsNode {
        [NonSerialized]
        public Gaussian meanPrior;
        [NonSerialized]
        public Gamma precPrior;
        [NonSerialized]
        public SharedVariable<double> meanShared;
        [NonSerialized]
        public SharedVariable<double> precShared;
        [NonSerialized]
        public Variable<double> mean;
        [NonSerialized]
        public Variable<double> prec;
        [NonSerialized]
        public Variable<double> val;

        public Gaussian meanPosterior;
        public Gamma precPosterior;
        override public int ParentCount() {
            return 0;
        }
        public NoParentNodeNumerical(ModelNode node,Model sharedModel) {
            this.node = node;
            meanPrior = Gaussian.FromMeanAndVariance(0, 100);//Variable.New<Gaussian>().Named(node.name + "Prior");
            precPrior = Gamma.FromMeanAndVariance(1, 1);
            meanShared = SharedVariable<double>.Random(meanPrior).Named(node.name + "MeanShared");
            precShared = SharedVariable<double>.Random(precPrior).Named(node.name + "PrecShared");
            mean = meanShared.GetCopyFor(sharedModel);
            prec = precShared.GetCopyFor(sharedModel);
            // meanPrior.ObservedValue = Gaussian.FromMeanAndVariance(0, 100);
           // precPrior = Variable.New<Gamma>().Named(node.name + "Prior");
          //  precPrior.ObservedValue = Gamma.FromMeanAndVariance(0, 100);
            val = Variable.GaussianFromMeanAndPrecision(mean, prec).Named(node.name + "val");
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N; 
            
            meanPrior = meanPosterior;
            precPrior = precPosterior;         
            mean = Variable<double>.Random(meanPrior).Named(node.name + "Mean");
            prec = Variable<double>.Random(precPrior).Named(node.name + "Prec");
            val = Variable.GaussianFromMeanAndPrecision(mean, prec).Named(node.name + "val");
        }
        public override void AddParents() {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);
            ObservedNumerical[N] = val.ForEach(N);
        }
        public override void Infer(InferenceEngine engine) {
            meanPosterior = engine.Infer<Gaussian>(mean);
            precPosterior = engine.Infer<Gamma>(prec);
        }
        public override void SetPriorToPosterior() {
            meanPosterior = meanShared.Marginal<Gaussian>();
            precPosterior = precShared.Marginal<Gamma>();
            meanPrior = meanPosterior;
            precPrior = precPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            /*
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
          //      str += node.name + " P(" + ii + ") = " + ProbPosterior.GetMean()[ii] + "\n";
            }
             */
            str += node.name + " N(" + meanPosterior.GetMean() + ", " + precPosterior.GetMean() + ")\n";
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }

    }
    [Serializable]
    public class SoftmaxCategorical : DistributionsNode {
        [NonSerialized]
        public VectorGaussian[] Bprior;
        [NonSerialized]
        public Gaussian[] mPrior;
        [NonSerialized]
        public VariableArray<Vector> B;
        [NonSerialized]
        public VariableArray<double> m;
        [NonSerialized]
        public SharedVariableArray<Vector> BShared;
        [NonSerialized]
        public SharedVariableArray<double> mShared;
        [NonSerialized]
        public VariableArray<Vector> x;
        public VectorGaussian[] bPost;
        public Gaussian[] mPost;
      //  VariableArray<VariableArray<int>, int[][]> yData;
        override public int ParentCount() {
            return 0;
        }
        public SoftmaxCategorical(ModelNode node,Model sharedModel) {
            this.node = node;
            Bprior = Enumerable.Repeat(VectorGaussian.FromMeanAndPrecision(
                Vector.Zero(node.parents.Count), PositiveDefiniteMatrix.Identity(node.parents.Count)),node.states.SizeAsInt).ToArray();
            
            BShared = SharedVariable<Vector>.Random(node.states, (VectorGaussianArray) Distribution<Vector>.Array<VectorGaussian>(Bprior) ).Named(node.name + "BShared");
            B = BShared.GetCopyFor(sharedModel);
            // B = Variable.Array<Vector>(node.states).Named(node.name + "Coefficients");
            // The weight vector for each class.
           // B[node.states] = Variable<Vector>.Random(Bprior[node.states]);
            mPrior = Enumerable.Repeat(Gaussian.FromMeanAndVariance(0, 100), node.states.SizeAsInt).ToArray();
            mShared = SharedVariable<double>.Random(node.states, (GaussianArray)Distribution<double>.Array<Gaussian>(mPrior)).Named(node.name + "mShared"); 


            m = mShared.GetCopyFor(sharedModel);
            Variable.ConstrainEqualRandom(B[node.states.SizeAsInt - 1], VectorGaussian.PointMass(Vector.Zero(node.parents.Count)));
            Variable.ConstrainEqualRandom(m[node.states.SizeAsInt - 1], Gaussian.PointMass(0));
           
            
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
         //   Bprior = Variable.Array<VectorGaussian>(node.states).Named(node.name + "CoefficientsPrior");
         //   Bprior.ObservedValue = bPost;
            B = Variable.Array<Vector>(node.states).Named(node.name + "Coefficients");
            // The weight vector for each class.
         //   B[node.states] = Variable<Vector>.Random(Bprior[node.states]);
        //    mPrior = Variable.Array<Gaussian>(node.states).Named(node.name + "BiasPrior");
        //    mPrior.ObservedValue = mPost;
            m = Variable.Array<double>(node.states).Named(node.name + "Bias");
           // m[node.states] = Variable<double>.Random(mPrior[node.states]);
            Variable.ConstrainEqualRandom(B[node.states.SizeAsInt - 1], VectorGaussian.PointMass(Vector.Zero(node.parents.Count)));
            Variable.ConstrainEqualRandom(m[node.states.SizeAsInt - 1], Gaussian.PointMass(0));
        }
        public override void AddParents() {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed.SetValueRange(node.states);
            //Variable.Multinomial(trialsCount[N], p[N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian[]>(B);
            mPost = engine.Infer<Gaussian[]>(m);
        }
        public override void SetPriorToPosterior() {
            bPost = BShared.Marginal<VectorGaussian[]>();
            mPost = mShared.Marginal<Gaussian[]>();
            Bprior = bPost;
            mPrior = mPost;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
         //       str += node.name + " P(" + ii + ") = " + ProbPosterior.GetMean()[ii] + "\n";
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Range parentRange = new Range(node.parents.Count);
          
            x = Variable.Array<Vector>(N).Named("x" + node.name);
            Observed.ObservedValue = observedValues.Item1;
            for (int ii = 0; ii < observedValues.Item1.Length; ii++) {
                //double[] row = new double[node.parents.Count];
                VariableArray<double> row = Variable<double>.Array(parentRange);
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    row[jj] = (node.parents[jj].distributions.ObservedNumerical[ii]);
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
    }
    [Serializable]
    public class SoftmaxOneParentCategorical : DistributionsNode {
        [NonSerialized] public VectorGaussian[][] Bprior;
        [NonSerialized] public Gaussian[][] mPrior;
        [NonSerialized]
        public ISharedVariableArray<VariableArray<Vector>, Vector[][]> BShared;
        [NonSerialized]
        public ISharedVariableArray<VariableArray<double>, double[][]> mShared;
        [NonSerialized]
        public  VariableArray< VariableArray<Vector>, Vector[][]> B;
        [NonSerialized]
        public VariableArray<VariableArray<double>, double[][]> m;
        [NonSerialized] public VariableArray<Vector> x;
        public VectorGaussian[][] bPost;
        public Gaussian[][] mPost;
        //  VariableArray<VariableArray<int>, int[][]> yData;
        override public int ParentCount() {
            return 0;
        }
        public SoftmaxOneParentCategorical(ModelNode node,Model sharedModel) {
            this.node = node;
            Range parentStates = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentStates = parent.states;
                }
            }

            Bprior = Enumerable.Repeat(Enumerable.Repeat(VectorGaussian.FromMeanAndPrecision(
                Vector.Zero(node.parents.Count), PositiveDefiniteMatrix.Identity(node.parents.Count)), node.states.SizeAsInt).ToArray(),parentStates.SizeAsInt).ToArray();
            BShared = SharedVariable<Vector[][]>.Random(Variable.Array<Vector>(parentStates), node.states, (VectorGaussianArray2D)Distribution<Vector>.Array<VectorGaussian>(Bprior)).Named(node.name + "BShared"); 
            B = BShared.GetCopyFor(sharedModel);  
            
            mPrior = Enumerable.Repeat(Enumerable.Repeat(Gaussian.FromMeanAndVariance(0, 100), node.states.SizeAsInt).ToArray(), parentStates.SizeAsInt).ToArray();
            mShared = SharedVariable<double[][]>.Random(Variable.Array<double>(parentStates), node.states, (GaussianArray2D)Distribution<double>.Array<Gaussian>(mPrior)).Named(node.name + "mShared"); 
            m = mShared.GetCopyFor(sharedModel);
            Variable.ConstrainEqualRandom(B[node.states.SizeAsInt - 1][parentStates], VectorGaussian.PointMass(Vector.Zero(node.parents.Count)));
            Variable.ConstrainEqualRandom(m[node.states.SizeAsInt - 1][parentStates], Gaussian.PointMass(0));


        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parentStates = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentStates = parent.states;
                }
            }

         //   Bprior = Variable.Array(Variable.Array<VectorGaussian>(node.states), parentStates).Named(node.name + "CoefficientsPrior");
         //   Bprior.ObservedValue = bPost;
            B = Variable.Array(Variable.Array<Vector>(node.states), parentStates).Named(node.name + "Coefficients");
            // The weight vector for each class.
         //   B[parentStates][node.states] = Variable<Vector>.Random(Bprior[parentStates][node.states]);
         //   mPrior = Variable.Array(Variable.Array<Gaussian>(node.states), parentStates).Named(node.name + "BiasPrior");
         //   mPrior.ObservedValue = mPost;
            m = Variable.Array(Variable.Array<double>(node.states), parentStates).Named(node.name + "Bias");
          //  m[parentStates][node.states] = Variable<double>.Random(mPrior[parentStates][node.states]);
            Variable.ConstrainEqualRandom(B[parentStates][node.states.SizeAsInt - 1], VectorGaussian.PointMass(Vector.Zero(node.parents.Count)));
            Variable.ConstrainEqualRandom(m[parentStates][node.states.SizeAsInt - 1], Gaussian.PointMass(0));
        }
        public override void AddParents() {
            Observed = Variable.Array<int>(N).Named(node.name);
            Observed.SetValueRange(node.states);
            //Variable.Multinomial(trialsCount[N], p[N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian[][]>(B);
            mPost = engine.Infer<Gaussian[][]>(m);
        }
        public override void SetPriorToPosterior() {
            bPost = BShared.Marginal<VectorGaussian[][]>();
            mPost = mShared.Marginal<Gaussian[][]>();
            Bprior = bPost;
            mPrior = mPost;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                //       str += node.name + " P(" + ii + ") = " + ProbPosterior.GetMean()[ii] + "\n";
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Range parentRange = new Range(node.parents.Count);

            x = Variable.Array<Vector>(N).Named("x" + node.name);
            Observed.ObservedValue = observedValues.Item1;
            for (int ii = 0; ii < observedValues.Item1.Length; ii++) {
                //double[] row = new double[node.parents.Count];
                VariableArray<double> row = Variable<double>.Array(parentRange);
                int offset = 0;
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    if (node.parents[jj].distributionType == DistributionType.Categorical) {
                        offset = -1;
                    }
                    else {
                        row[jj + 1 + offset] = (node.parents[jj].distributions.ObservedNumerical[ii]);
                    }
                }
                x[ii] = Variable.Vector(row);
            }
            
            Range parentStates = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentStates = parent.states;
                }
            }
            var g = Variable.Array(Variable.Array(Variable.Array<double>(node.states), N),parentStates).Named("g" + node.name);
            g[parentStates][N][node.states] = Variable.InnerProduct(B[node.states][parentStates], (x[N])) + m[node.states][parentStates];
            var p = Variable.Array(Variable.Array<Vector>(N).Named("p" + node.name),parentStates);
            p[parentStates][N] = Variable.Softmax(g[parentStates][N]);
         
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
    }
    [Serializable]
    public class LinearRegression : DistributionsNode {
        [NonSerialized] public VectorGaussian Bprior;
        [NonSerialized]
        public SharedVariable<Vector> BShared;
        [NonSerialized]
        public Variable<Vector> B;
        [NonSerialized] public VariableArray<Vector> x;
        public VectorGaussian bPost;

        [NonSerialized]
        public bool firstTime = true;
        //  VariableArray<VariableArray<int>, int[][]> yData;
        override public int ParentCount() {
            return 0;
        }
        public LinearRegression(ModelNode node,Model sharedModel) {
            this.node = node;
            Bprior = VectorGaussian.FromMeanAndPrecision(
                Vector.Zero(node.parents.Count+1), PositiveDefiniteMatrix.Identity(node.parents.Count+1));
         //   B = Variable<Vector>.Random(Bprior).Named(node.name + "Coefficients");
            BShared = SharedVariable<Vector>.Random(Bprior).Named(node.name + "BShared"); 
            B = BShared.GetCopyFor(sharedModel);
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
          //  Bprior = Variable.New<VectorGaussian>().Named(node.name + "CoefficientsPrior");
     //       Bprior.ObservedValue = bPost;
            B = Variable<Vector>.Random(Bprior).Named(node.name + "Coefficients");
        }
        public override void AddParents() {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);
            //Variable.Multinomial(trialsCount[N], p[N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian>(B);
        }
        public override void SetPriorToPosterior() {
            bPost = BShared.Marginal<VectorGaussian>();
            Bprior = bPost;
        }
        public override string PosteriorToString() {
            string str = "";

            str += node.name + bPost.GetMean() + "\n";
          
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Range parentRange = new Range(node.parents.Count+1);
            ObservedNumerical.ObservedValue = observedValues.Item2;
            if (firstTime) {
                firstTime = false;
                x = Variable.Array<Vector>(N).Named("x" + node.name);
                for (int ii = 0; ii < observedValues.Item2.Length; ii++) {
                    //double[] row = new double[node.parents.Count];
                    VariableArray<double> row = Variable<double>.Array(parentRange);
                    row[0] = 1;
                    for (int jj = 0; jj < node.parents.Count; jj++) {
                        row[jj + 1] = (node.parents[jj].distributions.ObservedNumerical[ii]);
                    }
                    x[ii] = Variable.Vector(row);
                }

                //  var g = Variable<double>.Array(N).Named("g" + node.name);
                ObservedNumerical[N] = Variable.GaussianFromMeanAndVariance(Variable.InnerProduct(B, x[N]), 1.0);
            }
        }
    }
    [Serializable]
    public class LinearRegressionOneParent : DistributionsNode {
        [NonSerialized] public VectorGaussian[] Bprior;
        [NonSerialized]
        public SharedVariableArray<Vector> BShared;
        [NonSerialized]
        public VariableArray<Vector> B;
        [NonSerialized] public VariableArray<Vector> x;
        public VectorGaussian[] bPost;
        //  VariableArray<VariableArray<int>, int[][]> yData;
        override public int ParentCount() {
            return 0;
        }
        public LinearRegressionOneParent(ModelNode node,Model sharedModel) {
            this.node = node;
            Range parentRange = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentRange = parent.states;
                }
            }
            Bprior = Enumerable.Repeat(VectorGaussian.FromMeanAndPrecision(
                Vector.Zero(node.parents.Count), PositiveDefiniteMatrix.Identity(node.parents.Count)), parentRange.SizeAsInt).ToArray();
            BShared = SharedVariable<Vector>.Random(parentRange, (VectorGaussianArray)Distribution<Vector>.Array<VectorGaussian>(Bprior)).Named(node.name + "BShared"); 
            B = BShared.GetCopyFor(sharedModel);
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parentRange = null;
            foreach (var parent in node.parents) {
                if (parent.distributionType == DistributionType.Categorical) {
                    parentRange = parent.states;
                }
            }
            //Bprior = Variable.Array<VectorGaussian>(parentRange).Named(node.name + "CoefficientsPrior");
            //Bprior.ObservedValue = bPost;
            B = Variable.Array<Vector>(parentRange).Named(node.name + "Coefficients");
         //   B[parentRange] = Variable<Vector>.Random(Bprior[parentRange]);
        }
        public override void AddParents() {
            ObservedNumerical = Variable.Array<double>(N).Named(node.name);
            //Variable.Multinomial(trialsCount[N], p[N]);
        }
        public override void Infer(InferenceEngine engine) {
            bPost = engine.Infer<VectorGaussian[]>(B);
        }
        public override void SetPriorToPosterior() {
            bPost = BShared.Marginal<VectorGaussian[]>();
            Bprior = bPost;
        }
        public override string PosteriorToString() {
            string str = "";
            foreach (var bpost in bPost) {
                str += node.name + bpost.GetMean() + "\n";
            }
            return str;
        }

        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Range parentRange = new Range(node.parents.Count + 1);

            x = Variable.Array<Vector>(N).Named("x" + node.name);
            ObservedNumerical.ObservedValue = observedValues.Item2;
            for (int ii = 0; ii < observedValues.Item2.Length; ii++) {
                //double[] row = new double[node.parents.Count];
                VariableArray<double> row = Variable<double>.Array(parentRange);
                row[0] = 1;
                int offset = 0;
                for (int jj = 0; jj < node.parents.Count; jj++) {
                    if (node.parents[jj].distributionType == DistributionType.Categorical) {
                        offset = -1;
                    }
                    else {
                        row[jj + 1 + offset] = (node.parents[jj].distributions.ObservedNumerical[ii]);
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
            //  var g = Variable<double>.Array(N).Named("g" + node.name);
            using (Variable.ForEach(N))
            using (Variable.Switch(parentObserved[N]))
                ObservedNumerical[N] = Variable.GaussianFromMeanAndVariance(Variable.InnerProduct(B[parentObserved[N]], x[N]), 1.0);
            
        }
    }
    [Serializable]
    public class OneCategoricalParentNodeCategorical : DistributionsNode {
        [NonSerialized]
        public SharedVariableArray<Vector> CPTShared;
        [NonSerialized]
        public VariableArray<Vector> CPT;
        [NonSerialized] public Dirichlet[] CPTPrior;
        public Dirichlet[] CPTPosterior;
        public OneCategoricalParentNodeCategorical(ModelNode node,Model sharedModel) {
            this.node = node;
            Range parentStates = node.parents[0].states;
            CPTPrior = Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parentStates.SizeAsInt).ToArray();

            CPTShared = SharedVariable<Vector>.Random(parentStates, (DirichletArray)Distribution<Vector>.Array<Dirichlet>(CPTPrior)).Named(node.name + "CPTShared"); 
            CPT = CPTShared.GetCopyFor(sharedModel);
            CPT.SetValueRange(node.states);
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parentStates = node.parents[0].states;
        //    CPTPrior = Variable.Array<Dirichlet>(parentStates).Named("Prob" + node.name + "Prior");
        //    CPTPrior.ObservedValue = CPTPosterior;
            CPT = Variable.Array<Vector>(parentStates).Named("Prob" + node.name);
        //    CPT[parentStates] = Variable<Vector>.Random(CPTPrior[parentStates]); // Softmax over feature vector of size 1
            CPT.SetValueRange(node.states);
        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[]>(CPT);
            
        }
        public override void SetPriorToPosterior() {
           CPTPosterior = CPTShared.Marginal<Dirichlet[]>();
           CPTPrior = CPTPosterior;
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
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    [Serializable]
    public class OneCategoricalParentNodeNumerical : DistributionsNode {

        [NonSerialized] public Gaussian[] meanPrior;
        [NonSerialized] public Gamma[] precisionPrior;
        [NonSerialized]
        public SharedVariableArray<double> meanShared;
        [NonSerialized]
        public SharedVariableArray<double> precisionShared;
        [NonSerialized]
        public VariableArray<double> mean;
        [NonSerialized]
        public VariableArray<double> precision;
        [NonSerialized] public VariableArray<double> val;
        public Gaussian[] meanPosterior;
        public Gamma[] precisionPosterior;
        public Gaussian[] valPosterior;
        override public int ParentCount() {
            return 0;
        }
        public OneCategoricalParentNodeNumerical(ModelNode node,Model sharedModel) {
            this.node = node;
            Range parentStates = node.parents[0].states;
            meanPrior = Enumerable.Repeat( Gaussian.FromMeanAndVariance(0, 100),parentStates.SizeAsInt).ToArray();
            precisionPrior = Enumerable.Repeat(Gamma.FromShapeAndScale(1, 1), parentStates.SizeAsInt).ToArray();
            meanShared = SharedVariable<double>.Random(parentStates, (GaussianArray)Distribution<double>.Array<Gaussian>(meanPrior)).Named(node.name + "uShared");
            precisionShared = SharedVariable<double>.Random(parentStates, (GammaArray)Distribution<double>.Array<Gamma>(precisionPrior)).Named(node.name + "sShared");
            mean = meanShared.GetCopyFor(sharedModel);
            precision = precisionShared.GetCopyFor(sharedModel);

            val = Variable.Array<double>(parentStates).Named(node.name + "val");
            val[parentStates] = Variable.GaussianFromMeanAndVariance(mean[parentStates], precision[parentStates]);
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parentStates = node.parents[0].states;
          //  meanPrior = Variable.Array<Gaussian>(parentStates).Named(node.name + "mean" + "Prior");
           // meanPrior.ObservedValue = meanPosterior;
          //  precisionPrior = Variable.Array<Gamma>(parentStates).Named(node.name + "prec" + "Prior");
         //   precisionPrior.ObservedValue = precisionPosterior;
            mean = Variable.Array<double>(parentStates).Named(node.name + "mean");
          //  mean[parentStates] = Variable<double>.Random(meanPrior[parentStates]);

            precision = Variable.Array<double>(parentStates).Named(node.name + "prec");
         //   precision[parentStates] = Variable<double>.Random(precisionPrior[parentStates]);

            val = Variable.Array<double>(parentStates).Named(node.name + "val");
            val[parentStates] = Variable.GaussianFromMeanAndVariance(mean[parentStates], precision[parentStates]);
        }
        public override void AddParents() {
            ObservedNumerical = DistributionsNode.AddChildFromOneParent(node.parents[0].distributions.Observed, val).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            meanPosterior = engine.Infer<Gaussian[]>(mean);
            precisionPosterior = engine.Infer<Gamma[]>(precision);
        }
        public override void SetPriorToPosterior() {
            meanPosterior = meanShared.Marginal<Gaussian[]>();
            precisionPosterior = precisionShared.Marginal<Gamma[]>();
            meanPrior = meanPosterior;
            precisionPrior = precisionPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
            //        str += node.name + " P(" + ii + "|" + p1 + ") = " + CPTPosterior[p1].GetMean()[ii] + "\n";
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }
    }
    /*
    [Serializable]
    public class TwoCategoricalParentNodeCategorical : DistributionsNode {
        [NonSerialized] public VariableArray<VariableArray<Vector>, Vector[][]> CPT;
        [NonSerialized] public VariableArray<VariableArray<Dirichlet>, Dirichlet[][]> CPTPrior;
        public Dirichlet[][] CPTPosterior;
        public TwoCategoricalParentNodeCategorical(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parent1States.SizeAsInt).ToArray(), parent2States.SizeAsInt).ToArray(); ;
            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States).Named("Prob" + node.name);
            CPT[parent2States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States][parent1States]); //Softmax with feature vector 
            CPT.SetValueRange(node.states);
        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = CPTPosterior;
            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States).Named("Prob" + node.name);
            CPT[parent2States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States][parent1States]); //Softmax with feature vector 
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
        public override void SetPriorToPosterior() {
            CPTPrior.IsReadOnly = false;
            CPTPrior.ObservedValue = CPTPosterior;
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
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    [Serializable]
    public class TwoCategoricalParentNodeNumerical : DistributionsNode {

        [NonSerialized] public VariableArray<VariableArray<Gaussian>, Gaussian[][]> meanPrior;
        [NonSerialized] public VariableArray<VariableArray<Gamma>, Gamma[][]> precisionPrior;
        [NonSerialized] public VariableArray<VariableArray<double>, double[][]> mean;
        [NonSerialized] public VariableArray<VariableArray<double>, double[][]> precision;
        [NonSerialized] public VariableArray<VariableArray<double>, double[][]> val;
        public Gaussian[][] meanPosterior;
        public Gamma[][] precisionPosterior;
        public Gaussian[][] valPosterior;
        public TwoCategoricalParentNodeNumerical(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            meanPrior = Variable.Array(Variable.Array<Gaussian>(parent1States), parent2States).Named(node.name + "mean" + "Prior");
            meanPrior.ObservedValue = Enumerable.Repeat(Enumerable.Repeat(Gaussian.FromMeanAndVariance(0, 100), parent1States.SizeAsInt).ToArray(), parent2States.SizeAsInt).ToArray(); ;
           
            precisionPrior = Variable.Array(Variable.Array<Gamma>(parent1States), parent2States).Named(node.name + "mean" + "Prior");
            precisionPrior.ObservedValue = Enumerable.Repeat(Enumerable.Repeat(Gamma.FromShapeAndScale(1, 1), parent1States.SizeAsInt).ToArray(), parent2States.SizeAsInt).ToArray(); ;
          
            mean = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "prec");
            mean[parent2States][parent1States] = Variable<double>.Random(meanPrior[parent2States][parent1States]);

            precision = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "prec");
            precision[parent2States][parent1States] = Variable<double>.Random(precisionPrior[parent2States][parent1States]);

            val = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "val");
            val[parent2States][parent1States] = Variable.GaussianFromMeanAndVariance(mean[parent2States][parent1States], precision[parent2States][parent1States]);

        }
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            meanPrior = Variable.Array(Variable.Array<Gaussian>(parent1States), parent2States).Named(node.name + "mean" + "Prior");
            meanPrior.ObservedValue = meanPosterior;

            precisionPrior = Variable.Array(Variable.Array<Gamma>(parent1States), parent2States).Named(node.name + "mean" + "Prior");
            precisionPrior.ObservedValue = precisionPosterior;

            mean = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "prec");
            mean[parent2States][parent1States] = Variable<double>.Random(meanPrior[parent2States][parent1States]);

            precision = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "prec");
            precision[parent2States][parent1States] = Variable<double>.Random(precisionPrior[parent2States][parent1States]);

            val = Variable.Array(Variable.Array<double>(parent1States), parent2States).Named(node.name + "val");
            val[parent2States][parent1States] = Variable.GaussianFromMeanAndVariance(mean[parent2States][parent1States], precision[parent2States][parent1States]);
        }
        override public int ParentCount() {
            return 2;
        }
        public override void AddParents() {
            ObservedNumerical = DistributionsNode.AddChildFromTwoParents(node.parents[0].distributions.Observed,
                node.parents[1].distributions.Observed, val).Named(node.name);
        }
        public override void Infer(InferenceEngine engine) {
            meanPosterior = engine.Infer<Gaussian[][]>(mean);
            precisionPosterior = engine.Infer<Gamma[][]>(precision);
        }
        public override void SetPriorToPosterior() {
            meanPrior.IsReadOnly = false;
            meanPrior.ObservedValue = meanPosterior;
            precisionPrior.IsReadOnly = false;
            precisionPrior.ObservedValue = precisionPosterior;
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int p2 = 0; p2 < node.parents[1].states.SizeAsInt; p2++) {
                    for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                    //    str += node.name + " P(" + ii + "|" + p2 + "," + p1 + ") = " + CPTPosterior[p2][p1].GetMean()[ii] + "\n";
                    }
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            ObservedNumerical.ObservedValue = observedValues.Item2;
        }
    }
    [Serializable]
    public class ThreeCategoricalParentNodeCategorical : DistributionsNode {
        [NonSerialized] public VariableArray2D<VariableArray<Vector>, Vector[,][]> CPT;
        [NonSerialized] public VariableArray2D<VariableArray<Dirichlet>, Dirichlet[,][]> CPTPrior;
        public Dirichlet[,][] CPTPosterior;
        public ThreeCategoricalParentNodeCategorical(ModelNode node) {
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
        public override void LoadAfterSerialization(Range N) {
            this.N = N;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            Range parent3States = node.parents[2].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States, parent3States).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = CPTPosterior;
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
        public override void SetPriorToPosterior() {
            CPTPrior.IsReadOnly = false;
            CPTPrior.ObservedValue = CPTPosterior;
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
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
     * */
    /*
    public class OneCategoricalParentNodeSoftmaxCategorical : DistributionsNode {
        public VariableArray<VariableArray<Vector>, Vector[][]> B;
        public VariableArray<VariableArray<double>> m;
        VectorGaussian[][] bPost;
        Gaussian[][] meanPost;
        public VariableArray<Vector> CPT;
        public VariableArray<Dirichlet> CPTPrior;
        public Dirichlet[] CPTPosterior;
        public OneCategoricalParentNodeSoftmaxCategorical(ModelNode node) {

        }
        override public int ParentCount() {
            return 1;
        }
        public override void Infer(InferenceEngine engine) {
            CPTPosterior = engine.Infer<Dirichlet[]>(CPT);
        }
        public override void SetPriorToPosterior() {
            CPTPrior.ObservedValue = CPTPosterior;
            CPTPrior.ObservedValue = CPTPosterior;
        }

        public override void AddParents() {
            Observed = DistributionsNode.AddChildFromOneParent(node.parents[0].distributions.Observed, CPT).Named(node.name);
        }
        public override string PosteriorToString() {
            string str = "";
            for (int p1 = 0; p1 < node.parents[0].states.SizeAsInt; p1++) {
                for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                    str += node.name + " P(" + ii + "|" + p1 + ") = " + CPTPosterior[p1].GetMean()[ii] + "\n";
                }
            }
            return str;
        }
    }
    public class TwoCategoricalParentNodeSoftmaxCategorical : DistributionsNode {
        public VariableArray<VariableArray<Vector>, Vector[][]> CPT;
        public VariableArray<VariableArray<Dirichlet>, Dirichlet[][]> CPTPrior;
        public Dirichlet[][] CPTPosterior;
        public TwoCategoricalParentNodeSoftmaxCategorical(ModelNode node) {
            this.node = node;
            Range parent1States = node.parents[0].states;
            Range parent2States = node.parents[1].states;
            CPTPrior = Variable.Array(Variable.Array<Dirichlet>(parent1States), parent2States).Named("Prob" + node.name + "Prior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Enumerable.Repeat(Dirichlet.Uniform(node.states.SizeAsInt), parent1States.SizeAsInt).ToArray(), parent2States.SizeAsInt).ToArray(); ;
            CPT = Variable.Array(Variable.Array<Vector>(parent1States), parent2States).Named("Prob" + node.name);
            CPT[parent2States][parent1States] = Variable<Vector>.Random(CPTPrior[parent2States][parent1States]); //Softmax with feature vector 
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
                    for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                        str += node.name + " P(" + ii + "|" + p2 + "," + p1 + ") = " + CPTPosterior[p2][p1].GetMean()[ii] + "\n";
                    }
                }
            }
            return str;
        }
    }
    public class ThreeCategoricalParentNodeSoftmaxCategorical : DistributionsNode {
        public VariableArray2D<VariableArray<Vector>, Vector[,][]> CPT;
        public VariableArray2D<VariableArray<Dirichlet>, Dirichlet[,][]> CPTPrior;
        public Dirichlet[,][] CPTPosterior;
        public ThreeCategoricalParentNodeSoftmaxCategorical(ModelNode node) {
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
                        for (int ii = 0; ii < node.states.SizeAsInt - 1; ii++) {
                            str += node.name + " P(" + ii + "|" + p2 + "," + p3 + "," + p1 + ") = " + CPTPosterior[p2, p3][p1].GetMean()[ii] + "\n";
                        }
                    }
                }
            }
            return str;
        }
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
        }
    }
    
    public class FourCategoricalParentNodeCategorical : DistributionsNode {
        public VariableArray3D<VariableArray<Vector>, Vector[, ,][]> CPT;
        public VariableArray3D<VariableArray<Dirichlet>, Dirichlet[, ,][]> CPTPrior;
        public Dirichlet[, ,][] CPTPosterior;
        public FourCategoricalParentNodeCategorical(ModelNode node) {
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
        public override void SetObservedData(Tuple<int[], double[]> observedValues) {
            Observed.ObservedValue = observedValues.Item1;
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
     */
}
