using UnityEngine;
using System.Collections.Generic;


public class Demo : MonoBehaviour {
    [SerializeField] private string voxFileName;

    [SerializeField] private bool optimise = false;
    [SerializeField] private bool probabilisticModel = true;

    private DiscreteModel model;

    [SerializeField] private int gridCellSize = 3;
    [SerializeField] Vector3 outputSize = new Vector3(6, 6, 6);

    private void Start() {
        //Read the .vox file
        var voxels = VoxReaderWriter.ReadVoxelFile(voxFileName);

        //Display the voxel model.
        DisplayVoxelModel(voxels);

        //Center the 3D model and init grid
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.AngleAxis(90, Vector3.left);

    }


    private void Update() {
        if (Input.GetKeyDown("space")) {
            while (!model.GenerationFinished) {
                model.Observe();

                if (model.Contradiction) {
                    Debug.Log($"Generation Failed after {model.NumGen} iterations!");
                    model.Clear();
                }
            }
            Debug.Log($"Generation finished after {model.NumGen} iterations!");
        }
        if (Input.GetKeyDown("v")) {
        }
    }

    private void DisplayVoxelModel(List<Voxel> voxels) {
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