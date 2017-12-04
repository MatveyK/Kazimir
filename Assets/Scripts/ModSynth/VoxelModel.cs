using System.Collections.Generic;
using UnityEngine;

public class VoxelModel : MonoBehaviour {

    //Max number of vertices allowed in a single mesh
    private const int VertexLimit = 60000;

    private readonly List<GameObject> allVoxels = new List<GameObject>();

    public void Display(byte[,,] output, bool optimise = true) {

        var voxelCube = Resources.Load("Prefabs/Cube");

        for (var x = 0; x < output.GetLength(0); x++) {
            for (var y = 0; y < output.GetLength(1); y++) {
                for (var z = 0; z < output.GetLength(2); z++) {

                    if (output[x, y, z] != 0) {
                        var cube = Instantiate(voxelCube) as GameObject;
                        cube.transform.parent = transform;
                        cube.transform.localPosition = new Vector3(x, y, z);
                        cube.transform.localScale = Vector3.one;
                        cube.transform.localRotation = Quaternion.identity;
                        
                        //Set colour from the .vox palette
                        cube.GetComponent<Renderer>().material.color = VoxReaderWriter.Palette[output[x, y, z]];

                        cube.tag = "Voxel";
                        
                        allVoxels.Add(cube);
                    }
                }
            }
        }

        if (optimise) {
            combineVoxelMeshes();
            foreach (var voxel in allVoxels) {
                Destroy(voxel);
            }
        }
    }

    public void Display(List<Voxel> voxels, bool optimise = true) {
        //Load the voxel prefab
        var voxelCube = Resources.Load("Prefabs/Cube");

        voxels.ForEach(voxel => {
            var cube = Instantiate(voxelCube) as GameObject;
            cube.transform.parent = transform;
            cube.transform.localPosition = new Vector3(voxel.X, voxel.Y, voxel.Z);
            cube.transform.localScale = Vector3.one;
            cube.transform.localRotation = Quaternion.identity;
            
            //Set colours from the .vox file
            cube.GetComponent<Renderer>().material.color = VoxReaderWriter.Palette[voxel.Color];

            cube.tag = "Voxel";
        });
        Debug.Log("TOTAL CUBES: " + voxels.Count);

        if (optimise) {
            combineVoxelMeshes();
            foreach (var voxel in allVoxels) {
                Destroy(voxel);
            }
        }
    }
    
    
    public List<GameObject> combinedMeshList = new List<GameObject>();

    //Combine the voxel meshes
    private void combineVoxelMeshes() {
        var voxelList = new List<CombineInstance>();
        
        var combine = new CombineInstance();
        
        //Keep track of the vertices in the list so we know when we have reached the limit.
        var verticesSoFar = 0;
        
        //Keep track of which gameobject the mesh has been combined into.
        var meshListCounter = 0;
        
        //Loop through all the voxels.
        for (var i = 0; i < allVoxels.Count; i++) {
            allVoxels[i].SetActive(false);
            
            //Get the mesh
            MeshFilter meshFilter = allVoxels[i].GetComponent<MeshFilter>();

            combine.mesh = meshFilter.mesh;
            combine.transform = meshFilter.transform.localToWorldMatrix;
            
            //Add it to the list of the mesh data
            voxelList.Add(combine);

            verticesSoFar += meshFilter.mesh.vertexCount;
            
            //Have we reached the limit?
            if (verticesSoFar > VertexLimit) {
                //If so we have added too many vertices, we thus undo the last step
                i -= 1;
                
                //And we remove the last mesh added to the list
                voxelList.RemoveAt(voxelList.Count - 1);
                
                //Now we can create a combined mesh of the meshes we have collected so far
                CreateCombinedMesh(voxelList, combinedMeshList);
                
                //Reset the lists with mesh data
                voxelList.Clear();

                verticesSoFar = 0;
                meshListCounter++;
            }
        }

        CreateCombinedMesh(voxelList, combinedMeshList);
    }
    
    //Creates a combined mesh from a list and adds it to a game object
    void CreateCombinedMesh(List<CombineInstance> meshDataList, List<GameObject> combinedHolderList) {
        
        //Create the new combined mesh
        var newMesh = new Mesh();
        newMesh.CombineMeshes(meshDataList.ToArray());
        
        var meshHolderObj = new GameObject();
        
        //Create new game object that will hold the combined mesh
        var combinedMeshHolder = Instantiate(meshHolderObj, Vector3.zero, Quaternion.identity);

        combinedMeshHolder.transform.parent = transform;

        combinedMeshHolder.AddComponent<MeshFilter>();
        combinedMeshHolder.AddComponent<MeshRenderer>();
        
        //Add to the mesh
        combinedMeshHolder.GetComponent<MeshFilter>().mesh = newMesh;
        combinedMeshHolder.GetComponent<MeshRenderer>().material.color = Color.white;
        
        //Add the combined holder to the list
        combinedHolderList.Add(combinedMeshHolder);
    }

}