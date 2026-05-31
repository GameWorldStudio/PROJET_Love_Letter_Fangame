using System;
using System.Collections.Generic;

[Serializable]
public class BotMemory
{
    // Carte actuellement connue sur un joueur (si encore valide)
    public Dictionary<int, RevealedCardInfo> KnownCardsByTarget = new Dictionary<int, RevealedCardInfo>();

    // Historique de ce que le bot a vu/joué publiquement ou privé
    public List<BotMemoryEvent> MemoryEvents = new List<BotMemoryEvent>();
}