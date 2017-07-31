using UnityEngine;
using UnityEngine.Networking;

public class ClientNetworkManager : NetworkBehaviour
{

    public static ClientNetworkManager instance;
	// Use this for initialization
	void Start () {
        instance = this;
	}
}
