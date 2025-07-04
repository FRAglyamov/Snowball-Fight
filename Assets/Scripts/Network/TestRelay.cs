using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    public string JoinCode;
    [SerializeField]
    private TMP_Text joinCodeText;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        };        
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    [ContextMenu("Create Relay")]
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(20);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);
            joinCodeText.text = joinCode;
            GUIUtility.systemCopyBuffer = joinCode;

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (NetworkManager.Singleton.StartHost())
            {
                //NetworkManager.Singleton.SceneManager.OnLoadComplete += GameController.Instance.OnLoadComplete;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ContextMenu("Join Relay")]
    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with: " + joinCode);
            joinCodeText.text = joinCode;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (NetworkManager.Singleton.StartClient())
            {
                //NetworkManager.Singleton.SceneManager.OnLoadComplete += GameController.Instance.OnLoadComplete;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
