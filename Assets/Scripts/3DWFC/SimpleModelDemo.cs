using UnityEngine;

public class SimpleModelDemo : Demo {

    [SerializeField] private bool augmentNeighbours = true;

    private void Start() {
        var inputModel = Init();
        
        var outputSizeInCoord = new Coord3D((int) outputSize.x, (int) outputSize.y, (int) outputSize.z);
        
        Model = new SimpleModel(inputModel, patternSize, outputSizeInCoord, periodic, augmentNeighbours, probabilisticModel);
    }

    private void Update() {
        base.Update();
    }
}