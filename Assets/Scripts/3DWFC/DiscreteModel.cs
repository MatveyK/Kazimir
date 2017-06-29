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

    private readonly bool probabilisticModel;

    private int[,,] patternMatrix;
    private List<byte[,,]> patterns;
    private readonly int patternSize;
    private Dictionary<int, double> probabilites;

    private Dictionary<int, Dictionary<Coord3D, List<int>>> neighboursMap;
    private bool periodic;

    private readonly bool[,,] mapOfChanges;
    private List<int>[,,] outputMatrix;
    
    //Keep a list of states for backtracking
    private Stack<List<int>[,,]> States;
    private bool RollingBack = false;
    private List<Coord3D> ChosenPoints;
    private int TotalRollbacks;
    private const int TOTAL_ROLLBACKS_ALLOWED = 50;

    //Save these fields in case of reintialisation
    private readonly Coord3D outputSize;

    private bool generationFinished = false;
    private bool contradiction = false;
    private int numGen;

    private bool Ground;


    public DiscreteModel(InputModel inputModel, int patternSize, Coord3D outputSize, bool overlapping = true, bool periodic = true, bool addNeighbours = false, bool probabilisticModel = true) {
        mapOfChanges = new bool[outputSize.X, outputSize.Y, outputSize.Z];
        neighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        this.periodic = periodic;
        this.probabilisticModel = probabilisticModel;
        this.patternSize = patternSize;
        numGen = 0;

        this.outputSize = outputSize;

        if (overlapping) {
            InitOverlappingModel(inputModel, patternSize, periodic);
            FindNeighbours();
        }
        else {
            InitSimpleModel(inputModel, patternSize, false);
            InitNeighboursMap(periodic);
            if (addNeighbours) {
                DetectNeighbours();
            }
        }
        InitOutputMatrix(outputSize);

        States = new Stack<List<int>[,,]>();
        ChosenPoints = new List<Coord3D>();
        TotalRollbacks = 0;
        

        Debug.Log($"Model size: {new Vector3(inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z)}");
        Debug.Log("Model Ready!");
    }

    public void InitSimpleModel(InputModel inputModel, int patternSize, bool ground) {
        var inputMatrix = new byte[inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z];
        patterns = new List<byte[,,]>();
        patternMatrix = new int[(int) Math.Ceiling((double) (inputModel.Size.X / patternSize)),
            (int) Math.Ceiling((double) (inputModel.Size.Y / patternSize)),
            (int) Math.Ceiling((double) (inputModel.Size.Z / patternSize))];
        probabilites = new Dictionary<int, double>();

        Ground = ground;

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


    private static byte[,,] GetCurrentPattern(byte[,,] matrix, int x, int y, int z, int patternSize) {
        var pattern = new byte[patternSize, patternSize, patternSize];
        for (var i = x; i < x + patternSize; i++) {
            for (var j = y; j < y + patternSize; j++) {
                for (var k = z; k < z + patternSize; k++) {
                    pattern[i - x, j - y, k - z] = matrix[i % matrix.GetLength(0), j % matrix.GetLength(1), k % matrix.GetLength(2)];
                }
            }
        }
        return pattern;
    }

    private void InitNeighboursMap(bool periodic) {
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

                    if (x - 1 >= 0) {
                        neighboursMap[currentPattern][Coord3D.Left].Add(patternMatrix[x - 1, y, z]);
                    } else {
                        if (periodic) {
                            neighboursMap[currentPattern][Coord3D.Left].Add(patternMatrix[patternMatrix.GetLength(0) - 1, y, z]);
                        }
                    }
                    if (x + 1 < patternMatrix.GetLength(0)) {
                        neighboursMap[currentPattern][Coord3D.Right].Add(patternMatrix[x + 1, y, z]);
                    } else {
                        if (periodic) {
                            neighboursMap[currentPattern][Coord3D.Right].Add(patternMatrix[0, y, z]);
                        }
                    }

                    if (y - 1 >= 0) {
                        neighboursMap[currentPattern][Coord3D.Down].Add(patternMatrix[x, y - 1, z]);
                    } else {
                        if (periodic) {
                            if (!Ground) {
                                neighboursMap[currentPattern][Coord3D.Down].Add(patternMatrix[x, patternMatrix.GetLength(1) - 1, z]);
                            }
                        }
                    }
                    if (y + 1 < patternMatrix.GetLength(1)) {
                        neighboursMap[currentPattern][Coord3D.Up].Add(patternMatrix[x, y + 1, z]);
                    } else {
                        if (periodic) {
                            neighboursMap[currentPattern][Coord3D.Up].Add(patternMatrix[x, 0, z]);
                        }
                    }

                    if (z - 1 >= 0) {
                        neighboursMap[currentPattern][Coord3D.Back].Add(patternMatrix[x, y, z - 1]);
                    } else {
                        if (periodic) {
                            neighboursMap[currentPattern][Coord3D.Back].Add(patternMatrix[x, y, patternMatrix.GetLength(2) - 1]);
                        }
                    }
                    if (z + 1 < patternMatrix.GetLength(2)) {
                        neighboursMap[currentPattern][Coord3D.Forward].Add(patternMatrix[x, y, z + 1]);
                    } else {
                        if (periodic) {
                            neighboursMap[currentPattern][Coord3D.Forward].Add(patternMatrix[x, y, 0]);
                        }
                    }
                }
            }
        }
        
        //Eliminate duplicates in the neighbours map.
        for (var i = 0; i < patterns.Count; i++) {
            foreach (var direction in Directions) {
                neighboursMap[i][direction] = neighboursMap[i][direction].Distinct().ToList();
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
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Left) && !neighboursMap[pattern][Coord3D.Left].Contains(otherPattern)) 
            neighboursMap[pattern][Coord3D.Left].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Right) && !neighboursMap[pattern][Coord3D.Right].Contains(otherPattern))
            neighboursMap[pattern][Coord3D.Right].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Down) && !neighboursMap[pattern][Coord3D.Down].Contains(otherPattern)) 
            neighboursMap[pattern][Coord3D.Down].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Up) && !neighboursMap[pattern][Coord3D.Up].Contains(otherPattern)) 
            neighboursMap[pattern][Coord3D.Up].Add(otherPattern);
        
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Back) && !neighboursMap[pattern][Coord3D.Back].Contains(otherPattern)) 
            neighboursMap[pattern][Coord3D.Back].Add(otherPattern);
        if(patternStruct.FitsPattern(otherPatternStruct, Coord3D.Forward) && !neighboursMap[pattern][Coord3D.Forward].Contains(otherPattern)) 
            neighboursMap[pattern][Coord3D.Forward].Add(otherPattern);
    }

    private void InitOutputMatrix(Coord3D size) {
        outputMatrix = new List<int>[size.X, size.Y, size.Z];

        for (var x = 0; x < size.X; x++) {
            for (var y = 0; y < size.Y; y++) {
                for (var z = 0; z < size.Z; z++) {
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

        //Save the current state if we are not "rolling back"
        if (!RollingBack) {
            States.Push(outputMatrix.CloneMatrix());
        } else {
            RollingBack = false;
        }

        //Build a list of nodes that have not been collapsed to a definite state.
        var collapsableNodes = GetCollapsableNodes();
        
        //Remove the previously picked state from the list of possible states in case of backtrack.
        collapsableNodes.RemoveAll(x => ChosenPoints.Contains(x));

        //Pick a random node from the collapsible nodes.
        if (collapsableNodes.Count == 0) {
            contradiction = true;
            return;
        }
        var nodeCoords = collapsableNodes[Rnd.Next(collapsableNodes.Count)];
        var availableNodeStates = outputMatrix[nodeCoords.X, nodeCoords.Y, nodeCoords.Z];

        if (probabilisticModel) {

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


        Propagate(nodeCoords.X, nodeCoords.Y, nodeCoords.Z);

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
		    mapOfChanges.SetValue(true, current.X, current.Y, current.Z);

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

		            if (mapOfChanges.OutOfBounds(nodeToBeChanged)) {
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
		            try {
		                ChosenPoints.Add(new Coord3D(x, y, z));
		                RollbackState();
		                TotalRollbacks++;
		                return;
		            }
		            catch (InvalidOperationException e) {
		                contradiction = true;
		                return;
		            }
		        }

		        //Queue it up in order to spread the info to its neighbours and mark it as visited.
		        if (statesBefore != statesAfter) {
		            if (!nodesToVisit.Contains(nodeToBeChanged)) {
		                nodesToVisit.Enqueue(nodeToBeChanged);
		            }
		        }
		    }
		}

        ChosenPoints.Clear();
        if (TotalRollbacks > TOTAL_ROLLBACKS_ALLOWED) {
            contradiction = true;
            return;
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
        
        States.Clear();
        ChosenPoints.Clear();
        TotalRollbacks = 0;
    }

    public byte[,,] GetOutput() {
        var res = new byte[outputMatrix.GetLength(0) * patternSize, outputMatrix.GetLength(1) * patternSize, outputMatrix.GetLength(2) * patternSize];
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
        DisplayOutputStats(outputMatrix);
        return res;
    }

    private void DisplayOutputStats(List<int>[,,] matrix) {
        var stats = matrix.Cast<List<int>>()
            .ToList()
            .GroupBy(state => state.First())
            .ToDictionary(group => group.Key, group => (float) group.Count() / (float) outputMatrix.Length);
    }

    private static byte[,,] CreateEmptyPattern(int pSize) {
        var res = new byte[pSize, pSize, pSize];

        for (var i = 0; i < res.GetLength(0); i++) {
            for (var j = 0; j < res.GetLength(1); j++) {
                for (var k = 0; k < res.GetLength(2); k++) {
                    res[i, j, k] = 0;
                }
            }
        }
        return res;
    }

    private void RollbackState() {
        if(States.Count == 0) throw new InvalidOperationException();

        var mean = (int) Math.Ceiling((double) (States.Count * 2 / 3));
        var variance = (int) States.Count - mean;

        var stateIndex = Extensions.GenerateNormalDistrVar(mean, variance);

        if (mean == 1 || stateIndex > States.Count) {
            outputMatrix = States.Pop();
        } else {
            for (int i = 0; i < States.Count - stateIndex - 1; i++) {
                States.Pop();
            }
            outputMatrix = States.Pop();
        }

        RollingBack = true;
    }
    
    
    //Overlapping model section ===============================================================

    public void InitOverlappingModel(InputModel inputModel, int patternSize, bool periodic) {
        var inputMatrix = new byte[inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z];
        patterns = new List<byte[,,]>();
        probabilites = new Dictionary<int, double>();
        
        if (periodic) {
            patternMatrix = new int[inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z];
        } else {
            patternMatrix = new int[inputModel.Size.X - patternSize + 1,
                inputModel.Size.Y - patternSize + 1,
                inputModel.Size.Z - patternSize + 1];
        }

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

    private void FindNeighbours() {
        neighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        
        //Init the structure
        for (var i = 0; i < patterns.Count; i++) {
            neighboursMap[i] = new Dictionary<Coord3D, List<int>>();
            for (var j = 0; j < (2 * patternSize - 1); j++) {
                for (var k = 0; k < (2 * patternSize - 1); k++) {
                    for (var l = 0; l < (2 * patternSize - 1); l++) {
                        neighboursMap[i][new Coord3D(j, k, l)] = new List<int>();
                    }
                }
            }
        }
        
        for (var i = 0; i < patterns.Count; i++) {
            for (var j = 0; j < patterns.Count; j++) {
                ConvolutePatterns(i, j);
            }
        }
    }

    private void ConvolutePatterns(int patternIndex, int otherPatternIndex) {
        var pattern = patterns[patternIndex];
        var otherPattern = patterns[otherPatternIndex];

        var h = otherPattern;

        for (var i = 0; i < (2 * patternSize - 1); i++) {
            for (var j = 0; j < (2 * patternSize - 1); j++) {
                for (var k = 0; k < (2 * patternSize - 1); k++) {
                    
                    var f = ConstructKernel(pattern, otherPattern, i, j, k);

                    var matchingVoxels = 0;
                    for (var x = 0; x < patternSize; x++) {
                        for (var y = 0; y < patternSize; y++) {
                            for (var z = 0; z < patternSize; z++) {

                                if (f[x + i, y + j, z + k] == h[x, y, z]) {
                                    matchingVoxels++;
                                }
                            } 
                        }
                    }

                    if (matchingVoxels == Math.Pow(patternSize, 3)) {
                        //add to the pattern matrix
                        neighboursMap[patternIndex][new Coord3D(i, j, k)].Add(otherPatternIndex);
                    }
                }
            }
        }
    }

    private byte[,,] ConstructKernel(byte[,,] pattern1, byte[,,] pattern2, int originX, int originY, int originZ) {
        var kernelSize = patternSize + 2 * (patternSize - 1);
        var res = new byte[kernelSize, kernelSize, kernelSize];


        for (var x = 0; x < kernelSize; x++) {
            for (var y = 0; y < kernelSize; y++) {
                for (var z = 0; z < kernelSize; z++) {

                    if ((x >= originX && x < originX + patternSize) &&
                        (y >= originY && y < originY + patternSize) &&
                        (z >= originZ && z < originZ + patternSize)) {

                        res[x, y, z] = pattern2[x - originX, y - originY, z - originZ];
                        
                    }
                    
                    if((x >= patternSize - 1 && x < kernelSize - patternSize + 1) &&
                       (y >= patternSize - 1 && y < kernelSize - patternSize + 1) &&
                       (z >= patternSize - 1 && z < kernelSize - patternSize + 1)) {

                        res[x, y, z] = pattern1[x - (patternSize - 1), y - (patternSize - 1), z - (patternSize - 1)];
                    }
                }
            }
        }
        
        return res;
    }

    public void Observe2() {

        //Build a list of nodes that have not been collapsed into a definite state.
        var collapsableNodes = GetCollapsableNodes();
        
        //Pick a random node from the collapsable nodes.
        if (collapsableNodes.Count == 0) {
            contradiction = true;
            return;
        }
        var nodeCoords = collapsableNodes[Rnd.Next(collapsableNodes.Count)];
        var availableNodeStates = outputMatrix[nodeCoords.X, nodeCoords.Y, nodeCoords.Z];
        
        if (probabilisticModel) {

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
        } else {
            //Collapse into random definite state.
            outputMatrix.SetValue(new List<int>() { availableNodeStates[Rnd.Next(availableNodeStates.Count)] }, nodeCoords.X, nodeCoords.Y, nodeCoords.Z);
        }
        
        
        Propagate2(nodeCoords.X, nodeCoords.Y, nodeCoords.Z);

        numGen++;
    }

    private void Propagate2(int x, int y, int z) {
        //Reset the map that keeps track of the changes.
        ReInitMapOfChanges();
        
        //Queue the first element.
        var nodesToVisit = new Queue<Coord3D>();
        nodesToVisit.Enqueue(new Coord3D(x, y, z));

        while (nodesToVisit.Any()) {
            var current = nodesToVisit.Dequeue();
            
            //Get the list of the allowed neighbours of the current node
            var nghbrsMaps = outputMatrix[current.X, current.Y, current.Z]
                .Select(possibleElem => NeighboursMap[possibleElem]).ToList();

            var allowedNghbrs = nghbrsMaps.SelectMany(dict => dict)
                .ToLookup(pair => pair.Key, pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.SelectMany(list => list).ToList());
            
            //For every possible direction check if the node has already been reached by the propagtion.
            //If not, queue it up and mark it as visited, otherwise move on.
            for (var dx = -patternSize + 1; dx < patternSize; dx++) {
                for (var dy = -patternSize + 1; dy < patternSize; dy++) {
                    for (var dz = -patternSize + 1; dz < patternSize; dz++) {
                        
                        var nodeToBeChanged = current.Add(dx, dy, dz);
                        
                        //Manage the periodic vs non-periodic cases.
                        if (outputMatrix.OutOfBounds(nodeToBeChanged) && !Periodic) {
                            continue;
                        }
                        
                        if (outputMatrix.OutOfBounds(nodeToBeChanged) && Periodic) {
                            nodeToBeChanged = new Coord3D(Mod(nodeToBeChanged.X, outputMatrix.GetLength(0)),
                                Mod(nodeToBeChanged.Y, outputMatrix.GetLength(1)),
                                Mod(nodeToBeChanged.Z, outputMatrix.GetLength(2)));
                            
                            if (mapOfChanges.OutOfBounds(nodeToBeChanged)) {
                                continue;
                            }
                        }

                        //Count the states before the propagation.
                        var statesBefore = outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count;

                        //Eliminate all neighbours that are not allowed form the output matrix
                        var propMatrixDirection =
                            new Coord3D(patternSize - 1 - dx, patternSize - 1 - dy, patternSize - 1 - dz);
                        var allowedNghbrsInDirection =
                            allowedNghbrs[propMatrixDirection].Distinct().ToList();

                        outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z]
                            .RemoveAll(neighbour => !allowedNghbrsInDirection.Contains(neighbour));

                        if (outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count == 0) {
                            contradiction = true;
                            return;
                        }
                        
                        //Count the states after, if nbBefore != nbAfter queue it up.
                        var statesAfter = outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count;

                        if (statesBefore != statesAfter) {

                            if (!nodesToVisit.Contains(nodeToBeChanged)) {
                                nodesToVisit.Enqueue(nodeToBeChanged);
                            }
                        }

                    }
                }
            }
        }
        
        generationFinished = CheckIfFinished();
    }

    public byte[,,] GetOutput2() {
        var res = new byte[outputMatrix.GetLength(0), outputMatrix.GetLength(1), outputMatrix.GetLength(2)];
        for (var x = 0; x < outputMatrix.GetLength(0); x++) {
            for (var y = 0; y < outputMatrix.GetLength(1); y++) {
                for (var z = 0; z < outputMatrix.GetLength(2); z++) {
                    var pattern = outputMatrix[x, y, z].First();
                    res[x, y, z] = patterns[pattern][0,0,0];
                }
            }
        }
        return res;
    }

    public static int Mod(int n, int m) {
        return ((n % m) + m) % m;
    }


    public Dictionary<int, Dictionary<Coord3D, List<int>>> NeighboursMap => neighboursMap;
    
    public bool Periodic => periodic;

    public bool GenerationFinished => generationFinished;

    public bool Contradiction => contradiction;

    public int NumGen => numGen;
}
