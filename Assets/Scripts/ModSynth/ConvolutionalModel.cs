using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ConvolutionalModel : Model {

    public ConvolutionalModel(InputModel inputModel, int patternSize, Coord3D outputSize, bool periodic,
        bool probabilisticModel) {
        NeighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        Periodic = periodic;
        ProbabilisticModel = probabilisticModel;
        PatternSize = patternSize;
        NumGen = 0;

        OutputSize = outputSize;
        
        Init(inputModel, patternSize, periodic);
        FindNeighbours();
        
        InitOutputMatrix(outputSize);
        
        Debug.Log($"Model size: {new Vector3(inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z)}");
        Debug.Log("Model Ready!");
    }
    
    protected override void Init(InputModel inputModel, int patternSize, bool periodic) {
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
        NeighboursMap = new Dictionary<int, Dictionary<Coord3D, List<int>>>();
        
        //Init the structure
        for (var i = 0; i < patterns.Count; i++) {
            NeighboursMap[i] = new Dictionary<Coord3D, List<int>>();
            for (var j = 0; j < (2 * PatternSize - 1); j++) {
                for (var k = 0; k < (2 * PatternSize - 1); k++) {
                    for (var l = 0; l < (2 * PatternSize - 1); l++) {
                        NeighboursMap[i][new Coord3D(j, k, l)] = new List<int>();
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

        for (var i = 0; i < (2 * PatternSize - 1); i++) {
            for (var j = 0; j < (2 * PatternSize - 1); j++) {
                for (var k = 0; k < (2 * PatternSize - 1); k++) {
                    
                    var f = ConstructKernel(pattern, otherPattern, i, j, k);

                    var matchingVoxels = 0;
                    for (var x = 0; x < PatternSize; x++) {
                        for (var y = 0; y < PatternSize; y++) {
                            for (var z = 0; z < PatternSize; z++) {

                                if (f[x + i, y + j, z + k] == h[x, y, z]) {
                                    matchingVoxels++;
                                }
                            } 
                        }
                    }

                    if (matchingVoxels == PatternSize * PatternSize * PatternSize) {
                        //add to the pattern matrix
                        NeighboursMap[patternIndex][new Coord3D(i, j, k)].Add(otherPatternIndex);
                    }
                }
            }
        }
    }
    
    private byte[,,] ConstructKernel(byte[,,] pattern1, byte[,,] pattern2, int originX, int originY, int originZ) {
        var kernelSize = PatternSize + 2 * (PatternSize - 1);
        var res = new byte[kernelSize, kernelSize, kernelSize];


        for (var x = 0; x < kernelSize; x++) {
            for (var y = 0; y < kernelSize; y++) {
                for (var z = 0; z < kernelSize; z++) {

                    if ((x >= originX && x < originX + PatternSize) &&
                        (y >= originY && y < originY + PatternSize) &&
                        (z >= originZ && z < originZ + PatternSize)) {

                        res[x, y, z] = pattern2[x - originX, y - originY, z - originZ];
                        
                    }
                    
                    if((x >= PatternSize - 1 && x < kernelSize - PatternSize + 1) &&
                       (y >= PatternSize - 1 && y < kernelSize - PatternSize + 1) &&
                       (z >= PatternSize - 1 && z < kernelSize - PatternSize + 1)) {

                        res[x, y, z] = pattern1[x - (PatternSize - 1), y - (PatternSize - 1), z - (PatternSize - 1)];
                    }
                }
            }
        }
        
        return res;
    }


    public override void Observe() {
        //Build a list of nodes that have not been collapsed into a definite state.
        var collapsableNodes = GetCollapsableNodes();
        
        //Pick a random node from the collapsable nodes.
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
        } else {
            //Collapse into random definite state.
            outputMatrix.SetValue(new List<int>() { availableNodeStates[Rnd.Next(availableNodeStates.Count)] }, nodeCoords.X, nodeCoords.Y, nodeCoords.Z);
        }
        
        
        Propagate(nodeCoords);

        NumGen++;
    }

    protected override void Propagate(Coord3D startPoint) {
        
        //Queue the first element.
        var nodesToVisit = new Queue<Coord3D>();
        nodesToVisit.Enqueue(startPoint);

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
            for (var dx = -PatternSize + 1; dx < PatternSize; dx++) {
                for (var dy = -PatternSize + 1; dy < PatternSize; dy++) {
                    for (var dz = -PatternSize + 1; dz < PatternSize; dz++) {
                        
                        var nodeToBeChanged = current.Add(dx, dy, dz);
                        
                        //Manage the periodic vs non-periodic cases.
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

                        //Eliminate all neighbours that are not allowed form the output matrix
                        var propMatrixDirection =
                            new Coord3D(PatternSize - 1 - dx, PatternSize - 1 - dy, PatternSize - 1 - dz);
                        var allowedNghbrsInDirection =
                            allowedNghbrs[propMatrixDirection].Distinct().ToList();

                        outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z]
                            .RemoveAll(neighbour => !allowedNghbrsInDirection.Contains(neighbour));

                        if (outputMatrix[nodeToBeChanged.X, nodeToBeChanged.Y, nodeToBeChanged.Z].Count == 0) {
                            Contradiction = true;
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
        
        GenerationFinished = CheckIfFinished();
    }

    public override byte[,,] GetOutput() {
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
}