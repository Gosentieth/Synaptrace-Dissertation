using System.Collections.Generic;
using System.Linq;
using Synaptrace.Adaptation;
using Synaptrace.Core;
using Synaptrace.Player;
using Synaptrace.Systems;
using Synaptrace.Telemetry;
using Synaptrace.UI;
using Synaptrace.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Synaptrace.EditorTools
{
    public sealed class RuinedTransitHubValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public bool Passed => Errors.Count == 0;

        public void Error(string message)
        {
            Errors.Add(message);
        }

        public void Warning(string message)
        {
            Warnings.Add(message);
        }
    }

    public static class RuinedTransitHubValidator
    {
        private const float Tolerance = 0.01f;
        private const float MaximumOrdinaryJumpRise = 2.2936f;
        private const float MaximumSameHeightCenterTravel = 5.3517f;

        [MenuItem("Synaptrace/World/Validate Ruined Transit Hub")]
        public static void ValidateOpenSceneFromMenu()
        {
            RuinedTransitHubValidationReport report = ValidateOpenScene();
            LogReport(report);
        }

        public static void ValidateSceneBatch()
        {
            EditorSceneManager.OpenScene(RuinedTransitHubSceneGenerator.ScenePath, OpenSceneMode.Single);
            ValidateOpenSceneOrExit();
        }

        public static void ValidateOpenSceneOrExit()
        {
            RuinedTransitHubValidationReport report = ValidateOpenScene();
            LogReport(report);

            if (!report.Passed && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        public static RuinedTransitHubValidationReport ValidateOpenScene()
        {
            Physics2D.SyncTransforms();

            RuinedTransitHubValidationReport report = new RuinedTransitHubValidationReport();
            ValidateFallbackScene(report);
            ValidateSingletons(report);
            ValidateStableIds(report);
            ValidateNoPrototypeBootstrapper(report);
            ValidateNoCompositeColliders(report);
            ValidateDecoration(report);
            ValidateGeometryLayers(report);
            ValidateSpawn(report);
            ValidateDoorway(report);
            ValidateTraversalLinks(report);
            ValidateContinuousFloors(report);
            ValidateTriggers(report);
            ValidateRoutes(report);
            return report;
        }

        private static void ValidateFallbackScene(RuinedTransitHubValidationReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0 || scenes[0].path != "Assets/Scenes/Main.unity" || !scenes[0].enabled)
            {
                report.Error("Prototype fallback Main.unity is not enabled first in build settings.");
            }

            if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Assets/Scenes/Main.unity")))
            {
                report.Error("Prototype fallback Main.unity is missing.");
            }
        }

        private static void ValidateSingletons(RuinedTransitHubValidationReport report)
        {
            ExpectCount<PlayerController>(report, 1, "player");
            ExpectCount<Camera>(report, 1, "camera");
            ExpectCount<PlayerSpawnPoint>(report, 1, "active spawn");
            ExpectCount<TelemetryTracker>(report, 1, "TelemetryTracker");
            ExpectCount<DifficultyManager>(report, 1, "DifficultyManager");
            ExpectCount<GameManager>(report, 1, "GameManager");
            ExpectCount<LevelManager>(report, 1, "LevelManager");
            ExpectCount<PrototypeHUD>(report, 1, "PrototypeHUD");
        }

        private static void ValidateStableIds(RuinedTransitHubValidationReport report)
        {
            WorldStableId[] ids = Object.FindObjectsByType<WorldStableId>(FindObjectsSortMode.None);
            HashSet<string> seen = new HashSet<string>();

            foreach (WorldStableId id in ids)
            {
                if (string.IsNullOrWhiteSpace(id.StableId))
                {
                    report.Error(id.name + " has a missing stable ID.");
                    continue;
                }

                if (!seen.Add(id.StableId))
                {
                    report.Error("Duplicate stable ID: " + id.StableId);
                }
            }

            string[] required =
            {
                RuinedTransitHubSceneGenerator.RegionId,
                RuinedTransitHubSceneGenerator.IntroZoneId,
                "hub_intro_complete",
                RuinedTransitHubSceneGenerator.CentralZoneId,
                RuinedTransitHubSceneGenerator.MainDoorOpeningId,
                RuinedTransitHubSceneGenerator.MandatoryJumpLinkId,
                RuinedTransitHubSceneGenerator.WreckageRouteId,
                RuinedTransitHubSceneGenerator.SunkenRouteId,
                RuinedTransitHubSceneGenerator.FacilityRouteId,
                "spawn_hub_intro"
            };

            foreach (string requiredId in required)
            {
                if (!seen.Contains(requiredId))
                {
                    report.Error("Missing required stable ID: " + requiredId);
                }
            }
        }

        private static void ValidateNoPrototypeBootstrapper(RuinedTransitHubValidationReport report)
        {
            if (Object.FindObjectsByType<RuntimePrototypeBootstrapper>(FindObjectsSortMode.None).Length > 0)
            {
                report.Error("Ruined Transit Hub scene must not contain RuntimePrototypeBootstrapper.");
            }
        }

        private static void ValidateNoCompositeColliders(RuinedTransitHubValidationReport report)
        {
            CompositeCollider2D[] composites = Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None);

            if (composites.Length > 0)
            {
                report.Error("CompositeCollider2D is not allowed in the Ruined Transit Hub greybox.");
            }
        }

        private static void ValidateDecoration(RuinedTransitHubValidationReport report)
        {
            foreach (GameObject decoration in AllSceneGameObjects().Where(go => go.name.StartsWith("THub_Deco_")))
            {
                if (decoration.GetComponentInChildren<Collider2D>(true) != null)
                {
                    report.Error(decoration.name + " is decoration but contains a Collider2D.");
                }

                if (decoration.GetComponentInChildren<Rigidbody2D>(true) != null)
                {
                    report.Error(decoration.name + " is decoration but contains a Rigidbody2D.");
                }
            }
        }

        private static void ValidateGeometryLayers(RuinedTransitHubValidationReport report)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");

            foreach (GameObject geometry in AllSceneGameObjects().Where(go => go.name.StartsWith("THub_Geo_")))
            {
                Collider2D collider = geometry.GetComponent<Collider2D>();

                if (collider == null)
                {
                    report.Error(geometry.name + " is gameplay geometry but has no Collider2D.");
                    continue;
                }

                if (collider.isTrigger)
                {
                    report.Error(geometry.name + " is gameplay geometry but its collider is a trigger.");
                }

                if (geometry.layer != groundLayer)
                {
                    report.Error(geometry.name + " must be on the Ground layer.");
                }
            }
        }

        private static void ValidateSpawn(RuinedTransitHubValidationReport report)
        {
            PlayerSpawnPoint spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>();
            BoxCollider2D supportingFloor = FindBox("THub_Geo_SpawnCalibrationFloor");

            if (spawn == null || supportingFloor == null)
            {
                report.Error("Spawn or supporting floor is missing.");
                return;
            }

            float expectedY = supportingFloor.bounds.max.y + RuinedTransitHubSceneGenerator.SpawnCenterY;

            if (Mathf.Abs(spawn.transform.position.y - expectedY) > Tolerance)
            {
                report.Error("Spawn Y is " + spawn.transform.position.y + ", expected floor top plus 0.605 (" + expectedY + ").");
            }

            ContactFilter2D solidGroundFilter = CreateGroundSolidFilter();
            Collider2D[] overlaps = new Collider2D[16];
            int count = Physics2D.OverlapBox(spawn.transform.position, RuinedTransitHubSceneGenerator.PlayerClearanceSize, 0f, solidGroundFilter, overlaps);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = overlaps[i];

                if (hit == supportingFloor)
                {
                    continue;
                }

                report.Error("Spawn clearance overlaps solid collider: " + hit.name);
            }
        }

        private static void ValidateDoorway(RuinedTransitHubValidationReport report)
        {
            WorldOpeningDefinition opening = Object.FindFirstObjectByType<WorldOpeningDefinition>();
            BoxCollider2D header = FindBox("THub_Geo_DoorOverheadHeader");

            if (opening == null || header == null)
            {
                report.Error("Main doorway opening metadata or header is missing.");
                return;
            }

            float verticalClearance = header.bounds.min.y - opening.FloorTopY;

            if (verticalClearance + Tolerance < opening.MinimumVerticalClearance)
            {
                report.Error("Main doorway vertical clearance is " + verticalClearance + ", expected at least " + opening.MinimumVerticalClearance + ".");
            }

            ValidateOpeningCast(report, opening, opening.PlayerBodySize, opening.CastStart, "body");
            Vector2 marginCenterStart = new Vector2(opening.CastStart.x, opening.FloorTopY + opening.MarginSize.y * 0.5f);
            Vector2 marginCenterEnd = new Vector2(opening.CastEnd.x, opening.FloorTopY + opening.MarginSize.y * 0.5f);
            ValidateOpeningCast(report, opening, opening.MarginSize, marginCenterStart, "safety margin", marginCenterEnd);
        }

        private static void ValidateOpeningCast(RuinedTransitHubValidationReport report, WorldOpeningDefinition opening, Vector2 size, Vector2 start, string label)
        {
            ValidateOpeningCast(report, opening, size, start, label, opening.CastEnd);
        }

        private static void ValidateOpeningCast(RuinedTransitHubValidationReport report, WorldOpeningDefinition opening, Vector2 size, Vector2 start, string label, Vector2 end)
        {
            Vector2 direction = end - start;
            float distance = direction.magnitude;
            RaycastHit2D[] hits = new RaycastHit2D[16];
            int count = Physics2D.BoxCast(start, size, 0f, direction.normalized, CreateGroundSolidFilter(), hits, distance);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hits[i].collider;

                if (hit == null || hit == opening.SupportingFloor)
                {
                    continue;
                }

                report.Error("Main doorway " + label + " BoxCast hit solid collider: " + hit.name);
            }
        }

        private static void ValidateTraversalLinks(RuinedTransitHubValidationReport report)
        {
            foreach (WorldTraversalLinkDefinition link in Object.FindObjectsByType<WorldTraversalLinkDefinition>(FindObjectsSortMode.None))
            {
                if (link.TakeoffCollider == null || link.LandingCollider == null)
                {
                    report.Error(link.name + " has missing takeoff or landing collider.");
                    continue;
                }

                float actualGap = link.LandingCollider.bounds.min.x - link.TakeoffCollider.bounds.max.x;
                float actualRise = link.LandingCollider.bounds.max.y - link.TakeoffCollider.bounds.max.y;

                if (link.Mandatory)
                {
                    if (Mathf.Abs(actualGap - link.ExpectedGap) > Tolerance)
                    {
                        report.Error(link.LinkId + " gap is " + actualGap + ", expected " + link.ExpectedGap + ".");
                    }

                    if (Mathf.Abs(actualRise - link.ExpectedLandingRise) > Tolerance)
                    {
                        report.Error(link.LinkId + " landing rise is " + actualRise + ", expected " + link.ExpectedLandingRise + ".");
                    }
                }

                if (link.Optional && actualRise > 1f + Tolerance)
                {
                    report.Error(link.LinkId + " optional route rise exceeds 1.0 unit: " + actualRise + ".");
                }

                if (link.TakeoffCollider.bounds.size.x + Tolerance < link.MinimumTakeoffWidth)
                {
                    report.Error(link.LinkId + " takeoff width is below minimum.");
                }

                if (link.LandingCollider.bounds.size.x + Tolerance < link.MinimumLandingWidth)
                {
                    report.Error(link.LinkId + " landing width is below minimum.");
                }

                if (link.Mandatory && (actualRise > MaximumOrdinaryJumpRise - 0.35f || actualGap > MaximumSameHeightCenterTravel * 0.7f))
                {
                    report.Error(link.LinkId + " exceeds the mandatory traversal envelope margin.");
                }
            }
        }

        private static void ValidateContinuousFloors(RuinedTransitHubValidationReport report)
        {
            ValidateAdjacent(report, "THub_Geo_SpawnCalibrationFloor", "THub_Geo_DoorPassageFloor");
            ValidateAdjacent(report, "THub_Geo_DoorPassageFloor", "THub_Geo_SmallJumpTakeoff");
            ValidateAdjacent(report, "THub_Geo_SmallJumpLanding", "THub_Geo_ChamberFloor");
            ValidateAdjacent(report, "THub_Geo_ChamberFloor", "THub_Geo_BasinExitFloor");
        }

        private static void ValidateTriggers(RuinedTransitHubValidationReport report)
        {
            ContactFilter2D solidGroundFilter = CreateGroundSolidFilter();

            foreach (BoxCollider2D trigger in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).Where(c => c.isTrigger))
            {
                if (trigger.name == "THub_Hazard_FallReset")
                {
                    continue;
                }

                Collider2D[] overlaps = new Collider2D[16];
                int count = Physics2D.OverlapBox(trigger.bounds.center, trigger.bounds.size, 0f, solidGroundFilter, overlaps);

                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = overlaps[i];

                    if (hit == null || hit == trigger)
                    {
                        continue;
                    }

                    report.Error(trigger.name + " trigger unexpectedly overlaps solid collider: " + hit.name);
                }
            }
        }

        private static void ValidateRoutes(RuinedTransitHubValidationReport report)
        {
            Dictionary<string, WorldRouteDefinition> routes = Object.FindObjectsByType<WorldRouteDefinition>(FindObjectsSortMode.None)
                .ToDictionary(route => route.RouteId, route => route);

            foreach (string requiredRoute in new[] { RuinedTransitHubSceneGenerator.WreckageRouteId, RuinedTransitHubSceneGenerator.SunkenRouteId, RuinedTransitHubSceneGenerator.FacilityRouteId })
            {
                if (!routes.TryGetValue(requiredRoute, out WorldRouteDefinition route))
                {
                    report.Error("Missing route definition: " + requiredRoute);
                    continue;
                }

                if (route.RouteTrigger == null)
                {
                    report.Error(requiredRoute + " has no route trigger.");
                }

                if (route.TerminalBlocker == null)
                {
                    report.Error(requiredRoute + " has no visible terminal blocker.");
                }
            }
        }

        private static void ValidateAdjacent(RuinedTransitHubValidationReport report, string leftName, string rightName)
        {
            BoxCollider2D left = FindBox(leftName);
            BoxCollider2D right = FindBox(rightName);

            if (left == null || right == null)
            {
                report.Error("Missing floor pair for adjacency validation: " + leftName + " / " + rightName);
                return;
            }

            float gap = right.bounds.min.x - left.bounds.max.x;

            if (Mathf.Abs(gap) > Tolerance)
            {
                report.Error(leftName + " to " + rightName + " has unintended gap/overlap of " + gap + ".");
            }

            if (Mathf.Abs(right.bounds.max.y - left.bounds.max.y) > Tolerance)
            {
                report.Warning(leftName + " and " + rightName + " are adjacent with different surface heights.");
            }
        }

        private static void ExpectCount<T>(RuinedTransitHubValidationReport report, int expected, string label) where T : Object
        {
            int count = Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;

            if (count != expected)
            {
                report.Error("Expected exactly " + expected + " " + label + ", found " + count + ".");
            }
        }

        private static BoxCollider2D FindBox(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<BoxCollider2D>() : null;
        }

        private static ContactFilter2D CreateGroundSolidFilter()
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = 1 << LayerMask.NameToLayer("Ground"),
                useTriggers = false
            };
            return filter;
        }

        private static IEnumerable<GameObject> AllSceneGameObjects()
        {
            Scene scene = SceneManager.GetActiveScene();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    yield return transform.gameObject;
                }
            }
        }

        private static void LogReport(RuinedTransitHubValidationReport report)
        {
            foreach (string warning in report.Warnings)
            {
                Debug.LogWarning("[Synaptrace Hub Validation] " + warning);
            }

            foreach (string error in report.Errors)
            {
                Debug.LogError("[Synaptrace Hub Validation] " + error);
            }

            if (report.Passed)
            {
                Debug.Log("[Synaptrace Hub Validation] Passed with " + report.Warnings.Count + " warning(s).");
            }
        }
    }
}
