using Synaptrace.Core;
using Synaptrace.Player;
using UnityEngine;

namespace Synaptrace.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class RushEnemyController : MonoBehaviour
    {
        private enum EnemyState
        {
            Patrol,
            Alert,
            Rush,
            Recovery,
            Stunned
        }

        [Header("Patrol")]
        [SerializeField] private float patrolHalfDistance = 0.85f;
        [SerializeField] private float patrolSpeed = 0.8f;

        [Header("Rush")]
        [SerializeField] private float detectionRange = 2.4f;
        [SerializeField] private float verticalDetectionTolerance = 1.5f;
        [SerializeField] private float alertDuration = 0.4f;
        [SerializeField] private float rushSpeed = 5.2f;
        [SerializeField] private float rushDuration = 0.5f;
        [SerializeField] private float recoveryDuration = 0.8f;
        [SerializeField] private float phaseStunDuration = 1.2f;
        [SerializeField] private string enemyName = "Corrupted Sentinel";

        private Rigidbody2D body;
        private Collider2D contactCollider;
        private PlayerController player;
        private Transform visualRoot;
        private SpriteRenderer[] visualRenderers;
        private Color[] baseColors;
        private Vector3 visualBaseScale;
        private float patrolCenterX;
        private float fixedY;
        private float moveDirection = 1f;
        private float stateTimer;
        private EnemyState state = EnemyState.Patrol;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            contactCollider = GetComponent<Collider2D>();
            patrolCenterX = transform.position.x;
            fixedY = transform.position.y;
        }

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Update()
        {
            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            switch (state)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Alert:
                    UpdateAlert();
                    break;
                case EnemyState.Rush:
                    UpdateRush();
                    break;
                case EnemyState.Recovery:
                    UpdateRecovery();
                    break;
                case EnemyState.Stunned:
                    UpdateStunned();
                    break;
            }
        }

        public void Configure(PlayerController targetPlayer, Transform newVisualRoot)
        {
            player = targetPlayer;
            visualRoot = newVisualRoot;

            if (visualRoot != null)
            {
                visualBaseScale = visualRoot.localScale;
                visualRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
                baseColors = new Color[visualRenderers.Length];

                for (int i = 0; i < visualRenderers.Length; i++)
                {
                    baseColors[i] = visualRenderers[i].color;
                }
            }
        }

        private void UpdatePatrol()
        {
            if (CanDetectPlayer())
            {
                moveDirection = player.transform.position.x < transform.position.x ? -1f : 1f;
                state = EnemyState.Alert;
                stateTimer = alertDuration;
                return;
            }

            bool reachedBoundary = MoveWithinPatrolBounds(moveDirection * patrolSpeed);

            if (reachedBoundary)
            {
                moveDirection *= -1f;
            }
        }

        private void UpdateAlert()
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.fixedDeltaTime);

            if (player != null)
            {
                moveDirection = player.transform.position.x < transform.position.x ? -1f : 1f;
            }

            if (stateTimer <= 0f)
            {
                state = EnemyState.Rush;
                stateTimer = rushDuration;
            }
        }

        private void UpdateRush()
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.fixedDeltaTime);
            bool reachedBoundary = MoveWithinPatrolBounds(moveDirection * rushSpeed);

            if (stateTimer <= 0f || reachedBoundary)
            {
                state = EnemyState.Recovery;
                stateTimer = recoveryDuration;
            }
        }

        private void UpdateRecovery()
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.fixedDeltaTime);

            if (stateTimer <= 0f)
            {
                state = EnemyState.Patrol;
                moveDirection *= -1f;
            }
        }

        private void UpdateStunned()
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.fixedDeltaTime);

            if (stateTimer <= 0f)
            {
                contactCollider.enabled = true;
                state = EnemyState.Recovery;
                stateTimer = recoveryDuration * 0.5f;
            }
        }

        private bool CanDetectPlayer()
        {
            if (player == null || !player.ControlsEnabled)
            {
                return false;
            }

            Vector2 difference = player.transform.position - transform.position;
            return Mathf.Abs(difference.x) <= detectionRange
                && Mathf.Abs(difference.y) <= verticalDetectionTolerance;
        }

        private bool MoveWithinPatrolBounds(float speed)
        {
            float minimumX = patrolCenterX - patrolHalfDistance;
            float maximumX = patrolCenterX + patrolHalfDistance;
            float requestedX = body.position.x + speed * Time.fixedDeltaTime;
            float clampedX = Mathf.Clamp(requestedX, minimumX, maximumX);
            body.MovePosition(new Vector2(clampedX, fixedY));
            return !Mathf.Approximately(requestedX, clampedX);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController contactedPlayer = other.GetComponentInParent<PlayerController>();

            if (contactedPlayer == null)
            {
                return;
            }

            if (contactedPlayer.IsPhasing)
            {
                BeginPhaseStun();
                return;
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegisterPlayerDeath(enemyName, false);
            }
        }

        private void BeginPhaseStun()
        {
            state = EnemyState.Stunned;
            stateTimer = phaseStunDuration;
            contactCollider.enabled = false;
        }

        private void UpdateVisuals()
        {
            if (visualRoot == null || visualRenderers == null)
            {
                return;
            }

            float facing = moveDirection < 0f ? -1f : 1f;
            float bob = Mathf.Sin(Time.time * 6f) * 0.025f;
            float xScale = Mathf.Abs(visualBaseScale.x) * facing;
            float yScale = visualBaseScale.y;
            Color tint = Color.white;

            if (state == EnemyState.Alert)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 24f) * 0.08f;
                xScale *= pulse;
                yScale *= pulse;
                tint = new Color(1f, 0.45f, 0.55f, 1f);
            }
            else if (state == EnemyState.Rush)
            {
                xScale *= 1.2f;
                yScale *= 0.86f;
                bob = 0f;
            }
            else if (state == EnemyState.Stunned)
            {
                float flicker = 0.72f + Mathf.Sin(Time.time * 30f) * 0.12f;
                xScale *= 0.92f;
                yScale *= 0.92f;
                tint = new Color(0.45f, 0.9f, 1f, flicker);
            }

            visualRoot.localPosition = new Vector3(0f, bob, 0f);
            visualRoot.localScale = new Vector3(xScale, yScale, visualBaseScale.z);

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Color color = Color.Lerp(baseColors[i], tint, state == EnemyState.Patrol ? 0f : 0.4f);

                if (state == EnemyState.Stunned)
                {
                    color.a = Mathf.Min(color.a, tint.a);
                }

                visualRenderers[i].color = color;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.35f, 0.65f);
            Gizmos.DrawWireCube(transform.position, new Vector3(detectionRange * 2f, verticalDetectionTolerance * 2f, 0f));
        }
    }
}
