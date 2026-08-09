using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text opponentNameText;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        ShowPlayerNames();
    }

    private void ShowPlayerNames()
    {
        
        playerNameText.text = "" + PhotonNetwork.LocalPlayer.NickName;

       
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer)
            {
                opponentNameText.text = "" + player.NickName;
                break;
            }
        }

        
        if (PhotonNetwork.IsMasterClient)
        {
            statusText.text = "You are Player 1";
        }
        else
        {
            statusText.text = "You are Player 2";
        }
    }
}