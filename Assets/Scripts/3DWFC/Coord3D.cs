public struct Coord3D {

    public int X, Y, Z;

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

    public static Coord3D Left => new Coord3D(-1, 0, 0);

    public static Coord3D Right => new Coord3D(1, 0, 0);

    public static Coord3D Down => new Coord3D(0, -1, 0);

    public static Coord3D Up => new Coord3D(0, 1, 0);

    public static Coord3D Forward => new Coord3D(0, 0, 1);

    public static Coord3D Back => new Coord3D(0, 0, -1);
}
