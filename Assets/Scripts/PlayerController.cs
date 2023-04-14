using UnityEngine;

public class PlayerController : MonoBehaviour
{

    // components on player game object; set in Start
    private CharacterController _controller;

    // references to other game objects; set in Inspector
    [Header("References")]
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject bulletImpact;
    [SerializeField] private Gun[] allGuns;

    // user settings; set in Inspector - this will be changed later
    [Header("User Settings")]
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private bool invertLook;
    
    // player stats; set in Inspector
    [Header("Player Stats")]
    [SerializeField] private float moveSpeed = 5f; 
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float gravityModifier = 2.5f;
    
    [Header("Gun Stats")]
    [SerializeField] private float maxHeat = 10f;
    [SerializeField] private float coolRate = 4f;
    [SerializeField] private float overheatCoolRate = 5f;
    [SerializeField] private float muzzleFlashDuration = 0.1f;

    // private references; set in Start
    private Camera _camera;

    // player state variables; set in Update
    private Vector2 _mouseInput;
    private Vector3 _moveDirection, _movement;
    private float _verticalRotationStore;
    private float _activeMoveSpeed;
    private bool _isGrounded;
    private int _currentGunIndex;
    
    // gun state variables
    private float _shotCounter;
    private float _heatCounter;
    private bool _isOverheated;
    private float _muzzleFlashCounter;

    // Start is called before the first frame update
    private void Start()
    {
        // lock cursor to game; can be unlocked with Escape key
        Cursor.lockState = CursorLockMode.Locked;
        
        // get components
        _controller = GetComponent<CharacterController>();

        // get references to other game objects
        _camera = Camera.main;
        
        // set initial gun
        ChangeGun();
        
        // set spawn point
        var spawnPoint = SpawnManager.Instance.GetSpawnPoint();
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
    }

    // Update is called once per frame
    private void Update()
    {
        HandleCamera();
        HandleMovement();
        HandleMouseLock();
        HandleShoot();
        HandleChangeGun();
    }
    
    private void LateUpdate()
    {
        _camera.transform.position = viewPoint.position;
        _camera.transform.rotation = viewPoint.rotation;
    }
    
    private void HandleCamera()
    {
        // get mouse input
        _mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        // horizontal player rotation
        var playerRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(playerRotation.eulerAngles.x,
            playerRotation.eulerAngles.y + _mouseInput.x, playerRotation.eulerAngles.z);
        
        // vertical camera rotation
        var viewPointRotation = viewPoint.rotation;
        _verticalRotationStore = Mathf.Clamp(_verticalRotationStore + _mouseInput.y, -60, 60);
        viewPoint.rotation = Quaternion.Euler((invertLook ? 1 : -1) * _verticalRotationStore,
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
        if(Input.GetKeyDown(KeyCode.Escape)) 
            Cursor.lockState = CursorLockMode.None;
        else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
            Cursor.lockState = CursorLockMode.Locked;
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
        if (!Physics.Raycast(ray, out var hit)) return;
        var bulletImpactObject = Instantiate(bulletImpact, hit.point + hit.normal * 0.002f,
            Quaternion.LookRotation(hit.normal, Vector3.up));
        Destroy(bulletImpactObject, 10f);
        
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

    }

    private void HandleChangeGun()
    {
        for(int i = 0; i < allGuns.Length; i++)
        {
            if (!Input.GetKeyDown((i + 1).ToString())) continue;
            _currentGunIndex = i;
            ChangeGun();
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

}
