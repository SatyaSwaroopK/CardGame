using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    private const byte MaxPlayers = 2;


    
    public void QuickMatch()
    {
        
        if (!SetPlayerName())
            return;

        statusText.text = "Searching for opponent...";

        PhotonNetwork.JoinRandomRoom();
    }


    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        statusText.text = "No opponent found. Creating room...";

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = MaxPlayers;

       
        PhotonNetwork.CreateRoom(null, roomOptions);
    }


    // -------------------------
    // CREATE ROOM
    // -------------------------

    public void CreateRoom()
    {
        if (!SetPlayerName())
            return;

        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "Please enter room name.";
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = MaxPlayers;

        statusText.text = "Creating room...";

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }


    // -------------------------
    // JOIN ROOM
    // -------------------------

    public void JoinRoom()
    {
        if (!SetPlayerName())
            return;

        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "Please enter room name.";
            return;
        }

        statusText.text = "Joining room...";

        PhotonNetwork.JoinRoom(roomName);
    }


    // -------------------------
    // ROOM JOINED
    // -------------------------

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        if (playerCount == 1)
        {
            statusText.text = "Waiting for opponent...";
        }

        TryStartGame();
    }


    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " joined.");

        TryStartGame();
    }


    // -------------------------
    // START GAME
    // -------------------------

    private void TryStartGame()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount != MaxPlayers)
            return;

        statusText.text = "Opponent found! Starting game...";

       
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            PhotonNetwork.LoadLevel("Game");
        }
    }


    
    private bool SetPlayerName()
    {
        string playerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            statusText.text = "Please enter player name.";
            return false;
        }

        PhotonNetwork.NickName = playerName;

        return true;
    }


   

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Room already exists. Try another name.";
    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Room not found or room is full.";
    }
}