using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Synaptrace.EditorTools.Tests
{
    public sealed class RuinedTransitHubValidationTests
    {
        [Test]
        public void GeneratedHubPassesValidation()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneContents();

            object report = ValidateOpenScene();

            IList errors = GetErrors(report);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.Cast<object>()));
        }

        [Test]
        public void MandatoryJumpUsesExactGapAndRise()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneContents();

            BoxCollider2D takeoff = GameObject.Find("THub_Geo_SmallJumpTakeoff").GetComponent<BoxCollider2D>();
            BoxCollider2D landing = GameObject.Find("THub_Geo_SmallJumpLanding").GetComponent<BoxCollider2D>();

            float gap = landing.bounds.min.x - takeoff.bounds.max.x;
            float rise = landing.bounds.max.y - takeoff.bounds.max.y;

            Assert.That(gap, Is.EqualTo(1.2f).Within(0.01f));
            Assert.That(rise, Is.EqualTo(0.325f).Within(0.01f));
        }

        [Test]
        public void DoorwayValidationFailsWhenSolidBlockerIsInserted()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneContents();

            GameObject blocker = new GameObject("THub_Geo_TemporaryDoorwayBlocker");
            blocker.layer = LayerMask.NameToLayer("Ground");
            blocker.transform.position = new Vector3(8.5f, 0.7f, 0f);
            BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, 1.2f);

            object report = ValidateOpenScene();
            IList errors = GetErrors(report);

            Assert.That(errors.Cast<object>().Select(error => error.ToString()), Has.Some.Contains("Main doorway"));
        }

        [Test]
        public void GeneratedHubUsesReadableCameraAndAuthoredProtagonist()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneContents();

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            GameObject visualRoot = GameObject.Find("Visual Root - Protagonist");
            GameObject spriteObject = GameObject.Find("Protagonist Sprite");

            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographicSize, Is.EqualTo(3f).Within(0.01f));
            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(spriteObject, Is.Not.Null);
            Assert.That(GameObject.Find("Visual Root - Tech Knight"), Is.Null);
            Assert.That(GameObject.Find("Knight Armour Body"), Is.Null);

            SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(160f).Within(0.01f));
            Assert.That(renderer.sprite.rect.size, Is.EqualTo(new Vector2(200f, 200f)));
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Sprites/Default"));
            Assert.That(visualRoot.transform.lossyScale.x, Is.GreaterThan(0f));
            Animator animator = visualRoot.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(visualRoot.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(spriteObject.transform.localPosition, Is.EqualTo(Vector3.zero));

            foreach (BoxCollider2D collider in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
                         .Where(candidate => candidate.name.StartsWith("THub_Geo_", StringComparison.Ordinal)))
            {
                SpriteRenderer fill = collider.transform.Find("Greybox Fill").GetComponent<SpriteRenderer>();
                Assert.That(fill.bounds.size.x, Is.EqualTo(collider.bounds.size.x).Within(0.01f), collider.name);
                Assert.That(fill.bounds.size.y, Is.EqualTo(collider.bounds.size.y).Within(0.01f), collider.name);
            }
        }

        [Test]
        public void FacingRuleMapsNativeRightArtworkAndRetainsLastMeaningfulDirection()
        {
            MethodInfo resolveFlip = GetLoadedType("Synaptrace.Player.PlayerVisualAnimator")
                .GetMethod("ResolveSpriteFlipX", BindingFlags.Public | BindingFlags.Static);

            Assert.That(resolveFlip, Is.Not.Null);
            Assert.That(InvokeFacingRule(resolveFlip, 1f, true, true), Is.False, "Positive movement must show native-right art unflipped.");
            Assert.That(InvokeFacingRule(resolveFlip, -1f, true, false), Is.True, "Negative movement must flip native-right art.");
            Assert.That(InvokeFacingRule(resolveFlip, 0.05f, true, true), Is.True, "Dead-zone input must retain the previous flip state.");
            Assert.That(InvokeFacingRule(resolveFlip, 0f, true, false), Is.False, "Zero input must retain the previous flip state.");
        }

        [Test]
        public void IdleClipUsesCompleteFootAlignedSpritesAndNoTransformCurves()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneContents();

            const string clipPath = "Assets/Animations/Player/ProtagonistIdleBreathing.anim";
            const string artFolder = "Assets/Art/Player/Protagonist/Resources/Synaptrace/Player";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null);

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            Assert.That(objectBindings, Has.Length.EqualTo(1));
            Assert.That(objectBindings[0].path, Is.EqualTo("Protagonist Sprite"));
            Assert.That(objectBindings[0].propertyName, Is.EqualTo("m_Sprite"));
            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Empty, "Idle clip must not animate transforms, scale, or other float properties.");

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, objectBindings[0]);
            Assert.That(keyframes, Has.Length.EqualTo(6));

            int[] expectedPixelCounts = { 5826, 6052, 6023, 6041, 5786, 6507 };
            RectInt[] expectedAlphaBounds =
            {
                new RectInt(65, 1, 66, 169),
                new RectInt(59, 1, 74, 170),
                new RectInt(57, 0, 74, 171),
                new RectInt(52, 0, 77, 170),
                new RectInt(50, 0, 79, 171),
                new RectInt(58, 0, 71, 172)
            };

            List<float> horizontalFootAnchors = new List<float>();
            List<float> verticalFootAnchors = new List<float>();

            for (int i = 0; i < keyframes.Length; i++)
            {
                string expectedPath = artFolder + "/synaptrace-protagonist-idle-" + (i + 1).ToString("00") + ".png";
                Sprite sprite = keyframes[i].value as Sprite;
                Assert.That(sprite, Is.Not.Null, "Idle keyframe " + i + " has a null Sprite.");
                Assert.That(keyframes[i].time, Is.EqualTo(i * 0.25f).Within(0.001f));
                Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo(expectedPath));
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(196f, 196f)));

                AlphaMetrics metrics = AnalyzeAlpha(expectedPath);
                Assert.That(metrics.NonTransparentPixelCount, Is.EqualTo(expectedPixelCounts[i]), expectedPath);
                Assert.That(metrics.Bounds, Is.EqualTo(expectedAlphaBounds[i]), expectedPath);
                Assert.That(metrics.Bounds.width, Is.GreaterThanOrEqualTo(60), expectedPath + " is missing full-body width.");
                Assert.That(metrics.Bounds.height, Is.GreaterThanOrEqualTo(165), expectedPath + " is missing head-to-foot height.");
                Assert.That(metrics.SupportPixelCount, Is.GreaterThanOrEqualTo(300), expectedPath + " is missing planted feet.");

                horizontalFootAnchors.Add(metrics.SupportMeanX - sprite.pivot.x);
                verticalFootAnchors.Add(metrics.Bounds.yMin + 0.5f - sprite.pivot.y);
            }

            Assert.That(horizontalFootAnchors.Max() - horizontalFootAnchors.Min(), Is.LessThanOrEqualTo(1f));
            Assert.That(verticalFootAnchors.Max() - verticalFootAnchors.Min(), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void AuthoredAnimationUsesIdleOnlyForIdleState()
        {
            Type animatorType = GetLoadedType("Synaptrace.Player.PlayerVisualAnimator");
            Type visualStateType = animatorType.GetNestedType("VisualState", BindingFlags.Public);
            MethodInfo shouldPlayIdle = animatorType.GetMethod("ShouldPlayIdleAnimation", BindingFlags.Public | BindingFlags.Static);

            Assert.That(visualStateType, Is.Not.Null);
            Assert.That(shouldPlayIdle, Is.Not.Null);

            foreach (string stateName in Enum.GetNames(visualStateType))
            {
                object state = Enum.Parse(visualStateType, stateName);
                bool actual = (bool)shouldPlayIdle.Invoke(null, new[] { state });
                Assert.That(actual, Is.EqualTo(stateName == "Idle"), stateName);
            }
        }

        private static void BuildSceneContents()
        {
            GetEditorToolType("Synaptrace.EditorTools.RuinedTransitHubSceneGenerator")
                .GetMethod("BuildSceneContents", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
        }

        private static object ValidateOpenScene()
        {
            return GetEditorToolType("Synaptrace.EditorTools.RuinedTransitHubValidator")
                .GetMethod("ValidateOpenScene", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
        }

        private static IList GetErrors(object report)
        {
            return (IList)report.GetType()
                .GetField("Errors", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(report);
        }

        private static Type GetEditorToolType(string fullName)
        {
            Type type = GetLoadedType(fullName);

            Assert.That(type, Is.Not.Null, "Could not find editor tool type " + fullName);
            return type;
        }

        private static Type GetLoadedType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(candidate => candidate != null);
        }

        private static bool InvokeFacingRule(MethodInfo method, float direction, bool nativeFacesRight, bool currentFlip)
        {
            return (bool)method.Invoke(null, new object[] { direction, nativeFacesRight, currentFlip, 0.1f });
        }

        private static AlphaMetrics AnalyzeAlpha(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            byte[] bytes = File.ReadAllBytes(Path.Combine(projectRoot, assetPath));
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                Assert.That(ImageConversion.LoadImage(texture, bytes, false), Is.True, assetPath);
                Color32[] pixels = texture.GetPixels32();
                int minX = texture.width;
                int minY = texture.height;
                int maxX = -1;
                int maxY = -1;
                int nonTransparent = 0;

                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[y * texture.width + x].a == 0)
                        {
                            continue;
                        }

                        nonTransparent++;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                int supportCount = 0;
                float supportX = 0f;

                for (int y = minY; y <= Mathf.Min(maxY, minY + 12); y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (pixels[y * texture.width + x].a == 0)
                        {
                            continue;
                        }

                        supportCount++;
                        supportX += x;
                    }
                }

                return new AlphaMetrics
                {
                    Bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1),
                    NonTransparentPixelCount = nonTransparent,
                    SupportPixelCount = supportCount,
                    SupportMeanX = supportX / supportCount
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private struct AlphaMetrics
        {
            public RectInt Bounds;
            public int NonTransparentPixelCount;
            public int SupportPixelCount;
            public float SupportMeanX;
        }
    }
}
