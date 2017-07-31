using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour {

    public Transform destination;

	// Use this for initialization
	void OnTriggerEnter(Collider coll)
    {
        if (coll.tag == "Player")
        {
            StartCoroutine(TeleportPlayer(coll.transform));
        }
    }

    IEnumerator TeleportPlayer(Transform player)
    {
        yield return new WaitForSeconds(2);
        player.position = destination.position;
    }
}
