using System.Collections;
using Synaptrace.Adaptation;
using Synaptrace.Player;
using Synaptrace.Telemetry;
using UnityEngine;

namespace Synaptrace.Core
{
    public sealed class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private string levelId = "Prototype-01";
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;
        [SerializeField] private float deathRestartDelay = 0.75f;

        private GameManager gameManager;
        private TelemetryTracker telemetryTracker;
        private DifficultyManager difficultyManager;
        private Coroutine restartRoutine;
        private bool levelActive;
        private bool levelCompleted;

        public string LevelId => levelId;
        public bool LevelCompleted => levelCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            CacheSceneReferences();
            StartLevel();
        }

        public void StartLevel()
        {
            CacheSceneReferences();

            if (gameManager != null)
            {
                gameManager.RegisterLevelStarted(levelId);
            }

            if (difficultyManager != null)
            {
                difficultyManager.ConfigureLevel(this, telemetryTracker);
            }

            MovePlayerToSpawn();
            levelActive = true;
            levelCompleted = false;
        }

        public void RegisterPlayerDeath(string sourceName, bool countsAsHazardHit)
        {
            if (!levelActive || levelCompleted)
            {
                return;
            }

            levelActive = false;

            if (player != null)
            {
                player.SetControlsEnabled(false);
            }

            if (gameManager != null)
            {
                gameManager.RegisterPlayerDeath(sourceName, countsAsHazardHit);
            }

            if (restartRoutine != null)
            {
                StopCoroutine(restartRoutine);
            }

            restartRoutine = StartCoroutine(RestartAfterDelay(sourceName));
        }

        public void RestartLevelFromInput()
        {
            if (levelCompleted)
            {
                StartLevel();
                return;
            }

            RestartLevel("manual input");
        }

        public void RestartLevel(string reason)
        {
            CacheSceneReferences();

            if (restartRoutine != null)
            {
                StopCoroutine(restartRoutine);
                restartRoutine = null;
            }

            MovePlayerToSpawn();
            levelActive = true;
            levelCompleted = false;

            if (gameManager != null)
            {
                gameManager.RegisterRetry(reason);
            }
        }

        public void CompleteLevel()
        {
            if (!levelActive || levelCompleted)
            {
                return;
            }

            levelActive = false;
            levelCompleted = true;

            if (player != null)
            {
                player.SetControlsEnabled(false);
            }

            if (gameManager != null)
            {
                gameManager.RegisterLevelCompleted();
            }
        }

        public void ApplyDifficultyProfile(DifficultyProfile profile)
        {
            CacheSceneReferences();

            if (player != null)
            {
                player.ApplyDifficultyProfile(profile);
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(PlayerController editorPlayer, PlayerSpawnPoint editorSpawnPoint)
        {
            player = editorPlayer;
            playerSpawnPoint = editorSpawnPoint;
        }
#endif

        private IEnumerator RestartAfterDelay(string sourceName)
        {
            yield return new WaitForSeconds(deathRestartDelay);
            RestartLevel("automatic after " + sourceName);
        }

        private void MovePlayerToSpawn()
        {
            if (player == null || playerSpawnPoint == null)
            {
                Debug.LogWarning("[Synaptrace] LevelManager is missing a player or spawn point.");
                return;
            }

            player.SetTelemetry(telemetryTracker);
            player.RespawnAt(playerSpawnPoint.transform.position);
        }

        private void CacheSceneReferences()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            if (telemetryTracker == null)
            {
                telemetryTracker = FindFirstObjectByType<TelemetryTracker>();
            }

            if (difficultyManager == null)
            {
                difficultyManager = FindFirstObjectByType<DifficultyManager>();
            }

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (playerSpawnPoint == null)
            {
                playerSpawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();
            }
        }
    }
}
