using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace mattatz.VoxelSystem {

    [RequireComponent (typeof(MeshFilter))]
    public class Demo : MonoBehaviour {

        [SerializeField] int count = 10;
        List<Voxel> voxels;

        void Start () {
            var filter = GetComponent<MeshFilter>();
            voxels = Voxelizer.Voxelize(filter.mesh, count);
            voxels.ForEach(voxel => {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = transform;
                cube.transform.localPosition = voxel.position;
                cube.transform.localScale = voxel.size * Vector3.one;
                cube.transform.localRotation = Quaternion.identity;

                var boxCollider = cube.AddComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = Vector3.one;
                var rigidBody = cube.AddComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = true;
                rigidBody.mass = 0f;
                rigidBody.angularDrag = 0f;
                var renderer = cube.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            });
			Debug.Log ("TOTAL CUBES: " + voxels.Count);
            Debug.Log("SIZE: " + GetComponent<MeshCollider>().bounds.size);

            //Lock the Framerate to 30 FPS
            Application.targetFrameRate = 30;

            //Center the model and init grid
            transform.position = Vector3.zero;
            var inputModelSize = GetComponent<MeshCollider>().bounds.size;


            var gridPrefab = Resources.Load("Prefabs/Grid");
            var gridObj = Instantiate(gridPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            var grid = gridObj.GetComponent<Grid>();
            //grid.Init2(inputModelSize.y, 200f);
            grid.Init(this.gameObject, 5f);
        }
        
        // void Update () {}

        void OnDrawGizmos () {
            if (voxels == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            voxels.ForEach(voxel => {
                Gizmos.DrawCube(voxel.position, voxel.size * Vector3.one);
            });
        }

    }

}


