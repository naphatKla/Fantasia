using System;
using System.Collections;
using System.Linq;
using UnityEngine;


namespace PlayerBehavior
{
    public enum PlayerState
    {
        Idle,
        Walk,
        Sprint,
        Dash
    }

    public class Player : MonoBehaviour
    {
        #region Declare Variables

        [Header("Player Movement")] 
        public PlayerState playerState;
        [SerializeField] private float walkSpeed;
        [SerializeField] private float sprintSpeed;
        [SerializeField] private float dashSpeed;
        [SerializeField] private float dashDuration;
        [SerializeField] private float dashCooldown;
        [SerializeField] private KeyCode sprintKey;
        [SerializeField] private KeyCode dashKey;
        [SerializeField] private Transform canvasTransform;
        [SerializeField] private LayerMask visibleLayerMask;

        [Header("Player Stamina")] 
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaRegenSpeed;
        [SerializeField] private float staminaRegenCooldown;
        [SerializeField] private float sprintStaminaDrain;
        [SerializeField] private float dashStaminaDrain;

        private bool _isDash;
        private bool _isDashCooldown;
        private float _currentSpeed;
        [SerializeField] private float _currentStamina; //When stamina bar is done, delete [SerializeField] this code
        private float _staminaRegenCurrentCooldown;
        private Animator _animator;
        private Rigidbody2D _playerRigidbody2D;
        public static Player Instance;
        private static readonly int IsDashAnimation = Animator.StringToHash("IsDash");
        public Animator Animator => _animator;
        public bool IsDash => _isDash;
        public float MaxStamina => maxStamina;
        public float CurrentStamina { get => _currentStamina; set => _currentStamina = value; }

        #endregion

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerRigidbody2D = GetComponent<Rigidbody2D>();
            ResetState();
            Instance = this;
            _currentStamina = maxStamina;
        }

        private void Update()
        {
            MovementHandle();
            RegenStaminaHandle();
        }

        private void LateUpdate()
        {
            // Lock the canvas UI rotation.
            canvasTransform.right = Vector3.right;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Base")) return;
            StartCoroutine(LerpAlpha(other.GetComponent<SpriteRenderer>(), 0.75f, 0.5f));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Base")) return;
            StartCoroutine(LerpAlpha(other.GetComponent<SpriteRenderer>(), 1f, 0.5f));
        }

        #region Methods

        /// <summary>
        /// Use for control the player movement system.
        /// </summary>
        private void MovementHandle()
        {
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerAttackState_1") ||
                _animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerAttackState_2") ||
                _animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerAttackState_3") ||
                _animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerHeavyAttack"))
            {
                _playerRigidbody2D.velocity = Vector2.zero;
                return;
            }

            WalkHandle();
            SprintHandle();
            DashHandle();

            Vector2 playerVelocity =
                new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * _currentSpeed;
            _playerRigidbody2D.velocity = Vector2.ClampMagnitude(playerVelocity, _currentSpeed);
            
            _animator.SetTrigger(playerState.ToString());
            _animator.SetBool(IsDashAnimation, _isDash);

            // flip player horizontal direction
            if (Input.GetAxisRaw("Horizontal") != 0)
                transform.right = Input.GetAxisRaw("Horizontal") < 0 ? Vector2.left : Vector2.right;
        }

        /// <summary>
        /// Walk System 
        /// </summary>
        private void WalkHandle()
        {
            if (CheckPlayerState(PlayerState.Dash)) return;
            _currentSpeed = walkSpeed;

            if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            {
                SetPlayerState(PlayerState.Idle);
                return;
            }

            SetPlayerState(PlayerState.Walk);
        }

        /// <summary>
        /// Sprint System
        /// </summary>
        private void SprintHandle()
        {
            if (CheckPlayerState(PlayerState.Dash) || CheckPlayerState(PlayerState.Idle)) return;
            if (_currentStamina <= 0) return;
            if (!Input.GetKey(sprintKey)) return;
      
            _currentSpeed = sprintSpeed;
            SetPlayerState(PlayerState.Sprint);
            _currentStamina -= sprintStaminaDrain * Time.deltaTime;
        }

        /// <summary>
        /// Use for handle dash system.
        /// </summary>
        private void DashHandle()
        {
            if (CheckPlayerState(PlayerState.Idle) || _isDash) return;
            if (_isDashCooldown || _currentStamina < dashStaminaDrain) return;
            if (!Input.GetKeyDown(dashKey)) return;

            StartCoroutine(Dash());
            _currentStamina -= dashStaminaDrain;
        }

        /// <summary>
        /// Dash behavior for start coroutine in dash system.
        /// </summary>
        private IEnumerator Dash()
        {
            _isDash = true;
            _isDashCooldown = true;
            _currentSpeed = dashSpeed;
            SetPlayerState(PlayerState.Dash);

            yield return new WaitForSeconds(dashDuration);
            _isDash = false;
            SetPlayerState(PlayerState.Idle);

            yield return new WaitForSeconds(dashCooldown);
            _isDashCooldown = false;
        }

        /// <summary>
        /// Use for set player state.
        /// </summary>
        /// <param name="state">State that you want to set.</param>
        private void SetPlayerState(PlayerState state)
        {
            playerState = state;
        }

        /// <summary>
        /// Regen stamina when player is not running or dashing.
        /// </summary>
        private void RegenStaminaHandle()
        {
            _currentStamina = Mathf.Clamp(_currentStamina, 0, maxStamina);
            if (CheckPlayerState(PlayerState.Sprint) || CheckPlayerState(PlayerState.Dash))
            {
                _staminaRegenCurrentCooldown = 0f;
                return;
            }

            _staminaRegenCurrentCooldown += Time.deltaTime;
            if (_staminaRegenCurrentCooldown < staminaRegenCooldown) return;
            _currentStamina += staminaRegenSpeed * Time.deltaTime;
        }
        
        /// <summary>
        /// Use to check the current player state.
        /// </summary>
        /// <param name="state">State that you want to check.</param>
        /// <returns>Is current state is equal to the input state or not. (True/False)</returns>
        private bool CheckPlayerState(PlayerState state)
        {
            return playerState == state;
        }

        /// <summary>
        /// Reset every behavior to default. ( Use when start / respawn. )
        /// </summary>
        private void ResetState()
        {
            SetPlayerState(PlayerState.Idle);
            _isDash = false;
            _currentSpeed = walkSpeed;
        }

        private IEnumerator LerpAlpha(SpriteRenderer spriteRenderer,float destination, float time)
        {
            float timeCout = 0;
            while (timeCout < time)
            {
                if(!spriteRenderer) yield break;
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(color.a, destination, timeCout / time);
                spriteRenderer.color = color;
                timeCout += Time.deltaTime;
                yield return null;
            }
        }
        #endregion
    }
}