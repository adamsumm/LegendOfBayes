using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
public class EqualityComparison<T> : IEqualityComparer<T> {
    public EqualityComparison(Func<T, T, bool> cmp) {
        this.cmp = cmp;
    }
    public bool Equals(T x, T y) {
        return cmp(x, y);
    }

    public int GetHashCode(T obj) {
        return obj.GetHashCode();
    }

    public Func<T, T, bool> cmp { get; set; }
}
public static class Extensions {
    public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable {
        return listToClone.Select(item => (T)item.Clone()).ToList();
    }
    public static void Fill(this Bitmap bmp, Color color) {
        for (int ii = 0; ii < bmp.Width; ii++) {
            for (int jj = 0; jj < bmp.Height; jj++) {
                bmp.SetPixel(ii, jj, color);
            }
        }
    }
    public static void Fill<T>(this T[,] arr, T val) {
        for (int ii = 0; ii < arr.GetLength(0); ii++) {
            for (int jj = 0; jj < arr.GetLength(1); jj++) {
                arr[ii, jj] = val;
            }
        }
    }
    public static int IndexOf<T>(this T[] arr, T val, EqualityComparison<T> comparer) {

        for (int ii = 0; ii < arr.GetLength(0); ii++) {
            if (comparer.Equals(arr[ii], val)) {
                return ii;
            }
        }
        return -1;
    }
    public static Tuple<int,int> IndexOf<T>(this T[,] arr, T val) {

        for (int ii = 0; ii < arr.GetLength(0); ii++) {
            for (int jj = 0; jj < arr.GetLength(1); jj++) {
                if (EqualityComparer<T>.Default.Equals(arr[ii, jj], val)) {
                    return new Tuple<int, int>(ii, jj);
                }
            }
        }
        return null;
    }
    public static void Shuffle<T>(this IList<T> list, Random rng) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static T GetRandom<T>(this IList<T> list) {
        Random rng = new Random();
        return list[rng.Next(list.Count)];
    }
    public static int IndexOf<T>(this T[] arr,T thing,EqualityComparer<T> comparer = null ) {
        if (comparer == null) {
            comparer = EqualityComparer<T>.Default;
        }
        for (int ii = 0; ii < arr.Length; ii++) {
            if (comparer.Equals(arr[ii], thing)) {
                return ii;
            }
        }
        return -1;
    }
} 