using UnityEngine;
using System.Collections;
using FishNet.Object;

public class bullet : NetworkBehaviour
{
	public GameObject explo;

	private void OnCollisionEnter(Collision col)
	{
		// Only the server should handle hit logic.
		if (!IsServerStarted)
			return;

		Vector3 hitPoint = (col.contacts != null && col.contacts.Length > 0)
			? col.contacts[0].point
			: transform.position;

		GameObject explosionInstance = Instantiate(explo, hitPoint, Quaternion.identity);

		// If the explosion prefab has a NetworkObject, spawn it so clients see it.
		NetworkObject explosionNob = explosionInstance.GetComponent<NetworkObject>();
		if (explosionNob != null)
			explosionNob.Spawn(explosionNob);

		// Despawn the bullet over the network (or destroy as a fallback).
		if (NetworkObject != null)
			NetworkObject.Despawn();
		else
			Destroy(gameObject);
	}
}
