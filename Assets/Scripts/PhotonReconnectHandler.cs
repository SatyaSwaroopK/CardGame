using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;


public class PhotonReconnectHandler :
    MonoBehaviourPunCallbacks
{
    [SerializeField]
    private TMP_Text statusText;


    private bool reconnecting;

    private int attempts;

    private const int MaxAttempts = 5;


    public override void OnDisconnected(
        DisconnectCause cause)
    {
        Debug.LogWarning(
            "Photon disconnected: " +
            cause
        );


        if (cause ==
            DisconnectCause
                .DisconnectByClientLogic)
        {
            return;
        }


        if (!reconnecting)
        {
            StartCoroutine(
                TryReconnect()
            );
        }
    }


    private IEnumerator TryReconnect()
    {
        reconnecting = true;


        while (
            attempts < MaxAttempts &&
            !PhotonNetwork
                .IsConnectedAndReady)
        {
            attempts++;


            if (statusText != null)
            {
                statusText.text =
                    "Connection lost. Reconnecting...";
            }


            Debug.Log(
                "Reconnect attempt: " +
                attempts
            );


            bool started =
                PhotonNetwork
                    .ReconnectAndRejoin();


            if (!started)
            {
                PhotonNetwork
                    .ConnectUsingSettings();
            }


            yield return
                new WaitForSecondsRealtime(
                    2f
                );
        }


        reconnecting = false;
    }


    public override void OnJoinedRoom()
    {
        reconnecting = false;
        attempts = 0;


        if (statusText != null)
        {
            statusText.text =
                "Reconnected.";
        }


        Debug.Log(
            "Player connected/rejoined room."
        );
    }
}