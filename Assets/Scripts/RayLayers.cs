using System.Collections;
using UnityEngine;

public class RayLayers {
	public static readonly int fullCollisions;
	public static readonly int upRay;
	public static readonly int downRay;
	public static readonly int sideRay;

	static RayLayers() {
		fullCollisions = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftTop")
			| 1 << LayerMask.NameToLayer("SoftBottom");

		upRay = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftTop");

		downRay = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftBottom");
		
		sideRay = 1 << LayerMask.NameToLayer("NormalCollisions");
	}
}
