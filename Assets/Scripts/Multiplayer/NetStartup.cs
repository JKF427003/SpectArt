using UnityEngine;
using Unity.Netcode;

public class NetStartup : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null)
        {
            GUI.Label(new Rect(10, 10, 400, 20), "No NetworkManager found");
            return;
        }

        // Show connection state
        string state = NetworkManager.Singleton.IsServer ? "Server" :
                       NetworkManager.Singleton.IsClient ? "Client" : "Not connected";
        GUI.Label(new Rect(10, 10, 300, 20), "Net state: " + state);

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            return;

        const int width = 200;
        const int height = 40;
        int x = 10;
        int y = 40;

        if (GUI.Button(new Rect(x, y, width, height), "Start Host"))
        {
            Debug.Log("[NetStartup] StartHost clicked");
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(x, y + 50, width, height), "Start Client"))
        {
            Debug.Log("[NetStartup] StartClient clicked");
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log("[NetStartup] StartClient returned: " + ok);
        }
    }
}