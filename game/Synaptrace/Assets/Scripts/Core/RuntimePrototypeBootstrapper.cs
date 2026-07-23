using Synaptrace.Adaptation;
using Synaptrace.Enemies;
using Synaptrace.Player;
using Synaptrace.Systems;
using Synaptrace.Telemetry;
using Synaptrace.UI;
using UnityEngine;

namespace Synaptrace.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class RuntimePrototypeBootstrapper : MonoBehaviour
    {
        private const float ShaftWallThickness = 0.5f;
        private static readonly Vector2 PlayerColliderSize = new Vector2(0.78f, 1.18f);

        [SerializeField] private bool buildOnAwake = true;

        private Sprite squareSprite;
        private Sprite circleSprite;
        private Sprite softCircleSprite;
        private Sprite triangleSprite;
        private Sprite trapezoidSprite;
        private PhysicsMaterial2D noFrictionMaterial;
        private Transform levelRoot;
        private Transform environmentRoot;
        private Transform platformRoot;
        private Transform hazardRoot;
        private Transform enemyRoot;
        private Transform goalRoot;

        private readonly struct PlatformSpec
        {
            public PlatformSpec(string name, float x, float y, float width, float height)
            {
                Name = name;
                Position = new Vector2(x, y);
                Size = new Vector2(width, height);
            }

            public string Name { get; }
            public Vector2 Position { get; }
            public Vector2 Size { get; }
        }

        private void Awake()
        {
            if (!buildOnAwake)
            {
                return;
            }

            EnsureGameSystems();

            if (FindFirstObjectByType<PlayerController>() != null)
            {
                return;
            }

            BuildPrototypeLevel();
        }

        private void EnsureGameSystems()
        {
            GetOrAdd<TelemetryTracker>(gameObject);
            GetOrAdd<DifficultyManager>(gameObject);
            GetOrAdd<GameManager>(gameObject);
            GetOrAdd<LevelManager>(gameObject);
            GetOrAdd<PrototypeHUD>(gameObject);
        }

        private void BuildPrototypeLevel()
        {
            CreateRuntimeSprites();
            CreateLevelContainers();

            Vector3 spawnPosition = new Vector3(-17.2f, -1.42f, 0f);
            PlayerController player = CreatePlayer(spawnPosition);

            CreateSpawnPoint(player.transform.position);
            CreateBackground();
            CreateStartBeacon(player.transform.position);
            CreatePlatformLayout();
            CreateEnemies(player);
            CreateGoal();
            ConfigureCamera(player.transform);
        }

        private void CreateRuntimeSprites()
        {
            squareSprite = CreateFilledSprite("Synaptrace Square Sprite", 16);
            circleSprite = CreateCircleSprite("Synaptrace Circle Sprite", 32, false);
            softCircleSprite = CreateCircleSprite("Synaptrace Soft Glow Sprite", 64, true);
            triangleSprite = CreateTriangleSprite("Synaptrace Spike Sprite", 32);
            trapezoidSprite = CreateTrapezoidSprite("Synaptrace Tapered Armour Sprite", 32);
            noFrictionMaterial = CreateNoFrictionMaterial();
        }

        private void CreateLevelContainers()
        {
            levelRoot = CreateContainer("Prototype Level - Expanded Route", null);
            environmentRoot = CreateContainer("Background - Simulation Grid", levelRoot);
            platformRoot = CreateContainer("Gameplay Sections - Adaptive Ready", levelRoot);
            hazardRoot = CreateContainer("Hazards", levelRoot);
            enemyRoot = CreateContainer("Enemies - Adaptive Ready", levelRoot);
            goalRoot = CreateContainer("Goal", levelRoot);
        }

        private PlayerController CreatePlayer(Vector3 position)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.position = position;
            playerObject.transform.SetParent(levelRoot, true);

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = PlayerColliderSize;
            collider.sharedMaterial = noFrictionMaterial;

            PlayerController controller = playerObject.AddComponent<PlayerController>();
            int groundLayer = GetLayerOrDefault("Ground");
            controller.SetGroundLayers(1 << groundLayer);

            PlayerVisualFactory.Create(playerObject.transform, controller);
            return controller;
        }

        private GameObject CreatePlayerVisual(Transform parent)
        {
            GameObject visualRoot = new GameObject("Visual Root - Tech Knight");
            visualRoot.transform.SetParent(parent, false);

            Color darkSteel = new Color(0.08f, 0.11f, 0.16f, 1f);
            Color armourSteel = new Color(0.42f, 0.5f, 0.58f, 1f);
            Color brightSteel = new Color(0.72f, 0.8f, 0.84f, 1f);
            Color capeColor = new Color(0.28f, 0.025f, 0.12f, 1f);
            Color energyColor = new Color(0.1f, 1f, 0.82f, 1f);

            CreateChildSprite(visualRoot.transform, "Phase Aura", softCircleSprite, new Vector3(0f, 0.02f, 0f), new Vector3(1.45f, 1.65f, 1f), new Color(0.1f, 0.95f, 1f, 0.08f), 8);

            Transform cape = CreateContainer("Crimson Tech Cape", visualRoot.transform);
            cape.localPosition = new Vector3(-0.13f, 0.13f, 0f);
            cape.localRotation = Quaternion.Euler(0f, 0f, 8f);
            CreateChildSprite(cape, "Cape Cloth", triangleSprite, new Vector3(-0.06f, -0.2f, 0f), new Vector3(0.38f, 0.72f, 1f), capeColor, 9);
            CreateChildSprite(cape, "Cape Circuit Trim", squareSprite, new Vector3(-0.19f, -0.2f, 0f), new Vector3(0.035f, 0.52f, 1f), new Color(0.08f, 0.72f, 0.68f, 0.8f), 10);

            Transform ponytail = CreateContainer("Knight Ponytail", visualRoot.transform);
            ponytail.localPosition = new Vector3(-0.17f, 0.46f, 0f);
            ponytail.localRotation = Quaternion.Euler(0f, 0f, 68f);
            CreateChildSprite(ponytail, "Ponytail Plume", triangleSprite, new Vector3(-0.04f, -0.06f, 0f), new Vector3(0.16f, 0.34f, 1f), new Color(0.34f, 0.035f, 0.15f, 1f), 10);

            CreateKnightLeg(visualRoot.transform, "Left", -0.095f, darkSteel, armourSteel, brightSteel, energyColor, 10);
            CreateKnightArm(visualRoot.transform, "Left", -0.215f, darkSteel, armourSteel, brightSteel, 10);

            CreateChildSprite(visualRoot.transform, "Knight Armour Body", trapezoidSprite, new Vector3(0f, -0.025f, 0f), new Vector3(0.32f, 0.42f, 1f), darkSteel, 11);
            CreateChildSprite(visualRoot.transform, "Knight Chest Plate", circleSprite, new Vector3(0.015f, 0.08f, 0f), new Vector3(0.36f, 0.23f, 1f), armourSteel, 12);
            CreateChildSprite(visualRoot.transform, "Chest Collar", squareSprite, new Vector3(0.015f, 0.18f, 0f), new Vector3(0.24f, 0.05f, 1f), brightSteel, 13);
            CreateChildSprite(visualRoot.transform, "Waist Guard", squareSprite, new Vector3(0f, -0.205f, 0f), new Vector3(0.22f, 0.095f, 1f), armourSteel, 12);
            CreateChildSprite(visualRoot.transform, "Hip Belt", squareSprite, new Vector3(0f, -0.25f, 0f), new Vector3(0.31f, 0.05f, 1f), brightSteel, 13);

            GameObject leftHipGuard = CreateChildSprite(visualRoot.transform, "Left Hip Guard", triangleSprite, new Vector3(-0.105f, -0.292f, 0f), new Vector3(0.15f, 0.165f, 1f), armourSteel, 12);
            leftHipGuard.transform.localRotation = Quaternion.Euler(0f, 0f, 172f);
            GameObject rightHipGuard = CreateChildSprite(visualRoot.transform, "Right Hip Guard", triangleSprite, new Vector3(0.105f, -0.292f, 0f), new Vector3(0.15f, 0.165f, 1f), armourSteel, 12);
            rightHipGuard.transform.localRotation = Quaternion.Euler(0f, 0f, 188f);

            CreateKnightLeg(visualRoot.transform, "Right", 0.095f, darkSteel, armourSteel, brightSteel, energyColor, 12);
            CreateKnightArm(visualRoot.transform, "Right", 0.215f, darkSteel, armourSteel, brightSteel, 13);

            CreateChildSprite(visualRoot.transform, "Left Shoulder Plate", circleSprite, new Vector3(-0.215f, 0.15f, 0f), new Vector3(0.16f, 0.13f, 1f), brightSteel, 13);
            CreateChildSprite(visualRoot.transform, "Right Shoulder Plate", circleSprite, new Vector3(0.215f, 0.15f, 0f), new Vector3(0.16f, 0.13f, 1f), brightSteel, 14);

            CreateChildSprite(visualRoot.transform, "Knight Helmet", circleSprite, new Vector3(0f, 0.405f, 0f), new Vector3(0.35f, 0.32f, 1f), armourSteel, 12);
            CreateChildSprite(visualRoot.transform, "Helmet Crown", squareSprite, new Vector3(-0.02f, 0.52f, 0f), new Vector3(0.27f, 0.08f, 1f), brightSteel, 13);
            CreateChildSprite(visualRoot.transform, "Helmet Jaw Guard", squareSprite, new Vector3(0.03f, 0.335f, 0f), new Vector3(0.255f, 0.065f, 1f), darkSteel, 13);
            CreateChildSprite(visualRoot.transform, "Neon Visor", squareSprite, new Vector3(0.045f, 0.41f, 0f), new Vector3(0.25f, 0.06f, 1f), energyColor, 14);

            GameObject core = CreateChildSprite(visualRoot.transform, "Chest Energy Core", squareSprite, new Vector3(0.025f, 0.08f, 0f), new Vector3(0.12f, 0.12f, 1f), new Color(0.82f, 1f, 0.94f, 1f), 14);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateChildSprite(visualRoot.transform, "Armour Circuit Line", squareSprite, new Vector3(0.025f, -0.105f, 0f), new Vector3(0.03f, 0.19f, 1f), energyColor, 14);

            return visualRoot;
        }

        private void CreateKnightArm(Transform parent, string side, float xPosition, Color darkSteel, Color armourSteel, Color brightSteel, int sortingOrder)
        {
            Transform upperArm = CreateContainer($"{side} Arm Rig", parent);
            upperArm.localPosition = new Vector3(xPosition, 0.125f, 0f);

            CreateChildSprite(upperArm, $"{side} Upper Arm", trapezoidSprite, new Vector3(0f, -0.085f, 0f), new Vector3(0.095f, 0.18f, 1f), darkSteel, sortingOrder);

            Transform forearm = CreateContainer($"{side} Forearm Rig", upperArm);
            forearm.localPosition = new Vector3(0f, -0.17f, 0f);
            CreateChildSprite(forearm, $"{side} Forearm Armour", trapezoidSprite, new Vector3(0f, -0.07f, 0f), new Vector3(0.105f, 0.15f, 1f), armourSteel, sortingOrder + 1);
            CreateChildSprite(forearm, $"{side} Gauntlet", squareSprite, new Vector3(0.015f, -0.158f, 0f), new Vector3(0.125f, 0.095f, 1f), brightSteel, sortingOrder + 2);
        }

        private void CreateKnightLeg(Transform parent, string side, float xPosition, Color darkSteel, Color armourSteel, Color brightSteel, Color energyColor, int sortingOrder)
        {
            Transform upperLeg = CreateContainer($"{side} Leg Rig", parent);
            upperLeg.localPosition = new Vector3(xPosition, -0.16f, 0f);

            CreateChildSprite(upperLeg, $"{side} Thigh Armour", trapezoidSprite, new Vector3(0f, -0.09f, 0f), new Vector3(0.12f, 0.2f, 1f), darkSteel, sortingOrder);

            Transform lowerLeg = CreateContainer($"{side} Lower Leg Rig", upperLeg);
            lowerLeg.localPosition = new Vector3(0f, -0.18f, 0f);
            CreateChildSprite(lowerLeg, $"{side} Shin Armour", trapezoidSprite, new Vector3(0f, -0.08f, 0f), new Vector3(0.11f, 0.17f, 1f), armourSteel, sortingOrder + 1);
            CreateChildSprite(lowerLeg, $"{side} Knee Rune", squareSprite, new Vector3(0f, 0.005f, 0f), new Vector3(0.1f, 0.032f, 1f), energyColor, sortingOrder + 3);
            CreateChildSprite(lowerLeg, $"{side} Armoured Boot", squareSprite, new Vector3(0.025f, -0.18f, 0f), new Vector3(0.17f, 0.1f, 1f), brightSteel, sortingOrder + 2);
        }

        private void CreateEnemies(PlayerController player)
        {
            GameObject enemyObject = new GameObject("Enemy 01 - Corrupted Sentinel");
            enemyObject.transform.position = new Vector3(-1.7f, -0.1f, 0f);
            enemyObject.transform.SetParent(enemyRoot, true);

            GameObject visualRoot = new GameObject("Corrupted Sentinel Visual");
            visualRoot.transform.SetParent(enemyObject.transform, false);

            CreateChildSprite(visualRoot.transform, "Sentinel Aura", softCircleSprite, Vector3.zero, new Vector3(1.4f, 1.4f, 1f), new Color(0.8f, 0.08f, 0.48f, 0.2f), 8);
            CreateChildSprite(visualRoot.transform, "Tattered Data Mantle", triangleSprite, new Vector3(-0.12f, -0.05f, 0f), new Vector3(0.62f, 0.82f, 1f), new Color(0.16f, 0.02f, 0.2f, 1f), 9);
            CreateChildSprite(visualRoot.transform, "Sentinel Armour", squareSprite, new Vector3(0f, -0.04f, 0f), new Vector3(0.56f, 0.58f, 1f), new Color(0.14f, 0.1f, 0.2f, 1f), 10);
            CreateChildSprite(visualRoot.transform, "Sentinel Helmet", squareSprite, new Vector3(0f, 0.31f, 0f), new Vector3(0.6f, 0.32f, 1f), new Color(0.38f, 0.3f, 0.46f, 1f), 11);

            GameObject leftHorn = CreateChildSprite(visualRoot.transform, "Left Rune Horn", triangleSprite, new Vector3(-0.22f, 0.54f, 0f), new Vector3(0.16f, 0.3f, 1f), new Color(0.48f, 0.32f, 0.56f, 1f), 10);
            leftHorn.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            GameObject rightHorn = CreateChildSprite(visualRoot.transform, "Right Rune Horn", triangleSprite, new Vector3(0.22f, 0.54f, 0f), new Vector3(0.16f, 0.3f, 1f), new Color(0.48f, 0.32f, 0.56f, 1f), 10);
            rightHorn.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

            CreateChildSprite(visualRoot.transform, "Rush Visor", squareSprite, new Vector3(0.08f, 0.32f, 0f), new Vector3(0.36f, 0.08f, 1f), new Color(1f, 0.08f, 0.24f, 1f), 13);
            GameObject enemyCore = CreateChildSprite(visualRoot.transform, "Corruption Core", squareSprite, new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.18f, 1f), new Color(1f, 0.12f, 0.62f, 1f), 13);
            enemyCore.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateChildSprite(visualRoot.transform, "Left Sentinel Foot", squareSprite, new Vector3(-0.17f, -0.46f, 0f), new Vector3(0.2f, 0.16f, 1f), new Color(0.3f, 0.22f, 0.38f, 1f), 11);
            CreateChildSprite(visualRoot.transform, "Right Sentinel Foot", squareSprite, new Vector3(0.17f, -0.46f, 0f), new Vector3(0.2f, 0.16f, 1f), new Color(0.3f, 0.22f, 0.38f, 1f), 11);

            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.9f);
            collider.isTrigger = true;

            RushEnemyController enemyController = enemyObject.AddComponent<RushEnemyController>();
            enemyController.Configure(player, visualRoot.transform);
        }

        private void CreateSpawnPoint(Vector3 position)
        {
            GameObject spawnObject = new GameObject("Start Spawn");
            spawnObject.transform.position = position;
            spawnObject.transform.SetParent(levelRoot, true);
            spawnObject.AddComponent<PlayerSpawnPoint>();
        }

        private void CreatePlatformLayout()
        {
            int groundLayer = GetLayerOrDefault("Ground");
            int hazardLayer = GetLayerOrDefault("Hazard");
            Transform introSection = CreateContainer("01 Intro Section", platformRoot);
            Transform jumpSection = CreateContainer("02 Basic Jump Section", platformRoot);
            Transform firstHazardSection = CreateContainer("03 First Hazard Introduction", platformRoot);
            Transform wallJumpSection = CreateContainer("04 Wall Jump Tutorial Shaft", platformRoot);
            Transform mixedSection = CreateContainer("05 Mixed Platform Hazard Section", platformRoot);
            Transform optionalSection = CreateContainer("06 Optional Upper Route", platformRoot);
            Transform finalClimbSection = CreateContainer("07 Final Vertical Climb", platformRoot);
            Transform goalSection = CreateContainer("08 Elevated Finish Area", platformRoot);

            BuildIntroSection(groundLayer, introSection);
            BuildBasicJumpSection(groundLayer, jumpSection);
            BuildFirstHazardSection(groundLayer, hazardLayer, firstHazardSection);
            BuildWallJumpSection(groundLayer, wallJumpSection);
            BuildMixedPlatformHazardSection(groundLayer, hazardLayer, mixedSection);
            BuildOptionalUpperRoute(groundLayer, optionalSection);
            BuildFinalClimbSection(groundLayer, finalClimbSection);
            BuildGoalSection(groundLayer, goalSection);
            CreateFallResetZone(hazardLayer);
        }

        private void BuildIntroSection(int groundLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Intro Runway", -15.6f, -2.3f, 6.6f, 0.55f),
                new PlatformSpec("Intro Step", -10.9f, -1.85f, 2.4f, 0.45f));
        }

        private void BuildBasicJumpSection(int groundLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Basic Jump Pad 01", -7.9f, -1.15f, 2.1f, 0.45f),
                new PlatformSpec("Basic Jump Pad 02", -5.0f, -0.55f, 2.0f, 0.45f),
                new PlatformSpec("Basic Jump Recovery Deck", -1.7f, -0.85f, 3.0f, 0.5f));
        }

        private void BuildFirstHazardSection(int groundLayer, int hazardLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("First Hazard Approach Deck", 1.6f, -1.3f, 2.8f, 0.5f),
                new PlatformSpec("First Hazard Exit Deck", 5.7f, -0.75f, 2.7f, 0.45f),
                new PlatformSpec("Post Hazard Reset Deck", 8.7f, -1.1f, 2.7f, 0.5f));

            CreateSpikeHazard("Spike Field - First Hazard Introduction", new Vector3(3.75f, -1.02f, 0f), new Vector2(1.0f, 0.55f), hazardLayer, true);
        }

        private void BuildWallJumpSection(int groundLayer, Transform parent)
        {
            const float leftWallX = 10.55f;
            const float innerWidth = 2.3f;
            float rightWallX = leftWallX + ShaftWallThickness + innerWidth;

            // The raised left wall leaves 1.55 units above the safe floor, so the
            // 1.18-unit player can walk into the shaft before beginning wall jumps.
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Tutorial Shaft Entry and Safe Floor", 12.1f, -1.125f, 4.1f, 0.55f),
                new PlatformSpec("Tutorial Shaft Raised Entrance Wall", leftWallX, 2.35f, ShaftWallThickness, 3.3f),
                new PlatformSpec("Tutorial Shaft Opposing Wall", rightWallX, 1.6f, ShaftWallThickness, 4.9f),
                new PlatformSpec("Tutorial Shaft Lower Catch Ledge", 11.35f, 0.95f, 1.1f, 0.35f),
                new PlatformSpec("Tutorial Shaft Exit Ledge", 14.65f, 3.75f, 2.1f, 0.45f),
                new PlatformSpec("Post Tutorial Main Route Deck", 17.4f, 2.8f, 2.4f, 0.45f));
        }

        private void BuildMixedPlatformHazardSection(int groundLayer, int hazardLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Mixed Route Drop Deck", 20.1f, 2.55f, 2.6f, 0.45f),
                new PlatformSpec("Hazard Bridge Left Deck", 23.0f, 2.25f, 2.0f, 0.45f),
                new PlatformSpec("Hazard Bridge Right Deck", 26.5f, 2.95f, 2.4f, 0.45f));

            CreateSpikeHazard("Spike Field - Mixed Route Gap", new Vector3(24.65f, 2.42f, 0f), new Vector2(1.1f, 0.55f), hazardLayer, true);
        }

        private void BuildOptionalUpperRoute(int groundLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Optional Route Entry Ledge", 16.9f, 5.05f, 1.5f, 0.4f),
                new PlatformSpec("Optional Route Crest", 19.3f, 5.65f, 1.7f, 0.4f),
                new PlatformSpec("Optional Route High Bridge", 22.2f, 5.1f, 2.4f, 0.4f),
                new PlatformSpec("Optional Route Rejoin", 25.3f, 4.15f, 2.4f, 0.4f));
        }

        private void BuildFinalClimbSection(int groundLayer, Transform parent)
        {
            const float leftWallX = 31.1f;
            const float innerWidth = 2.0f;
            float rightWallX = leftWallX + ShaftWallThickness + innerWidth;

            // This repeats the tutorial pattern with a narrower shaft and a smaller
            // recovery ledge, but preserves a clear 1.825-unit walk-under entrance.
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Final Climb Entry Deck", 29.0f, 3.1f, 2.6f, 0.5f),
                new PlatformSpec("Final Shaft Safe Floor", 32.05f, 2.6f, 4.5f, 0.55f),
                new PlatformSpec("Final Shaft Raised Entrance Wall", leftWallX, 5.975f, ShaftWallThickness, 2.55f),
                new PlatformSpec("Final Shaft Opposing Wall", rightWallX, 5.1f, ShaftWallThickness, 4.45f),
                new PlatformSpec("Final Shaft Recovery Ledge", 31.8f, 5.1f, 0.9f, 0.35f),
                new PlatformSpec("Final Shaft Exit to Goal Ledge", 34.55f, 6.65f, 1.4f, 0.45f));
        }

        private void BuildGoalSection(int groundLayer, Transform parent)
        {
            CreatePlatforms(groundLayer, parent,
                new PlatformSpec("Elevated Goal Platform", 36.2f, 6.65f, 3.8f, 0.55f));
        }

        private void CreateFallResetZone(int hazardLayer)
        {
            GameObject killPlane = CreateTriggerVolume("Fall Reset Zone", new Vector3(8.7f, -6.25f, 0f), new Vector2(64f, 0.7f), hazardLayer);
            killPlane.transform.SetParent(hazardRoot, true);
            killPlane.AddComponent<Hazard>().Configure("Fall Reset Zone", false);
        }

        private void CreateGoal()
        {
            int goalLayer = GetLayerOrDefault("Goal");
            GameObject goal = CreateTriggerVolume("Finish Portal - Elevated Exit", new Vector3(36.9f, 7.75f, 0f), new Vector2(0.9f, 2.2f), goalLayer);
            goal.transform.SetParent(goalRoot, true);
            goal.AddComponent<GoalZone>();

            CreateChildSprite(goal.transform, "Portal Aura", softCircleSprite, Vector3.zero, new Vector3(2.4f, 2.4f, 1f), new Color(0.0f, 0.9f, 1f, 0.18f), 5);
            CreateChildSprite(goal.transform, "Outer Ring", circleSprite, Vector3.zero, new Vector3(1.25f, 1.25f, 1f), new Color(0.1f, 1f, 0.82f, 0.9f), 6);
            CreateChildSprite(goal.transform, "Inner Gate Void", circleSprite, Vector3.zero, new Vector3(0.86f, 0.86f, 1f), new Color(0.04f, 0.08f, 0.13f, 1f), 7);
            CreateChildSprite(goal.transform, "Data Core", squareSprite, Vector3.zero, new Vector3(0.42f, 0.42f, 1f), new Color(1f, 0.92f, 0.32f, 1f), 8).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateChildSprite(goal.transform, "Left Gate Pylon", squareSprite, new Vector3(-0.62f, 0f, 0f), new Vector3(0.14f, 2.3f, 1f), new Color(0.08f, 0.44f, 0.55f, 1f), 7);
            CreateChildSprite(goal.transform, "Right Gate Pylon", squareSprite, new Vector3(0.62f, 0f, 0f), new Vector3(0.14f, 2.3f, 1f), new Color(0.08f, 0.44f, 0.55f, 1f), 7);
            CreateChildSprite(goal.transform, "Gate Top Pulse", squareSprite, new Vector3(0f, 1.12f, 0f), new Vector3(1.18f, 0.08f, 1f), new Color(0.8f, 1f, 0.38f, 1f), 9);
        }

        private GameObject CreateStyledPlatform(string name, Vector3 position, Vector2 size, int layer, Transform parent)
        {
            GameObject platform = new GameObject(name);
            platform.layer = layer;
            platform.transform.position = position;
            platform.transform.SetParent(parent, true);

            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.sharedMaterial = noFrictionMaterial;
            platform.AddComponent<SurfaceModifier>();

            CreateChildSprite(platform.transform, "Panel Base", squareSprite, Vector3.zero, new Vector3(size.x, size.y, 1f), new Color(0.11f, 0.16f, 0.2f, 1f), 0);
            CreateChildSprite(platform.transform, "Upper Walkable Edge", squareSprite, new Vector3(0f, size.y * 0.5f - 0.055f, 0f), new Vector3(size.x, 0.09f, 1f), new Color(0.38f, 0.94f, 1f, 1f), 2);
            CreateChildSprite(platform.transform, "Lower Shadow", squareSprite, new Vector3(0f, -size.y * 0.5f + 0.05f, 0f), new Vector3(size.x, 0.1f, 1f), new Color(0.02f, 0.04f, 0.07f, 1f), 1);
            CreateChildSprite(platform.transform, "Left Edge Glow", squareSprite, new Vector3(-size.x * 0.5f + 0.035f, 0f, 0f), new Vector3(0.07f, size.y, 1f), new Color(0.0f, 0.75f, 0.9f, 0.85f), 3);
            CreateChildSprite(platform.transform, "Right Edge Glow", squareSprite, new Vector3(size.x * 0.5f - 0.035f, 0f, 0f), new Vector3(0.07f, size.y, 1f), new Color(0.0f, 0.75f, 0.9f, 0.85f), 3);

            if (size.x >= 1.5f)
            {
                GameObject rune = CreateChildSprite(platform.transform, "Gothic Tech Rune", squareSprite, new Vector3(0f, -0.02f, 0f), new Vector3(0.1f, 0.1f, 1f), new Color(0.72f, 0.2f, 0.88f, 0.55f), 3);
                rune.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                CreateChildSprite(platform.transform, "Rune Circuit Bar", squareSprite, new Vector3(0f, -0.02f, 0f), new Vector3(0.38f, 0.025f, 1f), new Color(0.18f, 0.72f, 0.82f, 0.35f), 3);
            }

            int panelCount = Mathf.Max(1, Mathf.RoundToInt(size.x));

            for (int i = 1; i < panelCount; i++)
            {
                float x = -size.x * 0.5f + (size.x / panelCount) * i;
                CreateChildSprite(platform.transform, "Panel Seam " + i, squareSprite, new Vector3(x, -0.02f, 0f), new Vector3(0.025f, size.y * 0.72f, 1f), new Color(0.4f, 0.82f, 1f, 0.22f), 3);
            }

            return platform;
        }

        private void CreatePlatforms(int layer, Transform parent, params PlatformSpec[] platformSpecs)
        {
            for (int i = 0; i < platformSpecs.Length; i++)
            {
                PlatformSpec platformSpec = platformSpecs[i];
                Vector3 position = new Vector3(platformSpec.Position.x, platformSpec.Position.y, 0f);
                CreateStyledPlatform(platformSpec.Name, position, platformSpec.Size, layer, parent);
            }
        }

        private void CreateSpikeHazard(string name, Vector3 position, Vector2 size, int layer, bool countsAsHazardHit)
        {
            GameObject hazard = CreateTriggerVolume(name, position, size, layer);
            hazard.transform.SetParent(hazardRoot, true);
            hazard.AddComponent<Hazard>().Configure(name, countsAsHazardHit);

            CreateChildSprite(hazard.transform, "Danger Field Glow", softCircleSprite, Vector3.zero, new Vector3(size.x * 1.25f, size.y * 1.6f, 1f), new Color(1f, 0.05f, 0.18f, 0.2f), 4);
            CreateChildSprite(hazard.transform, "Energy Base", squareSprite, new Vector3(0f, -size.y * 0.34f, 0f), new Vector3(size.x, size.y * 0.28f, 1f), new Color(0.55f, 0.02f, 0.08f, 1f), 5);
            CreateChildSprite(hazard.transform, "Warning Strip", squareSprite, new Vector3(0f, -size.y * 0.06f, 0f), new Vector3(size.x, 0.055f, 1f), new Color(1f, 0.26f, 0.28f, 1f), 7);

            int spikeCount = Mathf.Max(1, Mathf.RoundToInt(size.x / 0.32f));

            for (int i = 0; i < spikeCount; i++)
            {
                float t = spikeCount == 1 ? 0.5f : (float)i / (spikeCount - 1);
                float x = Mathf.Lerp(-size.x * 0.42f, size.x * 0.42f, t);
                CreateChildSprite(hazard.transform, "Energy Spike " + (i + 1), triangleSprite, new Vector3(x, size.y * 0.12f, 0f), new Vector3(0.36f, size.y * 1.05f, 1f), new Color(1f, 0.1f, 0.16f, 1f), 8);
            }
        }

        private GameObject CreateTriggerVolume(string name, Vector3 position, Vector2 size, int layer)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.layer = layer;
            triggerObject.transform.position = position;

            BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;

            return triggerObject;
        }

        private void CreateBackground()
        {
            CreateChildSprite(environmentRoot, "Deep Simulation Backdrop", squareSprite, new Vector3(8.5f, 1.6f, 2f), new Vector3(64f, 20f, 1f), new Color(0.025f, 0.04f, 0.07f, 1f), -40);
            CreateChildSprite(environmentRoot, "Lower Data Horizon Band", squareSprite, new Vector3(8.5f, -2.35f, 1f), new Vector3(64f, 1.1f, 1f), new Color(0.04f, 0.09f, 0.13f, 1f), -35);
            CreateChildSprite(environmentRoot, "Upper Climb Signal Band", squareSprite, new Vector3(26f, 5.75f, 1f), new Vector3(26f, 0.9f, 1f), new Color(0.03f, 0.12f, 0.15f, 0.55f), -35);

            for (int i = 0; i <= 43; i++)
            {
                float x = -22f + i * 1.5f;
                CreateChildSprite(environmentRoot, "Vertical Grid Line " + i, squareSprite, new Vector3(x, 1.6f, 1f), new Vector3(0.018f, 17.5f, 1f), new Color(0.18f, 0.75f, 1f, 0.07f), -32);
            }

            for (int i = 0; i <= 20; i++)
            {
                float y = -6f + i * 0.85f;
                CreateChildSprite(environmentRoot, "Horizontal Grid Line " + i, squareSprite, new Vector3(8.5f, y, 1f), new Vector3(64f, 0.014f, 1f), new Color(0.18f, 0.75f, 1f, 0.055f), -32);
            }

            CreateBackgroundPanel("Distant Panel A", new Vector3(-16.8f, 1.1f, 1f), new Vector2(2.0f, 2.3f));
            CreateBackgroundPanel("Distant Panel B", new Vector3(-8.6f, 2.15f, 1f), new Vector2(2.5f, 1.6f));
            CreateBackgroundPanel("Distant Panel C", new Vector3(2.5f, 1.7f, 1f), new Vector2(1.7f, 2.8f));
            CreateBackgroundPanel("Distant Panel D", new Vector3(11.8f, 2.7f, 1f), new Vector2(2.2f, 2.1f));
            CreateBackgroundPanel("Distant Panel E", new Vector3(20.7f, 4.2f, 1f), new Vector2(2.8f, 1.5f));
            CreateBackgroundPanel("Distant Panel F", new Vector3(29.8f, 6.4f, 1f), new Vector2(2.0f, 2.7f));
            CreateBackgroundPanel("Distant Panel G", new Vector3(36.2f, 7.6f, 1f), new Vector2(2.8f, 1.9f));

            CreateGothicTower("Gothic Data Tower A", new Vector3(-11.5f, 0.1f, 1f), new Vector2(2.2f, 4.2f));
            CreateGothicTower("Gothic Data Tower B", new Vector3(5.0f, 0.75f, 1f), new Vector2(1.8f, 4.8f));
            CreateGothicTower("Gothic Data Tower C", new Vector3(17.0f, 2.0f, 1f), new Vector2(2.0f, 5.4f));
            CreateGothicTower("Gothic Data Tower D", new Vector3(30.5f, 3.8f, 1f), new Vector2(2.3f, 5.8f));

            CreateChildSprite(environmentRoot, "Background Node A", circleSprite, new Vector3(-13.6f, 2.15f, 1f), new Vector3(0.18f, 0.18f, 1f), new Color(0.2f, 1f, 0.8f, 0.24f), -29);
            CreateChildSprite(environmentRoot, "Background Node B", circleSprite, new Vector3(-2.2f, 2.75f, 1f), new Vector3(0.14f, 0.14f, 1f), new Color(0.95f, 0.88f, 0.28f, 0.2f), -29);
            CreateChildSprite(environmentRoot, "Background Node C", circleSprite, new Vector3(8.8f, 1.45f, 1f), new Vector3(0.16f, 0.16f, 1f), new Color(0.2f, 1f, 0.8f, 0.22f), -29);
            CreateChildSprite(environmentRoot, "Background Node D", circleSprite, new Vector3(14.1f, 4.4f, 1f), new Vector3(0.14f, 0.14f, 1f), new Color(0.95f, 0.88f, 0.28f, 0.18f), -29);
            CreateChildSprite(environmentRoot, "Background Node E", circleSprite, new Vector3(24.8f, 5.85f, 1f), new Vector3(0.18f, 0.18f, 1f), new Color(0.2f, 1f, 0.8f, 0.22f), -29);
            CreateChildSprite(environmentRoot, "Background Node F", circleSprite, new Vector3(34.8f, 7.9f, 1f), new Vector3(0.22f, 0.22f, 1f), new Color(0.95f, 0.88f, 0.28f, 0.26f), -29);
        }

        private void CreateBackgroundPanel(string name, Vector3 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.position = position;
            panel.transform.SetParent(environmentRoot, true);

            CreateChildSprite(panel.transform, "Panel Fill", squareSprite, Vector3.zero, new Vector3(size.x, size.y, 1f), new Color(0.08f, 0.14f, 0.18f, 0.38f), -31);
            CreateChildSprite(panel.transform, "Panel Top Edge", squareSprite, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, 0.025f, 1f), new Color(0.3f, 0.9f, 1f, 0.18f), -30);
            CreateChildSprite(panel.transform, "Panel Side Edge", squareSprite, new Vector3(-size.x * 0.5f, 0f, 0f), new Vector3(0.025f, size.y, 1f), new Color(0.3f, 0.9f, 1f, 0.13f), -30);
        }

        private void CreateGothicTower(string name, Vector3 position, Vector2 size)
        {
            GameObject tower = new GameObject(name);
            tower.transform.position = position;
            tower.transform.SetParent(environmentRoot, true);

            Color silhouette = new Color(0.035f, 0.045f, 0.075f, 0.92f);
            Color roofColor = new Color(0.09f, 0.025f, 0.1f, 0.82f);
            CreateChildSprite(tower.transform, "Tower Silhouette", squareSprite, Vector3.zero, new Vector3(size.x, size.y, 1f), silhouette, -31);
            CreateChildSprite(tower.transform, "Left Battlement", squareSprite, new Vector3(-size.x * 0.34f, size.y * 0.54f, 0f), new Vector3(size.x * 0.2f, size.y * 0.16f, 1f), silhouette, -31);
            CreateChildSprite(tower.transform, "Right Battlement", squareSprite, new Vector3(size.x * 0.34f, size.y * 0.54f, 0f), new Vector3(size.x * 0.2f, size.y * 0.16f, 1f), silhouette, -31);
            CreateChildSprite(tower.transform, "Tower Spire", triangleSprite, new Vector3(0f, size.y * 0.7f, 0f), new Vector3(size.x * 0.72f, size.y * 0.45f, 1f), roofColor, -30);
            CreateChildSprite(tower.transform, "Rune Window", squareSprite, new Vector3(0f, size.y * 0.15f, 0f), new Vector3(size.x * 0.18f, size.y * 0.2f, 1f), new Color(0.35f, 0.1f, 0.48f, 0.3f), -29);
            CreateChildSprite(tower.transform, "Tower Circuit", squareSprite, new Vector3(0f, -size.y * 0.12f, 0f), new Vector3(0.035f, size.y * 0.45f, 1f), new Color(0.12f, 0.72f, 0.78f, 0.16f), -29);
        }

        private void CreateStartBeacon(Vector3 spawnPosition)
        {
            GameObject beacon = new GameObject("Start Beacon");
            beacon.transform.position = spawnPosition + new Vector3(-0.55f, 0.05f, 0f);
            beacon.transform.SetParent(levelRoot, true);

            CreateChildSprite(beacon.transform, "Beacon Glow", softCircleSprite, new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 1.5f, 1f), new Color(0.1f, 1f, 0.52f, 0.16f), 3);
            CreateChildSprite(beacon.transform, "Beacon Mast", squareSprite, new Vector3(0f, 0.15f, 0f), new Vector3(0.12f, 1.3f, 1f), new Color(0.12f, 0.95f, 0.58f, 1f), 4);
            CreateChildSprite(beacon.transform, "Beacon Core", circleSprite, new Vector3(0f, 0.86f, 0f), new Vector3(0.32f, 0.32f, 1f), new Color(0.9f, 1f, 0.72f, 1f), 5);
            CreateChildSprite(beacon.transform, "Beacon Base", squareSprite, new Vector3(0f, -0.55f, 0f), new Vector3(0.62f, 0.12f, 1f), new Color(0.06f, 0.32f, 0.28f, 1f), 4);
        }

        private GameObject CreateChildSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return child;
        }

        private Transform CreateContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name);

            if (parent != null)
            {
                container.transform.SetParent(parent, false);
            }

            return container.transform;
        }

        private void ConfigureCamera(Transform target)
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(-14.7f, -0.2f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.07f, 1f);

            CameraFollow2D cameraFollow = GetOrAdd<CameraFollow2D>(camera.gameObject);
            cameraFollow.Configure(target, new Vector3(1.4f, 1.2f, -10f), 6.5f, new Vector2(-14.7f, -1.2f), new Vector2(31.6f, 5.9f));
        }

        private PhysicsMaterial2D CreateNoFrictionMaterial()
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D("Synaptrace No Friction Contact")
            {
                friction = 0f,
                bounciness = 0f,
                hideFlags = HideFlags.HideAndDontSave
            };

            return material;
        }

        private Sprite CreateFilledSprite(string spriteName, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Sprite CreateCircleSprite(string spriteName, int size, bool soft)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = ((float)x + 0.5f) / size * 2f - 1f;
                    float normalizedY = ((float)y + 0.5f) / size * 2f - 1f;
                    float distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    float alpha = soft ? Mathf.Clamp01(1f - Mathf.InverseLerp(0.2f, 1f, distance)) : (distance <= 0.92f ? 1f : 0f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Sprite CreateTriangleSprite(string spriteName, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = ((float)x + 0.5f) / size;
                    float normalizedY = ((float)y + 0.5f) / size;
                    float halfWidth = (1f - normalizedY) * 0.5f;
                    bool inside = Mathf.Abs(normalizedX - 0.5f) <= halfWidth && normalizedY <= 0.96f;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Sprite CreateTrapezoidSprite(string spriteName, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = ((float)x + 0.5f) / size;
                    float normalizedY = ((float)y + 0.5f) / size;
                    float halfWidth = Mathf.Lerp(0.31f, 0.48f, normalizedY);
                    bool inside = Mathf.Abs(normalizedX - 0.5f) <= halfWidth;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private int GetLayerOrDefault(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();

            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }
    }
}
