using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(PhotonView))]
public class TurnManager : MonoBehaviourPunCallbacks
{
    [Header("Managers")]
    [SerializeField] private HandManager handManager;


    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text opponentScoreText;

    [SerializeField] private Button endTurnButton;


    [Header("Folded Cards")]
    [SerializeField] private Transform opponentCardsPanel;
    [SerializeField] private GameObject cardBackPrefab;


    [Header("Settings")]
    [SerializeField] private float turnDuration = 30f;
    [SerializeField] private float revealDelay = 1f;


    private int currentRound = 1;

    private double roundEndTime;

    private bool roundActive;
    private bool submitted;


    // Master stores hidden selections.
    private readonly Dictionary<int, int[]>
        playerSubmissions = new();


    private readonly HashSet<int>
        submittedPlayers = new();


    // Master stores scores.
    private readonly Dictionary<int, int>
        playerScores = new();


    private readonly List<GameObject>
        opponentCardBacks = new();


    // =====================================================
    // NETWORK MESSAGE
    // =====================================================

    [System.Serializable]
    private class NetworkMessage
    {
        public string action;

        public int round;

        public int playerId;

        public int cardCount;

        public int[] cardIds;

        public int cardId;

        public int orderIndex;

        public int score;

        public int power;

        public int initiativePlayerId;

        public int winnerPlayerId;

        public double endTime;
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        endTurnButton.onClick.AddListener(
            EndTurn
        );


        endTurnButton.interactable = false;

        handManager.StopTurn();


        UpdateLocalScoreUI(0, 0);


        if (PhotonNetwork.IsMasterClient)
        {
            InitialiseScores();

            StartCoroutine(
                StartGameAfterDelay()
            );
        }
    }


    private void InitialiseScores()
    {
        playerScores.Clear();

        foreach (
            Player player
            in PhotonNetwork.PlayerList)
        {
            playerScores[player.ActorNumber] = 0;
        }
    }


