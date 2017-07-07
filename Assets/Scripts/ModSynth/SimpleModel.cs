using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class SimpleModel : Model {
    
    //The six possible directions.
    public readonly Coord3D[] Directions = new Coord3D[6]
        {Coord3D.Right, Coord3D.Left, Coord3D.Up, Coord3D.Down, Coord3D.Forward, Coord3D.Back};

    public SimpleModel(InputModel inputModel, int patternSize, Coord3D outputSize, bool periodic, bool addNeighbours, bool probabilisticModel) {
        NeighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        Periodic = periodic;
        ProbabilisticModel = probabilisticModel;
        PatternSize = patternSize;
        NumGen = 0;

        OutputSize = outputSize;
        
        Init(inputModel, patternSize, periodic);
        InitNeighboursMap();

        if (addNeighbours) {
            DetectNeighbours();
        }

        InitOutputMatrix(outputSize);
        
        Debug.Log($"Model size: {new Vector3(inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z)}");
        Debug.Log("Model Ready!");
    }
    
    protected override void Init(InputModel inputModel, int patternSize, bool periodic) {
        var inputMatrix = new byte[inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z];
        patterns = new List<byte[,,]>();
        patternMatrix = new int[(int) Math.Ceiling((double) (inputModel.Size.X / patternSize)),
            (int) Math.Ceiling((double) (inputModel.Size.Y / patternSize)),
            (int) Math.Ceiling((double) (inputModel.Size.Z / patternSize))];
        probabilites = new Dictionary<int, double>();


        inputModel.Voxels.ForEach(voxel => inputMatrix[voxel.X, voxel.Y, voxel.Z] = voxel.Color);
        
        //Add "empty space" pattern.
        //patterns.Add(CreateEmptyPattern(patternSize));
        //probabilites[0] = 0;

        for (var x = 0; x < patternMatrix.GetLength(0); x++) {
            for (var y = 0; y < patternMatrix.GetLength(1); y++) {
                for (var z = 0; z < patternMatrix.GetLength(2); z++) {
                    var currentPattern = GetCurrentPattern(inputMatrix, x * patternSize, y * patternSize, z * patternSize, patternSize);

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

    private void InitNeighboursMap() {
        //Init the data structure.
        for (var i = 0; i < patterns.Count; i++) {
            NeighboursMap[i] = new Dictionary<Coord3D, List<int>>();

            foreach (var direction in Directions) {
                NeighboursMap[i][direction] = new List<int>();
            }
        }

        //Populate the data structure.
        for (var x = 0; x < patternMatrix.GetLength(0); x++) {
            for (var y = 0; y < patternMatrix.GetLength(1); y++) {
                for (var z = 0; z < patternMatrix.GetLength(2); z++) {
                    var currentPattern = patternMatrix[x, y, z];

                    if (x - 1 >= 0) {
                        NeighboursMap[currentPattern][Coord3D.Left].Add(patternMatrix[x - 1, y, z]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Left]
                                .Add(patternMatrix[patternMatrix.GetLength(0) - 1, y, z]);
                        }
                    }
                    if (x + 1 < patternMatrix.GetLength(0)) {
                        NeighboursMap[currentPattern][Coord3D.Right].Add(patternMatrix[x + 1, y, z]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Right].Add(patternMatrix[0, y, z]);
                        }
                    }

                    if (y - 1 >= 0) {
                        NeighboursMap[currentPattern][Coord3D.Down].Add(patternMatrix[x, y - 1, z]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Down]
                                .Add(patternMatrix[x, patternMatrix.GetLength(1) - 1, z]);
                        }
                    }
                    if (y + 1 < patternMatrix.GetLength(1)) {
                        NeighboursMap[currentPattern][Coord3D.Up].Add(patternMatrix[x, y + 1, z]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Up].Add(patternMatrix[x, 0, z]);
                        }
                    }

                    if (z - 1 >= 0) {
                        NeighboursMap[currentPattern][Coord3D.Back].Add(patternMatrix[x, y, z - 1]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Back]
                                .Add(patternMatrix[x, y, patternMatrix.GetLength(2) - 1]);
                        }
                    }
                    if (z + 1 < patternMatrix.GetLength(2)) {
                        NeighboursMap[currentPattern][Coord3D.Forward].Add(patternMatrix[x, y, z + 1]);
                    }
                    else {
                        if (Periodic) {
                            NeighboursMap[currentPattern][Coord3D.Forward].Add(patternMatrix[x, y, 0]);
                        }
                    }
                }
            }
        }

        //Eliminate duplicates in the neighbours map.
        for (var i = 0; i < patterns.Count; i++) {
            foreach (var direction in Directions) {
                NeighboursMap[i][direction] = NeighboursMap[i][direction].Distinct().ToList();
            }
        }

        //Add the empty space in case a pattern has no neighbour.
        /*
        for (var i = 0; i < patterns.Count; i++) {
            foreach (var direction in Directions) {
                if(neighboursMap[i][direction].Count == 0) 
                    neighboursMap[i][direction].Add(0);
            }
        }
        */
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
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Left) && !NeighboursMap[pattern][Coord3D.Left].Contains(otherPattern)) 
            NeighboursMap[pattern][Coord3D.Left].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Right) && !NeighboursMap[pattern][Coord3D.Right].Contains(otherPattern))
            NeighboursMap[pattern][Coord3D.Right].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Down) && !NeighboursMap[pattern][Coord3D.Down].Contains(otherPattern)) 
            NeighboursMap[pattern][Coord3D.Down].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Up) && !NeighboursMap[pattern][Coord3D.Up].Contains(otherPattern)) 
            NeighboursMap[pattern][Coord3D.Up].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Back) && !NeighboursMap[pattern][Coord3D.Back].Contains(otherPattern)) 
            NeighboursMap[pattern][Coord3D.Back].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Forward) && !NeighboursMap[pattern][Coord3D.Forward].Contains(otherPattern)) 
            NeighboursMap[pattern][Coord3D.Forward].Add(otherPattern);
    }


    public override void Observe() {
        
        //Build a list of nodes that have not been collapsed to a definite state.
        var collapsableNodes = GetCollapsableNodes();
        
        //Pick a random node from the collapsible nodes.
        if (collapsableNodes.Count == 0) {
            Contradiction = true;
            return;
        }
        var nodeCoords = collapsableNodes[Rnd.Next(collapsableNodes.Count)];
        var availableNodeStates = outputMatrix[nodeCoords.X, nodeCoords.Y, nodeCoords.Z];

        if (ProbabilisticModel) {

            //Eliminate all duplicates from the list of possible states.
            availableNodeStates = availableNodeStates.Distinct().ToList().Shuffle().ToList();

            //Choose a state according to the probability distribution of the states in the input model.
            double runningTotal = 0;
            var totalProb = probabilites.Select(x => x)
                .Where(x => availableNodeStates.Contains(x.Key))
                .Sum(x => x.Value);
            var rndNumb = Rnd.NextDouble() * totalProb;
            foreach (var availableNodeState in availableNodeStates) {
                runningTotal += probabilites[availableNodeState];
                if (runningTotal > rndNumb) {
                    outputMatrix.SetValue(new List<int>() {availableNodeState}, nodeCoords.X, nodeCoords.Y,
                        nodeCoords.Z);
                    break;
                }
            }
        }
        else {
            //Collapse it to a random definite state.
            outputMatrix.SetValue(new List<int>() { availableNodeStates[Rnd.Next(availableNodeStates.Count)] }, nodeCoords.X, nodeCoords.Y, nodeCoords.Z);
        }


        Propagate(nodeCoords);

        NumGen++;
    }

    protected override void Propagate(Coord3D startPoint) {

        //Queue the first element.
        var nodesToVisit = new Queue<Coord3D>();
        nodesToVisit.Enqueue(startPoint);

        //Perform a Breadth-First grid traversal.
		while (nodesToVisit.Any()) {
            var current = nodesToVisit.Dequeue();

            //Get the list of the allowed neighbours of the current node
		    var nghbrsMaps = outputMatrix[current.X, current.Y, current.Z].Select(possibleElement => NeighboursMap[possibleElement]).ToList();

		    var allowedNghbrs = nghbrsMaps.SelectMany(dict => dict)
		        .ToLookup(pair => pair.Key, pair => pair.Value)
		        .ToDictionary(group => group.Key, group => group.SelectMany(list => list).ToList());


		    //For every possible direction check if the node has already been affected by the propagation.
		    //If it hasn't queue it up and mark it as visited, otherwise move on.
		    foreach (var direction in Directions) {
		        var nodeToBeChanged = current.Add(direction.X, direction.Y, direction.Z);

		        if (outputMatrix.OutOfBounds(nodeToBeChanged) && !Periodic) {
		            continue;
		        }

		        if (outputMatrix.OutOfBounds(nodeToBeChanged) && Periodic) {
		            nodeToBeChanged = new Coord3D(Mod(nodeToBeChanged.X, outputMatrix.GetLength(0)),
		                Mod(nodeToBeChanged.Y, outputMatrix.GetLength(1)),
		                Mod(nodeToBeChanged.Z, outputMatrix.GetLength(2)));

		            if (outputMatrix.OutOfBounds(nodeToBeChanged)) {
		                continue;
		            }
		        }

		        //Count the states before the propagation.
		        var statesBefore = outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count;

		        //Eliminate neighbours that are not allowed from the output matrix
		        var allowedNghbrsInDirection = allowedNghbrs[direction].Distinct().ToList();
		        outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z]
		            .RemoveAll(neighbour => !allowedNghbrsInDirection.Contains(neighbour));

		        //Count the states after, if nbBefore != nbAfter queue it up.
		        var statesAfter = outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count;

		        //Check for contradictions
		        // TODO Add a backtrack recovery system to remedy the contradictions.
		        if (outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count == 0) {
		            Contradiction = true;
		            return;
		        }

		        //Queue it up in order to spread the info to its neighbours and mark it as visited.
		        if (statesBefore != statesAfter) {
		            if (!nodesToVisit.Contains(nodeToBeChanged)) {
		                nodesToVisit.Enqueue(nodeToBeChanged);
		            }
		        }
		    }
		}

        GenerationFinished = CheckIfFinished();
    }

    public override byte[,,] GetOutput() {
        var res = new byte[outputMatrix.GetLength(0) * PatternSize, outputMatrix.GetLength(1) * PatternSize, outputMatrix.GetLength(2) * PatternSize];
        for (var x = 0; x < outputMatrix.GetLength(0); x++) {
            for (var y = 0; y < outputMatrix.GetLength(1); y++) {
                for (var z = 0; z < outputMatrix.GetLength(2); z++) {

                    var currentPattern = patterns[outputMatrix[x, y, z].First()];
                    for (var i = 0; i < currentPattern.GetLength(0); i++) {
                        for (var j = 0; j < currentPattern.GetLength(1); j++) {
                            for (var k = 0; k < currentPattern.GetLength(2); k++) {
                                res[(x * currentPattern.GetLength(0)) + i, (y * currentPattern.GetLength(1)) + j,
                                    (z * currentPattern.GetLength(2)) + k] = currentPattern[i, j, k];
                            }
                        }
                    }
                }
            }
        }
        return res;
    }
}