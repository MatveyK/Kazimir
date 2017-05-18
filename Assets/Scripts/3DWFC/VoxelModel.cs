using System.Collections.Generic;
using UnityEngine;

public class VoxelModel : MonoBehaviour {

    public void Display(byte[,,] output, bool optimise = true) {

        var voxelCube = Resources.Load("Prefabs/Cube");

        for (var x = 0; x < output.GetLength(0); x++) {
            for (var y = 0; y < output.GetLength(1); y++) {
                for (var z = 0; z < output.GetLength(2); z++) {

                    if (output[x, y, z] != 0) {
                        var cube = Instantiate(voxelCube) as GameObject;
                        cube.transform.parent = transform;
                        cube.transform.localPosition = new Vector3(x, y, z);
                        cube.transform.localScale = Vector3.one;
                        cube.transform.localRotation = Quaternion.identity;

                        cube.tag = "Voxel";
                    }
                }
            }
        }
    }

    public void Display(List<Voxel> voxels) {
        //Load the voxel prefab
        var voxelCube = Resources.Load("Prefabs/Cube");

        voxels.ForEach(voxel => {
            var cube = Instantiate(voxelCube) as GameObject;
            cube.transform.parent = transform;
            cube.transform.localPosition = new Vector3(voxel.X, voxel.Y, voxel.Z);
            cube.transform.localScale = Vector3.one;
            cube.transform.localRotation = Quaternion.identity;

            cube.tag = "Voxel";
        });
        Debug.Log("TOTAL CUBES: " + voxels.Count);
    }

}