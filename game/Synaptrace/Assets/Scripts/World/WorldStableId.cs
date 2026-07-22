using UnityEngine;

namespace Synaptrace.World
{
    public enum WorldStableIdKind
    {
        Region,
        Zone,
        Opening,
        TraversalLink,
        Route,
        Spawn,
        Geometry,
        Decoration,
        Validation
    }

    [DisallowMultipleComponent]
    public sealed class WorldStableId : MonoBehaviour
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private WorldStableIdKind kind = WorldStableIdKind.Validation;

        public string StableId => stableId;
        public WorldStableIdKind Kind => kind;

#if UNITY_EDITOR
        public void ConfigureForEditor(string editorStableId, WorldStableIdKind editorKind)
        {
            stableId = editorStableId;
            kind = editorKind;
        }
#endif
    }
}
