using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace mattatz.VoxelSystem {

    [RequireComponent (typeof(MeshFilter))]
    public class Demo : MonoBehaviour {

        [SerializeField] int count = 10;
        [SerializeField] private bool optimise = false;
        [SerializeField] private bool probabilisticModel = true;

        List<Voxel> voxels;

        private Grid grid;
        private GameObject gridObj;
        private DiscreteModel model;

        [SerializeField] private float gridCellSize = 5f;
        [SerializeField] Vector3 outputSize = new Vector3(6, 6, 6);

        private void Start () {
            var filter = GetComponent<MeshFilter>();
            voxels = Voxelizer.Voxelize(filter.mesh, count);

            //Load the voxel prefab
            var voxelCube = Resources.Load("Prefabs/Cube");
            voxels.ForEach(voxel => {
                var cube = Instantiate(voxelCube) as GameObject;
                cube.transform.parent = transform;
                cube.transform.localPosition = voxel.position;
                cube.transform.localScale = voxel.size * Vector3.one;
                cube.transform.localRotation = Quaternion.identity;

                cube.tag = "Voxel";
            });
			Debug.Log ("TOTAL CUBES: " + voxels.Count);

            //Center the 3D model and init grid
            transform.position = Vector3.zero;


            var gridPrefab = Resources.Load("Prefabs/Grid");
            gridObj = Instantiate(gridPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            grid = gridObj.GetComponent<Grid>();
            grid.Init(this.gameObject, gridCellSize);
        }


        private void Update() {
            if (Input.GetKeyDown("b")) {
                model = new DiscreteModel(grid.GridMatrix, outputSize, probabilisticModel);
            }
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
                var gridPrefab = Resources.Load("Prefabs/Grid");
                var resultGridObj = Instantiate(gridPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                Grid resultGrid = resultGridObj.GetComponent<Grid>();
                resultGrid.InitOutputGrid(model.GetOutput(), grid, optimise);

                //Disable the input grid.
                gridObj.SetActive(false);
            }
        }

        void OnDrawGizmos () {
            if (voxels == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            voxels.ForEach(voxel => {
                Gizmos.DrawCube(voxel.position, voxel.size * Vector3.one);
            });
        }

    }

}


