using System.Collections.Generic;
using UnityEngine;

namespace Synaptrace.Player
{
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        public enum VisualState
        {
            Idle,
            Run,
            Jump,
            Fall,
            WallSlide,
            WallJump,
            Phase,
            Disabled
        }

        [SerializeField] private PlayerController controller;
        [SerializeField] private float idleFrequency = 3f;
        [SerializeField] private float runFrequency = 14f;

        private readonly Dictionary<Transform, int> partIndices = new Dictionary<Transform, int>();
        private Transform[] parts;
        private Vector3[] basePositions;
        private Quaternion[] baseRotations;
        private Vector3[] baseScales;
        private SpriteRenderer[] renderers;
        private Color[] baseColors;
        private Vector3 rootBasePosition;
        private Quaternion rootBaseRotation;
        private Vector3 rootBaseScale;
        private Transform cape;
        private Transform ponytail;
        private Transform body;
        private Transform helmet;
        private Transform leftShoulder;
        private Transform rightShoulder;
        private Transform leftBoot;
        private Transform rightBoot;
        private Transform core;
        private Transform phaseAura;
        private SpriteRenderer phaseAuraRenderer;
        private bool visualsCached;

        public VisualState CurrentState { get; private set; }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<PlayerController>();
            }

            CacheVisuals();
        }

        private void LateUpdate()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<PlayerController>();
            }

            if (controller == null)
            {
                return;
            }

            CacheVisuals();
            ResetVisuals();
            CurrentState = ResolveState();
            ApplyState(CurrentState);
            ApplyRendererFeedback(CurrentState);
        }

        public void Configure(PlayerController newController)
        {
            controller = newController;
            CacheVisuals();
        }

        private void CacheVisuals()
        {
            if (visualsCached)
            {
                return;
            }

            rootBasePosition = transform.localPosition;
            rootBaseRotation = transform.localRotation;
            rootBaseScale = transform.localScale;

            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            parts = new Transform[Mathf.Max(0, allTransforms.Length - 1)];
            basePositions = new Vector3[parts.Length];
            baseRotations = new Quaternion[parts.Length];
            baseScales = new Vector3[parts.Length];

            int partIndex = 0;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform part = allTransforms[i];

                if (part == transform)
                {
                    continue;
                }

                parts[partIndex] = part;
                basePositions[partIndex] = part.localPosition;
                baseRotations[partIndex] = part.localRotation;
                baseScales[partIndex] = part.localScale;
                partIndices[part] = partIndex;
                partIndex++;
            }

            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                baseColors[i] = renderers[i].color;
            }

            cape = transform.Find("Crimson Tech Cape");
            ponytail = transform.Find("Knight Ponytail");
            body = transform.Find("Knight Armour Body");
            helmet = transform.Find("Knight Helmet");
            leftShoulder = transform.Find("Left Shoulder Plate");
            rightShoulder = transform.Find("Right Shoulder Plate");
            leftBoot = transform.Find("Left Armoured Boot");
            rightBoot = transform.Find("Right Armoured Boot");
            core = transform.Find("Chest Energy Core");
            phaseAura = transform.Find("Phase Aura");
            phaseAuraRenderer = phaseAura != null ? phaseAura.GetComponent<SpriteRenderer>() : null;
            visualsCached = true;
        }

        private void ResetVisuals()
        {
            transform.localPosition = rootBasePosition;
            transform.localRotation = rootBaseRotation;
            transform.localScale = new Vector3(
                Mathf.Abs(rootBaseScale.x) * controller.FacingDirection,
                rootBaseScale.y,
                rootBaseScale.z);

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].localPosition = basePositions[i];
                parts[i].localRotation = baseRotations[i];
                parts[i].localScale = baseScales[i];
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = baseColors[i];
            }
        }

        private VisualState ResolveState()
        {
            if (!controller.ControlsEnabled)
            {
                return VisualState.Disabled;
            }

            if (controller.IsPhasing)
            {
                return VisualState.Phase;
            }

            if (controller.IsWallJumping)
            {
                return VisualState.WallJump;
            }

            if (controller.IsWallSliding)
            {
                return VisualState.WallSlide;
            }

            if (!controller.IsGrounded)
            {
                return controller.CurrentVelocity.y > 0.15f ? VisualState.Jump : VisualState.Fall;
            }

            return Mathf.Abs(controller.CurrentVelocity.x) > 0.15f ? VisualState.Run : VisualState.Idle;
        }

        private void ApplyState(VisualState state)
        {
            float time = Time.time;

            switch (state)
            {
                case VisualState.Idle:
                    ApplyIdle(time);
                    break;
                case VisualState.Run:
                    ApplyRun(time);
                    break;
                case VisualState.Jump:
                    ApplyJump();
                    break;
                case VisualState.Fall:
                    ApplyFall();
                    break;
                case VisualState.WallSlide:
                    ApplyWallSlide();
                    break;
                case VisualState.WallJump:
                    ApplyWallJump();
                    break;
                case VisualState.Phase:
                    ApplyPhase(time);
                    break;
                case VisualState.Disabled:
                    ApplyDisabled(time);
                    break;
            }
        }

        private void ApplyIdle(float time)
        {
            float breath = Mathf.Sin(time * idleFrequency);
            SetRootPose(breath * 0.018f, 0f, 1f, 1f + breath * 0.012f);
            PosePart(cape, new Vector3(0f, breath * 0.01f, 0f), -4f + breath * 2f, Vector3.one);
            PosePart(ponytail, Vector3.zero, -8f + breath * 3f, Vector3.one);
            PosePart(core, Vector3.zero, 0f, Vector3.one * (1f + breath * 0.06f));
        }

        private void ApplyRun(float time)
        {
            float stride = Mathf.Sin(time * runFrequency);
            float bounce = Mathf.Abs(stride) * 0.035f;
            SetRootPose(bounce, stride * 1.2f, 1f, 1f);
            PosePart(leftBoot, new Vector3(stride * 0.045f, Mathf.Max(0f, stride) * 0.07f, 0f), stride * 8f, Vector3.one);
            PosePart(rightBoot, new Vector3(-stride * 0.045f, Mathf.Max(0f, -stride) * 0.07f, 0f), -stride * 8f, Vector3.one);
            PosePart(body, Vector3.zero, stride * 1.5f, Vector3.one);
            PosePart(cape, Vector3.zero, -13f - Mathf.Abs(stride) * 5f, Vector3.one);
            PosePart(ponytail, Vector3.zero, -18f - Mathf.Abs(stride) * 5f, Vector3.one);
            PosePart(leftShoulder, new Vector3(0f, stride * 0.015f, 0f), 0f, Vector3.one);
            PosePart(rightShoulder, new Vector3(0f, -stride * 0.015f, 0f), 0f, Vector3.one);
        }

        private void ApplyJump()
        {
            SetRootPose(0.025f, -3f * controller.FacingDirection, 0.96f, 1.04f);
            PosePart(leftBoot, new Vector3(0.035f, 0.09f, 0f), -8f, Vector3.one);
            PosePart(rightBoot, new Vector3(-0.035f, 0.06f, 0f), 8f, Vector3.one);
            PosePart(cape, Vector3.zero, 12f, Vector3.one);
            PosePart(ponytail, Vector3.zero, 18f, Vector3.one);
        }

        private void ApplyFall()
        {
            SetRootPose(0f, 3f * controller.FacingDirection, 1.02f, 0.98f);
            PosePart(leftBoot, new Vector3(-0.025f, -0.025f, 0f), 5f, Vector3.one);
            PosePart(rightBoot, new Vector3(0.025f, -0.025f, 0f), -5f, Vector3.one);
            PosePart(cape, Vector3.zero, 22f, Vector3.one);
            PosePart(ponytail, Vector3.zero, 27f, Vector3.one);
        }

        private void ApplyWallSlide()
        {
            SetRootPose(0f, -7f * controller.FacingDirection, 0.98f, 1.02f);
            PosePart(leftBoot, new Vector3(0.025f, 0.045f, 0f), -7f, Vector3.one);
            PosePart(rightBoot, new Vector3(-0.025f, 0.02f, 0f), 7f, Vector3.one);
            PosePart(cape, Vector3.zero, 28f, Vector3.one);
            PosePart(ponytail, Vector3.zero, 34f, Vector3.one);
        }

        private void ApplyWallJump()
        {
            SetRootPose(0.02f, 11f * controller.FacingDirection, 1.08f, 0.94f);
            PosePart(leftBoot, new Vector3(0.035f, 0.085f, 0f), -12f, Vector3.one);
            PosePart(rightBoot, new Vector3(-0.035f, 0.065f, 0f), 12f, Vector3.one);
            PosePart(cape, Vector3.zero, -25f, Vector3.one);
            PosePart(ponytail, Vector3.zero, -32f, Vector3.one);
        }

        private void ApplyPhase(float time)
        {
            float shimmer = Mathf.Sin(time * 45f) * 0.012f;
            SetRootPose(shimmer, 0f, 1.2f, 0.82f);
            PosePart(cape, new Vector3(-0.08f, 0f, 0f), -32f, new Vector3(1.25f, 0.8f, 1f));
            PosePart(ponytail, new Vector3(-0.06f, 0f, 0f), -38f, new Vector3(1.2f, 0.85f, 1f));
            PosePart(phaseAura, Vector3.zero, 0f, new Vector3(1.28f, 0.92f, 1f));
        }

        private void ApplyDisabled(float time)
        {
            float pulse = Mathf.Sin(time * 4f) * 0.01f;
            SetRootPose(-0.035f + pulse, 5f * controller.FacingDirection, 1f, 0.96f);
            PosePart(cape, Vector3.zero, 12f, new Vector3(1f, 0.92f, 1f));
            PosePart(ponytail, Vector3.zero, 16f, Vector3.one);
        }

        private void ApplyRendererFeedback(VisualState state)
        {
            if (state == VisualState.Phase)
            {
                Color phaseTint = new Color(0.18f, 0.95f, 1f, 1f);

                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];

                    if (renderer == phaseAuraRenderer)
                    {
                        renderer.color = new Color(0.18f, 0.95f, 1f, 0.52f);
                        continue;
                    }

                    Color color = Color.Lerp(baseColors[i], phaseTint, 0.42f);
                    color.a = Mathf.Min(baseColors[i].a, 0.52f);
                    renderer.color = color;
                }
            }
            else if (state == VisualState.Disabled)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Color color = baseColors[i];
                    color.a *= 0.72f;
                    renderers[i].color = color;
                }
            }
        }

        private void SetRootPose(float yOffset, float rotationDegrees, float xScale, float yScale)
        {
            transform.localPosition = rootBasePosition + new Vector3(0f, yOffset, 0f);
            transform.localRotation = rootBaseRotation * Quaternion.Euler(0f, 0f, rotationDegrees);
            transform.localScale = new Vector3(
                Mathf.Abs(rootBaseScale.x) * controller.FacingDirection * xScale,
                rootBaseScale.y * yScale,
                rootBaseScale.z);
        }

        private void PosePart(Transform part, Vector3 offset, float rotationDegrees, Vector3 scaleMultiplier)
        {
            if (part == null || !partIndices.TryGetValue(part, out int index))
            {
                return;
            }

            part.localPosition = basePositions[index] + offset;
            part.localRotation = baseRotations[index] * Quaternion.Euler(0f, 0f, rotationDegrees);
            part.localScale = Vector3.Scale(baseScales[index], scaleMultiplier);
        }
    }
}
