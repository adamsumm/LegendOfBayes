/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

namespace BayesPointMachine {
    using System;
    using System.Collections.Generic;

    using MicrosoftResearch.Infer;
    using MicrosoftResearch.Infer.Distributions;
    using MicrosoftResearch.Infer.Maths;
    using MicrosoftResearch.Infer.Models;
    using MicrosoftResearch.Infer.Utils;

    /// <summary>
    /// An example of a Bayes Point Machine (BPM) using dense features.
    /// </summary>
    public class BPM {
        // Training and prediction models.
        private Model trainModel;
        private Model testModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BPM"/> class.
        /// </summary>
        /// <param name="numClasses">The number of classes.</param>
        /// <param name="noisePrecision">The precision of the noise.</param>
        public BPM(int numClasses, double noisePrecision) {
            this.trainModel = new Model();
            this.testModel = new Model();

            // Observe the number of classes.
            this.trainModel.numClasses.ObservedValue = numClasses;
            this.testModel.numClasses.ObservedValue = numClasses;

            // Observe the noise precision.
            this.trainModel.noisePrecision.ObservedValue = noisePrecision;
            this.testModel.noisePrecision.ObservedValue = noisePrecision;
        }

        /// <summary>
        /// Trains the BPM using a dense feature representation.
        /// </summary>
        /// <param name="featureVectors">The dense feature vectors</param>
        /// <param name="labels">The corresponding labels</param>
        /// <returns>A posterior distribution over weights</returns>
        public VectorGaussian[] Train(Vector[] featureVectors, int[] labels) {
            // Initialize weight priors if necessary.
            if (!this.trainModel.wPrior.IsObserved) {
                int numFeatures = featureVectors[0].Count;
                int numClasses = this.trainModel.numClasses.ObservedValue;
                this.trainModel.wPrior.ObservedValue = InitializePrior(numClasses, numFeatures);
            }

            // Observe features and labels.
            this.trainModel.numItems.ObservedValue = featureVectors.Length;
            this.trainModel.x.ObservedValue = featureVectors;
            this.trainModel.y.ObservedValue = labels;

            // Infer the weights.
            VectorGaussian[] posteriorWeights = this.trainModel.engine.Infer<VectorGaussian[]>(this.trainModel.w);

            // Store posterior weights in prior.
            this.trainModel.wPrior.ObservedValue = posteriorWeights;

            return posteriorWeights;
        }

        /// <summary>
        /// Predicts the labels for some given dense feature vectors.
        /// </summary>
        /// <param name="featureVectors">The dense feature vectors</param>
        /// <returns>A posterior distribution over corresponding labels</returns>
        public Discrete[] Test(Vector[] featureVectors) {
            // Store weight prior from training as weight prior for prediction.
            this.testModel.wPrior.ObservedValue = this.trainModel.wPrior.ObservedValue;

            // Predict one item after the other.
            Discrete[] predictions = new Discrete[featureVectors.Length];
            for (int i = 0; i < featureVectors.Length; i++) {
                // Observe a single feature vector.
                this.testModel.numItems.ObservedValue = 1;
                this.testModel.x.ObservedValue = new Vector[] { featureVectors[i] };

                // Infer the posterior probabilities for its label.
                predictions[i] = this.testModel.engine.Infer<IList<Discrete>>(this.testModel.y)[0];
            }
            return predictions;
        }

        private VectorGaussian[] InitializePrior(int numClasses, int numFeatures) {
            return Util.ArrayInit(numClasses, c => (c == 0) ?
                    VectorGaussian.PointMass(Vector.Zero(numFeatures)) :
                    VectorGaussian.FromMeanAndPrecision(Vector.Zero(numFeatures), PositiveDefiniteMatrix.Identity(numFeatures)));
        }

        #region Dense BPM model

        /// <summary>
        /// An Infer.NET model of a BPM using dense features.
        /// </summary>
        private class Model {
            public Variable<int> numClasses;
            public Range c;
            public Variable<int> numItems;
            public Range i;

            public VariableArray<VectorGaussian> wPrior;
            public VariableArray<Vector> w;
            public VariableArray<Vector> x;
            public Variable<double> noisePrecision;
            public VariableArray<double> score;
            public VariableArray<int> y;

            public InferenceEngine engine = new InferenceEngine();

            public Model() {
                // Classes.
                numClasses = Variable.New<int>().Named("numClasses");
                c = new Range(numClasses).Named("c");

                // Items.
                numItems = Variable.New<int>().Named("numItems");
                i = new Range(numItems).Named("i");
                i.AddAttribute(new Sequential());

                // The prior distribution for weight vector for each class. When
                // <see cref="Test"/> is called, this is set to the posterior weight
                // distributions from <see cref="Train"/>.
                wPrior = Variable.Array<VectorGaussian>(c).Named("wPrior");

                // The weight vector for each class.
                w = Variable.Array<Vector>(c).Named("w");
                w[c] = Variable<Vector>.Random(wPrior[c]);

                noisePrecision = Variable.New<double>().Named("noisePrecision");

                // Arrays of <see cref="Vector"/>-valued items (feature vectors) and integer labels.
                x = Variable.Array<Vector>(i).Named("x");
                y = Variable.Array<int>(i).Named("y");

                // For all items...
                using (Variable.ForEach(i)) {
                    // ...compute the score of this item across all classes...
                    score = BPMUtils.ComputeClassScores(w, x[i], noisePrecision);
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

