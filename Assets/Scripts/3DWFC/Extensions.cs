using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Extensions {

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
        if (coords.x >= array.GetLowerBound(0) && coords.x <= array.GetUpperBound(0) &&
            coords.y >= array.GetLowerBound(1) && coords.y <= array.GetUpperBound(1) &&
            coords.z >= array.GetLowerBound(2) && coords.z <= array.GetUpperBound(2)) {
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

    public static void WriteString(this BinaryWriter writer, string str) {
        foreach (var chr in str) {
            writer.Write(chr);
        }
    }

    public static int ContainsPattern(this List<byte[,,]> patternList, byte[,,] pattern) {
        var index = -1;

        //If the list is empty return false.
        if (patternList.Count == 0) return -1;

        foreach (var patt in patternList) {
            for (var x = 0; x < pattern.GetLength(0); x++) {
                for (var y = 0; y < pattern.GetLength(1); y++) {
                    for (var z = 0; z < pattern.GetLength(2); z++) {
                        if (patt[x, y, z] != pattern[x, y, z]) {
                            return -1;
                        }
                    }
                }
            }
            index++;
        }
        return index;
    }
}