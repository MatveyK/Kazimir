using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour {

    public int Id;

	List<GameObject> containedVoxels;

	Bounds bounds;

    float size;

	void Start () {
	}

	public void Init(Vector3 center, float size) {
	    //Transform
	    this.transform.position = center;
	    this.size = size;

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

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(size, size, size));
    }
}
