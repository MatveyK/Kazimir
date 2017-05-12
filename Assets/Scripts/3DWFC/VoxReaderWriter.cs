using System.Collections.Generic;
using System.IO;
using System.Text;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_Elsewhere

public class VoxReaderWriter {

    private static List<Voxel> ReadVoxelStream(BinaryReader stream) {
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
                    int x = stream.ReadInt32();
                    int y = stream.ReadInt32();
                    int z = stream.ReadInt32();
                    stream.ReadBytes(chunkSize - 4 * 3);
                    break;
                case "XYZI":
                    int numVoxels = stream.ReadInt32();
                    for(var i = 0; i < numVoxels; i++) voxels.Add(new Voxel(stream));
                    break;
            }
        }

        return voxels;
    }

    public static List<Voxel> ReadVoxelFile(string fileName) {
        return ReadVoxelStream(new BinaryReader(File.Open(fileName, FileMode.Open)));
    }
}