using Synaptrace.Adaptation;
using Synaptrace.Core;
using Synaptrace.Systems;
using Synaptrace.Telemetry;
using UnityEngine;

namespace Synaptrace.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpImpulse = 12f;
        [SerializeField] private float maxFallSpeed = -18f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.58f);
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.68f, 0.12f);

        [Header("Wall Interaction")]
        [SerializeField] private float wallCheckDistance = 0.46f;
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.12f, 0.88f);
        [SerializeField] private float wallSlideSpeed = 1.8f;
        [SerializeField] private Vector2 wallJumpImpulse = new Vector2(8.5f, 11.5f);
        [SerializeField] private float wallJumpControlLockTime = 0.16f;

        [Header("Phase Dodge")]
        [SerializeField] private float phaseSpeed = 11f;
        [SerializeField] private float phaseDuration = 0.22f;
        [SerializeField] private float phaseCooldown = 0.8f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private TelemetryTracker telemetryTracker;
        private SurfaceModifier groundSurface;
        private SurfaceModifier wallSurface;
        private float baseMoveSpeed;
        private float baseJumpImpulse;
        private float moveInput;
        private float wallJumpControlTimer;
        private float phaseTimeRemaining;
        private float phaseCooldownRemaining;
        private int wallDirection;
        private int phaseDirection = 1;
        private bool controlsEnabled = true;
        private bool jumpRequested;
        private bool phaseRequested;
        private bool isTouchingWall;

        public bool IsGrounded { get; private set; }
        public bool IsWallSliding { get; private set; }
        public bool IsPhasing => phaseTimeRemaining > 0f;
        public bool IsWallJumping => wallJumpControlTimer > 0f && !IsGrounded;
        public bool ControlsEnabled => controlsEnabled;
        public float FacingDirection { get; private set; } = 1f;
        public Vector2 CurrentVelocity => body != null ? body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            baseMoveSpeed = moveSpeed;
            baseJumpImpulse = jumpImpulse;
            bodyCollider.sharedMaterial = CreateNoFrictionMaterial("Synaptrace Player Contact");

            if (groundLayers.value == 0)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                groundLayers = groundLayer >= 0 ? 1 << groundLayer : Physics2D.AllLayers;
            }
        }

        private void Start()
        {
            if (telemetryTracker == null)
            {
                telemetryTracker = FindFirstObjectByType<TelemetryTracker>();
            }
        }

        private void Update()
        {
            if (!controlsEnabled)
            {
                moveInput = 0f;
                jumpRequested = false;
                return;
            }

            ReadMovementInput();
            ReadJumpInput();
            ReadPhaseInput();
            ReadRestartInput();
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();
            UpdateWallState();
            ProcessPhaseRequest();

            if (IsPhasing)
            {
                IsWallSliding = false;
                jumpRequested = false;
                ApplyPhaseMovement();
            }
            else
            {
                ApplyMovement();
                ApplyJump();
                ApplyWallSlide();
                ClampFallSpeed();
            }

            UpdateTimers();
        }

        public void SetTelemetry(TelemetryTracker tracker)
        {
            telemetryTracker = tracker;
        }

        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled && body != null)
            {
                phaseTimeRemaining = 0f;
                phaseRequested = false;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
        }

        public void RespawnAt(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            moveInput = 0f;
            jumpRequested = false;
            phaseRequested = false;
            wallJumpControlTimer = 0f;
            phaseTimeRemaining = 0f;
            phaseCooldownRemaining = 0f;
            wallDirection = 0;
            phaseDirection = 1;
            FacingDirection = 1f;
            groundSurface = null;
            wallSurface = null;
            isTouchingWall = false;
            IsWallSliding = false;
            SetControlsEnabled(true);
        }

        public void ApplyDifficultyProfile(DifficultyProfile profile)
        {
            if (profile == null)
            {
                moveSpeed = baseMoveSpeed;
                jumpImpulse = baseJumpImpulse;
                return;
            }

            moveSpeed = baseMoveSpeed * profile.PlayerMoveSpeedMultiplier;
            jumpImpulse = baseJumpImpulse * profile.PlayerJumpImpulseMultiplier;
        }

        public void SetGroundLayers(LayerMask layers)
        {
            groundLayers = layers;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(LayerMask editorGroundLayers)
        {
            SetGroundLayers(editorGroundLayers);
        }
#endif

        private void ReadMovementInput()
        {
            float keyboardInput = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                keyboardInput -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                keyboardInput += 1f;
            }

            moveInput = Mathf.Clamp(keyboardInput, -1f, 1f);

            if (Mathf.Abs(moveInput) > 0.1f)
            {
                FacingDirection = Mathf.Sign(moveInput);
            }
        }

        private void ReadJumpInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                jumpRequested = true;
            }
        }

        private void ReadPhaseInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                phaseRequested = true;
            }
        }

        private void ReadRestartInput()
        {
            if (Input.GetKeyDown(KeyCode.R) && LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartLevelFromInput();
            }
        }

        private void ApplyMovement()
        {
            if (wallJumpControlTimer > 0f)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = moveInput * moveSpeed * GetMovementControlMultiplier();
            body.linearVelocity = velocity;
        }

        private void ApplyJump()
        {
            if (!jumpRequested)
            {
                return;
            }

            if (IsGrounded)
            {
                PerformGroundJump();
            }
            else if (IsWallSliding || isTouchingWall)
            {
                PerformWallJump();
            }

            jumpRequested = false;
        }

        private void PerformGroundJump()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpImpulse * GetGroundJumpMultiplier();
            body.linearVelocity = velocity;

            RecordJumpTelemetry();
        }

        private void PerformWallJump()
        {
            if (!CanWallJumpOnCurrentSurface())
            {
                return;
            }

            int jumpDirection = wallDirection != 0 ? -wallDirection : -1;
            Vector2 activeWallJumpImpulse = GetWallJumpImpulse();
            body.linearVelocity = new Vector2(activeWallJumpImpulse.x * jumpDirection, activeWallJumpImpulse.y);
            FacingDirection = jumpDirection;
            wallJumpControlTimer = wallJumpControlLockTime;
            IsWallSliding = false;

            RecordJumpTelemetry();
        }

        private void ProcessPhaseRequest()
        {
            if (!phaseRequested)
            {
                return;
            }

            phaseRequested = false;

            if (phaseCooldownRemaining > 0f || IsPhasing)
            {
                return;
            }

            float requestedDirection = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : FacingDirection;
            phaseDirection = requestedDirection < 0f ? -1 : 1;
            FacingDirection = phaseDirection;
            phaseTimeRemaining = phaseDuration;
            phaseCooldownRemaining = phaseCooldown;
            wallJumpControlTimer = 0f;
            jumpRequested = false;
            IsWallSliding = false;

            if (telemetryTracker != null)
            {
                telemetryTracker.RecordDodge();
            }
        }

        private void ApplyPhaseMovement()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = phaseDirection * phaseSpeed;
            body.linearVelocity = velocity;
        }

        private void ApplyWallSlide()
        {
            if (!IsWallSliding)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            float activeWallSlideSpeed = GetWallSlideSpeed();

            if (velocity.y < -activeWallSlideSpeed)
            {
                velocity.y = -activeWallSlideSpeed;
                body.linearVelocity = velocity;
            }
        }

        private void ClampFallSpeed()
        {
            if (body.linearVelocity.y >= maxFallSpeed)
            {
                return;
            }

            body.linearVelocity = new Vector2(body.linearVelocity.x, maxFallSpeed);
        }

        private void UpdateTimers()
        {
            if (wallJumpControlTimer > 0f)
            {
                wallJumpControlTimer = Mathf.Max(0f, wallJumpControlTimer - Time.fixedDeltaTime);
            }

            if (phaseTimeRemaining > 0f)
            {
                phaseTimeRemaining = Mathf.Max(0f, phaseTimeRemaining - Time.fixedDeltaTime);
            }

            if (phaseCooldownRemaining > 0f)
            {
                phaseCooldownRemaining = Mathf.Max(0f, phaseCooldownRemaining - Time.fixedDeltaTime);
            }
        }

        private void UpdateWallState()
        {
            SurfaceModifier rightWallSurface;
            SurfaceModifier leftWallSurface;
            bool touchingRightWall = CheckWall(1, out rightWallSurface);
            bool touchingLeftWall = CheckWall(-1, out leftWallSurface);

            if (touchingRightWall)
            {
                wallDirection = 1;
                wallSurface = rightWallSurface;
            }
            else if (touchingLeftWall)
            {
                wallDirection = -1;
                wallSurface = leftWallSurface;
            }
            else
            {
                wallDirection = 0;
                wallSurface = null;
            }

            isTouchingWall = wallDirection != 0;

            if (IsGrounded)
            {
                wallJumpControlTimer = 0f;
                IsWallSliding = false;
                return;
            }

            bool pressingTowardWall = (wallDirection > 0 && moveInput > 0.1f)
                || (wallDirection < 0 && moveInput < -0.1f);

            // This stays intentionally simple for now. SurfaceModifier gives later adaptive
            // difficulty work a clean place to tune wall slide/jump behaviour per surface.
            IsWallSliding = isTouchingWall
                && pressingTowardWall
                && body.linearVelocity.y <= 0f
                && wallJumpControlTimer <= 0f
                && CanWallSlideOnCurrentSurface();
        }

        private bool CheckGrounded()
        {
            Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
            Collider2D hit = Physics2D.OverlapBox(checkCenter, groundCheckSize, 0f, groundLayers);

            if (hit != null && hit != bodyCollider)
            {
                groundSurface = hit.GetComponentInParent<SurfaceModifier>();
                return true;
            }

            groundSurface = null;
            return false;
        }

        private bool CheckWall(int direction, out SurfaceModifier surfaceModifier)
        {
            surfaceModifier = null;
            Vector2 checkCenter = (Vector2)transform.position + new Vector2(wallCheckDistance * direction, 0f);
            Collider2D hit = Physics2D.OverlapBox(checkCenter, wallCheckSize, 0f, groundLayers);

            if (hit != null && hit != bodyCollider)
            {
                surfaceModifier = hit.GetComponentInParent<SurfaceModifier>();
                return true;
            }

            return false;
        }

        private float GetMovementControlMultiplier()
        {
            if (IsGrounded && groundSurface != null)
            {
                return Mathf.Max(0f, groundSurface.MovementSpeedMultiplier);
            }

            if (!IsGrounded && wallSurface != null)
            {
                return Mathf.Max(0f, wallSurface.AirControlMultiplier);
            }

            return 1f;
        }

        private float GetGroundJumpMultiplier()
        {
            return groundSurface != null ? Mathf.Max(0f, groundSurface.GroundJumpMultiplier) : 1f;
        }

        private float GetWallSlideSpeed()
        {
            float surfaceMultiplier = wallSurface != null ? Mathf.Max(0f, wallSurface.WallSlideSpeedMultiplier) : 1f;
            return wallSlideSpeed * surfaceMultiplier;
        }

        private Vector2 GetWallJumpImpulse()
        {
            if (wallSurface == null)
            {
                return wallJumpImpulse;
            }

            float horizontalMultiplier = Mathf.Max(0f, wallSurface.WallJumpHorizontalMultiplier);
            float verticalMultiplier = Mathf.Max(0f, wallSurface.WallJumpVerticalMultiplier);
            return new Vector2(wallJumpImpulse.x * horizontalMultiplier, wallJumpImpulse.y * verticalMultiplier);
        }

        private bool CanWallSlideOnCurrentSurface()
        {
            return wallSurface == null || wallSurface.AllowsWallSlide;
        }

        private bool CanWallJumpOnCurrentSurface()
        {
            return wallSurface == null || wallSurface.AllowsWallJump;
        }

        private void RecordJumpTelemetry()
        {
            if (telemetryTracker != null)
            {
                telemetryTracker.RecordJump();
            }
        }

        private PhysicsMaterial2D CreateNoFrictionMaterial(string materialName)
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D(materialName)
            {
                friction = 0f,
                bounciness = 0f,
                hideFlags = HideFlags.HideAndDontSave
            };

            return material;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Vector2 groundCheckCenter = (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);

            Gizmos.color = IsWallSliding ? Color.cyan : new Color(0.3f, 0.8f, 1f, 0.8f);
            Vector2 rightWallCheckCenter = (Vector2)transform.position + new Vector2(wallCheckDistance, 0f);
            Vector2 leftWallCheckCenter = (Vector2)transform.position + new Vector2(-wallCheckDistance, 0f);
            Gizmos.DrawWireCube(rightWallCheckCenter, wallCheckSize);
            Gizmos.DrawWireCube(leftWallCheckCenter, wallCheckSize);
        }
    }
}
