using Mirror;
using UnityEngine;

public class LocalPlayerProvider : MonoBehaviour
{
    public LoveLetterNetworkPlayer LocalPlayer { get; private set; }

    private void Start()
    {
        Invoke(nameof(FindLocalPlayer), 0.5f);
    }

    private void FindLocalPlayer()
    {
        LocalPlayer = NetworkClient.localPlayer.GetComponent<LoveLetterNetworkPlayer>();
    }
}