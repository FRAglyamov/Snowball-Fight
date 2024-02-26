using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NonPooledDynamicSpawner : NetworkBehaviour
{
    [SerializeField]
    private List<NetworkObject> prefabsToSpawn;
    [SerializeField]
    private bool destroyWithSpawner;
    [SerializeField]
    private float spawnTime = 10f;

    private NetworkObject _spawnedNetworkObject;
    private float _nextSpawn;

    private void Update()
    {
        if (!IsServer) return;
        if (prefabsToSpawn == null)
        {
            Debug.LogWarning("Missing prefab to spawn!");
            return;
        }

        if (_spawnedNetworkObject == null)
        {
            if (Time.time > _nextSpawn)
            {
                SpawnNetworkObject();
            }
        }
        else
        {
            _nextSpawn = Time.time + spawnTime;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Only the server spawns, clients will disable this component on their side
        enabled = IsServer;
        if (!enabled || prefabsToSpawn == null)
        {
            return;
        }

        SpawnNetworkObject();
    }

    private void SpawnNetworkObject()
    {
        int bonusRandomSelect = Random.Range(0, prefabsToSpawn.Count);
        _spawnedNetworkObject = Instantiate(prefabsToSpawn[bonusRandomSelect], transform.position, Quaternion.identity);
        _spawnedNetworkObject.Spawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && destroyWithSpawner && _spawnedNetworkObject != null && _spawnedNetworkObject.IsSpawned)
        {
            _spawnedNetworkObject.Despawn();
        }
        base.OnNetworkDespawn();
    }
}