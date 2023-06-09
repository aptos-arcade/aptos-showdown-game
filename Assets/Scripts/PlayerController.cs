using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{

    // components on player game object; set in Start
    private CharacterController _controller;

    // references to other game objects; set in Inspector
    [Header("Transform References")]
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform modelGunPoint;
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Transform adsInPoint;
    [SerializeField] private Transform adsOutPoint;
    
    [Header("Game Object References")]
    [SerializeField] private GameObject bulletImpact;
    [SerializeField] private GameObject playerHitImpact;
    [SerializeField] private GameObject playerModel;

    [Header("Layer References")]
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Skins and Guns")]
    [SerializeField] private Gun[] allGuns;
    [SerializeField] private Material[] playerSkins;
    
    [Header("Audio References")]
    [SerializeField] private AudioSource footstepsSlow;
    [SerializeField] private AudioSource footstepsFast;

    // player stats; set in Inspector
    [Header("Player Stats")]
    [SerializeField] private float moveSpeed = 5f; 
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float gravityModifier = 2.5f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Animator playerAnimator;
    
    [Header("Gun Stats")]
    [SerializeField] private float maxHeat = 10f;
    [SerializeField] private float coolRate = 4f;
    [SerializeField] private float overheatCoolRate = 5f;
    [SerializeField] private float muzzleFlashDuration = 0.1f;
    [SerializeField] private float adsSpeed = 10f;

    // private references; set in Start
    private Camera _camera;

    // player state variables; set in Update
    private Vector2 _mouseInput;
    private Vector3 _moveDirection, _movement;
    private float _verticalRotationStore, _activeMoveSpeed;
    private bool _isGrounded;
    private int _currentGunIndex, _currentHealth;
    
    // gun state variables
    private float _shotCounter, _heatCounter, _muzzleFlashCounter;
    private bool _isOverheated;
    
    // animator hashes
    private static readonly int Speed = Animator.StringToHash("speed");
    private static readonly int Grounded = Animator.StringToHash("grounded");

    private void Start()
    {
        // lock cursor to game; can be unlocked with Escape key
        Cursor.lockState = CursorLockMode.Locked;
        
        // get components
        _controller = GetComponent<CharacterController>();

        // get references to other game objects
        _camera = Camera.main;
        
        // set initial gun
        photonView.RPC("SetGun", RpcTarget.AllBuffered, 0);
        
        // set initial health
        _currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            playerModel.SetActive(false);
            UIController.Instance.SetMaxHealthSliderValue(maxHealth);
            UIController.Instance.SetHealthSliderValue(_currentHealth);
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
        
        // set initial skin
        playerModel.GetComponent<Renderer>().material =
            playerSkins[PhotonNetwork.LocalPlayer.ActorNumber % playerSkins.Length];

    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        HandleCamera();
        HandleMovement();
        HandleMouseLock();
        HandleShoot();
        HandleChangeGun();
        HandleAnimation();
        HandleZoom();
    }
    
    private void LateUpdate()
    {
        if (!photonView.IsMine) return;
        var cameraTransform = _camera.transform;
        if (MatchManager.Instance.CurrentGameState == MatchManager.GameState.Playing)
        {
            cameraTransform.position = viewPoint.position;
            cameraTransform.rotation = viewPoint.rotation;
        }
        else
        {
            var endCameraTransform = MatchManager.Instance.EndCameraPoint;
            cameraTransform.position = endCameraTransform.position;
            cameraTransform.rotation = endCameraTransform.rotation;
        }
    }
    
    private void HandleCamera()
    {
        // get mouse input
        _mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) *
                      PlayerPreferences.Instance.MouseSensitivity;

        // horizontal player rotation
        var playerRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(playerRotation.eulerAngles.x,
            playerRotation.eulerAngles.y + _mouseInput.x, playerRotation.eulerAngles.z);
        
        // vertical camera rotation
        var viewPointRotation = viewPoint.rotation;
        _verticalRotationStore = Mathf.Clamp(_verticalRotationStore + _mouseInput.y, -60, 60);
        viewPoint.rotation = Quaternion.Euler((PlayerPreferences.Instance.InvertLook ? 1 : -1) * _verticalRotationStore,
            viewPointRotation.eulerAngles.y, viewPointRotation.eulerAngles.z);
    }
    
    private void HandleMovement()
    {
        var playerTransform = transform;
        
        // get movement input
        _moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        _activeMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;

        // check if grounded
        _isGrounded = Physics.CheckSphere(groundCheckPoint.position, 0.25f, groundLayer);
        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _activeMoveSpeed = runSpeed;
            if (!footstepsFast.isPlaying && _moveDirection != Vector3.zero)
            {
                footstepsFast.Play();
                footstepsSlow.Stop();
            }
        }
        else
        {
            _activeMoveSpeed = moveSpeed;
            if (!footstepsSlow.isPlaying && _moveDirection != Vector3.zero)
            {
                footstepsSlow.Play();
                footstepsFast.Stop();
            }
        }
        
        if (_moveDirection == Vector3.zero || !_isGrounded)
        {
            footstepsSlow.Stop();
            footstepsFast.Stop();
        }

        // calculate movement vector
        var newMovement = (playerTransform.forward * _moveDirection.z + playerTransform.right * _moveDirection.x) *
                          _activeMoveSpeed;
        newMovement.y = _movement.y;

        // apply jump and ground force
        if (_isGrounded)
        {
            if(Input.GetButtonDown("Jump"))newMovement.y = jumpForce;
            else _movement.y = 0f;
        }

        // apply gravity
        newMovement.y += Physics.gravity.y * Time.deltaTime * gravityModifier;
        
        // move player
        _controller.Move(newMovement * Time.deltaTime);
        
        // update movement state
        _movement = newMovement;
    }

    private void HandleMouseLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0) &&
                 MatchManager.Instance.CurrentGameState == MatchManager.GameState.Playing &&
                 !UIController.Instance.IsOptionsMenuActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void HandleShoot()
    {
        if (allGuns[_currentGunIndex].MuzzleFlash.activeInHierarchy)
        {
            _muzzleFlashCounter -= Time.deltaTime;
            if (_muzzleFlashCounter <= 0)
            {
                allGuns[_currentGunIndex].MuzzleFlash.SetActive(false);
            }
        }

        if (!_isOverheated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            if (Input.GetMouseButton(0) && allGuns[_currentGunIndex].IsAutomatic)
            {
                _shotCounter -= Time.deltaTime;
                if(_shotCounter <= 0) Shoot();
            }

            // handle heat
            _heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            _heatCounter -= overheatCoolRate * Time.deltaTime;
            if (_heatCounter <= 0)
            {
                _isOverheated = false;
                _heatCounter = 0;
                UIController.Instance.SetOverheatedMessageActive(false);
            }
        }
        
        if(_heatCounter < 0) _heatCounter = 0;
        UIController.Instance.SetHeatSliderValue(_heatCounter / maxHeat);
    }
    
    private void Shoot()
    {
        // handle shoot raycast
        var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ray.origin = _camera.transform.position;
        
        // handle bullet impact
        if (Physics.Raycast(ray, out var hit))
        {
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC(
                    "DealDamage", 
                    RpcTarget.All, 
                    PhotonNetwork.LocalPlayer.NickName, 
                    allGuns[_currentGunIndex].ShotDamage,
                    PhotonNetwork.LocalPlayer.ActorNumber
                );
            }
            else
            {
                var bulletImpactObject = Instantiate(bulletImpact, hit.point + hit.normal * 0.002f,
                    Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }
        
        
        // reset shot counter
        _shotCounter = allGuns[_currentGunIndex].TimeBetweenShots;
        
        // handle heat
        _heatCounter += allGuns[_currentGunIndex].HeatPerShot;
        if (_heatCounter >= maxHeat)
        {
            _isOverheated = true;
            _heatCounter = maxHeat;
            UIController.Instance.SetOverheatedMessageActive(true);
        }
        
        // show muzzle flash
        allGuns[_currentGunIndex].MuzzleFlash.SetActive(true);
        _muzzleFlashCounter = muzzleFlashDuration;
        
        // play shoot sound
        allGuns[_currentGunIndex].PlaySound();

    }

    [PunRPC]
    public void DealDamage(string damageDealer, int damageAmount, int actorNumber)
    {
        TakeDamage(damageDealer, damageAmount, actorNumber);
    }

    private void TakeDamage(string damageDealer, int damageAmount, int actorNumber)
    {
        if (!photonView.IsMine) return;
        StartCoroutine(UIController.Instance.ShowDamageCoroutine());
        
        _currentHealth -= damageAmount;
        
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            PlayerSpawner.Instance.OnDeath(damageDealer);
            MatchManager.Instance.UpdateStatSend(actorNumber, 0, 1);
        }
        
        UIController.Instance.SetHealthSliderValue(_currentHealth);
    }

    private void HandleChangeGun()
    {
        for(int i = 0; i < allGuns.Length; i++)
        {
            if (!Input.GetKeyDown((i + 1).ToString())) continue;
            _currentGunIndex = i;
            photonView.RPC("SetGun", RpcTarget.AllBuffered, i);
        }
    }

    private void ChangeGun()
    {
        foreach (var gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[_currentGunIndex].gameObject.SetActive(true);
        allGuns[_currentGunIndex].MuzzleFlash.SetActive(false);
    }
    
    [PunRPC]
    public void SetGun(int gunIndex)
    {
        if(gunIndex < 0 || gunIndex >= allGuns.Length) return;
        _currentGunIndex = gunIndex;
        ChangeGun();
    }
    
    private void HandleAnimation()
    {
        playerAnimator.SetFloat(Speed, _moveDirection.magnitude);
        playerAnimator.SetBool(Grounded, _isGrounded);
    }
    
    private void HandleZoom()
    {
        if (Input.GetKey(KeyCode.RightShift) || Input.GetMouseButton(1))
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, allGuns[_currentGunIndex].AdsZoom,
                adsSpeed * Time.deltaTime);
            gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
        }
        else
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, 60, adsSpeed * Time.deltaTime);
            gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
        }
    }

}
