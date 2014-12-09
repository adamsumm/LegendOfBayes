using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Accord.Imaging.Converters;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.DensityKernels;

namespace LearnRooms {
    class Tile {
        public double[][] pixels;
        public Bitmap tileImage;
        public Tile(int width, int height, double[][] pixels) {
            this.pixels = pixels;
            ArrayToImage arrayToImage = new ArrayToImage(width, height, min: -1, max: +1);

            arrayToImage.Convert(pixels,out tileImage);
        }
        public static Tile[][] getTiles(Bitmap source, int width, int height) {
            int tileWidth = source.Width / width;
            int tileHeight = source.Height / height;
            Tile[][] tiles = new Tile[tileWidth * tileHeight][];
            ImageToArray imageToArray = new ImageToArray(min: -1, max: +1);
            double[][] pixels; imageToArray.Convert(source, out pixels);
            for (int yy = 0; yy < tileHeight; yy++) {
                for (int xx = 0; xx < tileWidth; xx++) {
                    tiles[yy * tileWidth + xx] = new Tile[]{new Tile(width,height,getPixels(pixels, xx * width, width, yy * height, height, source.Width))};
                }
            }

            return tiles;
        }
        public static double[][] getPixels(double[][] source, int xx, int width, int yy, int height, int imageWidth) {
            double[][] outPixels = new double[width * height][];
            for (int ii = 0; ii < height; ii++) {
                for (int jj = 0; jj < width; jj++) {
                    outPixels[ii * width + jj] = source[(ii + yy) * imageWidth + (jj + xx)];
                }
            }
            return outPixels;
        }
        public static double GetDistance(Tile[] a, Tile[] b) {
            double SSE = 0;
            for (int ii = 0; ii < a[0].pixels.Length; ii++) {
                for (int jj = 0; jj < a[0].pixels[ii].Length; jj++) {
                    SSE += Math.Pow(a[0].pixels[ii][jj] - b[0].pixels[ii][jj], 2.0);
                }
            }
            return Math.Sqrt(SSE);
        } 
    }  
}
