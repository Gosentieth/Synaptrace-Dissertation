using System;
using UnityEngine;

namespace Synaptrace.Player
{
    public static class PlayerVisualFactory
    {
        public const string VisualRootName = "Visual Root - Protagonist";
        public const string SpriteObjectName = "Protagonist Sprite";
        public const string MainSpriteResourcePath = "Synaptrace/Player/synaptrace-protagonist-main";
        public const string AnimatorControllerResourcePath = "Synaptrace/Player/Protagonist";
        public const bool ArtworkNativeFacesRight = true;
        public const int PlayerSortingOrder = 20;

        public static GameObject Create(Transform parent, PlayerController controller)
        {
            Sprite protagonistSprite = Resources.Load<Sprite>(MainSpriteResourcePath);

            if (protagonistSprite == null)
            {
                throw new InvalidOperationException(
                    "Could not load the protagonist sprite from Resources/" + MainSpriteResourcePath + ".");
            }

            GameObject visualRoot = new GameObject(VisualRootName);
            visualRoot.transform.SetParent(parent, false);

            GameObject spriteObject = new GameObject(SpriteObjectName);
            spriteObject.transform.SetParent(visualRoot.transform, false);

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = protagonistSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = PlayerSortingOrder;

            Animator animator = null;
            RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>(AnimatorControllerResourcePath);

            if (animatorController != null)
            {
                animator = visualRoot.AddComponent<Animator>();
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            PlayerVisualAnimator visualAnimator = visualRoot.AddComponent<PlayerVisualAnimator>();
            visualAnimator.ConfigureAuthoredSprite(
                controller,
                renderer,
                animator,
                protagonistSprite,
                ArtworkNativeFacesRight);
            return visualRoot;
        }
    }
}
