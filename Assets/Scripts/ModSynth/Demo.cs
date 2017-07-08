using UnityEngine;


public class Demo : MonoBehaviour {
    [SerializeField] private string voxFileName = "building";

    //[SerializeField] private bool optimise = false;
    [SerializeField] protected bool probabilisticModel = true;
    [SerializeField] protected bool periodic = true;

    protected Model Model;

    [SerializeField] protected int patternSize = 2;
    [SerializeField] protected Vector3 outputSize = new Vector3(5, 5, 5);

    [SerializeField] private string outVoxFileName = "test";

    private GameObject inputVoxelModelObj;
    private GameObject outputVoxelModelObj;

    protected InputModel Init() {
        //Read the .vox file
        var inputModel = VoxReaderWriter.ReadVoxelFile(voxFileName);

        //Display the voxel model.
        inputVoxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxModel = inputVoxelModelObj?.GetComponent<VoxelModel>();
        voxModel?.Display(inputModel.Voxels);

        //Center the 3D model and init grid
        voxModel.transform.position = Vector3.zero;

        return inputModel;
    }
    

    protected void GenerateOutput() {
        while (!Model.GenerationFinished) {
            Model.Observe();

            if (Model.Contradiction) {
                Debug.Log($"Generation Failed after {Model.NumGen} iterations!");
                Model.Clear();
            }
        }
        Debug.Log($"Generation finished after {Model.NumGen} iterations!");
    }

    protected void DisplayOutput() {
        //Stop displaying the input model.
        inputVoxelModelObj.SetActive(false);

        var output = Model.GetOutput();

        DisplayOutput(output);
    }

    protected void ClearModel() {
        Model.Clear();
        inputVoxelModelObj.SetActive(true);
        Destroy(outputVoxelModelObj);
        
        Debug.Log("Model cleared!");
    }

    protected void WriteToVoxFile() {
        var rawOutput = Model.GetOutput();
        var voxels = VoxReaderWriter.TransformOutputToVox(rawOutput);
        VoxReaderWriter.WriteVoxelFile(outVoxFileName, rawOutput.GetLength(0), rawOutput.GetLength(1), rawOutput.GetLength(2), voxels);
        Debug.Log($"Model written to {outVoxFileName}.vox !");
    }

    protected void Update() {
        if (Input.GetKeyDown("space")) {
            GenerateOutput();
        }
        if (Input.GetKeyDown("v")) {
            DisplayOutput();
        }
        if (Input.GetKeyDown("c")) {
            ClearModel();
        }
        //Write output to .vox format
        if (Input.GetKeyDown("w")) {
            WriteToVoxFile();
        }
    }

    private void DisplayOutput(byte[,,] output) {
        outputVoxelModelObj = Instantiate(Resources.Load("Prefabs/VoxelModel")) as GameObject;
        var voxelModel = outputVoxelModelObj?.GetComponent<VoxelModel>();

        voxelModel?.Display(output);

        outputVoxelModelObj.transform.position = Vector3.zero;
    }
}