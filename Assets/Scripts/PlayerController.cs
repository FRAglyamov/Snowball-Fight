using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
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
    private bool isFacingRight = true;
    private float _moveLimiter = 0.7f;
    private float _nextThrow = 0f;
    private int _health = 3;

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
    private GameObject snowballPrefab;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;

        BonusPickUp(BonusType.Invinsible, 2f);
    }

    private void Update()
    {
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

        if (_horizontalInput < 0 && isFacingRight)
        {
            Flip();
        }
        else if (_horizontalInput > 0 && !isFacingRight)
        {
            Flip();
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
        if (_horizontalInput != 0 && _verticalInput != 0) // Check for diagonal movement
        {
            // limit movement speed diagonally, so you move at 70% speed
            _horizontalInput *= _moveLimiter;
            _verticalInput *= _moveLimiter;
        }

        _rb.velocity = new Vector2(_horizontalInput * runSpeed, _verticalInput * runSpeed);
    }

    private void ThrowSnowball()
    {
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 direction = (Vector2)(worldMousePos - transform.position);
        direction.Normalize();

        var snowball = Instantiate(snowballPrefab, transform.position + (Vector3)(direction * throwOffset), transform.rotation);
        snowball.transform.localScale *= throwSize;

        // Give velocity to the snowball
        snowball.GetComponent<Rigidbody2D>().velocity = direction * throwForce;

        throwAudioSource.Play();
    }

    private void Flip()
    {
        //Vector3 currentScale = transform.localScale;
        //currentScale.x *= -1;
        //transform.localScale = currentScale;

        _spriteRenderer.flipX = isFacingRight;
        isFacingRight = !isFacingRight;

        //UpdateFlipServerRpc(_spriteRenderer.flipX);
    }

    public void GetDamage()
    {
        if(!_isInvincible)
        {
            _health--;
            if(_health <= 0)
            {
                // Respawn (Get position from game manager?)
            }
        }
    }

    public void BonusPickUp(BonusType bonusType, float time, float multiplier = 0) => StartCoroutine(Bonus(bonusType, time, multiplier));
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
                yield return new WaitForSeconds(time);
                _isInvincible = false;
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
