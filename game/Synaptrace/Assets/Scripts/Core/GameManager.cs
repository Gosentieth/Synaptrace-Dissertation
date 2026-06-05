using Synaptrace.Telemetry;
using Synaptrace.UI;
using UnityEngine;

namespace Synaptrace.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private TelemetryTracker telemetryTracker;
        private PrototypeHUD prototypeHUD;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CacheReferences();
        }

        public void RegisterLevelStarted(string levelId)
        {
            CacheReferences();

            if (telemetryTracker != null)
            {
                telemetryTracker.BeginLevel(levelId);
            }

            if (prototypeHUD != null)
            {
                prototypeHUD.SetStatus("Run started. Reach the finish zone.");
            }

            Debug.Log("[Synaptrace] Level started: " + levelId);
        }

        public void RegisterPlayerDeath(string sourceName, bool countsAsHazardHit)
        {
            CacheReferences();

            if (telemetryTracker != null)
            {
                if (countsAsHazardHit)
                {
                    telemetryTracker.RecordHazardHit(sourceName);
                }

                telemetryTracker.RecordDeath(sourceName);
            }

            if (prototypeHUD != null)
            {
                prototypeHUD.SetStatus("Failure: " + sourceName + ". Restarting...");
            }

            Debug.Log("[Synaptrace] Player died. Source: " + sourceName);
        }

        public void RegisterRetry(string reason)
        {
            CacheReferences();

            if (telemetryTracker != null)
            {
                telemetryTracker.RecordRetry(reason);
            }

            if (prototypeHUD != null)
            {
                prototypeHUD.SetStatus("Retry started.");
            }

            Debug.Log("[Synaptrace] Level restarted. Reason: " + reason);
        }

        public void RegisterLevelCompleted()
        {
            CacheReferences();

            if (telemetryTracker != null)
            {
                telemetryTracker.RecordCompletion();
                LevelTelemetrySnapshot snapshot = telemetryTracker.CreateSnapshot();
                Debug.Log("[Synaptrace] Level completed in " + snapshot.completionTime.ToString("0.00") + " seconds.");
                Debug.Log("[Synaptrace] Telemetry snapshot: " + telemetryTracker.ToJson());
            }

            if (prototypeHUD != null)
            {
                prototypeHUD.SetStatus("Level complete. Press R to run again.");
            }
        }

        public LevelTelemetrySnapshot GetTelemetrySnapshot()
        {
            CacheReferences();

            if (telemetryTracker == null)
            {
                return new LevelTelemetrySnapshot();
            }

            return telemetryTracker.CreateSnapshot();
        }

        private void CacheReferences()
        {
            if (telemetryTracker == null)
            {
                telemetryTracker = FindFirstObjectByType<TelemetryTracker>();
            }

            if (prototypeHUD == null)
            {
                prototypeHUD = FindFirstObjectByType<PrototypeHUD>();
            }
        }
    }
}
