using System;

public struct Coord3D : IEquatable<Coord3D> {

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public Coord3D(int x, int y, int z) {
        X = x;
        Y = y;
        Z = z;
    }

    public Coord3D Add(Coord3D coord) {
        return new Coord3D(X + coord.X, Y + coord.Y, Z + coord.Z);
    }

    public Coord3D Add(int x, int y, int z) {
        return new Coord3D(X + x, Y + y, Z + z);
    }

    public override int GetHashCode() {
        return new {X, Y, Z }.GetHashCode();
    }

    public override bool Equals(object obj) {
        return obj is Coord3D && Equals((Coord3D) obj);
    }

    public bool Equals(Coord3D other) {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public static Coord3D Left => new Coord3D(-1, 0, 0);

    public static Coord3D Right => new Coord3D(1, 0, 0);

    public static Coord3D Down => new Coord3D(0, -1, 0);

    public static Coord3D Up => new Coord3D(0, 1, 0);

    public static Coord3D Forward => new Coord3D(0, 0, 1);

    public static Coord3D Back => new Coord3D(0, 0, -1);
}
