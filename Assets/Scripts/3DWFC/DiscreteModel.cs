using System.Collections.Generic;
using UnityEngine;

public class DiscreteModel {

    private Dictionary<int, List<int>> neighboursMap;

    public DiscreteModel(GridCell[,,] matrix) {
        neighboursMap = new Dictionary<int, List<int>>();

        AssignIdsToCells(matrix);
        InitNeighboursMap(matrix);
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


    public Dictionary<int, List<int>> NeighboursMap {
        get { return neighboursMap; }
    }
}
