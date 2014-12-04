/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

namespace BayesPointMachine {
    using System;
    using System.Collections.Generic;

    using MicrosoftResearch.Infer;
    using MicrosoftResearch.Infer.Factors;
    using MicrosoftResearch.Infer.Maths;
    using MicrosoftResearch.Infer.Models;
    using MicrosoftResearch.Infer.Utils;

    using VariableArray2DDouble = MicrosoftResearch.Infer.Models.VariableArray<MicrosoftResearch.Infer.Models.VariableArray<double>, double[][]>;

    /// <summary>
    /// Computes class scores and defines constraints for both dense and sparse feature representations.
    /// </summary>
    public class BPMUtils {
        /// <summary>
        /// Computes the class scores for dense features.
        /// </summary>
        /// <param name="weights">Weights (per class)</param>
        /// <param name="features">Vector of values</param>
        /// <param name="noisePrecision">Noise precision</param>
        /// <returns>Score for each class</returns>
        public static VariableArray<double> ComputeClassScores(VariableArray<Vector> weights, Variable<Vector> features, Variable<double> noisePrecision) {
            Range c = weights.Range.Clone().Named("k");
            VariableArray<double> score = Variable.Array<double>(c).Named("score");
            VariableArray<double> scorePlusNoise = Variable.Array<double>(c).Named("scorePlusNoise");
            score[c] = Variable.InnerProduct(weights[c], features);
            scorePlusNoise[c] = Variable.GaussianFromMeanAndPrecision(score[c], noisePrecision);
            return scorePlusNoise;
        }

        /// <summary>
        /// Computes the class scores for sparse features.
        /// </summary>
        /// <param name="weights">Weight array per class</param>
        /// <param name="values">Array of values</param>
        /// <param name="indices">Array of indices</param>
        /// <param name="f">Feature range</param>
        /// <param name="noisePrecision">Noise precision</param>
        /// <returns>Score for each class</returns>
        public static VariableArray<double> ComputeClassScores(
            VariableArray2DDouble weights,
            VariableArray<double> values,
            VariableArray<int> indices,
            Range f,
            Variable<double> noisePrecision) {
            Range c = weights.Range.Clone().Named("k");
            VariableArray<double> score = Variable.Array<double>(c).Named("score");
            VariableArray<double> scorePlusNoise = Variable.Array<double>(c).Named("scorePlusNoise");
            VariableArray2DDouble wSparse = Variable.Array(Variable.Array<double>(f), c).Named("wSparse");
            VariableArray2DDouble product = Variable.Array(Variable.Array<double>(f), c).Named("product");
            wSparse[c] = Variable.Subarray<double>(weights[c], indices);
            product[c][f] = values[f] * wSparse[c][f];
            score[c] = Variable.Sum(product[c]);
            scorePlusNoise[c] = Variable.GaussianFromMeanAndPrecision(score[c], noisePrecision);

            return scorePlusNoise;
        }

        /// <summary>
        /// Builds a multiclass switch for the specified integer variable
        /// which builds a set of <see cref="ConstrainArgMax"/> constraints based
        /// on the value of the variable.
        /// </summary>
        /// <param name="argmax">The specified integer variable</param>
        /// <param name="score">The vector of score variables</param>
        public static void ConstrainMaximum(Variable<int> argmax, VariableArray<double> score) {
            Range c = score.Range.Clone();
            using (var cBlock = Variable.ForEach(c)) {
                using (Variable.If(argmax == cBlock.Index)) {
                    ConstrainArgMax(cBlock.Index, score);
                }
            }
        }

        /// <summary>
        /// Constrains the score for the specified class to be larger 
        /// than all the scores at the other classes.
        /// </summary>
        /// <param name="argmax">The specified integer variable</param>
        /// <param name="score">The vector of score variables</param>
        public static void ConstrainArgMax(Variable<int> argmax, VariableArray<double> score) {
            Range c = score.Range;
            using (var cBlock = Variable.ForEach(c)) {
                using (Variable.IfNot(cBlock.Index == argmax)) {
                    Variable.ConstrainPositive((score[argmax] - score[cBlock.Index]).Named("scoreDiff"));
                }
            }
        }
    }
}
