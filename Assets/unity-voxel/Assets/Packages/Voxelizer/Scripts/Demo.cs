using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace mattatz.VoxelSystem {

    [RequireComponent (typeof(MeshFilter))]
    public class Demo : MonoBehaviour {

        [SerializeField] int count = 10;
        List<Voxel> voxels;

        private Grid grid;
        private GameObject gridObj;
        private DiscreteModel model;

        [SerializeField] Vector3 outputSize = new Vector3(6, 6, 6);

        private void Start () {
            var filter = GetComponent<MeshFilter>();
            voxels = Voxelizer.Voxelize(filter.mesh, count);
            voxels.ForEach(voxel => {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = transform;
                cube.transform.localPosition = voxel.position;
                cube.transform.localScale = voxel.size * Vector3.one;
                cube.transform.localRotation = Quaternion.identity;

                cube.tag = "Voxel";

                var boxCollider = cube.AddComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = Vector3.one;
                var rigidBody = cube.AddComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = true;
                rigidBody.mass = 0f;
                rigidBody.angularDrag = 0f;
            });
			Debug.Log ("TOTAL CUBES: " + voxels.Count);

            //Center the 3D model and init grid
            transform.position = Vector3.zero;


            var gridPrefab = Resources.Load("Prefabs/Grid");
            gridObj = Instantiate(gridPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            grid = gridObj.GetComponent<Grid>();
            grid.Init(this.gameObject, 5f);
        }


        private void Update() {
            if (Input.GetKeyDown("b")) {
                model = new DiscreteModel(grid.GridMatrix, outputSize);
            }
            if (Input.GetKeyDown("space")) {
                while (!model.GenerationFinished) {
                    model.Observe();
                }
            }
            if (Input.GetKeyDown("v")) {
                var gridPrefab = Resources.Load("Prefabs/Grid");
                var resultGridObj = Instantiate(gridPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                Grid resultGrid = resultGridObj.GetComponent<Grid>();
                resultGrid.InitOutputGrid(model.GetOutput(), grid);

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


