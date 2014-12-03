using System;
using System.Collections.Generic;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Collections;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Graphs;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Transforms;
using MicrosoftResearch.Infer.Utils;
using MicrosoftResearch.Infer.Views;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[assembly: MicrosoftResearch.Infer.Factors.HasMessageFunctions]

namespace ZeldaInfer {
    public static class InferFactors {
        [ParameterNames("Histogramize", "Val")]
        public static int Histogramize(double value,double mean, double deviation, int numberOfBins) {
            
            for (int ii = 1; ii <= numberOfBins; ii++) {
                double above = MMath.NormalCdfInv(((double)ii)/((double)numberOfBins));
                if ( ((value - mean) / deviation) < above){
                    return ii - 1;
                }
            }
            return numberOfBins-1;
        }
    }

    [FactorMethod(typeof(InferFactors), "Histogramize")]
    public static class HistagramizeOp {

    }
}
