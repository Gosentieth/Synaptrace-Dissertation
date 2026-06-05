using Synaptrace.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Synaptrace.EditorTools
{
    public static class PrototypeSceneGenerator
    {
        private const int GroundLayerIndex = 6;
        private const int HazardLayerIndex = 7;
        private const int GoalLayerIndex = 8;
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Synaptrace/Rebuild Prototype Scene")]
        public static void RebuildPrototype()
        {
            EnsureFolders();
            EnsureProjectLayers();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            GameObject systemsObject = new GameObject("Game Systems");
            systemsObject.AddComponent<RuntimePrototypeBootstrapper>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Synaptrace] Layer 1 prototype bootstrap scene and build settings rebuilt.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Scripts");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "ScriptableObjects");
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets", "Documentation");
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Player");
            EnsureFolder("Assets/Art", "Environment");
            EnsureFolder("Assets/Art", "Hazards");
            EnsureFolder("Assets/Art", "UI");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureProjectLayers()
        {
            EnsureLayer(GroundLayerIndex, "Ground");
            EnsureLayer(HazardLayerIndex, "Hazard");
            EnsureLayer(GoalLayerIndex, "Goal");
        }

        private static void EnsureLayer(int index, string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

            if (assets.Length == 0)
            {
                Debug.LogWarning("[Synaptrace] Could not load TagManager.asset to configure layers.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);

            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != layerName)
            {
                Debug.LogWarning("[Synaptrace] Layer " + index + " is already named '" + layer.stringValue + "'. Expected '" + layerName + "'.");
                return;
            }

            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
