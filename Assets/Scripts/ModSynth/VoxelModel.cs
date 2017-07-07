using System.Collections.Generic;
using UnityEngine;

public class VoxelModel : MonoBehaviour {

    //Max number of vertices allowed in a single mesh by Unity
    private const double VertexLimit = 65500;

    public void Display(byte[,,] output, bool optimise) {

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
                    }
                }
            }
        }
        
        if(optimise) Optimise3DModel();
    }

    public void Display(List<Voxel> voxels, bool optimise) {
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
        
        if(optimise) Optimise3DModel();
    }

    private void Optimise3DModel() {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        var organisedMeshFilters = OrganiseMeshes(meshFilters);

        foreach (var organisedMeshFilter in organisedMeshFilters) {
            CombineInstance[] combine = new CombineInstance[organisedMeshFilter.Count];
            for (var i = 0; i < organisedMeshFilter.Count; i++) {
                combine[i].mesh = organisedMeshFilter[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                organisedMeshFilter[i].gameObject.SetActive(false);
            }

            //Create a new game object for each large mesh
            //TODO Look into a solution that uses submeshes
            var gameObj = new GameObject();

            //Combine the voxel meshes to form one big mesh
            gameObj.AddComponent<MeshFilter>();
            gameObj.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            gameObj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);

            //Add the renderer with a default material
            gameObj.AddComponent<MeshRenderer>();
            gameObj.GetComponent<MeshRenderer>().enabled = true;
            gameObj.GetComponent<MeshRenderer>().material = Resources.Load("Materials/VoxelMaterial") as Material;

            //Set the object containing the mesh as the child of the current object
            gameObj.transform.parent = transform;
            transform.gameObject.SetActive(true);
        }
    }

    private static List<List<MeshFilter>> OrganiseMeshes(MeshFilter[] meshFilters) {
        var res = new List<List<MeshFilter>>();
        var verticesSoFar = 0;

        var index = 0;
        res.Add(new List<MeshFilter>());
        var currList = res[index];
        for (var i = 0; i < meshFilters.Length; i++) {
            if (verticesSoFar + meshFilters[i].mesh.vertexCount > VertexLimit) {
                index++;
                res.Add(new List<MeshFilter>());
                currList = res[index];
                verticesSoFar = 0;
            }

            currList.Add(meshFilters[i]);
            verticesSoFar += meshFilters[i].mesh.vertexCount;
        }

        return res;
    }
}