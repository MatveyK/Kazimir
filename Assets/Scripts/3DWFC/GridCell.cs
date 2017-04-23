using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour {

    public int Id;

	List<GameObject> containedVoxels;

	Bounds bounds;

	void Start () {
	}

	public void Init(Vector3 center, float size) {
	    //Transform
	    this.transform.position = center;

	    //Bounds
		bounds = new Bounds (center, new Vector3(size, size, size));

	    //Contained Voxels
	    containedVoxels = collectVoxels();
	}


    private List<GameObject> collectVoxels() {
        var res = new List<GameObject>();
        foreach (var voxel in GameObject.FindGameObjectsWithTag("Voxel")) {
            if (bounds.Contains(voxel.transform.position)) {
                voxel.transform.parent = transform;
                res.Add(voxel);
            }
        }
        return res;
    }


    public List<GameObject> ContainedVoxels {
		get {
			return containedVoxels;
		}
	}
}
