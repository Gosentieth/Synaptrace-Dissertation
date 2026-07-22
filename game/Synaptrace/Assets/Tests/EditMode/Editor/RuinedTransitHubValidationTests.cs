using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(candidate => candidate != null);

            Assert.That(type, Is.Not.Null, "Could not find editor tool type " + fullName);
            return type;
        }
    }
}
