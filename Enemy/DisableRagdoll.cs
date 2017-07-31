using System.Collections;
using UnityEngine;

public class DisableRagdoll : MonoBehaviour {


	void Start () {
        StartCoroutine(disableRagdoll());
	}

    IEnumerator disableRagdoll()
    {
        //delay disable to allow zombie to fall
        yield return new WaitForSeconds(5);
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (Rigidbody ridgy in GetComponentsInChildren<Rigidbody>())
            ridgy.isKinematic = true;

        //delete corpse after 10 seconds
        Destroy(gameObject, 10);
    }
}
