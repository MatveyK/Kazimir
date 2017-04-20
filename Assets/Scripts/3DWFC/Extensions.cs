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
}