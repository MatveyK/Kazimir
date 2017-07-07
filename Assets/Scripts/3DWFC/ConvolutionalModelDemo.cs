using UnityEngine;

public class ConvolutionalModelDemo : Demo {

    [SerializeField] private bool cleanOutput = false;

    private void Start() {
        var inputModel = Init();
        
        var outputSizeInCoord = new Coord3D((int) outputSize.x, (int) outputSize.y, (int) outputSize.z);

        Model = new ConvolutionalModel(inputModel, patternSize, outputSizeInCoord, periodic, probabilisticModel);
    }

    private void Update() {
        base.Update();
    }
}