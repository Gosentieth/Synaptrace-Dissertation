using UnityEngine;

namespace Synaptrace.Adaptation
{
    [CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Synaptrace/Difficulty Profile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "static-prototype";
        [SerializeField] [Min(0.1f)] private float playerMoveSpeedMultiplier = 1f;
        [SerializeField] [Min(0.1f)] private float playerJumpImpulseMultiplier = 1f;

        public string ProfileId => profileId;
        public float PlayerMoveSpeedMultiplier => playerMoveSpeedMultiplier;
        public float PlayerJumpImpulseMultiplier => playerJumpImpulseMultiplier;

#if UNITY_EDITOR
        public void ConfigureForEditor(string editorProfileId, float editorMoveMultiplier, float editorJumpMultiplier)
        {
            profileId = editorProfileId;
            playerMoveSpeedMultiplier = editorMoveMultiplier;
            playerJumpImpulseMultiplier = editorJumpMultiplier;
        }
#endif
    }
}
