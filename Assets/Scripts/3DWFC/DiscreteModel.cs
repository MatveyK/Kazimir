using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiscreteModel {

    private readonly Dictionary<int, Dictionary<string, int>> neighboursMap;

    private bool[,,] mapOfChanges;
    private List<int>[,,] outputMatrix;

    public DiscreteModel(GridCell[,,] inputMatrix, Vector3 outputSize) {
        mapOfChanges = new bool[(int) outputSize.x, (int) outputSize.y, (int) outputSize.z];
        neighboursMap = new Dictionary<int, Dictionary<string, int>>();

        MergeDoubleCells(inputMatrix);

        AssignIdsToCells(inputMatrix);
        InitNeighboursMap(inputMatrix);

        InitOutputMatrix(outputSize, inputMatrix);
        Observe();
    }

    private void AssignIdsToCells(GridCell[,,] matrix) {
        var index = 0;

        foreach (var cell in matrix) {
            cell.Id = index;
            neighboursMap[cell.Id] = new Dictionary<string, int>();

            index++;
        }
    }

    private void InitNeighboursMap(GridCell[,,] matrix) {

        for (var x = 0; x < matrix.GetLength(0); x++) {
            for (var y = 0; y < matrix.GetLength(1); y++) {
                for (var z = 0; z < matrix.GetLength(2); z++) {
                    var currentCell = matrix[x, y, z];

                    if(x-1 >= 0) neighboursMap[currentCell.Id]["left"] = matrix[x-1, y ,z].Id;
                    if (x + 1 < matrix.GetLength(0)) neighboursMap[currentCell.Id]["right"] = matrix[x+1, y, z].Id;

                    if(y-1 >= 0) neighboursMap[currentCell.Id]["down"] = matrix[x, y-1, z].Id;
                    if(y+1 < matrix.GetLength(1)) neighboursMap[currentCell.Id]["up"] = matrix[x, y+1, z].Id;

                    if(z-1 >= 0) neighboursMap[currentCell.Id]["back"] = matrix[x, y, z-1].Id;
                    if(z+1 < matrix.GetLength(2)) neighboursMap[currentCell.Id]["front"] = matrix[x, y, z+1].Id;
                }
            }
        }
    }

    private void InitOutputMatrix(Vector3 size, GridCell[,,] inputMatrix) {
        outputMatrix = new List<int>[(int) size.x, (int) size.y, (int) size.z];

        for (var x = 0; x < size.x; x++) {
            for (var y = 0; y < size.y; y++) {
                for (var z = 0; z < size.z; z++) {
                    outputMatrix[x, y, z] = new List<int>();

                    foreach (var cell in inputMatrix) {
                        outputMatrix[x, y, z].Add(cell.Id);
                    }
                }
            }
        }
    }

    private void MergeDoubleCells(GridCell[,,] inputMatrix) {
        int same = 0;
        foreach (var gridCell in inputMatrix) {
            foreach (var otherGridCell in inputMatrix) {
                if (CompareCells(gridCell, otherGridCell)) same++;
            }
        }
        Debug.Log("SAME CELLS: " + same);
        Debug.Log(Vector3.SqrMagnitude(new Vector3(2.45f, 2.97f, -1.5f) - new Vector3(1.70f, 2.97f, -1.5f)));
    }

    private static bool CompareCells(GridCell firstCell, GridCell secondCell) {

        //First check if they have the same number of voxels
        if (firstCell.ContainedVoxels.Count != secondCell.ContainedVoxels.Count)
            return false;

        //Then check if the positions of each two voxels in the cell match to a certain
        //threshold.
        var sameVoxels = (from voxel in firstCell.ContainedVoxels
            from otherVoxel in secondCell.ContainedVoxels
            where Vector3.SqrMagnitude(voxel.transform.localPosition - otherVoxel.transform.localPosition) < 0.6f
            select voxel)
            .Count();

        return sameVoxels == firstCell.ContainedVoxels.Count;
    }

    private void ReInitMapOfChanges() {
        for (var x = 0; x < outputMatrix.GetLength(0); x++) {
            for (var y = 0; y < outputMatrix.GetLength(1); y++) {
                for (var z = 0; z < outputMatrix.GetLength(2); z++) {
                    mapOfChanges[x, y, z] = false;
                }
            }
        }
    }

    private void Observe() {

        var cellCollapsed = true;

        while (cellCollapsed) {
            //Generate random coordinates for random cell selection
            var randomX = Random.Range(0, outputMatrix.GetLength(0));
            var randomY = Random.Range(0, outputMatrix.GetLength(1));
            var randomZ = Random.Range(0, outputMatrix.GetLength(2));

            //Check if the cell has already collapsed into a definite state
            //If not, collapse it into a definite state
            // TODO Add distribution criteria to the cell state collapse
            var cell = outputMatrix[randomX, randomY, randomZ];
            if (cell.Count == 1) {
                cellCollapsed = true;
            }
            else {
                outputMatrix[randomX, randomY, randomZ] = cell.Where((value, index) => index == Random.Range(0, cell.Count)).ToList();
                cellCollapsed = false;
            }
        }
    }

    private void Propagate(int changedCellX, int changedCellY, int changedCellZ) {
        
    }



    public Dictionary<int, Dictionary<string, int>> NeighboursMap {
        get { return neighboursMap; }
    }
}
