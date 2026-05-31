using TMPro;
using UnityEngine;
using Mirror;

public class MainMenuUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField pseudoInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;

    private LoveLetterRoomManager roomManager;

    private void Awake()
    {
        roomManager = NetworkManager.singleton as LoveLetterRoomManager;
    }

    public void OnClickCreateRoom()
    {
        if (roomManager == null)
        {
            Debug.LogError("LoveLetterRoomManager introuvable dans la scène.");
            return;
        }

        string pseudo = pseudoInput != null ? pseudoInput.text.Trim() : "Joueur";
        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "Salon LAN";

        int maxPlayers = 2;
        if (maxPlayersInput != null && !string.IsNullOrWhiteSpace(maxPlayersInput.text))
        {
            int.TryParse(maxPlayersInput.text, out maxPlayers);
        }

        maxPlayers = Mathf.Max(2, maxPlayers);

        MenuNetworkData.PlayerName = string.IsNullOrWhiteSpace(pseudo) ? "Joueur" : pseudo;
        MenuNetworkData.RoomName = string.IsNullOrWhiteSpace(roomName) ? "Salon LAN" : roomName;
        MenuNetworkData.MaxPlayers = maxPlayers;

        roomManager.ConfigureRoom(MenuNetworkData.RoomName, MenuNetworkData.MaxPlayers);
        roomManager.StartHost();
    }

    public void OnClickJoinRoom(string address)
    {
        if (roomManager == null)
        {
            Debug.LogError("LoveLetterRoomManager introuvable dans la scène.");
            return;
        }

        string pseudo = pseudoInput != null ? pseudoInput.text.Trim() : "Joueur";
        MenuNetworkData.PlayerName = string.IsNullOrWhiteSpace(pseudo) ? "Joueur" : pseudo;

        roomManager.networkAddress = address;
        roomManager.StartClient();
    }
}