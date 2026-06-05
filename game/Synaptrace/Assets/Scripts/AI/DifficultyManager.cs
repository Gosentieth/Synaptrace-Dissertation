using Synaptrace.Core;
using Synaptrace.Telemetry;
using UnityEngine;

namespace Synaptrace.Adaptation
{
    public enum AdaptationMode
    {
        Static,
        RuleBasedPlaceholder,
        ReinforcementLearningPlaceholder
    }

    public interface IDifficultyAdapter
    {
        string AdapterName { get; }
        void ConfigureLevel(LevelManager levelManager, TelemetryTracker telemetryTracker);
    }

    public sealed class DifficultyManager : MonoBehaviour
    {
        [SerializeField] private AdaptationMode currentMode = AdaptationMode.Static;
        [SerializeField] private DifficultyProfile staticProfile = null;

        public AdaptationMode CurrentMode => currentMode;

        public void ConfigureLevel(LevelManager levelManager, TelemetryTracker telemetryTracker)
        {
            if (levelManager == null)
            {
                return;
            }

            // Future static, rule-based, and RL adapters should connect here.
            // The base game only applies a simple static profile for now.
            switch (currentMode)
            {
                case AdaptationMode.Static:
                    ApplyStaticProfile(levelManager);
                    break;
                case AdaptationMode.RuleBasedPlaceholder:
                    ApplyPlaceholderMode(levelManager, "rule-based adaptation");
                    break;
                case AdaptationMode.ReinforcementLearningPlaceholder:
                    ApplyPlaceholderMode(levelManager, "reinforcement learning adaptation");
                    break;
                default:
                    ApplyStaticProfile(levelManager);
                    break;
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(DifficultyProfile editorProfile, AdaptationMode editorMode)
        {
            staticProfile = editorProfile;
            currentMode = editorMode;
        }
#endif

        private void ApplyStaticProfile(LevelManager levelManager)
        {
            levelManager.ApplyDifficultyProfile(staticProfile);

            string profileName = staticProfile != null ? staticProfile.ProfileId : "default player settings";
            Debug.Log("[Synaptrace] Difficulty mode: Static. Profile: " + profileName);
        }

        private void ApplyPlaceholderMode(LevelManager levelManager, string modeName)
        {
            levelManager.ApplyDifficultyProfile(staticProfile);
            Debug.Log("[Synaptrace] Difficulty mode placeholder active: " + modeName + ". Static profile is used until an adapter is implemented.");
        }
    }
}
