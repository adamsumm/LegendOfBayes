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


using BayesPointMachine;
namespace ZeldaInfer {
    class InferTest {
        public static void Test2() {
            double[] observed = new double[]{21,27,41,35,47,54,59,19,19,16,23,35,19,30,31,37,66,20,27,25,35,28,62,31,
                                                49,40,31,14,20,37,38,25,31,39};

            double[] pathObserved = new double[] { 22, 24, 32, 50, 62, 73, 69, 19, 13, 8, 23, 27, 32, 22, 
                                                    25, 39, 39, 17, 22, 24, 26, 26, 53, 26, 36, 38, 34, 19, 20, 45, 37, 21, 45, 41 };

            int[] classes = new int[]{ 2, 2, 3, 5, 6, 7, 6, 1, 1, 0, 2, 2, 3, 2, 
                                        2, 3, 3, 1, 2, 2, 2, 2, 5, 2, 3, 3, 3, 1, 2, 4, 3, 2, 4, 4 };
            int[][] classesCounts = new int[classes.Length][];
            for (int ii = 0; ii < classes.Length; ii++) {
                classesCounts[ii] = new int[8];
                classesCounts[ii][classes[ii]] = 1;
                
            }

            Vector[] xdata = new Vector[observed.Length];
            for (int i = 0; i < xdata.Length; i++)
                xdata[i] = Vector.FromArray(observed[i], pathObserved[i]);

            Range range = new Range(observed.Length);


            Variable<Gaussian> meanPrior = Gaussian.FromMeanAndVariance(0, 100);
            Variable<double> roomsMean = Variable<double>.Random(meanPrior).Named("room Mean");
            Variable<double> roomsStd = Variable.GammaFromMeanAndVariance(2,2).Named("room sigma");
            VariableArray<double> observedVar = Variable.Array<double>(range).Named("observedVar");
            Variable<double> roomGauss = Variable.GaussianFromMeanAndPrecision(roomsMean, roomsStd).Named("roomGauss");
            observedVar[range] = roomGauss.ForEach(range);


            Variable<double> pathsMean = Variable.GaussianFromMeanAndVariance(0, 200).Named("path Mean");
            Variable<double> pathsStd = Variable.GammaFromMeanAndVariance(1, 1.5).Named("path sigma");
            VariableArray<double> observedPath = Variable.Array<double>(range).Named("observedPath");
            observedPath[range] = observedVar[range] + (Variable.GaussianFromMeanAndPrecision(pathsMean, pathsStd)).ForEach(range);


            observedVar.ObservedValue = observed;
            observedPath.ObservedValue = pathObserved;
            int C = 8;
            var c = new Range(C);
            int K = 2;
            var B = Variable.Array<Vector>(c).Named("coefficients");
            B[c] = Variable.VectorGaussianFromMeanAndPrecision(
                Vector.Zero(K), PositiveDefiniteMatrix.Identity(K)).ForEach(c);
            var m = Variable.Array<double>(c).Named("mean");
            m[c] = Variable.GaussianFromMeanAndPrecision(0, 1).ForEach(c);
            Variable.ConstrainEqualRandom(B[C - 1], VectorGaussian.PointMass(Vector.Zero(K)));
            Variable.ConstrainEqualRandom(m[C - 1], Gaussian.PointMass(0));
            var x = Variable.Array<Vector>(range).Named("x");
            x.ObservedValue = xdata;
            //var x = Variable.Array(Variable.Array<double>(c), range).Named("x");
          //  Variable.ConstrainEqual(x[range][0], observedVar[range]);
          //  Variable.ConstrainEqual(x[range][1], observedPath[range]);
           // x[range][1] = observedPath[range];
        //    var x = xTemp;// Variable.Vector(xTemp[range]);
            var yData = Variable.Array(Variable.Array<int>(c), range).Named("y");
            yData.ObservedValue = classesCounts;
            var trialsCount = Variable.Array<int>(range).Named("trialsCount");
            trialsCount.ObservedValue = classesCounts.Select(o => o.Sum()).ToArray();
            var g = Variable.Array(Variable.Array<double>(c), range).Named("g");
            g[range][c] = Variable.InnerProduct(B[c], (x[range])) + m[c];
            var p = Variable.Array<Vector>(range).Named("p");
            p[range] = Variable.Softmax(g[range]);
            
            using (Variable.ForEach(range))
                yData[range] = Variable.Multinomial(trialsCount[range], p[range]);

            // inference
          //  var ie = new InferenceEngine(new VariationalMessagePassing());



            InferenceEngine Engine = new InferenceEngine(new VariationalMessagePassing());
            Gaussian posterior = Engine.Infer<Gaussian>(roomsMean);
            Gamma posteriorStd = Engine.Infer<Gamma>(roomsStd);

            Gaussian posteriorPath = Engine.Infer<Gaussian>(pathsMean);
            Gamma posteriorStdPath = Engine.Infer<Gamma>(pathsStd);
            Console.WriteLine(posterior + " " + posteriorStd);
            Console.WriteLine(posteriorPath + " " + posteriorStdPath);
            var bPost = Engine.Infer<VectorGaussian[]>(B);
            var meanPost = Engine.Infer<Gaussian[]>(m); 
            for (int i = 0; i < C; i++) {
                Console.WriteLine("C " + bPost[i].GetMean());
                Console.WriteLine("m " + meanPost[i].GetMean());
            }

            for (int ii = 0; ii < classes.Length; ii++) {
                for (int i = 0; i < C; i++) {
                     Console.WriteLine("C " + i + " " + (meanPost[i].GetMean() + bPost[i].GetMean()[0] * observed[ii] + bPost[i].GetMean()[1] * pathObserved[ii]) + " - " + classes[ii]);
                }
            }
          //  Engine.Compiler.GivePriorityTo(typeof(SaulJordanSoftmaxOp_NCVMP));

        }






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
