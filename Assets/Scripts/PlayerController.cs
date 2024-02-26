using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D.Animation;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;
    [SerializeField]
    private AudioSource throwAudioSource;
    [SerializeField]
    private AudioSource footstepsAudioSource;
    [SerializeField]
    private List<AudioClip> footstepAudioClips = new List<AudioClip>();
    private float _horizontalInput;
    private float _verticalInput;
    private float _moveLimiter = 0.7f;
    private float _nextThrow = 0f;

    [SerializeField]
    private NetworkVariable<int> _health = new NetworkVariable<int>(3);
    private NetworkVariable<bool> _isFlipped = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> nickName = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _avatarIndex = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField]
    private TMP_Text _nickNameText;
    [SerializeField]
    private string[] statuses = new string[3];
    [SerializeField]
    private float throwCooldown = 0.5f;
    [SerializeField]
    private float throwForce = 10f;
    [SerializeField]
    private float throwSize = 1f;
    [SerializeField]
    private float throwOffset = 0.5f;
    [SerializeField]
    private bool _isInvincible = false;
    [SerializeField]
    private float runSpeed = 7f;
    [SerializeField]
    private Transform snowballPrefab;
    private Transform _spawnedSnowball;
    private bool _isDead;
    private SpriteLibrary _spriteLibrary;
    private ulong _lastDamagePlayerId;

    public static PlayerController thisClientPlayer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteLibrary = GetComponent<SpriteLibrary>();
        _mainCamera = Camera.main;
        thisClientPlayer = this;

        BonusPickUpClientRpc(BonusType.Invinsible, 3f);

        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(_mainCamera);
    }

    public override void OnNetworkSpawn()
    {
        _isFlipped.OnValueChanged += OnFlipChange;
        _health.OnValueChanged += OnHealthChange;
        nickName.OnValueChanged += OnNicknameChange;
        _avatarIndex.OnValueChanged += OnAvatarIndexChange;
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        // Update Avatars of others for a new client / player
        _spriteLibrary.spriteLibraryAsset = PlayerSpawner.Instance.GetAvatarByIndex(_avatarIndex.Value);


        RespawnClientRpc();

        if (IsServer)
        {
            GameController.Instance.AddPlayer(OwnerClientId, this);
            //RespawnClientRpc();
        }

        if (!IsOwner) return;

        Debug.Log($"Input field text: {MainMenu.Instance.nicknameInputField.text}");
        if (MainMenu.Instance.nicknameInputField.text != "")
        {
            nickName.Value = MainMenu.Instance.nicknameInputField.text;
        }
        else
        {
            nickName.Value = $"Player: {OwnerClientId}";
        }
        MainMenu.Instance.nicknameInputField.onEndEdit.AddListener(OnNicknameInput);
        //MainMenu.Instance.nicknameInputField.onValueChanged.AddListener(OnNicknameInput);

        Camera.SetupCurrent(Camera.main);
    }

    public override void OnNetworkDespawn()
    {
        GameController.Instance.RemovePlayer(OwnerClientId);
    }

    #region OnChangeEvents

    private void OnAvatarIndexChange(int previousValue, int newValue)
    {
        _spriteLibrary.spriteLibraryAsset = PlayerSpawner.Instance.GetAvatarByIndex(newValue);
    }

    public void OnNicknameInput(string text)
    {
        // Max Nickname size = 16 character to fit in FixedString32Bytes
        if (text.Length > 9)
        {
            nickName.Value = text.Substring(0, 16);
        }
        else
        {
            nickName.Value = text;
        }
    }

    private void OnNicknameChange(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        _nickNameText.text = newValue.ToString();
    }

    private void OnFlipChange(bool oldValue, bool newValue)
    {
        // Sprite Flip
        _spriteRenderer.flipX = newValue;
    }

    private void OnHealthChange(int oldValue, int newValue)
    {
        if (IsOwner)
        {
            // Update hearts
            HealthUI.Instance.UpdateHealth(newValue);
        }

        // Check death
        if (newValue <= 0)
        {
            _isDead = true;
            _spriteRenderer.color = Color.red;
            //Debug.Log("Health <= 0. Respawning...");
            if (IsServer)
            {
                GameController.Instance.AddScoreToPlayer(_lastDamagePlayerId);
                RespawnClientRpc();
            }
        }
    }
    

    #endregion


    private void Update()
    {
        if (!IsOwner || !Application.isFocused || _isDead) return;

        // Gives a value between -1 and 1
        _horizontalInput = Input.GetAxisRaw("Horizontal"); // -1 is left
        _verticalInput = Input.GetAxisRaw("Vertical"); // -1 is down

        if (_horizontalInput != 0 || _verticalInput != 0)
        {
            _animator.SetBool("isMoving", true);
        }
        else
        {
            _animator.SetBool("isMoving", false);
        }

        if (_horizontalInput < 0 && !_isFlipped.Value)
        {
            _isFlipped.Value = true;
        }
        else if (_horizontalInput > 0 && _isFlipped.Value)
        {
            _isFlipped.Value = false;
        }

        if (Input.GetMouseButton(0) && Time.time > _nextThrow)
        {
            ThrowSnowball();
            _nextThrow = Time.time + throwCooldown;
        }

        _mainCamera.transform.position = transform.position + new Vector3(0, 0, -10);
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !Application.isFocused || _isDead) return;

        if (_horizontalInput != 0 && _verticalInput != 0) // Check for diagonal movement
        {
            // limit movement speed diagonally, so you move at 70% speed
            _horizontalInput *= _moveLimiter;
            _verticalInput *= _moveLimiter;
        }

        _rb.velocity = new Vector2(_horizontalInput * runSpeed, _verticalInput * runSpeed);
    }

    #region Snowball
    private void ThrowSnowball()
    {
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(worldMousePos - transform.position);
        direction.Normalize();

        ThrowSnowballServerRpc(direction);
    }

    [ServerRpc]
    private void ThrowSnowballServerRpc(Vector2 direction)
    {
        ThrowSnowballClientRpc(direction);
    }

    [ClientRpc]
    private void ThrowSnowballClientRpc(Vector2 direction)
    {
        _spawnedSnowball = SpawnSnowball(direction);

        if (IsHost) _spawnedSnowball.GetComponent<Snowball>().isServerObject = true;
    }

    private Transform SpawnSnowball(Vector2 direction)
    {
        var snowball = Instantiate(snowballPrefab, transform.position + (Vector3)(direction * throwOffset), transform.rotation);
        snowball.localScale *= throwSize;
        snowball.right = direction;
        // Give velocity to the snowball
        snowball.GetComponent<Rigidbody2D>().velocity = direction * throwForce;
        snowball.GetComponent<Snowball>().playerOriginId = OwnerClientId;
        throwAudioSource.Play();
        return snowball;
    }
    #endregion


    public void GetDamage(ulong id)
    {
        if(!_isInvincible && !_isDead)
        {
            _lastDamagePlayerId = id;
            _health.Value--;
        }
    }


    [ClientRpc]
    private void RespawnOnLoadClientRpc()
    {
        if (IsOwner)
        {
            transform.position = PlayerSpawner.Instance.GetSpawnPosition();

            _avatarIndex.Value = PlayerSpawner.Instance.GetRandomAvatarIndex();
            _spriteLibrary.spriteLibraryAsset = PlayerSpawner.Instance.GetAvatarByIndex(_avatarIndex.Value);
        }
        _spriteRenderer.color = Color.white;
        _isDead = false;
        BonusPickUpClientRpc(BonusType.Invinsible, 2f);
    }

    [ClientRpc]
    public void RespawnClientRpc()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);
        if (IsHost)
        {
            _health.Value = 3;
        }
        if (IsOwner)
        {
            transform.position = PlayerSpawner.Instance.GetSpawnPosition();

            _avatarIndex.Value = PlayerSpawner.Instance.GetRandomAvatarIndex();
            _spriteLibrary.spriteLibraryAsset = PlayerSpawner.Instance.GetAvatarByIndex(_avatarIndex.Value);
            //UpdateAvatarServerRpc(avatarIndex);
        }
        _spriteRenderer.color = Color.white;
        _isDead = false;
        BonusPickUpClientRpc(BonusType.Invinsible, 2f);
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position)
    {
        if (IsOwner)
            transform.position = position;
    }

    [ClientRpc]
    public void BonusPickUpClientRpc(BonusType bonusType, float time, float multiplier = 0) => StartCoroutine(Bonus(bonusType, time, multiplier));
    IEnumerator Bonus(BonusType bonusType, float time, float multiplier = 0)
    {
        switch (bonusType)
        {
            case BonusType.ThrowSpeed:
                throwCooldown /= multiplier;
                yield return new WaitForSeconds(time);
                throwCooldown *= multiplier;
                break;
            case BonusType.ThrowForce:
                throwForce *= multiplier;
                yield return new WaitForSeconds(time);
                throwForce /= multiplier;
                break;
            case BonusType.ThrowSize:
                throwSize *= multiplier;
                yield return new WaitForSeconds(time);
                throwSize /= multiplier;
                break;
            case BonusType.RunSpeed:
                runSpeed *= multiplier;
                yield return new WaitForSeconds(time);
                runSpeed /= multiplier;
                break;
            case BonusType.Invisible:
                GetComponent<SpriteRenderer>().enabled = false;
                yield return new WaitForSeconds(time);
                GetComponent<SpriteRenderer>().enabled = true;
                break;
            case BonusType.Invinsible:
                _isInvincible = true;
                _spriteRenderer.color = Color.yellow;
                yield return new WaitForSeconds(time);
                _isInvincible = false;
                _spriteRenderer.color = Color.white;
                break;
            default:
                break;
        }
    }

    // For Animation Clip Event
    public void PlayFootstepSound()
    {
        if (!footstepsAudioSource.isPlaying)
            footstepsAudioSource.PlayOneShot(footstepAudioClips[Random.Range(0, footstepAudioClips.Count)], 1f);
    }

}
