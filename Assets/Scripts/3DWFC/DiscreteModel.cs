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

    private readonly Dictionary<int, Dictionary<Coord3D, List<int>>> neighboursMap;

    private readonly bool[,,] mapOfChanges;
    private List<int>[,,] outputMatrix;
    
    //Keep a list of states for backtracking
    private Stack<List<int>[,,]> States;
    private bool RollingBack = false;
    private int NbRollbackSteps;
    private List<Coord3D> ChosenPoints;
    private int TotalRollbacks;
    private const int TOTAL_ROLLBACKS_ALLOWED = 50;

    //Save these fields in case of reintialisation
    private readonly Coord3D outputSize;

    private bool generationFinished = false;
    private bool contradiction = false;
    private int numGen;


    public DiscreteModel(InputModel inputModel, int patternSize, Coord3D outputSize, bool overlapping = true, bool addNeighbours = false, bool probabilisticModel = true) {
        mapOfChanges = new bool[outputSize.X, outputSize.Y, outputSize.Z];
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
        if (addNeighbours) {
            DetectNeighbours();
        }
        InitOutputMatrix(outputSize);

        States = new Stack<List<int>[,,]>();
        NbRollbackSteps = 0;
        ChosenPoints = new List<Coord3D>();
        TotalRollbacks = 0;
        
        var j = 0;
        for (int i = 0; i < patterns.Count; i++) {
            j += Directions.Count(direction => neighboursMap[i][direction].Count == 0);
        }
        
        Debug.Log($"ERRORES {j}");

        Debug.Log($"Model size: {new Vector3(inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z)}");
        Debug.Log("Model Ready!");
    }

    public void InitSimpleModel(InputModel inputModel, int patternSize) {
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

    public void InitOverlappingModel(InputModel inputModel, int patternSize) {
        var inputMatrix = new byte[inputModel.Size.X, inputModel.Size.Y, inputModel.Size.Z];
        patterns = new List<byte[,,]>();
        patternMatrix = new int[inputModel.Size.X - patternSize + 1,
            inputModel.Size.Y - patternSize + 1,
            inputModel.Size.Z - patternSize + 1];
        probabilites = new Dictionary<int, double>();

        inputModel.Voxels.ForEach(voxel => inputMatrix[voxel.X, voxel.Y, voxel.Z] = voxel.Color);

        //Add "empty space" pattern.
        //patterns.Add(CreateEmptyPattern(patternSize));
        //probabilites[0] = 0;
        
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

                    neighboursMap[currentPattern][Coord3D.Left].Add(x - 1 >= 0 ? patternMatrix[x - 1, y, z] : patternMatrix[patternMatrix.GetLength(0) - 1, y, z]);
                    neighboursMap[currentPattern][Coord3D.Right].Add(x + 1 < patternMatrix.GetLength(0) ? patternMatrix[x + 1, y, z] : patternMatrix[0, y, z]);

                    neighboursMap[currentPattern][Coord3D.Down].Add(y - 1 >= 0 ? patternMatrix[x, y - 1, z] : patternMatrix[x, patternMatrix.GetLength(1) - 1, z]);
                    neighboursMap[currentPattern][Coord3D.Up].Add(y + 1 < patternMatrix.GetLength(1) ? patternMatrix[x, y + 1, z] : patternMatrix[x, 0, z]);

                    neighboursMap[currentPattern][Coord3D.Back].Add(z - 1 >= 0 ? patternMatrix[x, y, z - 1] : patternMatrix[x, y, patternMatrix.GetLength(2) - 1]);
                    neighboursMap[currentPattern][Coord3D.Forward].Add(z + 1 < patternMatrix.GetLength(2) ? patternMatrix[x, y, z + 1] : patternMatrix[x, y, 0]);
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
                if (!mapOfChanges.OutOfBounds(current.Add(direction)) &&
                    !mapOfChanges[current.X + direction.X, current.Y + direction.Y, current.Z + direction.Z] &&
                    !outputMatrix.OutOfBounds(current.Add(direction))) {

                    //Eliminate neighbours that are not allowed from the output matrix
                    var allowedNghbrsInDirection = allowedNghbrs[direction].Distinct().ToList();
                    outputMatrix[current.X + direction.X, current.Y + direction.Y, current.Z + direction.Z]
                        .RemoveAll(neighbour => !allowedNghbrsInDirection.Contains(neighbour));

                    //Check for contradictions
                    // TODO Add a backtrack recovery system to remedy the contradictions.
                    if (outputMatrix[current.X + direction.X, current.Y + direction.Y, current.Z + direction.Z].Count == 0) {
                        try {
                            NbRollbackSteps++;
                            ChosenPoints.Add(new Coord3D(x, y, z));
                            RollbackState(NbRollbackSteps);
                            TotalRollbacks++;
                            return;
                        }
                        catch (InvalidOperationException e) {
                            contradiction = true;
                            return;
                        }
                    }

                    //Queue it up in order to spread the info to its neighbours and mark it as visited.
                    nodesToVisit.Enqueue(current.Add(direction));
                    mapOfChanges.SetValue(true, current.X + direction.X, current.Y + direction.Y, current.Z + direction.Z);
                }
            }
        }

        NbRollbackSteps = 0;
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
        NbRollbackSteps = 0;
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

    private void RollbackState(int nbSteps) {
        if(States.Count == 0) throw new InvalidOperationException();
        
        for (var i = 0; i < nbSteps - 1 && States.Count > 0; i++) {
            States.Pop();
        }

        outputMatrix = States.Pop();
        RollingBack = true;
    }



    public Dictionary<int, Dictionary<Coord3D, List<int>>> NeighboursMap => neighboursMap;

    public bool GenerationFinished => generationFinished;

    public bool Contradiction => contradiction;

    public int NumGen => numGen;
}
