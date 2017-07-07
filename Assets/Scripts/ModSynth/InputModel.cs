using System.Collections.Generic;

public struct InputModel {

    public Coord3D Size;

    public List<Voxel> Voxels;

    public InputModel(Coord3D size, List<Voxel> voxels) {
        Size = size;
        Voxels = voxels;
    }
}