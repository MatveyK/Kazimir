using System.Collections.Generic;

public static class Extensions {

    public static bool OutOfBounds<T>(this T[,,] array, int x, int y, int z) {
        if (x >= array.GetLowerBound(0) && x <= array.GetUpperBound(0) &&
            y >= array.GetLowerBound(1) && y <= array.GetUpperBound(1) &&
            z >= array.GetLowerBound(2) && z <= array.GetUpperBound(2)) {
            return false;
        } else {
            return true;
        }
    }
}