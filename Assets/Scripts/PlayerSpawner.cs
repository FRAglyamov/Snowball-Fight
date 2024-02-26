using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField]
    private List<NetworkClient> clients = new List<NetworkClient>();
    [SerializeField]
    private List<Transform> spawnTransforms = new List<Transform>();
    NetworkVariable<int> _currentSpawnPoint = new NetworkVariable<int>();
    //private int _currentSpawnPoint = 0;
    [SerializeField]
    private List<SpriteLibraryAsset> playerAvatars = new List<SpriteLibraryAsset>();

    public static PlayerSpawner Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkDespawn()
    {
        Instance = null;
    }

    public Vector3 GetSpawnPosition()
    {
        //NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position = spawnTransforms[0].position;

        Vector3 spawnPosition = spawnTransforms[_currentSpawnPoint.Value].position;
        IncrementSpawnPointIndexServerRpc();
        return spawnPosition;

        //clients[0].PlayerObject.transform.position = Vector3.zero;
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncrementSpawnPointIndexServerRpc()
    {
        _currentSpawnPoint.Value++;
        if (_currentSpawnPoint.Value >= spawnTransforms.Count)
        {
            _currentSpawnPoint.Value = 0;
        }
    }

    public int GetRandomAvatarIndex()
    {
        return Random.Range(0, playerAvatars.Count);
    }

    public SpriteLibraryAsset GetAvatarByIndex(int index)
    {
        return playerAvatars[index];
    }
}
