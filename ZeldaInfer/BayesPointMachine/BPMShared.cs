/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

namespace BayesPointMachine {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MicrosoftResearch.Infer;
    using MicrosoftResearch.Infer.Distributions;
    using MicrosoftResearch.Infer.Models;
    using MicrosoftResearch.Infer.Maths;
    using MicrosoftResearch.Infer.Utils;

    /// <summary>
    /// An example of a Bayes Point Machine (BPM) using dense features and shared weights.
    /// </summary>
    public class BPMShared {
        // Shared weights and their priors.
        private DistributionRefArray<VectorGaussian, Vector> weightsPrior;
        private SharedVariableArray<Vector> weights;

        // Range over all classes.
        private Range c;

        // Training and prediction models.
        private Model trainModel;
        private Model testModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BPMShared"/> class.
        /// </summary>
        /// <param name="numClasses">The number of classes.</param>
        /// <param name="noisePrecision">The precision of the noise.</param>
        /// <param name="numFeatures">The number of features.</param>
        /// <param name="numChunksTraining">The number of training set chunks.</param>
        /// <param name="numChunksTesting">The number of test set chunks.</param>
        public BPMShared(int numClasses, double noisePrecision, int numFeatures, int numChunksTraining, int numChunksTesting) {
            // Range over classes.
            this.c = new Range(numClasses).Named("c");

            // Setup shared weights and weights' prior.
            this.weightsPrior = InitializePrior(numClasses, numFeatures);
            this.weights = SharedVariable<Vector>.Random(this.c, this.weightsPrior).Named("w");

            // Configure models.
            this.trainModel = new Model(this.weights, this.c, numChunksTraining);
            this.testModel = new Model(this.weights, this.c, numChunksTesting);

            // Observe the noise precision.
            this.trainModel.noisePrecision.ObservedValue = noisePrecision;
            this.testModel.noisePrecision.ObservedValue = noisePrecision;
        }

        /// <summary>
        /// Trains the BPM on a given chunk of the training data, using a dense 
        /// feature representation.
        /// </summary>
        /// <param name="featureVectors">The feature vectors</param>
        /// <param name="labels">The corresponding labels</param>
        /// <param name="chunkNumber">The number of the chunk</param>
        /// <returns>A posterior distribution over weights</returns>
        public VectorGaussian[] Train(Vector[] featureVectors, int[] labels, int chunkNumber) {
            // Observe features and labels.
            this.trainModel.numItems.ObservedValue = featureVectors.Length;
            this.trainModel.x.ObservedValue = featureVectors;
            this.trainModel.y.ObservedValue = labels;

            // Infer the weights.
            this.trainModel.model.InferShared(this.trainModel.engine, chunkNumber);
            VectorGaussian[] posteriorWeights = this.weights.Marginal<VectorGaussian[]>();

            return posteriorWeights;
        }

        /// <summary>
        /// Predicts the labels for a given chunk of dense feature vectors.
        /// </summary>
        /// <param name="featureVectors">The dense feature vectors</param>
        /// <param name="chunkNumber">The number of the chunk</param>
        /// <returns>A posterior distribution over corresponding labels</returns>
        public Discrete[] Test(Vector[] featureVectors, int chunkNumber) {
            // Predict one item after the other.
            Discrete[] predictions = new Discrete[featureVectors.Length];
            for (int i = 0; i < featureVectors.Length; i++) {
                this.testModel.numItems.ObservedValue = 1;
                this.testModel.x.ObservedValue = new Vector[] { featureVectors[i] };

                // Infer labels.
                this.testModel.model.InferShared(this.testModel.engine, chunkNumber);
                predictions[i] = Distribution.ToArray<Discrete[]>(this.testModel.engine.Infer(this.testModel.y))[0];
            }
            return predictions;
        }

        private DistributionRefArray<VectorGaussian, Vector> InitializePrior(int numClasses, int numFeatures) {
            return (DistributionRefArray<VectorGaussian, Vector>)Distribution<Vector>.Array(
                Util.ArrayInit(numClasses, c => (c == 0) ?
                    VectorGaussian.PointMass(Vector.Zero(numFeatures)) :
                    VectorGaussian.FromMeanAndPrecision(Vector.Zero(numFeatures), PositiveDefiniteMatrix.Identity(numFeatures))));
        }


        #region Shared dense BPM model

        /// <summary>
        /// An Infer.NET model of a BPM using dense features and shared weights.
        /// </summary>
        private class Model {
            public Variable<int> numItems;
            public Range i;

            public VariableArray<Vector> x;
            public Variable<double> noisePrecision;
            public VariableArray<double> score;
            public VariableArray<int> y;

            public MicrosoftResearch.Infer.Models.Model model;
            public VariableArray<Vector> wModel;

            public InferenceEngine engine = new InferenceEngine();

            public Model(SharedVariableArray<Vector> w, Range c, int numChunks) {
                // Items.
                numItems = Variable.New<int>().Named("numItems");
                i = new Range(numItems).Named("i");
                i.AddAttribute(new Sequential());

                // The model identifier for the shared variables.
                model = new MicrosoftResearch.Infer.Models.Model(numChunks).Named("model");
                // The weight vector for each submodel.
                wModel = w.GetCopyFor(model).Named("wModel");

                noisePrecision = Variable.New<double>().Named("noisePrecision");

                // Arrays of <see cref="Vector"/>-valued items (feature vectors) and integer labels.
                x = Variable.Array<Vector>(i).Named("x");
                y = Variable.Array<int>(i).Named("y");

                // For all items...
                using (Variable.ForEach(i)) {
                    // ...compute the score of this item across all classes...
                    score = BPMUtils.ComputeClassScores(wModel, x[i], noisePrecision);
                    y[i] = Variable.DiscreteUniform(c);

                    // ... and constrain the output.
                    BPMUtils.ConstrainMaximum(y[i], score);
                }

                // Inference engine settings (EP).
                engine.Compiler.UseSerialSchedules = true;
                engine.ShowProgress = false;
            }
        }

        #endregion
    }
}
