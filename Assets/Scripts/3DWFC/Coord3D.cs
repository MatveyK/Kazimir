public struct Coord3D {

    public int x, y, z;

    public Coord3D(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Coord3D Add(Coord3D coord) {
        return new Coord3D(this.x + coord.x, this.y + coord.y, this.z + coord.z);
    }

    public static Coord3D Left {
        get {
            return new Coord3D(-1, 0, 0);
        }
    }

    public static Coord3D Right {
        get {
            return new Coord3D(1, 0, 0);
        }
    }

    public static Coord3D Down {
        get {
            return new Coord3D(0, -1, 0);
        }
    }

    public static Coord3D Up {
        get {
            return new Coord3D(0, 1, 0);
        }
    }

    public static Coord3D Forward {
        get {
            return new Coord3D(0, 0, 1);
        }
    }

    public static Coord3D Back {
        get {
            return new Coord3D(0, 0, -1);
        }
    }
}
