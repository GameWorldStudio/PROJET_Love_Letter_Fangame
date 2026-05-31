using Mirror;
using UnityEngine;

public class LoveLetterRoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string DisplayName = "Joueur";

    public override void OnStartClient()
    {
        base.OnStartClient();

        LoveLetterRoomManager manager = NetworkManager.singleton as LoveLetterRoomManager;
        if (manager != null && !manager.RoomPlayers.Contains(this))
        {
            manager.RoomPlayers.Add(this);
        }
    }

    public override void OnStopClient()
    {
        LoveLetterRoomManager manager = NetworkManager.singleton as LoveLetterRoomManager;
        if (manager != null)
        {
            manager.RoomPlayers.Remove(this);
        }

        base.OnStopClient();
    }

    [Server]
    public void SetDisplayName(string value)
    {
        DisplayName = string.IsNullOrWhiteSpace(value) ? "Joueur" : value.Trim();
    }

    void OnNameChanged(string oldValue, string newValue)
    {
        // ici tu peux rafraîchir ton UI lobby si besoin
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);
        // utile si tu veux afficher "prêt / non prêt"
    }

    public override void IndexChanged(int oldIndex, int newIndex)
    {
        base.IndexChanged(oldIndex, newIndex);
    }

    [Command]
    public void CmdChangeReadyState(bool ready)
    {
        readyToBegin = ready;
    }
}