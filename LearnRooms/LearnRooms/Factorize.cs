using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Accord;
using Accord.Controls;
using Accord.Imaging;
using Accord.Imaging.Converters;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Math.Decompositions;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Statistics.Analysis;
namespace LearnRooms {
    public class Factorization {
        public static Tuple<Dictionary<string,Dictionary<int,List<Room>>>,Dictionary<string, double[,]>> Factorize(List<Room> rooms, int dimensionReduction) {
            Dictionary<string, double[,]> matrices = new Dictionary<string, double[,]>();
            int column = 0;
            int size = 0;
            foreach (var room in rooms) {
                foreach (var layer in room.objects) {
                    if (!matrices.ContainsKey(layer.Key)) {
                        size = layer.Value.GetLength(0) * layer.Value.GetLength(1);
                        matrices[layer.Key] = new double[rooms.Count, layer.Value.GetLength(0) * layer.Value.GetLength(1)];
                    }
                    matrices[layer.Key].FillColumn(column,layer.Value);
                }
                column++;
            }
            Dictionary<string, double[,]> Ws = new Dictionary<string, double[,]>();
            Dictionary<string, double[,]> Hs = new Dictionary<string, double[,]>();
            Dictionary<string, double[,]> components = new Dictionary<string, double[,]>();
            foreach (var mat in matrices) {
                PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(mat.Value);
                pca.Compute();
                for (int ii = 0; ii < dimensionReduction; ii++) {
                    pca.ComponentMatrix.rowToMatrix(ii, 12, 10).matToBitmap(0, 0).Save("pca" + mat.Key + ii + ".png");
                }
                components[mat.Key] = pca.ComponentMatrix;
                for (int jj = 0; jj < rooms.Count; jj++) {
                    rooms[jj].setCoefficients(mat.Key, pca.Result, jj, dimensionReduction);
                }
                /*
                NMF nmf = new NMF(mat.Value, dimensionReduction, 2000);
                Ws[mat.Key] = nmf.LeftNonnegativeFactors;
                Hs[mat.Key] = nmf.RightNonnegativeFactors;
                for (int ii = 0; ii < rooms.Count; ii++) {
                    rooms[ii].setCoefficients(mat.Key,nmf.RightNonnegativeFactors, ii);
                }
                string str = "";
                for (int xx = 0; xx < nmf.RightNonnegativeFactors.GetLength(1); xx++) {
                    for (int jj = 0; jj < nmf.RightNonnegativeFactors.GetLength(0); jj++) {
                        str += nmf.RightNonnegativeFactors[jj, xx] + ",";
                    }
                    str += "\n";
                }
                System.IO.File.WriteAllText(mat.Key + "W.txt", str);
                str = "";
                for (int xx = 0; xx < nmf.LeftNonnegativeFactors.GetLength(1); xx++) {
                    for (int jj = 0; jj < nmf.LeftNonnegativeFactors.GetLength(0); jj++) {
                        str += nmf.LeftNonnegativeFactors[jj, xx] + ",";
                    }
                    str += "\n";
                }
                System.IO.File.WriteAllText(mat.Key + "H.txt", str);
                for (int ii = 0; ii < nmf.LeftNonnegativeFactors.GetLength(1); ii++) {
                    double[,] W = nmf.LeftNonnegativeFactors.rowToMatrix(ii, 12, 10);
                    W.matToBitmap(0, 25).Save(mat.Key + ii + "W.png");
                }
                if (mat.Key == "blocks") {
                    double[,] reconstructed = new double[12, 10];
                    for (int ii = 0; ii < rooms[0].coefficients["blocks"].Length; ii++) {
                        double w = rooms[0].coefficients["blocks"][ii];
                        int counter = 0;
                        for (int xx = 0; xx < 12; xx++) {
                            for (int yy = 0; yy < 10; yy++) {
                                reconstructed[xx, yy] += w * Ws["blocks"][counter, ii];
                                counter++;
                            }
                        }
                    }
                    reconstructed.matToBitmap(0, 1).Save("room0Reconstructed.png");
                }
                 * */
            }
            int counter = 0;
         //   double[,] xy = new double[rooms.Count,2];
            double[][] clusterData = new double[rooms.Count][];
            foreach (var room in rooms) {
                int compCounter = 0;
                double[] coeffs = new double[room.coefficients.Count*dimensionReduction];
                foreach (var comp in room.coefficients) {
                    foreach (var coef in comp.Value) {
                        coeffs[compCounter] = coef;
                        compCounter++;
                    }
                }
                clusterData[counter] = coeffs;
                Room reconstructed = room.reconstruct(components,1);
                reconstructed.toBitmap().Save("room" + counter + "Reconstructed.png");
                counter++;
            }
            int numberofClusters = 30;
            KMeans kmeans = new KMeans(numberofClusters);
            kmeans.Tolerance = 0.25;
            int[] clusters = kmeans.Compute(clusterData);
            Dictionary<string, SortedSet<int>> clusteredRooms = new Dictionary<string, SortedSet<int>>();
            Dictionary<int, SortedSet<string>> roomClusters = new Dictionary<int, SortedSet<string>>();
            Dictionary<string, Dictionary<int, List<Room>>> output = new Dictionary<string, Dictionary<int, List<Room>>>();
            int[] clusterCounts = new int[numberofClusters];
            for (int ii = 0; ii < rooms.Count; ii++) {
                rooms[ii].setType();
                if (!clusteredRooms.ContainsKey(rooms[ii].roomType)){
                    output[rooms[ii].roomType] = new Dictionary<int, List<Room>>();
                    clusteredRooms[rooms[ii].roomType] = new SortedSet<int>();
                }
                if (!output[rooms[ii].roomType].ContainsKey(clusters[ii])) {
                    output[rooms[ii].roomType][clusters[ii]] = new List<Room>();
                }
                if (!roomClusters.ContainsKey(clusters[ii])) {
                    roomClusters[clusters[ii]] = new SortedSet<string>();
                }
                output[rooms[ii].roomType][clusters[ii]].Add(rooms[ii]);
                roomClusters[clusters[ii]].Add(rooms[ii].roomType);
                clusterCounts[clusters[ii]]++;
                clusteredRooms[rooms[ii].roomType].Add(clusters[ii]);
             //   Console.WriteLine(ii + " " + clusters[ii]);
            }
            for (int ii = 0; ii < clusterCounts.Length; ii++) {
                string str = "";
                foreach (var roomtype in roomClusters[ii]) {
                    str += roomtype + " ";
                }
             //   Console.WriteLine("Cluster "+ ii + " = " +clusterCounts[ii] + " : " + str);
            }
            foreach (var roomType in clusteredRooms) {
                string str = "";
                foreach (var cluster in roomType.Value) {
                    str += cluster + " ";
                }
             //   Console.WriteLine(roomType.Key + " " + str);

            }
            return new Tuple<Dictionary<string,Dictionary<int,List<Room>>>,Dictionary<string,double[,]>>(output,components);
        }
    }
    public static class MatrixExtensions{
        public static Bitmap matToBitmap(this double[,] me, double min, double max) {
            Bitmap output = new Bitmap(me.GetLength(0), me.GetLength(1));
            double maxVal = double.NegativeInfinity;
            double minVal = double.PositiveInfinity;
            for (int ii = 0; ii < me.GetLength(0); ii++) {
                for (int jj = 0; jj < me.GetLength(1); jj++) {
                    if (me[ii, jj] > maxVal) {
                        maxVal = me[ii, jj];
                    }
                    if (me[ii, jj] < minVal && me[ii,jj] > 0) {
                        minVal = me[ii, jj];
                    }
                 //   output.SetPixel(ii, jj, Color.FromArgb(color, color, color));
                }
            }
           // Console.WriteLine("maxVal = " + maxVal);
            if (maxVal > 0) {
                if (minVal >= maxVal) {
                    minVal = 0;
                }
                for (int ii = 0; ii < me.GetLength(0); ii++) {
                    for (int jj = 0; jj < me.GetLength(1); jj++) {
                        int color = (int)(255 * (me[ii, jj] - minVal) / (maxVal - minVal));
                       
                        /*if (me[ii, jj] > (minVal+maxVal)*0.015)
                            color = 255;
                         * */
                        color = color < 0 ? 0 : color;
                        color = color > 255 ? 255 : color;
                        output.SetPixel(ii, jj, Color.FromArgb(color, color, color));
                    }
                }
            }
            return output;
        }
        public static T[,] GetTranspose<T>(this T[,] me) {
            T[,] output = new T[me.GetLength(1),me.GetLength(0)];
            for (int ii = 0; ii < me.GetLength(1); ii++) {
                for (int jj = 0; jj < me.GetLength(0); jj++) {
                    output[ii, jj] = me[jj, ii];
                }
            }
            return output;
        }
        public static T[,] rowToMatrix<T>(this T[,] me, int row, int width, int height) {
            T[,] output = new T[width, height];
            int counter = 0;
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    output[ii,jj] = me[counter, row];
                    counter++;
                }
            }
            return output;
        }
        public static T[,] colToMatrix<T>(this T[,] me, int col, int width, int height) {
            T[,] output = new T[width, height];
            int counter = 0;
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    output[ii, jj] = me[col,counter];
                    counter++;
                }
            }
            return output;
        }
        public static T[,] vecToMatrix<T>(this T[] me,  int width, int height) {
            T[,] output = new T[width, height];
            int counter = 0;
            for (int ii = 0; ii < width; ii++) {
                for (int jj = 0; jj < height; jj++) {
                    output[ii, jj] = me[counter];
                    counter++;
                }
            }
            return output;
        }
        public static void FillColumn<T>(this T[,] me, int column, T[,] other) {
            int counter = 0;
            for (int ii = 0; ii < other.GetLength(0); ii++) {
                for (int jj = 0; jj < other.GetLength(1); jj++) {
                    me[column, counter] = other[ii, jj];
                    counter++;
                }
            }
        }
        public static void Fill<T>(this T[] me, T[,] other) {
            int counter = 0;
            for (int ii = 0; ii < other.GetLength(0); ii++) {
                for (int jj = 0; jj < other.GetLength(1); jj++) {
                    me[counter] = other[ii, jj];
                    counter++;
                }
            }
        }
    }
}
