using System.Collections;
using UnityEngine;

public class RayLayers {
	public static readonly int onlyCollisions;
	public static readonly int upRay;
	public static readonly int downRay;

	static RayLayers() {
		onlyCollisions = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftTop")
			| 1 << LayerMask.NameToLayer("SoftBottom");

		upRay = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftTop");

		downRay = 1 << LayerMask.NameToLayer("NormalCollisions")
			| 1 << LayerMask.NameToLayer("SoftBottom");
	}
}
