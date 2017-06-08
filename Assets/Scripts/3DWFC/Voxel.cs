using System.IO;


public struct Voxel {

    public byte X, Y, Z;

    public byte Color;

    public Voxel(BinaryReader stream) {

        X = stream.ReadByte();
        Z = stream.ReadByte();
        Y = stream.ReadByte();

        Color = stream.ReadByte();
    }
}