using UnityEngine;


public class Demo : MonoBehaviour {
    [SerializeField] private string voxFileName;

    [SerializeField] private bool optimise = false;
    [SerializeField] private bool overlapping = true;
    [SerializeField] private bool probabilisticModel = true;

    private DiscreteModel model;

    [SerializeField] private int patternSize = 3;
    [SerializeField] Vector3 outputSize = new Vector3(10, 10, 10);

    private GameObject inputVoxelModelObj;

    private void Start() {
        //Read the .vox file
        var inputModel = VoxReaderWriter.ReadVoxelFile(voxFileName);

        //Display the voxel model.
        inputVoxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxModel = inputVoxelModelObj?.GetComponent<VoxelModel>();
        voxModel?.Display(inputModel.Voxels);

        //Center the 3D model and init grid
        voxModel.transform.position = Vector3.zero;
        voxModel.transform.rotation = Quaternion.AngleAxis(90, Vector3.left);

        var outputSizeInCoord = new Coord3D((int) outputSize.x, (int) outputSize.y, (int) outputSize.z);
        model = new DiscreteModel(inputModel, patternSize, outputSizeInCoord, overlapping, probabilisticModel);
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
    }

    private static void DisplayOutput(byte[,,] output) {
        var voxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxelModel = voxelModelObj?.GetComponent<VoxelModel>();

        voxelModel?.Display(output);

        voxelModelObj.transform.position = Vector3.zero;
        voxelModelObj.transform.rotation = Quaternion.AngleAxis(90, Vector3.left);
    }
}