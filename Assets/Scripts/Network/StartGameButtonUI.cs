using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class StartGameButtonUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private LoveLetterRoomManager roomManager;

    private void Awake()
    {
        roomManager = NetworkManager.singleton as LoveLetterRoomManager;

        if (startButton == null)
            startButton = GetComponent<Button>();
    }

    private void Update()
    {
        if (roomManager == null || startButton == null)
            return;

        bool isHost = NetworkServer.active && NetworkClient.isConnected;
        bool canStart = roomManager.CanStartMatch();

        startButton.gameObject.SetActive(isHost);
        startButton.interactable = isHost && canStart;
    }

    public void OnClickStartGame()
    {
        if (roomManager == null)
            return;

        if (!NetworkServer.active)
            return;

        roomManager.StartMatch();
    }
}