    private IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        StartRound(currentRound);
    }


    // =====================================================
    // TIMER
    // =====================================================

    private void Update()
    {
        if (!roundActive)
            return;


        double remaining =
            roundEndTime -
            PhotonNetwork.Time;


        int seconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    (float)remaining
                )
            );


        timerText.text =
            seconds.ToString();


        if (remaining <= 0 &&
            !submitted)
        {
            EndTurn();
        }
    }


    // =====================================================
    // START ROUND
    // =====================================================

    private void StartRound(int round)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;


        submittedPlayers.Clear();
        playerSubmissions.Clear();


        NetworkMessage message =
            new NetworkMessage
            {
                action = "turnStart",

                round = round,

                endTime =
                    PhotonNetwork.Time +
                    turnDuration
            };


        SendToAll(message);
    }


    // =====================================================
    // RECEIVE NETWORK JSON
    // =====================================================

    [PunRPC]
    private void ReceiveNetworkMessage(
        string json,
        PhotonMessageInfo info)
    {
        NetworkMessage message =
            JsonUtility.FromJson<NetworkMessage>(
                json
            );


        if (message == null)
            return;


        switch (message.action)
        {
            case "turnStart":

                if (!IsMessageFromMaster(info))
                    return;

                HandleTurnStart(message);

                break;


            case "endTurn":

                if (!PhotonNetwork.IsMasterClient)
                    return;


                if (info.Sender.ActorNumber !=
                    message.playerId)
                {
                    return;
                }


                HandleEndTurnOnMaster(
                    message
                );

                break;


            case "syncBoard":

                if (!IsMessageFromMaster(info))
                    return;

                HandleSyncBoard(message);

                break;


            case "revealStart":

                if (!IsMessageFromMaster(info))
                    return;

                HandleRevealStart(message);

                break;


            case "revealSingleCard":

                if (!IsMessageFromMaster(info))
                    return;

                HandleRevealCard(message);

                break;


            case "scoreUpdated":

                if (!IsMessageFromMaster(info))
                    return;

                HandleScoreUpdated(message);

                break;


            case "turnEnd":

                if (!IsMessageFromMaster(info))
                    return;

                HandleTurnEnd(message);

                break;


            case "gameEnd":

                if (!IsMessageFromMaster(info))
                    return;

                HandleGameEnd(message);

                break;
        }
    }


    private bool IsMessageFromMaster(
        PhotonMessageInfo info)
    {
        return info.Sender ==
               PhotonNetwork.MasterClient;
    }


    // =====================================================
    // BEGIN ROUND
    // =====================================================

    private void HandleTurnStart(
        NetworkMessage message)
    {
        currentRound =
            message.round;


        roundEndTime =
            message.endTime;


        roundActive = true;
        submitted = false;


        ClearOpponentCardBacks();

        handManager.ClearFoldedCards();
        handManager.ClearRevealedCards();


        roundText.text =
            "Round " +
            currentRound +
            " / 6";


        turnText.text =
            "Choose Cards";


        statusText.text =
            "Select your cards";


        handManager.BeginTurn(
            currentRound
        );


        // Assignment:
        // draw +1 each turn.
        handManager.DrawCard();


        endTurnButton.interactable =
            true;
    }


    // =====================================================
    // END TURN
    // =====================================================

    public void EndTurn()
    {
        if (!roundActive)
            return;


        if (submitted)
            return;


        submitted = true;


        endTurnButton.interactable =
            false;


        int[] selectedCardIds =
            handManager.FoldSelectedCards(
                cardBackPrefab
            );


        statusText.text =
            "Cards locked. Waiting for opponent...";


        NetworkMessage message =
            new NetworkMessage
            {
                action = "endTurn",

                playerId =
                    PhotonNetwork
                    .LocalPlayer
                    .ActorNumber,

                cardIds =
                    selectedCardIds,

                cardCount =
                    selectedCardIds.Length,

                round =
                    currentRound
            };


        SendToMaster(message);
    }


    // =====================================================
    // MASTER RECEIVES END TURN
    // =====================================================

    private void HandleEndTurnOnMaster(
        NetworkMessage message)
    {
        int playerId =
            message.playerId;


        if (submittedPlayers.Contains(
            playerId))
        {
            return;
        }


        submittedPlayers.Add(playerId);


        playerSubmissions[playerId] =
            message.cardIds;


        NetworkMessage syncMessage =
            new NetworkMessage
            {
                action = "syncBoard",

                playerId = playerId,

                cardCount =
                    message.cardCount,

                round =
                    currentRound
            };


        SendToAll(syncMessage);


        if (submittedPlayers.Count == 2)
        {
            roundActive = false;

            StartCoroutine(
                RevealPhase()
            );
        }
    }


    // =====================================================
    // OPPONENT CARD BACKS
    // =====================================================

    private void HandleSyncBoard(
        NetworkMessage message)
    {
        if (message.playerId ==
            PhotonNetwork
                .LocalPlayer
                .ActorNumber)
        {
            return;
        }


        ClearOpponentCardBacks();


        for (int i = 0;
             i < message.cardCount;
             i++)
        {
            GameObject cardBack =
                Instantiate(
                    cardBackPrefab,
                    opponentCardsPanel
                );


            opponentCardBacks.Add(
                cardBack
            );
        }


        if (!submitted)
        {
            statusText.text =
                "Opponent locked " +
                message.cardCount +
                " card(s)";
        }
    }


    // =====================================================
    // REVEAL PHASE - MASTER
    // =====================================================

    private IEnumerator RevealPhase()
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;


        yield return new WaitForSeconds(
            0.5f
        );


        int initiativePlayer =
            DetermineInitiative();


        NetworkMessage revealStart =
            new NetworkMessage
            {
                action =
                    "revealStart",

                round =
                    currentRound,

                initiativePlayerId =
                    initiativePlayer
            };


        SendToAll(revealStart);


        yield return new WaitForSeconds(
            revealDelay
        );


        Player[] players =
            PhotonNetwork.PlayerList;


        int opponentPlayer;


        if (players[0].ActorNumber ==
            initiativePlayer)
        {
            opponentPlayer =
                players[1].ActorNumber;
        }
        else
        {
            opponentPlayer =
                players[0].ActorNumber;
        }


        int[] initiativeCards =
            GetSubmission(
                initiativePlayer
            );


        int[] opponentCards =
            GetSubmission(
                opponentPlayer
            );


        int maxCount =
            Mathf.Max(
                initiativeCards.Length,
                opponentCards.Length
            );


        // Alternating reveal sequence.
        for (int i = 0;
             i < maxCount;
             i++)
        {
            // Initiative player reveals first.
            if (i <
                initiativeCards.Length)
            {
                yield return StartCoroutine(
                    RevealAndResolveCard(
                        initiativePlayer,
                        initiativeCards[i],
                        i
                    )
                );
            }


            // Opponent reveals next.
            if (i <
                opponentCards.Length)
            {
                yield return StartCoroutine(
                    RevealAndResolveCard(
                        opponentPlayer,
                        opponentCards[i],
                        i
                    )
                );
            }
        }


        yield return new WaitForSeconds(
            revealDelay
        );


        NetworkMessage turnEnd =
            new NetworkMessage
            {
                action = "turnEnd",

                round =
                    currentRound
            };


        SendToAll(turnEnd);


        yield return new WaitForSeconds(
            1.5f
        );


        currentRound++;


        if (currentRound <= 6)
        {
            StartRound(
                currentRound
            );
        }
        else
        {
            EndGame();
        }
    }


    // =====================================================
    // INITIATIVE
    // =====================================================

    private int DetermineInitiative()
    {
        Player[] players =
            PhotonNetwork.PlayerList;


        int p1 =
            players[0].ActorNumber;


        int p2 =
            players[1].ActorNumber;


        int p1Score =
            playerScores[p1];


        int p2Score =
            playerScores[p2];


        // Higher score goes first.
        if (p1Score > p2Score)
            return p1;


        if (p2Score > p1Score)
            return p2;


        // Tie = random.
        return Random.value < 0.5f
            ? p1
            : p2;
    }


    // =====================================================
    // REVEAL ONE CARD
    // =====================================================

    private IEnumerator RevealAndResolveCard(
        int playerId,
        int cardId,
        int orderIndex)
    {
        CardData cardData =
            handManager.GetCardDataById(
                cardId
            );


        if (cardData == null)
        {
            Debug.LogError(
                "Could not find card: " +
                cardId
            );

            yield break;
        }


        // Tell clients which card is revealed.
        NetworkMessage revealMessage =
            new NetworkMessage
            {
                action =
                    "revealSingleCard",

                playerId =
                    playerId,

                cardId =
                    cardId,

                orderIndex =
                    orderIndex,

                power =
                    cardData.power
            };


        SendToAll(revealMessage);


        yield return new WaitForSeconds(
            revealDelay
        );


       
        playerScores[playerId] +=
            cardData.power;


        NetworkMessage scoreMessage =
            new NetworkMessage
            {
                action =
                    "scoreUpdated",

                playerId =
                    playerId,

                score =
                    playerScores[
                        playerId
                    ],

                power =
                    cardData.power
            };


        SendToAll(scoreMessage);


        
        yield return new WaitForSeconds(
            revealDelay
        );
    }


    private int[] GetSubmission(
        int playerId)
    {
        if (playerSubmissions.TryGetValue(
            playerId,
            out int[] cards))
        {
            return cards ??
                   new int[0];
        }


        return new int[0];
    }


    // =====================================================
    // CLIENT: REVEAL START
    // =====================================================

    private void HandleRevealStart(
        NetworkMessage message)
    {
        roundActive = false;


        timerText.text = "0";


        endTurnButton.interactable =
            false;


        handManager.StopTurn();


        Player initiative =
            PhotonNetwork.CurrentRoom
                .GetPlayer(
                    message
                    .initiativePlayerId
                );


        if (initiative != null)
        {
            statusText.text =
                initiative.NickName +
                " has initiative";
        }


        turnText.text =
            "Reveal";
    }


    // =====================================================
    // CLIENT: REVEAL CARD
    // =====================================================

    private void HandleRevealCard(
        NetworkMessage message)
    {
        CardData data =
            handManager.GetCardDataById(
                message.cardId
            );


        if (data == null)
            return;


        bool isMyCard =
            message.playerId ==
            PhotonNetwork
                .LocalPlayer
                .ActorNumber;


        if (isMyCard)
        {
            handManager.RevealLocalCard(
                message.cardId
            );
        }
        else
        {
            RemoveOneOpponentCardBack();


            handManager.CreateOpponentRevealCard(
                message.cardId,
                opponentCardsPanel
            );
        }


        Player player =
            PhotonNetwork.CurrentRoom
                .GetPlayer(
                    message.playerId
                );


        if (player != null)
        {
            statusText.text =
                player.NickName +
                " reveals " +
                data.cardName +
                " (+" +
                data.power +
                " Power)";
        }
    }


    // =====================================================
    // SCORE UPDATED
    // =====================================================

    private void HandleScoreUpdated(
        NetworkMessage message)
    {
        bool isMe =
            message.playerId ==
            PhotonNetwork
                .LocalPlayer
                .ActorNumber;


        if (isMe)
        {
            playerScoreText.text =
                "Score: " +
                message.score;
        }
        else
        {
            opponentScoreText.text =
                "Score: " +
                message.score;
        }
    }


    // =====================================================
    // TURN END
    // =====================================================

    private void HandleTurnEnd(
        NetworkMessage message)
    {
        statusText.text =
            "Round " +
            message.round +
            " complete";


        turnText.text =
            "Round Complete";
    }


    // =====================================================
    // GAME END
    // =====================================================

    private void EndGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;


        Player[] players =
            PhotonNetwork.PlayerList;


        int p1 =
            players[0].ActorNumber;

        int p2 =
            players[1].ActorNumber;


        int winner = 0;


        if (playerScores[p1] >
            playerScores[p2])
        {
            winner = p1;
        }
        else if (
            playerScores[p2] >
            playerScores[p1])
        {
            winner = p2;
        }


        NetworkMessage message =
            new NetworkMessage
            {
                action = "gameEnd",

                winnerPlayerId =
                    winner
            };


        SendToAll(message);
    }


    private void HandleGameEnd(
        NetworkMessage message)
    {
        roundActive = false;

        handManager.StopTurn();

        endTurnButton.interactable =
            false;


        turnText.text =
            "Game Finished";


        if (message.winnerPlayerId == 0)
        {
            statusText.text =
                "Match Draw!";
        }
        else if (
            message.winnerPlayerId ==
            PhotonNetwork
                .LocalPlayer
                .ActorNumber)
        {
            statusText.text =
                "YOU WIN!";
        }
        else
        {
            Player winner =
                PhotonNetwork.CurrentRoom
                    .GetPlayer(
                        message
                        .winnerPlayerId
                    );


            statusText.text =
                winner.NickName +
                " Wins!";
        }
    }


    // =====================================================
    // OPPONENT BACK
    // =====================================================

    private void RemoveOneOpponentCardBack()
    {
        if (opponentCardBacks.Count == 0)
            return;


        GameObject back =
            opponentCardBacks[0];


        opponentCardBacks.RemoveAt(0);


        if (back != null)
        {
            Destroy(back);
        }
    }


    private void ClearOpponentCardBacks()
    {
        foreach (
            GameObject card
            in opponentCardBacks)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }


        opponentCardBacks.Clear();
    }


    // =====================================================
    // SCORE UI
    // =====================================================

    private void UpdateLocalScoreUI(
        int myScore,
        int opponentScore)
    {
        playerScoreText.text =
            "Score: " +
            myScore;


        opponentScoreText.text =
            "Score: " +
            opponentScore;
    }


   
    // NETWORK SEND
    

    private void SendToMaster(
        NetworkMessage message)
    {
        string json =
            JsonUtility.ToJson(
                message
            );


        photonView.RPC(
            nameof(
                ReceiveNetworkMessage
            ),

            RpcTarget.MasterClient,

            json
        );
    }


    private void SendToAll(
        NetworkMessage message)
    {
        string json =
            JsonUtility.ToJson(
                message
            );


        photonView.RPC(
            nameof(
                ReceiveNetworkMessage
            ),

            RpcTarget.All,

            json
        );
    }
}