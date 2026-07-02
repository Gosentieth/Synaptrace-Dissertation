using System;
using UnityEngine;

namespace Synaptrace.Telemetry
{
    [Serializable]
    public sealed class LevelTelemetrySnapshot
    {
        public string levelId;
        public int deathCount;
        public int retryCount;
        public int jumpCount;
        public int dodgeCount;
        public int hazardHitCount;
        public float completionTime;
        public float elapsedTime;
        public bool completed;
        public string lastEvent;
    }

    public sealed class TelemetryTracker : MonoBehaviour
    {
        private string currentLevelId = "Prototype-01";
        private float levelStartTime;
        private float completionTime;
        private int deathCount;
        private int retryCount;
        private int jumpCount;
        private int dodgeCount;
        private int hazardHitCount;
        private bool running;
        private bool completed;
        private string lastEvent = "Waiting to start";

        public void BeginLevel(string levelId)
        {
            currentLevelId = string.IsNullOrWhiteSpace(levelId) ? "UnnamedLevel" : levelId;
            levelStartTime = Time.time;
            completionTime = 0f;
            deathCount = 0;
            retryCount = 0;
            jumpCount = 0;
            dodgeCount = 0;
            hazardHitCount = 0;
            running = true;
            completed = false;
            lastEvent = "Level started";
        }

        public void RecordJump()
        {
            if (!running || completed)
            {
                return;
            }

            jumpCount++;
            lastEvent = "Jump";
        }

        public void RecordHazardHit(string sourceName)
        {
            if (!running || completed)
            {
                return;
            }

            hazardHitCount++;
            lastEvent = "Hazard hit: " + sourceName;
        }

        public void RecordDodge()
        {
            if (!running || completed)
            {
                return;
            }

            dodgeCount++;
            lastEvent = "Phase dodge";
        }

        public void RecordDeath(string sourceName)
        {
            if (!running || completed)
            {
                return;
            }

            deathCount++;
            lastEvent = "Death: " + sourceName;
        }

        public void RecordRetry(string reason)
        {
            if (!running || completed)
            {
                return;
            }

            retryCount++;
            lastEvent = "Retry: " + reason;
        }

        public void RecordCompletion()
        {
            if (!running || completed)
            {
                return;
            }

            completionTime = Time.time - levelStartTime;
            completed = true;
            running = false;
            lastEvent = "Level completed";
        }

        public LevelTelemetrySnapshot CreateSnapshot()
        {
            LevelTelemetrySnapshot snapshot = new LevelTelemetrySnapshot
            {
                levelId = currentLevelId,
                deathCount = deathCount,
                retryCount = retryCount,
                jumpCount = jumpCount,
                dodgeCount = dodgeCount,
                hazardHitCount = hazardHitCount,
                completionTime = completionTime,
                elapsedTime = GetElapsedTime(),
                completed = completed,
                lastEvent = lastEvent
            };

            return snapshot;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(CreateSnapshot(), true);
        }

        private float GetElapsedTime()
        {
            if (completed)
            {
                return completionTime;
            }

            if (!running)
            {
                return 0f;
            }

            return Time.time - levelStartTime;
        }
    }
}
