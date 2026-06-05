using UnityEngine;

namespace Synaptrace.Systems
{
    public enum SurfaceType
    {
        Standard,
        Water,
        Oil,
        WetWall,
        StickyWall,
        Custom
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SurfaceModifier : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Standard;

        [Header("Movement Multipliers")]
        [SerializeField, Min(0f)] private float movementSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float airControlMultiplier = 1f;
        [SerializeField, Min(0f)] private float groundJumpMultiplier = 1f;

        [Header("Wall Interaction Multipliers")]
        [SerializeField, Min(0f)] private float wallSlideSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalMultiplier = 1f;
        [SerializeField, Min(0f)] private float wallJumpVerticalMultiplier = 1f;
        [SerializeField] private bool allowsWallSlide = true;
        [SerializeField] private bool allowsWallJump = true;

        [Header("Physics Contact")]
        [SerializeField, Range(0f, 1f)] private float contactFriction = 0f;

        public SurfaceType SurfaceType => surfaceType;
        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public float AirControlMultiplier => airControlMultiplier;
        public float GroundJumpMultiplier => groundJumpMultiplier;
        public float WallSlideSpeedMultiplier => wallSlideSpeedMultiplier;
        public float WallJumpHorizontalMultiplier => wallJumpHorizontalMultiplier;
        public float WallJumpVerticalMultiplier => wallJumpVerticalMultiplier;
        public bool AllowsWallSlide => allowsWallSlide;
        public bool AllowsWallJump => allowsWallJump;
        public float ContactFriction => contactFriction;

        private void Awake()
        {
            ApplyContactMaterial();
        }

        public void Configure(
            SurfaceType newSurfaceType,
            float newMovementSpeedMultiplier,
            float newAirControlMultiplier,
            float newGroundJumpMultiplier,
            float newWallSlideSpeedMultiplier,
            float newWallJumpHorizontalMultiplier,
            float newWallJumpVerticalMultiplier,
            bool newAllowsWallSlide,
            bool newAllowsWallJump,
            float newContactFriction)
        {
            surfaceType = newSurfaceType;
            movementSpeedMultiplier = Mathf.Max(0f, newMovementSpeedMultiplier);
            airControlMultiplier = Mathf.Max(0f, newAirControlMultiplier);
            groundJumpMultiplier = Mathf.Max(0f, newGroundJumpMultiplier);
            wallSlideSpeedMultiplier = Mathf.Max(0f, newWallSlideSpeedMultiplier);
            wallJumpHorizontalMultiplier = Mathf.Max(0f, newWallJumpHorizontalMultiplier);
            wallJumpVerticalMultiplier = Mathf.Max(0f, newWallJumpVerticalMultiplier);
            allowsWallSlide = newAllowsWallSlide;
            allowsWallJump = newAllowsWallJump;
            contactFriction = Mathf.Clamp01(newContactFriction);
            ApplyContactMaterial();
        }

        public void ApplyContactMaterial()
        {
            Collider2D surfaceCollider = GetComponent<Collider2D>();

            if (surfaceCollider == null)
            {
                return;
            }

            // Future surface types can raise this value for sticky/wet surfaces while the
            // player controller still applies explicit movement and wall-interaction rules.
            PhysicsMaterial2D material = new PhysicsMaterial2D("Synaptrace Surface - " + surfaceType)
            {
                friction = contactFriction,
                bounciness = 0f,
                hideFlags = HideFlags.HideAndDontSave
            };

            surfaceCollider.sharedMaterial = material;
        }
    }
}
