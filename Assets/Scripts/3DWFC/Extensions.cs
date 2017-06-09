using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = System.Random;

public static class Extensions {
    
    private static readonly Random rng = new Random();

    public static bool OutOfBounds<T>(this T[,,] array, Vector3 coords) {
        if (coords.x >= array.GetLowerBound(0) && coords.x <= array.GetUpperBound(0) &&
            coords.y >= array.GetLowerBound(1) && coords.y <= array.GetUpperBound(1) &&
            coords.z >= array.GetLowerBound(2) && coords.z <= array.GetUpperBound(2)) {
            return false;
        } else {
            return true;
        }
    }

    public static bool OutOfBounds<T>(this T[,,] array, Coord3D coords) {
        if (coords.X >= array.GetLowerBound(0) && coords.X <= array.GetUpperBound(0) &&
            coords.Y >= array.GetLowerBound(1) && coords.Y <= array.GetUpperBound(1) &&
            coords.Z >= array.GetLowerBound(2) && coords.Z <= array.GetUpperBound(2)) {
            return false;
        } else {
            return true;
        }
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector) {

        HashSet<TKey> seenKeys = new HashSet<TKey>();
        foreach (TSource element in source) {
            if (seenKeys.Add(keySelector(element))) {
                yield return element;
            }
        }
    }

    public static IEnumerable<T> Shuffle<T>(this IList<T> list) {
        var res = list;
        var n = res.Count;
        while (n > 1) {
            n--;
            var k = rng.Next(n + 1);
            var value = res[k];
            res[k] = res[n];
            res[n] = value;
        }

        return res;
    }

    public static void WriteString(this BinaryWriter writer, string str) {
        foreach (var chr in str) {
            writer.Write(chr);
        }
    }

    public static int ContainsPattern(this List<byte[,,]> patternList, byte[,,] pattern) {
        //If the list is empty return false.
        if (patternList.Count == 0) return -1;

        var index = 0;
        
        foreach (var patt in patternList) {
            var sameVox = 0;
            for (var x = 0; x < pattern.GetLength(0); x++) {
                for (var y = 0; y < pattern.GetLength(1); y++) {
                    for (var z = 0; z < pattern.GetLength(2); z++) {
                        if (patt[x, y, z] == pattern[x, y, z]) {
                            sameVox++;
                        }
                    }
                }
            }
            if (sameVox == pattern.Length) return index;
            index++;
        }

        return -1;
    }

    public static bool FitsPattern(this byte[,,] pattern, byte[,,] otherPattern, Coord3D side) {
        int startX = 0;
        int startY = 0;
        int startZ = 0;

        int endX = pattern.GetLength(0);
        int endY = pattern.GetLength(1);
        int endZ = pattern.GetLength(2);

        int startX2 = 0;
        int startY2 = 0;
        int startZ2 = 0;

        int endX2 = pattern.GetLength(0);
        int endY2 = pattern.GetLength(1);
        int endZ2 = pattern.GetLength(2);

        if (side.Equals(Coord3D.Left)) {
            
            startX = pattern.GetLength(0) - 1;
            endX2 = 1;

        } else if (side.Equals(Coord3D.Right)) {
            
            endX = 1;
            startX2 = pattern.GetLength(0) - 1;

        } else if (side.Equals(Coord3D.Down)) {

            endY = 1;
            startY2 = pattern.GetLength(1) - 1;

        } else if (side.Equals(Coord3D.Up)) {

            startY = pattern.GetLength(1) - 1;
            endY2 = 1;

        } else if (side.Equals(Coord3D.Back)) {

            startZ = pattern.GetLength(2) - 1;
            endZ2 = 1;

        } else if (side.Equals(Coord3D.Forward)) {

            endZ = 1;
            startZ2 = pattern.GetLength(2) - 1;

        }

        var loopX2 = startX2;
        var loopY2 = startY2;
        var loopZ2 = startZ2;
        
        for (var x = startX; x < endX; x++) {
            for (var y = startY; y < endY; y++) {
                for (var z = startZ; z < endZ; z++) {
                    if (pattern[x, y, z] != otherPattern[loopX2, loopY2, loopZ2])
                        return false;

                    loopZ2++;
                }
                loopZ2 = startZ2;
                loopY2++;
            }
            loopY2 = startY2;
            loopX2++;
        }

        return true;
    }

    public static List<int>[,,] CloneMatrix(this List<int>[,,] matrix) {
        var res = new List<int>[matrix.GetLength(0), matrix.GetLength(1), matrix.GetLength(2)];
        
        for (var x = 0; x < matrix.GetLength(0); x++) {
            for (var y = 0; y < matrix.GetLength(1); y++) {
                for (var z = 0; z < matrix.GetLength(2); z++) {
                    res[x, y, z] = new List<int>();
                    foreach (var value in matrix[x, y, z]) {
                        res[x, y, z].Add(value);
                    }
                }
            }
        }
        return res;
    }

    public static int GenerateRand(int mean, int stdDev) {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();

        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

        return (int) Math.Ceiling(mean + stdDev * randStdNormal);
    }
}