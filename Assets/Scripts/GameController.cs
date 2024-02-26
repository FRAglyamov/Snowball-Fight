using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : NetworkBehaviour
{
    [Serializable]
    class PlayerData
    {
        public ulong id;
        public PlayerController player;
        public int score;
    }

    private NetworkVariable<int> playersNum = new NetworkVariable<int>();
    [SerializeField]
    private TMP_Text top3ScoreText;
    //[SerializeField]
    //private TMP_Text winnerText;
    [SerializeField]
    private GameObject winnerBanner;
    [SerializeField]
    private Button changeMapButton;

    [SerializeField]
    private List<PlayerData> _players = new List<PlayerData>();
    NetworkVariable<FixedString128Bytes> scoreText = new NetworkVariable<FixedString128Bytes>();

    [SerializeField]
    private int _scoreToWinAmount = 30;
    [SerializeField]
    private float _timeToEndAmount = 300f;

    //List<Scene> scenes = new List<Scene>();
    //private string[] scenes = new string[SceneManager.sceneCountInBuildSettings];
    private string[] scenes;
    private int currentScene = 0;
    private ulong[] singleTarget = new ulong[1];

    public static GameController Instance;
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
        DontDestroyOnLoad(gameObject);
        scoreText.OnValueChanged += OnScoreTextChange;
        GetAllScenes();

        changeMapButton.gameObject.SetActive(IsServer);

        if (IsServer)
        {
            changeMapButton.onClick.AddListener(ChangeMap);
            //NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSyncComplete;
        }

    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadLevel;
    }

    public void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!IsServer) return;
        //var player = GetPlayerById(clientId);
        //player.transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        Debug.Log($"ClientID: {clientId}");

        //singleTarget[0] = clientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        GetPlayerById(clientId).RespawnClientRpc();

        //RespawnPlayerClientRpc(clientRpcParams);

        //RespawnServerRpc(clientId);
        //if (_players.player.IsOwner)
        //{
        //    p.player.transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        //}
    }

    //[ServerRpc(RequireOwnership = false)]
    //private void RespawnServerRpc(ulong clientId)
    //{
    //    singleTarget[0] = clientId;
    //    ClientRpcParams rpcParams = default;
    //    rpcParams.Send.TargetClientIds = singleTarget;
    //    RespawnPlayerClientRpc(clientId, rpcParams);
    //}

    [ClientRpc]
    private void RespawnPlayerClientRpc(ClientRpcParams rpcParams = default)
    {
        Debug.Log($"{PlayerController.thisClientPlayer.OwnerClientId}: {PlayerController.thisClientPlayer.nickName.Value.ToString()}");
        //GetPlayerById(id).transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        PlayerController.thisClientPlayer.transform.position = PlayerSpawner.Instance.GetSpawnPosition();
    }


    private void OnLoadLevel(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("0");
        // Reset scores
        foreach (var p in _players)
        {
            p.score = 0;
        }
        // Respawn all players
        RespawnPlayersClientRpc();
    }

    [ClientRpc]
    private void RespawnPlayersClientRpc()
    {
        Debug.Log("1");
        foreach (var p in _players)
        {
            Debug.Log("2");
            if (p.player.IsOwner)
            {
                Debug.Log("3");
                p.player.transform.position = PlayerSpawner.Instance.GetSpawnPosition();
            }
        }
    }

    private void GetAllScenes()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        scenes = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            //scenes[i] = SceneUtility.GetScenePathByBuildIndex(i);
        }
    }

    private void OnScoreTextChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        top3ScoreText.text = newValue.ToString();
    }

    private void Update()
    {
        if (!IsServer) return;

        UpdatePlayerInfo();
    }

    private void UpdatePlayerInfo()
    {
        playersNum.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (_players.Count < 0) return;
        _players = _players.OrderByDescending(x => x.score).ToList();

        if (_players[0].score >= _scoreToWinAmount || Time.time > _timeToEndAmount || Input.GetKeyDown(KeyCode.F))
        {
            EndGameClientRpc(_players[0].player.nickName.Value.ToString());
        }
        string top3 = "";
        int topSize = _players.Count < 3 ? _players.Count : 3;
        for (int i = 0; i < topSize; i++)
        {
            var nickname = _players[i].player.nickName.Value.ToString();
            if(nickname.Length > 9)
            {
                nickname = nickname.Substring(0, 9);
            }

            top3 += $"{nickname} - {_players[i].score} \n";
        }
        scoreText.Value = top3;
    }

    [ClientRpc]
    private void EndGameClientRpc(string winnerNickname)
    {
        StartCoroutine(GameEndText(winnerNickname));
    }

    IEnumerator GameEndText(string winnerNickname)
    {
        winnerBanner.SetActive(true);
        winnerBanner.GetComponentInChildren<TMP_Text>().text = $"Побеждает {winnerNickname}!";
        //winnerText.text = $"Побеждает {_players[0].player.nickName.Value}!";
        yield return new WaitForSeconds(5f);
        winnerBanner.SetActive(false);
        yield return new WaitForSeconds(2f);
        if(IsHost) ChangeMap();
    }

    public void ChangeMap()
    {
        // Reset score before change
        foreach (var p in _players)
        {
            p.score = 0;
        }

        currentScene++;
        if(currentScene >= scenes.Length) 
            currentScene = 1;
        NetworkManager.SceneManager.LoadScene(scenes[currentScene], LoadSceneMode.Single);
    }

    public PlayerController GetPlayerById(ulong id)
    {
        return _players.First(x => x.id == id).player;
    }
    private PlayerData GetPlayerDataById(ulong id)
    {
        return _players.First(x => x.id == id);
    }

    public void AddPlayer(ulong id, PlayerController player)
    {
        // TODO: Check for duplicate?

        _players.Add(new PlayerData { id = id, player = player, score = 0 });
    }

    public void RemovePlayer(ulong id)
    {
        _players.RemoveAll(x => x.id == id);
    }

    public void AddScoreToPlayer(ulong id)
    {
        GetPlayerDataById(id).score++;
    }
}
