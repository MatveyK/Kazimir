using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class DiscreteModel {

    //All the possible directions.
    public readonly Coord3D[] Directions = new Coord3D[6]
        {Coord3D.Right, Coord3D.Left, Coord3D.Up, Coord3D.Down, Coord3D.Forward, Coord3D.Back};

    private static readonly Random Rnd = new Random();

    private bool probabilisticModel;

    private int[,,] patternMatrix;
    private List<byte[,,]> patterns;
    private readonly int patternSize;
    private Dictionary<int, double> probabilites;

    private readonly Dictionary<int, Dictionary<Coord3D, List<int>>> neighboursMap;

    private bool[,,] mapOfChanges;
    private List<int>[,,] outputMatrix;

    //Save these fields in case of reintialisation
    private readonly Coord3D outputSize;

    private bool generationFinished = false;
    private bool contradiction = false;
    private int numGen;


    public DiscreteModel(InputModel inputModel, int patternSize, Coord3D outputSize, bool overlapping = true, bool probabilisticModel = true) {
        mapOfChanges = new bool[outputSize.x, outputSize.y, outputSize.z];
        neighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        this.probabilisticModel = probabilisticModel;
        this.patternSize = patternSize;
        numGen = 0;

        this.outputSize = outputSize;

        if (overlapping) {
            InitOverlappingModel(inputModel, patternSize);
        }
        else {
            InitSimpleModel(inputModel, patternSize);
        }
        InitNeighboursMap();
        DetectNeighbours();
        InitOutputMatrix(outputSize);


        Debug.Log($"Model size: {new Vector3(inputModel.Size.x, inputModel.Size.y, inputModel.Size.z)}");
        Debug.Log("Model Ready!");
    }

    public void InitOverlappingModel(InputModel inputModel, int patternSize) {
        var inputMatrix = new byte[inputModel.Size.x, inputModel.Size.y, inputModel.Size.z];
        patterns = new List<byte[,,]>();
        patternMatrix = new int[inputModel.Size.x - (patternSize - 1),
            inputModel.Size.y - (patternSize - 1),
            inputModel.Size.z - (patternSize - 1)];
        probabilites = new Dictionary<int, double>();

        inputModel.Voxels.ForEach(voxel => inputMatrix[voxel.X, voxel.Y, voxel.Z] = voxel.Color);

        for (var x = 0; x < patternMatrix.GetLength(0); x++) {
            for (var y = 0; y < patternMatrix.GetLength(1); y++) {
                for (var z = 0; z < patternMatrix.GetLength(2); z++) {
                    var currentPattern = GetCurrentPattern(inputMatrix, x, y, z, patternSize);

                    var index = patterns.ContainsPattern(currentPattern);
                    if (index < 0) {
                        patterns.Add(currentPattern);
                        patternMatrix[x, y, z] = patterns.Count - 1;
                        probabilites[patterns.Count - 1] = (double) 1 / patternMatrix.Length;
                    }
                    else {
                        patternMatrix[x, y, z] = index;
                        probabilites[index] += (double) 1 / patternMatrix.Length;
                    }
                }
            }
        }
    }

    private static byte[,,] GetCurrentPattern(byte[,,] matrix, int x, int y, int z, int patternSize) {
        var pattern = new byte[patternSize, patternSize, patternSize];
        for (var i = x; i < x + patternSize; i++) {
            for (var j = y; j < y + patternSize; j++) {
                for (var k = z; k < z + patternSize; k++) {
                    pattern[i - x, j - y, k - z] = matrix[i, j, k];
                }
            }
        }
        return pattern;
    }

    public void InitSimpleModel(InputModel inputModel, int patternSize) {
        var inputMatrix = new byte[inputModel.Size.x, inputModel.Size.y, inputModel.Size.y];
        patterns = new List<byte[,,]>();
        patternMatrix = new int[(int) Math.Ceiling((double) (inputModel.Size.x / patternSize) + 1),
            (int) Math.Ceiling((double) (inputModel.Size.y / patternSize) + 1),
            (int) Math.Ceiling((double) (inputModel.Size.z / patternSize) + 1)];

        inputModel.Voxels.ForEach(voxel => inputMatrix[voxel.X, voxel.Y, voxel.Z] = voxel.Color);

        var i = 0;
        var j = 0;
        var k = 0;

        for (var x = 0; x < inputModel.Size.x - patternSize; x += patternSize) {
            for (var y = 0; y < inputModel.Size.y - patternSize; y += patternSize) {
                for (var z = 0; z < inputModel.Size.z - patternSize; z += patternSize) {
                    var currentPattern = GetCurrentPattern(inputMatrix, x, y, z, patternSize);

                    var index = patterns.ContainsPattern(currentPattern);
                    if (index < 0) {
                        patterns.Add(currentPattern);
                        patternMatrix[i, j, k] = patterns.Count - 1;
                    }
                    else {
                        patternMatrix[i, j, k] = index;
                    }

                    i++;
                }
                i = 0;
                j++;
            }
            j = 0;
            k++;
        }
    }

    private void DetectNeighbours() {
        foreach (var pattern in patternMatrix) {
            foreach (var otherPattern in patternMatrix) {
                CheckAddNeighbour(pattern, otherPattern);
            }
        }
    }

    private void CheckAddNeighbour(int pattern, int otherPattern) {
        var patternStruct = patterns[pattern];
        var otherPatternStruct = patterns[otherPattern];
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Left)) neighboursMap[pattern][Coord3D.Left].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Right)) neighboursMap[pattern][Coord3D.Right].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Down)) neighboursMap[pattern][Coord3D.Down].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Up)) neighboursMap[pattern][Coord3D.Up].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Back)) neighboursMap[pattern][Coord3D.Back].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Forward)) neighboursMap[pattern][Coord3D.Forward].Add(otherPattern);
    }

    private void InitNeighboursMap() {
        //Init the data structure.
        for (var i = 0; i < patterns.Count; i++) {
            neighboursMap[i] = new Dictionary<Coord3D, List<int>>();

            foreach (var direction in Directions) {
                neighboursMap[i][direction] = new List<int>();
            }
        }

        //Populate the data structure.
        for (var x = 0; x < patternMatrix.GetLength(0); x++) {
            for (var y = 0; y < patternMatrix.GetLength(1); y++) {
                for (var z = 0; z < patternMatrix.GetLength(2); z++) {
                    var currentPattern = patternMatrix[x, y, z];

                    if(x-1 >= 0) neighboursMap[currentPattern][Coord3D.Left].Add(patternMatrix[x-1, y ,z]);
                    if (x + 1 < patternMatrix.GetLength(0)) neighboursMap[currentPattern][Coord3D.Right].Add(patternMatrix[x+1, y, z]);

                    if(y-1 >= 0) neighboursMap[currentPattern][Coord3D.Down].Add(patternMatrix[x, y-1, z]);
                    if(y+1 < patternMatrix.GetLength(1)) neighboursMap[currentPattern][Coord3D.Up].Add(patternMatrix[x, y+1, z]);

                    if(z-1 >= 0) neighboursMap[currentPattern][Coord3D.Back].Add(patternMatrix[x, y, z-1]);
                    if(z+1 < patternMatrix.GetLength(2)) neighboursMap[currentPattern][Coord3D.Forward].Add(patternMatrix[x, y, z+1]);
                }
            }
        }
    }

    private void InitOutputMatrix(Coord3D size) {
        outputMatrix = new List<int>[size.x, size.y, size.z];

        for (var x = 0; x < size.x; x++) {
            for (var y = 0; y < size.y; y++) {
                for (var z = 0; z < size.z; z++) {
                    outputMatrix[x, y, z] = new List<int>();

                    for (var i = 0; i < patterns.Count; i++) {
                        outputMatrix[x, y, z].Add(i);
                    }
                }
            }
        }
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

    public void Observe() {

        // TODO Add distribution criteria to the cell state collapse

        //Build a list of nodes that have not been collapsed to a definite state.
        var collapsableNodes = GetCollapsableNodes();

        //Pick a random node from the collapsible nodes.
        var nodeCoords = collapsableNodes[Rnd.Next(collapsableNodes.Count)];
        var availableNodeStates = outputMatrix[nodeCoords.x, nodeCoords.y, nodeCoords.z];

        if (probabilisticModel) {

            //Eliminate all duplicates from the list of possible states.
            availableNodeStates = availableNodeStates.Distinct().ToList();

            //Choose a state according to the probability distribution of the states in the input model.
            double runningTotal = 0;
            var totalProb = probabilites.Select(x => x)
                .Where(x => availableNodeStates.Contains(x.Key))
                .Sum(x => x.Value);
            var rndNumb = Rnd.NextDouble() * totalProb;
            foreach (var availableNodeState in availableNodeStates) {
                runningTotal += probabilites[availableNodeState];
                if (runningTotal > rndNumb) {
                    outputMatrix.SetValue(new List<int>() {availableNodeState}, nodeCoords.x, nodeCoords.y,
                        nodeCoords.z);
                    break;
                }
            }
        }
        else {
            //Collapse it to a random definite state.
            outputMatrix.SetValue(new List<int>() { availableNodeStates[Rnd.Next(availableNodeStates.Count)] }, nodeCoords.x, nodeCoords.y, nodeCoords.z);
        }


        Propagate(nodeCoords.x, nodeCoords.y, nodeCoords.z);

        numGen++;
    }

    private void Propagate(int x, int y, int z) {
        //Reset the map that keeps track of the changes.
        ReInitMapOfChanges();

        //Queue the first element.
        var nodesToVisit = new Queue<Coord3D>();
        nodesToVisit.Enqueue(new Coord3D(x, y, z));

        //Perform a Breadth-First grid traversal.
		while (nodesToVisit.Any()) {
            var current = nodesToVisit.Dequeue();
		    mapOfChanges.SetValue(true, current.x, current.y, current.z);

            //Get the list of the allowed neighbours of the current node
		    var nghbrsMaps = outputMatrix[current.x, current.y, current.z].Select(possibleElement => NeighboursMap[possibleElement]).ToList();

		    var allowedNghbrs = nghbrsMaps.SelectMany(dict => dict)
		        .ToLookup(pair => pair.Key, pair => pair.Value)
		        .ToDictionary(group => group.Key, group => group.SelectMany(list => list).ToList());


		    //For every possible direction check if the node has already been affected by the propagation.
		    //If it hasn't queue it up and mark it as visited, otherwise move on.
            foreach (var direction in Directions) {
                if (!mapOfChanges.OutOfBounds(current.Add(direction)) &&
                    !mapOfChanges[current.x + direction.x, current.y + direction.y, current.z + direction.z] &&
                    !outputMatrix.OutOfBounds(current.Add(direction))) {

                    //Eliminate neighbours that are not allowed from the output matrix
                    var allowedNghbrsInDirection = allowedNghbrs[direction].Distinct().ToList();
                    outputMatrix[current.x + direction.x, current.y + direction.y, current.z + direction.z]
                        .RemoveAll(neighbour => !allowedNghbrsInDirection.Contains(neighbour));

                    //Check for contradictions
                    // TODO Add a backtrack recovery system to remedy the contradictions.
                    if (outputMatrix[current.x + direction.x, current.y + direction.y, current.z + direction.z].Count == 0) {
                        contradiction = true;
                        return;
                    }

                    //Queue it up in order to spread the info to its neighbours and mark it as visited.
                    nodesToVisit.Enqueue(current.Add(direction));
                    mapOfChanges.SetValue(true, current.x + direction.x, current.y + direction.y, current.z + direction.z);
                }
            }
        }

        generationFinished = CheckIfFinished();
    }

    private List<Coord3D> GetCollapsableNodes() {
        var collapsableNodes = new List<Coord3D>();
        for (var x = 0; x < outputMatrix.GetLength(0); x++) {
            for (var y = 0; y < outputMatrix.GetLength(1); y++) {
                for (var z = 0; z < outputMatrix.GetLength(2); z++) {
                    if (outputMatrix[x, y, z].Count != 1 && outputMatrix[x, y, z].Count != 0) {
                        collapsableNodes.Add(new Coord3D(x, y, z));
                    }
                }
            }
        }
        return collapsableNodes;
    }

    private bool CheckIfFinished() {
        return outputMatrix.Cast<List<int>>().All(node => node.Count == 1);
    }

    public void Clear() {
        InitOutputMatrix(outputSize);
        contradiction = false;
        generationFinished = false;
        numGen = 0;
    }

    public byte[,,] GetOutput() {
        var res = new byte[outputMatrix.GetLength(0) * patternSize, outputMatrix.GetLength(1) * patternSize, outputMatrix.GetLength(2) * patternSize];
        for (var x = 0; x < outputMatrix.GetLength(0); x += patternSize) {
            for (var y = 0; y < outputMatrix.GetLength(1); y += patternSize) {
                for (var z = 0; z < outputMatrix.GetLength(2); z += patternSize) {

                    var currentPattern = patterns[outputMatrix[x, y, z].First()];
                    for (var i = 0; i < currentPattern.GetLength(0); i++) {
                        for (var j = 0; j < currentPattern.GetLength(1); j++) {
                            for (var k = 0; k < currentPattern.GetLength(2); k++) {
                                res[x + i, y + j, z + k] = currentPattern[i, j, k];
                            }
                        }
                    }
                }
            }
        }
        DisplayOutputStats(outputMatrix);
        return res;
    }

    private void DisplayOutputStats(List<int>[,,] matrix) {
        var stats = matrix.Cast<List<int>>()
            .ToList()
            .GroupBy(state => state.First())
            .ToDictionary(group => group.Key, group => (float) group.Count() / (float) outputMatrix.Length);
    }



    public Dictionary<int, Dictionary<Coord3D, List<int>>> NeighboursMap => neighboursMap;

    public bool GenerationFinished => generationFinished;

    public bool Contradiction => contradiction;

    public int NumGen => numGen;
}
