using System.Collections.Generic;
using System.IO;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_Elsewhere

// ReSharper disable UnusedVariable

public class VoxReaderWriter {

    private static int modelX;
    private static int modelY;
    private static int modelZ;

    private static InputModel ReadVoxelStream(BinaryReader stream) {
        var voxels = new List<Voxel>();

        string VOX = new string(stream.ReadChars(4));
        int version = stream.ReadInt32();

        while (stream.BaseStream.Position < stream.BaseStream.Length) {

            char[] chunkId = stream.ReadChars(4);

            int chunkSize = stream.ReadInt32();
            int childChunks = stream.ReadInt32();
            string chunkName = new string(chunkId);

            switch (chunkName) {
                case "PACK":
                    int numModels = stream.ReadInt32();
                    break;
                case "SIZE":
                    modelX = stream.ReadInt32();
                    modelY = stream.ReadInt32();
                    modelZ = stream.ReadInt32();
                    stream.ReadBytes(chunkSize - 4 * 3);
                    break;
                case "XYZI":
                    int numVoxels = stream.ReadInt32();
                    for(var i = 0; i < numVoxels; i++) voxels.Add(new Voxel(stream));
                    break;
                case "RGBA":
                    //TODO Treat the RGBA chunks
                    stream.Close();
                    return new InputModel(new Coord3D(modelX, modelY, modelZ), voxels);
            }
        }

        return new InputModel(new Coord3D(modelX, modelY, modelZ), voxels);
    }

    public static InputModel ReadVoxelFile(string fileName) {
        return ReadVoxelStream(new BinaryReader(File.Open(fileName, FileMode.Open)));
    }

    private static void WriteVoxelStream(BinaryWriter writer, int sizeX, int sizeY, int sizeZ, ICollection<Voxel> voxels) {
        //VOX with a space and the version number.
        writer.WriteString("VOX ");
        writer.Write(150);

        //MAIN chunk.
        writer.WriteString("MAIN");
        writer.Write(0);
        writer.Write((4 * 10) + voxels.Count * 4); //Number of SIZE + XYZI bytes + 4 bytes per voxel

        //SIZE chunk.
        writer.WriteString("SIZE");
        writer.Write(4 * 3);
        writer.Write(0);
        writer.Write(sizeX);
        writer.Write(sizeY);
        writer.Write(sizeZ);

        //XYZI chunk.
        writer.WriteString("XYZI");
        writer.Write(4 * (voxels.Count + 1));
        writer.Write(0);
        writer.Write(voxels.Count);
        foreach (var voxel in voxels) {
            writer.Write(voxel.X);
            writer.Write(voxel.Y);
            writer.Write(voxel.Z);
            writer.Write(voxel.Color);
        }

        writer.Close();
    }

    public static void WriteVoxelFile(string fileName, int sizeX, int sizeY, int sizeZ, ICollection<Voxel> voxels) {
        WriteVoxelStream(new BinaryWriter(File.Open(fileName, FileMode.Create)), sizeX, sizeY, sizeZ, voxels);
    }

    public static Coord3D ModelSize() {
        return new Coord3D(modelX, modelY, modelZ);
    }
}