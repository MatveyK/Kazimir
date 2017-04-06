using System.Collections.Generic;
using UnityEngine;

public class BenchMark : MonoBehaviour {

	List<GameObject> voxels = new List<GameObject>();

	[SerializeField] int nbCubes = 10000;
	[SerializeField] bool animate = true;

	//GridCell gCell;
    private Grid grid;

	void Start () {

		for(int i = 0; i < nbCubes; i++) {
			var voxel = Instantiate (Resources.Load ("Prefabs/Cube")) as GameObject;
			voxel.transform.parent = transform;
			voxel.transform.position = new Vector3 (Random.Range (0, 50), Random.Range (0, 50), Random.Range (0, 50));
			voxel.transform.localRotation = Quaternion.identity;

			voxels.Add (voxel);
		}
	}

	void Update() {
		if (animate) {
			for (var i = 0; i < voxels.Count; i++) {
				voxels [i].transform.Rotate (Vector3.up, Time.deltaTime * i);
				voxels [i].transform.Rotate (Vector3.left, Time.deltaTime * i);
			}
		}
	}
	
}
