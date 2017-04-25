using System;
using System.Collections.Generic;
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
}