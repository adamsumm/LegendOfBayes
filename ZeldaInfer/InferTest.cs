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
    class InferTest {
        public static void Test() {
            int[] observed = new int[]{21,27,41,35,47,54,59,19,19,16,23,35,19,30,31,37,66,20,27,25,35,28,62,31,
49,40,31,14,20,37,38,25,31,39};

            int[] pathObserved = new int[]{22,24,32,50,62,73,69,19,13,8,23,27,32,22,25,39,39,17,22,24,26,26,53,26,36,38,34,19,20,45,37,21,45,41};


            Range range = new Range(observed.Length);


            Variable<double> roomsMean = Variable.GammaFromMeanAndVariance(0.5, 0.5);
            Variable<int> rooms = Variable.Poisson(roomsMean);
            VariableArray<int> observedVar = Variable.Array<int>(range);
            observedVar[range] = rooms.ForEach(range);
            
            observedVar.ObservedValue = observed;
            Range roomsRange = new Range(100);
            Range pathRange = new Range(100);
            rooms.SetValueRange(roomsRange);
            observedVar.SetValueRange(roomsRange);
            VariableArray<Gamma> CPTPrior = Variable.Array<Gamma>(roomsRange).Named("ProbPrior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Gamma.FromShapeAndScale(1,1), pathRange.SizeAsInt).ToArray();
            VariableArray<double> CPT = Variable.Array<double>(roomsRange).Named("MoreQQLessPewPew");
            CPT[roomsRange] = Variable<double>.Random(CPTPrior[roomsRange]);
            CPT.SetValueRange(pathRange);
            var observedPaths = Variable.Array<int>(range);
            
            using (Variable.ForEach(range))
            using (Variable.Switch(observedVar[range]))
                observedPaths[range] = Variable.Poisson(CPT[observedVar[range]]);
            observedPaths.ObservedValue = pathObserved;
            InferenceEngine Engine = new InferenceEngine();
            Gamma posterior = Engine.Infer<Gamma>(roomsMean);
            Gamma[] CPTPosterior = Engine.Infer<Gamma[]>(CPT);
            string str = "";
            for (int p1 = 0; p1 < roomsRange.SizeAsInt; p1++) {
               // for (int ii = 0; ii < pathRange.SizeAsInt - 1; ii++) {
                    if (CPTPosterior[p1].GetMean() > 1) {
                        str += " E( path |" + p1 + ") = " + CPTPosterior[p1].GetMean() + "\n";
                       // str += " P(" + ii + "|" + p1 + ") = " + CPTPosterior[p1].GetMean() + "\n";
                    }
                
            }
            /*
            VariableArray<Dirichlet> CPTPrior = Variable.Array<Dirichlet>(roomsRange).Named("ProbPrior");
            CPTPrior.ObservedValue = Enumerable.Repeat(Dirichlet.Uniform(pathRange.SizeAsInt), pathRange.SizeAsInt).ToArray();
            VariableArray<Vector> CPT = Variable.Array<Vector>(roomsRange).Named("Prob");
            CPT[roomsRange] = Variable<Vector>.Random(CPTPrior[roomsRange]);
            CPT.SetValueRange(pathRange);
            var observedPaths = Variable.Array<int>(range);

            using (Variable.ForEach(range))
            using (Variable.Switch(observedVar[range]))
                observedPaths[range] = Variable.Discrete(CPT[observedVar[range]]);
            observedPaths.ObservedValue = pathObserved;
            InferenceEngine Engine = new InferenceEngine();
            Gamma posterior = Engine.Infer<Gamma>(roomsMean);
            Dirichlet[] CPTPosterior  = Engine.Infer<Dirichlet[]>(CPT);
            string str = "";
            for (int p1 = 0; p1 < roomsRange.SizeAsInt; p1++) {
                for (int ii = 0; ii < pathRange.SizeAsInt - 1; ii++) {
                    if (CPTPosterior[p1].GetMean()[ii] > 0.011) { 
                        str += " P(" + ii + "|" + p1 + ") = " + CPTPosterior[p1].GetMean()[ii] + "\n";
                    }
                }
            }
            */
            Console.WriteLine(posterior);
            Console.WriteLine(str);

        }
    }
}
