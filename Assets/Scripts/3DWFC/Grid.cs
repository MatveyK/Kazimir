using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Grid : MonoBehaviour {

    private GridCell[,,] cells;

    private const int BatchSize = 100;
    private int nbBatches;
    private int batchesFinished;


    private void Start () {
    }

    //Initialise the grid GameObject
    public void Init(GameObject model, float gridCellSize) {

        //Init the Prefab
        var gridCellPrefab = Resources.Load("Prefabs/GridCell");

        //Get the model size (represented as a vector in 3D) using its mesh collider
        var modelSize = FindMaxVectorPos(model);

        //Init the data matrix
        InitDataMatrix(modelSize, gridCellSize);
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
                    cells[xi, yi, zi] = gCell;
                    zi++;
                }
                zi = 0;
                yi++;
            }
            yi = 0;
            xi++;
        }

        //Determine the number of batches
        nbBatches = cells.Length/ BatchSize;

        DiscreteModel mod = new DiscreteModel(cells, 100);
        Debug.Log("HERE: " + mod.NeighboursMap[0].Count);
    }

    private void Update() {
        if (batchesFinished < nbBatches) {
            for (var i = batchesFinished * BatchSize; i < batchesFinished * BatchSize + BatchSize; i++) {
                /*
                foreach (var voxel in cells[i].ContainedVoxels) {
                    voxel.GetComponent<MeshRenderer>().enabled = false;
                }
*/
                //cells[i].transform.position = new Vector3(Random.Range(-50, 0), Random.Range(-50, 0), Random.Range(-50, 0));
            }
            batchesFinished++;
            Debug.Log("Processed: " + batchesFinished * BatchSize + "/" + cells.Length);
        }
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

    private void InitDataMatrix(Vector3 modelSize, float gridCellSize) {
        var cellsPerDimX = (int) (modelSize.x * 2 / gridCellSize) + 1;
        var cellsPerDimY = (int) (modelSize.y * 2 / gridCellSize) + 1;
        var cellsPerDimZ = (int) (modelSize.z * 2 / gridCellSize) + 1;
        cells = new GridCell[cellsPerDimX, cellsPerDimY, cellsPerDimZ];
    }


    public void ReArrange() {
        foreach (var cell in cells) {
            cell.transform.position = new Vector3(Random.Range(-50, 0), Random.Range(-50, 0), Random.Range(-50, 0));
        }
    }

    private IEnumerator ReArrangeStart() {
        yield return new WaitForSeconds(10);
        ReArrange();
    }

}
