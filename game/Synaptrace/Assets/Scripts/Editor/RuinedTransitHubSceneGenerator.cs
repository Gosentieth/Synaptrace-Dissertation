using System.Collections.Generic;
using System.IO;
using Synaptrace.Adaptation;
using Synaptrace.Core;
using Synaptrace.Player;
using Synaptrace.Systems;
using Synaptrace.Telemetry;
using Synaptrace.UI;
using Synaptrace.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Synaptrace.EditorTools
{
    public static class RuinedTransitHubSceneGenerator
    {
        public const string ScenePath = "Assets/Scenes/RuinedTransitHub.unity";
        public const string RegionId = "ruined_transit_hub";
        public const string IntroZoneId = "hub_intro_calibration";
        public const string CentralZoneId = "hub_central_chamber";
        public const string WreckageRouteId = "route_hub_to_wreckage_basin";
        public const string SunkenRouteId = "route_hub_to_sunken_district";
        public const string FacilityRouteId = "route_hub_to_neural_facility";
        public const string MainDoorOpeningId = "opening_hub_main_door";
        public const string MandatoryJumpLinkId = "link_hub_intro_small_jump";
        public const string ProtagonistArtFolder = "Assets/Art/Player/Protagonist/Resources/Synaptrace/Player";
        public const string ProtagonistMainSpritePath = ProtagonistArtFolder + "/synaptrace-protagonist-main.png";
        public const string ProtagonistStaticClipPath = "Assets/Animations/Player/ProtagonistStatic.anim";
        public const string ProtagonistIdleClipPath = "Assets/Animations/Player/ProtagonistIdleBreathing.anim";
        public const string ProtagonistControllerPath = "Assets/Animations/Player/Resources/Synaptrace/Player/Protagonist.controller";
        public const float ProtagonistPixelsPerUnit = 160f;
        public const float HubCameraOrthographicSize = 3f;

        private static readonly string[] ProtagonistIdleSpritePaths =
        {
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-01.png",
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-02.png",
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-03.png",
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-04.png",
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-05.png",
            ProtagonistArtFolder + "/synaptrace-protagonist-idle-06.png"
        };

        public static readonly Vector2 PlayerColliderSize = new Vector2(0.78f, 1.18f);
        public static readonly Vector2 PlayerClearanceSize = new Vector2(0.98f, 1.38f);
        public const float FloorHeight = 0.55f;
        public const float BaselineFloorTop = 0f;
        public const float RaisedFloorTop = 0.325f;
        public const float SpawnCenterY = 0.605f;
        public const float MandatoryGap = 1.2f;

        private static Sprite editorSprite;
        private static Material editorUnlitMaterial;

        [MenuItem("Synaptrace/World/Generate Ruined Transit Hub")]
        public static void GenerateScene()
        {
            EnsureSceneCanBeGenerated();
            EnsureFolders();
            EnsureProjectLayers();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RuinedTransitHub";
            BuildSceneContents();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Synaptrace] Ruined Transit Hub greybox scene generated at " + ScenePath + ".");
        }

        public static void GenerateSceneBatch()
        {
            try
            {
                GenerateScene();
                RuinedTransitHubValidator.ValidateOpenSceneOrExit();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }
        }

        public static void BuildSceneContents()
        {
            EnsureProtagonistAssets();
            editorSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            editorUnlitMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            if (editorSprite == null || editorUnlitMaterial == null)
            {
                throw new System.InvalidOperationException("Could not load Unity's built-in unlit sprite resources.");
            }

            Transform root = CreateRoot("TransitHub_Root");
            AddStableId(root.gameObject, RegionId, WorldStableIdKind.Region);

            Transform systemsRoot = CreateContainer("Game Systems", null);
            CreateGameSystems(systemsRoot.gameObject);

            Transform geometryRoot = CreateContainer("THub_Geometry", root);
            Transform metadataRoot = CreateContainer("THub_Metadata", root);
            Transform decorationRoot = CreateContainer("THub_Decoration", root);
            Transform playerRoot = CreateContainer("THub_Player", root);
            Transform cameraRoot = CreateContainer("THub_Camera", root);

            BoxCollider2D spawnFloor = CreateGround(
                geometryRoot,
                "THub_Geo_SpawnCalibrationFloor",
                new Vector2(2.5f, -0.275f),
                new Vector2(7f, FloorHeight));
            BoxCollider2D doorFloor = CreateGround(
                geometryRoot,
                "THub_Geo_DoorPassageFloor",
                new Vector2(8.5f, -0.275f),
                new Vector2(5f, FloorHeight));
            BoxCollider2D takeoff = CreateGround(
                geometryRoot,
                "THub_Geo_SmallJumpTakeoff",
                new Vector2(11.9f, -0.275f),
                new Vector2(1.8f, FloorHeight));
            BoxCollider2D landing = CreateGround(
                geometryRoot,
                "THub_Geo_SmallJumpLanding",
                new Vector2(15.1f, 0.05f),
                new Vector2(2.2f, FloorHeight));
            BoxCollider2D chamberFloor = CreateGround(
                geometryRoot,
                "THub_Geo_ChamberFloor",
                new Vector2(20f, 0.05f),
                new Vector2(7.6f, FloorHeight));
            BoxCollider2D basinFloor = CreateGround(
                geometryRoot,
                "THub_Geo_BasinExitFloor",
                new Vector2(25.9f, 0.05f),
                new Vector2(4.2f, FloorHeight));

            BoxCollider2D doorHeader = CreateGround(
                geometryRoot,
                "THub_Geo_DoorOverheadHeader",
                new Vector2(8.5f, 1.875f),
                new Vector2(2.8f, 0.45f));
            TintRenderer(doorHeader.gameObject, new Color(0.3f, 0.32f, 0.34f, 1f));

            BoxCollider2D wreckageBlocker = CreateGround(
                geometryRoot,
                "THub_Geo_WreckageStubTerminalBlocker",
                new Vector2(28.25f, 1.225f),
                new Vector2(0.5f, 1.8f));
            TintRenderer(wreckageBlocker.gameObject, new Color(0.28f, 0.2f, 0.15f, 1f));

            BoxCollider2D sunkenBlocker = CreateGround(
                geometryRoot,
                "THub_Geo_SunkenDistrictLockedGate",
                new Vector2(21.95f, 1.225f),
                new Vector2(0.45f, 1.8f));
            TintRenderer(sunkenBlocker.gameObject, new Color(0.12f, 0.12f, 0.16f, 1f));

            BoxCollider2D facilityBlocker = CreateGround(
                geometryRoot,
                "THub_Geo_NeuralFacilitySealedBulkhead",
                new Vector2(18.05f, 1.225f),
                new Vector2(0.45f, 1.8f));
            TintRenderer(facilityBlocker.gameObject, new Color(0.16f, 0.08f, 0.14f, 1f));

            BoxCollider2D optionalBase = CreateGround(
                geometryRoot,
                "THub_Geo_OptionalLiftBase",
                new Vector2(17.4f, 0.725f),
                new Vector2(1.5f, 0.4f));
            BoxCollider2D optionalLedgeA = CreateGround(
                geometryRoot,
                "THub_Geo_OptionalLedgeA",
                new Vector2(19.6f, 1.325f),
                new Vector2(1.6f, 0.4f));
            BoxCollider2D optionalLedgeB = CreateGround(
                geometryRoot,
                "THub_Geo_OptionalLedgeB",
                new Vector2(21.8f, 1.725f),
                new Vector2(1.7f, 0.4f));
            BoxCollider2D optionalRejoin = CreateGround(
                geometryRoot,
                "THub_Geo_OptionalRejoinDrop",
                new Vector2(23.35f, 0.725f),
                new Vector2(1.5f, 0.4f));

            Color optionalFill = new Color(0.24f, 0.42f, 0.43f, 1f);
            Color optionalEdge = new Color(0.28f, 0.95f, 0.9f, 1f);
            TintGround(optionalBase, optionalFill, optionalEdge);
            TintGround(optionalLedgeA, optionalFill, optionalEdge);
            TintGround(optionalLedgeB, optionalFill, optionalEdge);
            TintGround(optionalRejoin, optionalFill, optionalEdge);
            TintGround(wreckageBlocker, new Color(0.5f, 0.35f, 0.16f, 1f), new Color(1f, 0.65f, 0.2f, 1f));
            TintGround(sunkenBlocker, new Color(0.42f, 0.19f, 0.21f, 1f), new Color(1f, 0.28f, 0.3f, 1f));
            TintGround(facilityBlocker, new Color(0.42f, 0.18f, 0.35f, 1f), new Color(0.95f, 0.2f, 0.62f, 1f));

            CreatePlayer(playerRoot, new Vector3(0f, SpawnCenterY, 0f));
            CreateSpawn(metadataRoot, new Vector3(0f, SpawnCenterY, 0f));
            CreateCamera(cameraRoot);

            CreateFallReset(metadataRoot);
            CreateZones(metadataRoot);
            CreateOpening(metadataRoot, doorFloor);
            CreateTraversalLinks(metadataRoot, chamberFloor, takeoff, landing, optionalBase, optionalLedgeA, optionalLedgeB, optionalRejoin);
            CreateRoutes(metadataRoot, wreckageBlocker, sunkenBlocker, facilityBlocker);
            CreateDecoration(decorationRoot);

            Selection.activeObject = root.gameObject;
        }

        private static void EnsureSceneCanBeGenerated()
        {
            if (!File.Exists(ToProjectAbsolutePath(ScenePath)))
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("TransitHub_Root");
            WorldStableId stableId = root != null ? root.GetComponent<WorldStableId>() : null;

            if (stableId == null || stableId.StableId != RegionId)
            {
                throw new System.InvalidOperationException(
                    "Refusing to overwrite " + ScenePath + " because it does not look like a generated Ruined Transit Hub scene.");
            }
        }

        private static void CreateGameSystems(GameObject systemsObject)
        {
            systemsObject.AddComponent<TelemetryTracker>();
            systemsObject.AddComponent<DifficultyManager>();
            systemsObject.AddComponent<GameManager>();
            LevelManager levelManager = systemsObject.AddComponent<LevelManager>();
            systemsObject.AddComponent<PrototypeHUD>();
            SetSerializedString(levelManager, "levelId", "RuinedTransitHub-01");
        }

        private static void CreatePlayer(Transform parent, Vector3 position)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(parent, true);
            playerObject.transform.position = position;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = PlayerColliderSize;

            PlayerController controller = playerObject.AddComponent<PlayerController>();
            controller.ConfigureForEditor(1 << LayerMask.NameToLayer("Ground"));

            GameObject visualRoot = PlayerVisualFactory.Create(playerObject.transform, controller);
            visualRoot.GetComponentInChildren<SpriteRenderer>().sharedMaterial = editorUnlitMaterial;
        }

        private static GameObject CreatePlayerVisual(Transform parent)
        {
            GameObject visualRoot = new GameObject("Visual Root - Tech Knight");
            visualRoot.transform.SetParent(parent, false);

            CreateSprite(visualRoot.transform, "Phase Aura", new Vector3(0f, 0.02f, 0f), new Vector3(1.45f, 1.65f, 1f), new Color(0.1f, 0.95f, 1f, 0.08f), 8);

            Transform cape = CreateContainer("Crimson Tech Cape", visualRoot.transform);
            cape.localPosition = new Vector3(-0.13f, 0.13f, 0f);
            CreateSprite(cape, "Cape Cloth", new Vector3(-0.06f, -0.2f, 0f), new Vector3(0.38f, 0.72f, 1f), new Color(0.28f, 0.025f, 0.12f, 1f), 9);

            Transform ponytail = CreateContainer("Knight Ponytail", visualRoot.transform);
            ponytail.localPosition = new Vector3(-0.17f, 0.46f, 0f);
            CreateSprite(ponytail, "Ponytail Plume", new Vector3(-0.04f, -0.06f, 0f), new Vector3(0.16f, 0.34f, 1f), new Color(0.34f, 0.035f, 0.15f, 1f), 10);

            CreateRiggedLeg(visualRoot.transform, "Left", -0.095f, 10);
            CreateRiggedArm(visualRoot.transform, "Left", -0.215f, 10);
            CreateSprite(visualRoot.transform, "Knight Armour Body", new Vector3(0f, -0.025f, 0f), new Vector3(0.32f, 0.42f, 1f), new Color(0.08f, 0.11f, 0.16f, 1f), 11);
            CreateSprite(visualRoot.transform, "Waist Guard", new Vector3(0f, -0.205f, 0f), new Vector3(0.22f, 0.095f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), 12);
            GameObject leftHip = CreateSprite(visualRoot.transform, "Left Hip Guard", new Vector3(-0.105f, -0.292f, 0f), new Vector3(0.15f, 0.165f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), 12);
            GameObject rightHip = CreateSprite(visualRoot.transform, "Right Hip Guard", new Vector3(0.105f, -0.292f, 0f), new Vector3(0.15f, 0.165f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), 12);
            leftHip.transform.localRotation = Quaternion.Euler(0f, 0f, 172f);
            rightHip.transform.localRotation = Quaternion.Euler(0f, 0f, 188f);
            CreateRiggedLeg(visualRoot.transform, "Right", 0.095f, 12);
            CreateRiggedArm(visualRoot.transform, "Right", 0.215f, 13);
            CreateSprite(visualRoot.transform, "Left Shoulder Plate", new Vector3(-0.215f, 0.15f, 0f), new Vector3(0.16f, 0.13f, 1f), new Color(0.72f, 0.8f, 0.84f, 1f), 13);
            CreateSprite(visualRoot.transform, "Right Shoulder Plate", new Vector3(0.215f, 0.15f, 0f), new Vector3(0.16f, 0.13f, 1f), new Color(0.72f, 0.8f, 0.84f, 1f), 14);
            CreateSprite(visualRoot.transform, "Knight Helmet", new Vector3(0f, 0.405f, 0f), new Vector3(0.35f, 0.32f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), 12);
            CreateSprite(visualRoot.transform, "Chest Energy Core", new Vector3(0.025f, 0.08f, 0f), new Vector3(0.12f, 0.12f, 1f), new Color(0.82f, 1f, 0.94f, 1f), 14);

            return visualRoot;
        }

        private static void CreateRiggedArm(Transform parent, string side, float xPosition, int order)
        {
            Transform upperArm = CreateContainer(side + " Arm Rig", parent);
            upperArm.localPosition = new Vector3(xPosition, 0.125f, 0f);
            CreateSprite(upperArm, side + " Upper Arm", new Vector3(0f, -0.085f, 0f), new Vector3(0.095f, 0.18f, 1f), new Color(0.08f, 0.11f, 0.16f, 1f), order);
            Transform forearm = CreateContainer(side + " Forearm Rig", upperArm);
            forearm.localPosition = new Vector3(0f, -0.17f, 0f);
            CreateSprite(forearm, side + " Forearm Armour", new Vector3(0f, -0.07f, 0f), new Vector3(0.105f, 0.15f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), order + 1);
        }

        private static void CreateRiggedLeg(Transform parent, string side, float xPosition, int order)
        {
            Transform upperLeg = CreateContainer(side + " Leg Rig", parent);
            upperLeg.localPosition = new Vector3(xPosition, -0.16f, 0f);
            CreateSprite(upperLeg, side + " Thigh Armour", new Vector3(0f, -0.09f, 0f), new Vector3(0.12f, 0.2f, 1f), new Color(0.08f, 0.11f, 0.16f, 1f), order);
            Transform lowerLeg = CreateContainer(side + " Lower Leg Rig", upperLeg);
            lowerLeg.localPosition = new Vector3(0f, -0.18f, 0f);
            CreateSprite(lowerLeg, side + " Shin Armour", new Vector3(0f, -0.08f, 0f), new Vector3(0.11f, 0.17f, 1f), new Color(0.42f, 0.5f, 0.58f, 1f), order + 1);
            CreateSprite(lowerLeg, side + " Armoured Boot", new Vector3(0.025f, -0.18f, 0f), new Vector3(0.17f, 0.1f, 1f), new Color(0.72f, 0.8f, 0.84f, 1f), order + 2);
        }

        private static void CreateSpawn(Transform parent, Vector3 position)
        {
            GameObject spawnObject = new GameObject("TransitHub_Spawn");
            spawnObject.transform.SetParent(parent, true);
            spawnObject.transform.position = position;
            spawnObject.AddComponent<PlayerSpawnPoint>();
            AddStableId(spawnObject, "spawn_hub_intro", WorldStableIdKind.Spawn);
        }

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, true);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(1.4f, 1.65f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = HubCameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.105f, 0.09f, 0.075f, 1f);
            cameraObject.AddComponent<AudioListener>();

            CameraFollow2D cameraFollow = cameraObject.AddComponent<CameraFollow2D>();
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            cameraFollow.Configure(player.transform, new Vector3(1.4f, 1.05f, -10f), 6.5f, new Vector2(1.4f, -0.55f), new Vector2(25.5f, 2.65f));
        }

        private static void CreateFallReset(Transform parent)
        {
            GameObject fallReset = new GameObject("THub_Hazard_FallReset");
            fallReset.layer = LayerMask.NameToLayer("Hazard");
            fallReset.transform.SetParent(parent, true);
            fallReset.transform.position = new Vector3(13.6f, -4.2f, 0f);
            BoxCollider2D collider = fallReset.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(31f, 0.7f);
            collider.isTrigger = true;
            fallReset.AddComponent<Hazard>().ConfigureForEditor("Ruined Transit Hub fall reset", false);
            AddStableId(fallReset, "hazard_hub_fall_reset", WorldStableIdKind.Validation);
        }

        private static void CreateZones(Transform parent)
        {
            CreateZone(parent, "THub_Zone_IntroCalibration", IntroZoneId, new Vector2(7.4f, 0.8f), new Vector2(12.8f, 1.5f), false);
            CreateZone(parent, "THub_Zone_IntroComplete", "hub_intro_complete", new Vector2(15.1f, 1f), new Vector2(2.2f, 1.2f), false);
            CreateZone(parent, "THub_Zone_CentralChamber", CentralZoneId, new Vector2(20f, 0.65f), new Vector2(3f, 0.5f), false);
        }

        private static void CreateOpening(Transform parent, Collider2D supportingFloor)
        {
            GameObject opening = new GameObject("THub_Opening_MainDoor");
            opening.transform.SetParent(parent, true);
            opening.transform.position = Vector3.zero;
            opening.AddComponent<WorldOpeningDefinition>().ConfigureForEditor(
                MainDoorOpeningId,
                new Vector2(6.65f, SpawnCenterY),
                new Vector2(10.35f, SpawnCenterY),
                PlayerColliderSize,
                PlayerClearanceSize,
                BaselineFloorTop,
                1.55f,
                supportingFloor);
            AddStableId(opening, MainDoorOpeningId, WorldStableIdKind.Opening);
        }

        private static void CreateTraversalLinks(
            Transform parent,
            BoxCollider2D chamberFloor,
            BoxCollider2D takeoff,
            BoxCollider2D landing,
            BoxCollider2D optionalBase,
            BoxCollider2D optionalLedgeA,
            BoxCollider2D optionalLedgeB,
            BoxCollider2D optionalRejoin)
        {
            CreateTraversalLink(parent, "THub_Link_MandatorySmallJump", MandatoryJumpLinkId, true, false, takeoff, landing, MandatoryGap, 0.325f, 1.8f, 2.2f);
            CreateTraversalLink(parent, "THub_Link_OptionalRoute_A", "link_hub_optional_elevated_a", false, true, chamberFloor, optionalBase, 0f, 0.6f, 1.5f, 1.5f);
            CreateTraversalLink(parent, "THub_Link_OptionalRoute_B", "link_hub_optional_elevated_b", false, true, optionalBase, optionalLedgeA, 0f, 0.6f, 1.5f, 1.5f);
            CreateTraversalLink(parent, "THub_Link_OptionalRoute_C", "link_hub_optional_elevated_c", false, true, optionalLedgeA, optionalLedgeB, 0f, 0.4f, 1.5f, 1.5f);
            CreateTraversalLink(parent, "THub_Link_OptionalRoute_Rejoin", "link_hub_optional_elevated_rejoin", false, true, optionalLedgeB, optionalRejoin, 0f, -1f, 1.5f, 1.5f);
        }

        private static void CreateRoutes(Transform parent, Collider2D wreckageBlocker, Collider2D sunkenBlocker, Collider2D facilityBlocker)
        {
            CreateRoute(parent, "THub_Route_WreckageBasin", WreckageRouteId, WorldRouteStatus.OpenStub, new Vector2(26.6f, 0.65f), new Vector2(1.6f, 0.5f), wreckageBlocker);
            CreateRoute(parent, "THub_Route_SunkenDistrictLocked", SunkenRouteId, WorldRouteStatus.Locked, new Vector2(21.25f, 0.65f), new Vector2(0.9f, 0.5f), sunkenBlocker);
            CreateRoute(parent, "THub_Route_NeuralFacilitySealed", FacilityRouteId, WorldRouteStatus.Sealed, new Vector2(18.75f, 0.65f), new Vector2(0.9f, 0.5f), facilityBlocker);
        }

        private static void CreateDecoration(Transform parent)
        {
            CreateDecorationSprite(parent, "THub_Deco_BackdropBand", new Vector2(13.6f, 1.8f), new Vector2(31f, 5.8f), new Color(0.13f, 0.125f, 0.115f, 1f), -30);
            CreateDecorationSprite(parent, "THub_Deco_SpawnCanopy", new Vector2(2f, 1.8f), new Vector2(5.4f, 1.2f), new Color(0.39f, 0.32f, 0.22f, 0.72f), -10);
            CreateDecorationSprite(parent, "THub_Deco_BuriedRails_A", new Vector2(2.1f, 0.12f), new Vector2(5f, 0.12f), new Color(0.75f, 0.54f, 0.27f, 0.8f), -2);
            CreateDecorationSprite(parent, "THub_Deco_DoorHologram", new Vector2(8.5f, 2.35f), new Vector2(1.5f, 0.3f), new Color(0.16f, 1f, 1f, 0.9f), 2);
            CreateDecorationSprite(parent, "THub_Deco_DoorSignalLeft", new Vector2(7.05f, 0.85f), new Vector2(0.08f, 1.45f), new Color(0.16f, 0.86f, 0.9f, 0.9f), 2);
            CreateDecorationSprite(parent, "THub_Deco_DoorSignalRight", new Vector2(9.95f, 0.85f), new Vector2(0.08f, 1.45f), new Color(0.16f, 0.86f, 0.9f, 0.9f), 2);
            CreateDecorationSprite(parent, "THub_Deco_ChamberBackWall", new Vector2(20.6f, 1.65f), new Vector2(6.6f, 3f), new Color(0.31f, 0.29f, 0.25f, 0.82f), -10);
            CreateDecorationSprite(parent, "THub_Deco_NeuralSwitchboard", new Vector2(20.1f, 0.95f), new Vector2(1.5f, 1.1f), new Color(0.07f, 0.3f, 0.32f, 0.86f), -1);
            CreateDecorationSprite(parent, "THub_Deco_BasinWindGate", new Vector2(26.8f, 1.65f), new Vector2(2f, 2.4f), new Color(0.7f, 0.49f, 0.2f, 0.62f), -1);
            CreateDecorationSprite(parent, "THub_Deco_BasinRouteSignal", new Vector2(26.8f, 2.55f), new Vector2(1.5f, 0.16f), new Color(1f, 0.68f, 0.22f, 1f), 2);
            CreateDecorationSprite(parent, "THub_Deco_DistrictSignalRed", new Vector2(21.3f, 2.35f), new Vector2(1.2f, 0.3f), new Color(1f, 0.22f, 0.26f, 1f), 2);
            CreateDecorationSprite(parent, "THub_Deco_FacilitySeal", new Vector2(18.7f, 2.2f), new Vector2(1.4f, 1.8f), new Color(0.88f, 0.13f, 0.52f, 0.64f), 1);
            CreateDecorationSprite(parent, "THub_Deco_OptionalCableArch", new Vector2(20.2f, 2.45f), new Vector2(4f, 0.18f), new Color(0.12f, 0.92f, 0.86f, 0.88f), 1);
            CreateDecorationSprite(parent, "THub_Deco_OptionalRouteMarkerA", new Vector2(17.4f, 1.02f), new Vector2(1.15f, 0.08f), new Color(0.25f, 1f, 0.9f, 1f), 2);
            CreateDecorationSprite(parent, "THub_Deco_OptionalRouteMarkerB", new Vector2(19.6f, 1.62f), new Vector2(1.25f, 0.08f), new Color(0.25f, 1f, 0.9f, 1f), 2);
            CreateDecorationSprite(parent, "THub_Deco_OptionalRouteMarkerC", new Vector2(21.8f, 2.02f), new Vector2(1.35f, 0.08f), new Color(0.25f, 1f, 0.9f, 1f), 2);
            CreateDecorationSprite(parent, "THub_Deco_FallResetSignal", new Vector2(13.6f, -3.35f), new Vector2(31f, 0.12f), new Color(1f, 0.2f, 0.22f, 0.9f), 3);
        }

        private static BoxCollider2D CreateGround(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject ground = new GameObject(name);
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.SetParent(parent, true);
            ground.transform.position = position;
            AddStableId(ground, name.ToLowerInvariant(), WorldStableIdKind.Geometry);

            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = size;
            ground.AddComponent<SurfaceModifier>();
            CreateSprite(ground.transform, "Greybox Fill", Vector3.zero, new Vector3(size.x, size.y, 1f), new Color(0.46f, 0.48f, 0.47f, 1f), 0);
            CreateSprite(ground.transform, "Walkable Edge", new Vector3(0f, size.y * 0.5f - 0.05f, 0f), new Vector3(size.x, 0.1f, 1f), new Color(0.2f, 0.94f, 0.96f, 1f), 1);
            return collider;
        }

        private static void CreateZone(Transform parent, string name, string zoneId, Vector2 position, Vector2 size, bool optional)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(parent, true);
            zone.transform.position = position;
            BoxCollider2D collider = zone.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            zone.AddComponent<WorldZoneDefinition>().ConfigureForEditor(RegionId, zoneId, optional);
            AddStableId(zone, zoneId, WorldStableIdKind.Zone);
        }

        private static void CreateTraversalLink(Transform parent, string name, string linkId, bool mandatory, bool optional, BoxCollider2D takeoff, BoxCollider2D landing, float gap, float rise, float minimumTakeoffWidth, float minimumLandingWidth)
        {
            GameObject link = new GameObject(name);
            link.transform.SetParent(parent, true);
            link.AddComponent<WorldTraversalLinkDefinition>().ConfigureForEditor(linkId, mandatory, optional, takeoff, landing, gap, rise, minimumTakeoffWidth, minimumLandingWidth);
            AddStableId(link, linkId, WorldStableIdKind.TraversalLink);
        }

        private static void CreateRoute(Transform parent, string name, string routeId, WorldRouteStatus status, Vector2 position, Vector2 size, Collider2D blocker)
        {
            GameObject route = new GameObject(name);
            route.transform.SetParent(parent, true);
            route.transform.position = position;
            BoxCollider2D collider = route.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            route.AddComponent<WorldRouteDefinition>().ConfigureForEditor(RegionId, routeId, status, collider, blocker);
            AddStableId(route, routeId, WorldStableIdKind.Route);
        }

        private static GameObject CreateDecorationSprite(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder)
        {
            GameObject decoration = new GameObject(name);
            decoration.transform.SetParent(parent, true);
            decoration.transform.position = position;
            AddStableId(decoration, name.ToLowerInvariant(), WorldStableIdKind.Decoration);
            CreateSprite(decoration.transform, "Visual", Vector3.zero, new Vector3(size.x, size.y, 1f), color, sortingOrder);
            return decoration;
        }

        private static GameObject CreateSprite(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = localPosition;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = editorSprite;
            renderer.color = color;
            renderer.sharedMaterial = editorUnlitMaterial;
            renderer.sortingOrder = sortingOrder;
            Vector2 spriteSize = editorSprite.bounds.size;

            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                throw new System.InvalidOperationException("The generated greybox sprite must have non-zero bounds.");
            }

            spriteObject.transform.localScale = new Vector3(
                localScale.x / spriteSize.x,
                localScale.y / spriteSize.y,
                localScale.z);
            return spriteObject;
        }

        private static Transform CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            return root.transform;
        }

        private static Transform CreateContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name);

            if (parent != null)
            {
                container.transform.SetParent(parent, false);
            }

            return container.transform;
        }

        private static void AddStableId(GameObject target, string stableId, WorldStableIdKind kind)
        {
            target.AddComponent<WorldStableId>().ConfigureForEditor(stableId, kind);
        }

        private static void TintRenderer(GameObject target, Color color)
        {
            SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        private static void TintGround(Collider2D collider, Color fillColor, Color edgeColor)
        {
            if (collider == null)
            {
                return;
            }

            Transform fill = collider.transform.Find("Greybox Fill");
            Transform edge = collider.transform.Find("Walkable Edge");

            if (fill != null)
            {
                fill.GetComponent<SpriteRenderer>().color = fillColor;
            }

            if (edge != null)
            {
                edge.GetComponent<SpriteRenderer>().color = edgeColor;
            }
        }

        private static void SetSerializedString(Object target, string fieldName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureProtagonistAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder("Assets", "Animations");
            EnsureFolder("Assets/Animations", "Player");
            EnsureFolder("Assets/Animations/Player", "Resources");
            EnsureFolder("Assets/Animations/Player/Resources", "Synaptrace");
            EnsureFolder("Assets/Animations/Player/Resources/Synaptrace", "Player");

            ConfigureProtagonistTexture(ProtagonistMainSpritePath, 200, 200, new Vector2(0.4425f, 0.519f));

            Vector2[] idlePivots =
            {
                new Vector2(0.5f, 0.4989796f),
                new Vector2(0.5f, 0.4989796f),
                new Vector2(0.5f, 0.4938776f),
                new Vector2(0.5f, 0.4938776f),
                new Vector2(0.5f, 0.4938776f),
                new Vector2(0.5f, 0.4938776f)
            };

            Sprite[] idleSprites = new Sprite[ProtagonistIdleSpritePaths.Length];

            for (int i = 0; i < ProtagonistIdleSpritePaths.Length; i++)
            {
                ConfigureProtagonistTexture(ProtagonistIdleSpritePaths[i], 196, 196, idlePivots[i]);
                idleSprites[i] = LoadRequiredSprite(ProtagonistIdleSpritePaths[i]);
            }

            Sprite mainSprite = LoadRequiredSprite(ProtagonistMainSpritePath);
            AnimationClip staticClip = CreateOrUpdateSpriteClip(
                ProtagonistStaticClipPath,
                "Protagonist Static",
                new[] { mainSprite, mainSprite },
                4f,
                false);
            AnimationClip idleClip = CreateOrUpdateSpriteClip(
                ProtagonistIdleClipPath,
                "Protagonist Idle Breathing",
                idleSprites,
                4f,
                true);
            CreateOrUpdateProtagonistController(staticClip, idleClip);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureProtagonistTexture(string assetPath, int expectedWidth, int expectedHeight, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                throw new FileNotFoundException("Missing protagonist texture asset.", assetPath);
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                string actualSize = texture == null ? "unavailable" : texture.width + "x" + texture.height;
                throw new System.InvalidOperationException(
                    assetPath + " is " + actualSize + ", expected " + expectedWidth + "x" + expectedHeight + ".");
            }

            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(importer.spritePixelsPerUnit, ProtagonistPixelsPerUnit)
                || textureSettings.spriteAlignment != (int)SpriteAlignment.Custom
                || Vector2.Distance(textureSettings.spritePivot, pivot) > 0.0001f
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.npotScale != TextureImporterNPOTScale.None;

            if (!changed)
            {
                return;
            }

            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = pivot;
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ProtagonistPixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static Sprite LoadRequiredSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null)
            {
                throw new System.InvalidOperationException("Could not load imported protagonist sprite at " + assetPath + ".");
            }

            return sprite;
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string assetPath, string clipName, Sprite[] sprites, float frameRate, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, assetPath);
                AssetDatabase.SetLabels(clip, new[] { "SynaptraceGenerated" });
            }
            else
            {
                RequireGeneratedAsset(clip, assetPath);
            }

            clip.ClearCurves();
            clip.frameRate = frameRate;

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = PlayerVisualFactory.SpriteObjectName,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void CreateOrUpdateProtagonistController(AnimationClip staticClip, AnimationClip idleClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ProtagonistControllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ProtagonistControllerPath);
                AssetDatabase.SetLabels(controller, new[] { "SynaptraceGenerated" });
            }
            else
            {
                RequireGeneratedAsset(controller, ProtagonistControllerPath);
            }

            AnimatorControllerParameter[] existingParameters = controller.parameters;

            for (int i = 0; i < existingParameters.Length; i++)
            {
                controller.RemoveParameter(existingParameters[i]);
            }

            controller.AddParameter(PlayerVisualAnimator.IdleAnimatorParameter, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState staticState = FindOrCreateState(stateMachine, "Static");
            AnimatorState idleState = FindOrCreateState(stateMachine, "Idle Breathing");
            staticState.motion = staticClip;
            idleState.motion = idleClip;
            stateMachine.defaultState = staticState;
            RemoveTransitions(staticState);
            RemoveTransitions(idleState);

            AnimatorStateTransition toIdle = staticState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.If, 0f, PlayerVisualAnimator.IdleAnimatorParameter);

            AnimatorStateTransition toStatic = idleState.AddTransition(staticState);
            toStatic.hasExitTime = false;
            toStatic.duration = 0f;
            toStatic.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerVisualAnimator.IdleAnimatorParameter);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return stateMachine.AddState(stateName);
        }

        private static void RemoveTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] transitions = state.transitions;

            for (int i = 0; i < transitions.Length; i++)
            {
                state.RemoveTransition(transitions[i]);
            }
        }

        private static void RequireGeneratedAsset(Object asset, string assetPath)
        {
            string[] labels = AssetDatabase.GetLabels(asset);

            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == "SynaptraceGenerated")
                {
                    return;
                }
            }

            throw new System.InvalidOperationException(
                "Refusing to overwrite unowned asset at " + assetPath + ". Add the SynaptraceGenerated label only if it is generator-owned.");
        }

        private static void EnsureProjectLayers()
        {
            EnsureLayer(6, "Ground");
            EnsureLayer(7, "Hazard");
            EnsureLayer(8, "Goal");
        }

        private static void EnsureLayer(int index, string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

            if (assets.Length == 0)
            {
                throw new System.InvalidOperationException("Could not load TagManager.asset.");
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);

            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != layerName)
            {
                throw new System.InvalidOperationException("Layer " + index + " is '" + layer.stringValue + "', expected '" + layerName + "'.");
            }

            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Scripts");
            EnsureFolder("Assets/Scripts", "World");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true));
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string ToProjectAbsolutePath(string assetPath)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectPath, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
