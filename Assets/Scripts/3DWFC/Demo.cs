using UnityEngine;


public class Demo : MonoBehaviour {
    [SerializeField] private string voxFileName = "castle";

    [SerializeField] private bool optimise = false;
    [SerializeField] private bool overlapping = true;
    [SerializeField] private bool probabilisticModel = true;
    [SerializeField] private bool addNeighbours = false;

    private DiscreteModel model;

    [SerializeField] private int patternSize = 2;
    [SerializeField] Vector3 outputSize = new Vector3(5, 5, 5);

    private GameObject inputVoxelModelObj;
    private GameObject outputVoxelModelObj;

    private void Start() {
        //Read the .vox file
        var inputModel = VoxReaderWriter.ReadVoxelFile(voxFileName);

        //Display the voxel model.
        inputVoxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxModel = inputVoxelModelObj?.GetComponent<VoxelModel>();
        voxModel?.Display(inputModel.Voxels, optimise);

        //Center the 3D model and init grid
        voxModel.transform.position = Vector3.zero;

        var outputSizeInCoord = new Coord3D((int) outputSize.x, (int) outputSize.y, (int) outputSize.z);
        model = new DiscreteModel(inputModel, patternSize, outputSizeInCoord, overlapping, addNeighbours, probabilisticModel);
    }


    private void Update() {
        if (Input.GetKeyDown("space")) {
            while (!model.GenerationFinished) {
                model.Observe();

                if (model.Contradiction) {
                    Debug.Log($"Generation Failed after {model.NumGen} iterations!");
                    model.Clear();
                }
            }
            Debug.Log($"Generation finished after {model.NumGen} iterations!");
        }
        if (Input.GetKeyDown("v")) {
            //Stop displaying the input model.
            inputVoxelModelObj.SetActive(false);

            var output = model.GetOutput();

            DisplayOutput(output);
        }
        if (Input.GetKeyDown("c")) {
            model.Clear();
            inputVoxelModelObj.SetActive(true);
            Destroy(outputVoxelModelObj);
            
            Debug.Log("Model cleared!");
        }
    }

    private void DisplayOutput(byte[,,] output) {
        outputVoxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxelModel = outputVoxelModelObj?.GetComponent<VoxelModel>();

        voxelModel?.Display(output, optimise);

        outputVoxelModelObj.transform.position = Vector3.zero;
    }
}