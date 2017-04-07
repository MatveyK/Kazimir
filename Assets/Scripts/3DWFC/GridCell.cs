using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour {

    public int Id;

	List<GameObject> containedVoxels;

	Bounds bounds;
	BoxCollider boxCollider;

	void Start () {
	}

	public void Init(Vector3 center, float size) {
	    //Transform
	    this.transform.position = center;

	    //Bounds
		bounds = new Bounds (center, new Vector3(size, size, size));

	    //Contained Voxels
		containedVoxels = new List<GameObject> ();

		//Init the Trigger
		boxCollider = this.gameObject.GetComponent<BoxCollider>();
		boxCollider.size = new Vector3 (size, size, size);
		boxCollider.center = Vector3.zero;
	}

	void OnTriggerEnter(Collider other) {
	    if (!containedVoxels.Contains(other.gameObject)) {
	        other.gameObject.transform.parent = transform;
    		containedVoxels.Add (other.gameObject);
	    }
	}


    public List<GameObject> ContainedVoxels {
		get {
			return containedVoxels;
		}
	}
}
