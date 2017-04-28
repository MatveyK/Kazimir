using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grid : MonoBehaviour {

    private GridCell[,,] gridMatrix;

    private float gridCellSize;

    private const float vertexLimit = 65500;


    private void Start () {
    }

    //Initialise the grid GameObject
    public void Init(GameObject model, float gridCellSize) {

        this.gridCellSize = gridCellSize;

        //Init the Prefab
        var gridCellPrefab = Resources.Load("Prefabs/GridCell");

        //Get the model size (represented as a vector in 3D) using its mesh collider
        var modelSize = FindMaxVectorPos(model);

        //Init the data matrix
        InitGridMatrix(modelSize, gridCellSize);
        var xi = 0;
        var yi = 0;
        var zi = 0;

        //Init GameObject matrix
        for (var x = -modelSize.x; x < modelSize.x; x += gridCellSize) {
            for (float y = 0; y < modelSize.y; y += gridCellSize) {
                for (var z = -modelSize.z; z < modelSize.z; z += gridCellSize) {
                    var gridCellObj = Instantiate(gridCellPrefab) as GameObject;
                    var gCell = gridCellObj.GetComponent<GridCell>();

                    gridCellObj.transform.parent = transform;

                    var initPoint = new Vector3(x + gridCellSize / 2, y + gridCellSize / 2, z + gridCellSize / 2);
                    gCell.Init(initPoint, gridCellSize);

                    //Add cell to the data struct
                    gridMatrix[xi, yi, zi] = gCell;
                    zi++;
                }
                zi = 0;
                yi++;
            }
            yi = 0;
            xi++;
        }

        Debug.Log("TOTAL CELLS: " + gridMatrix.Length);
    }

    public void InitOutputGrid(int[,,] modelOutput, Grid inputGrid, bool optimise = false) {

        var gridCellSize = inputGrid.GridCellSize;

        for (var x = 0; x < modelOutput.GetLength(0); x++) {
            for (var y = 0; y < modelOutput.GetLength(1); y++) {
                for (var z = 0; z < modelOutput.GetLength(2); z++) {

                    //Find the GridCell GameObject using the id provided by the model output.
                    foreach (var gridCell in inputGrid.GridMatrix) {
                        if (gridCell.Id == modelOutput[x, y, z]) {
                            var position = new Vector3(x * gridCellSize + gridCellSize / 2,
                                y * gridCellSize + gridCellSize / 2,
                                z * gridCellSize + gridCellSize / 2);
                            var gCell = Instantiate(gridCell, position, Quaternion.identity);
                            gCell.transform.parent = transform;
                        }
                    }
                }
            }
        }

        if (optimise) {
            Optimise3DModel();
        }
    }


    private void Optimise3DModel() {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        var organisedMeshFilters = OrganiseMeshes(meshFilters);

        foreach (var organisedMeshFilter in organisedMeshFilters) {
            CombineInstance[] combine = new CombineInstance[organisedMeshFilter.Count];
            for (var i = 0; i < organisedMeshFilter.Count; i++) {
                combine[i].mesh = organisedMeshFilter[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                organisedMeshFilter[i].gameObject.SetActive(false);
            }

            //Create a new game object for each large mesh
            //TODO Look into a solution that uses submeshes
            var gameObj = new GameObject();

            //Combine the voxel meshes to form one big mesh
            gameObj.AddComponent<MeshFilter>();
            gameObj.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            gameObj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);

            //Add the renderer with a default material
            gameObj.AddComponent<MeshRenderer>();
            gameObj.GetComponent<MeshRenderer>().enabled = true;
            gameObj.GetComponent<MeshRenderer>().material = Resources.Load("Materials/VoxelMaterial") as Material;

            //Set the object containing the mesh as the child of the current object
            gameObj.transform.parent = transform;
            transform.gameObject.SetActive(true);
        }
    }

    private List<List<MeshFilter>> OrganiseMeshes(MeshFilter[] meshFilters) {
        var res = new List<List<MeshFilter>>();
        var verticesSoFar = 0;

        var index = 0;
        res.Add(new List<MeshFilter>());
        var currList = res[index];
        var insideIndex = 0;
        for (var i = 0; i < meshFilters.Length; i++) {
            if (verticesSoFar + meshFilters[i].mesh.vertexCount > vertexLimit) {
                index++;
                res.Add(new List<MeshFilter>());
                currList = res[index];
                verticesSoFar = 0;
            }

            currList.Add(meshFilters[i]);
            verticesSoFar += meshFilters[i].mesh.vertexCount;
        }

        return res;
    }

    private static Vector3 FindMaxVectorPos(GameObject model) {
        var sizeVec = Vector3.zero;

        foreach (Transform child in model.transform) {
            var absValueX = Math.Abs(sizeVec.x);
            var absValueY = Math.Abs(sizeVec.y);
            var absValueZ = Math.Abs(sizeVec.z);

            if (Math.Abs(child.position.x) > absValueX) {
                sizeVec.x = Math.Abs(child.position.x);
            }

            if (Math.Abs(child.position.y) > absValueY) {
                sizeVec.y = Math.Abs(child.position.y);
            }

            if (Math.Abs(child.position.z) > absValueZ) {
                sizeVec.z = Math.Abs(child.position.z);
            }
        }

        return sizeVec;
    }

    private void InitGridMatrix(Vector3 modelSize, float gridCellSize) {
        var cellsPerDimX = (int) (modelSize.x * 2 / gridCellSize) + 1;
        //Do not multiply y by two since we start y at zero coordinate.
        var cellsPerDimY = (int) (modelSize.y / gridCellSize) + 1;
        var cellsPerDimZ = (int) (modelSize.z * 2 / gridCellSize) + 1;
        gridMatrix = new GridCell[cellsPerDimX, cellsPerDimY, cellsPerDimZ];
    }

    public GridCell[,,] GridMatrix {
        get { return gridMatrix; }
    }

    public float GridCellSize {
        get { return gridCellSize; }
    }
}
