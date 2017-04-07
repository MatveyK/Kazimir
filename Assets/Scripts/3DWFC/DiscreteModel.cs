using System.Collections.Generic;
using UnityEngine;

public class DiscreteModel {

    private Dictionary<int, List<int>> neighboursMap;

    private List<int>[,,] outputMatrix;

    public DiscreteModel(GridCell[,,] inputMatrix, int outputSize) {
        neighboursMap = new Dictionary<int, List<int>>();

        AssignIdsToCells(inputMatrix);
        InitNeighboursMap(inputMatrix);

        InitOutputMatrix(outputSize, inputMatrix);
    }

    private void AssignIdsToCells(GridCell[,,] matrix) {
        var index = 0;

        foreach (var cell in matrix) {
            cell.Id = index;
            neighboursMap[cell.Id] = new List<int>();

            index++;
        }
    }

    private void InitNeighboursMap(GridCell[,,] matrix) {

        for (var x = 0; x < matrix.GetLength(0); x++) {
            for (var y = 0; y < matrix.GetLength(1); y++) {
                for (var z = 0; z < matrix.GetLength(2); z++) {
                    var currentCell = matrix[x, y, z];

                    if(x-1 >= 0) neighboursMap[currentCell.Id].Add(matrix[x-1, y, z].Id);
                    if(x+1 < matrix.GetLength(0)) neighboursMap[currentCell.Id].Add(matrix[x+1, y, z].Id);

                    if(y-1 >= 0) neighboursMap[currentCell.Id].Add(matrix[x, y-1, z].Id);
                    if(y+1 < matrix.GetLength(1)) neighboursMap[currentCell.Id].Add(matrix[x, y+1, z].Id);

                    if(z-1 >= 0) neighboursMap[currentCell.Id].Add(matrix[x, y, z-1].Id);
                    if(z+1 < matrix.GetLength(2)) neighboursMap[currentCell.Id].Add(matrix[x, y, z+1].Id);
                }
            }
        }
    }

    private void InitOutputMatrix(int size, GridCell[,,] inputMatrix) {
        outputMatrix = new List<int>[size, size, size];

        for (var x = 0; x < size; x++) {
            for (var y = 0; y < size; y++) {
                for (var z = 0; z < size; z++) {
                    outputMatrix[x, y, z] = new List<int>();

                    foreach (var cell in inputMatrix) {
                        outputMatrix[x, y, z].Add(cell.Id);
                    }
                }
            }
        }
    }



    public Dictionary<int, List<int>> NeighboursMap {
        get { return neighboursMap; }
    }
}
