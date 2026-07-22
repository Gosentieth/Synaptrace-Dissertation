using UnityEngine;

namespace Synaptrace.World
{
    [DisallowMultipleComponent]
    public sealed class WorldOpeningDefinition : MonoBehaviour
    {
        [SerializeField] private string openingId = string.Empty;
        [SerializeField] private Vector2 castStart;
        [SerializeField] private Vector2 castEnd;
        [SerializeField] private Vector2 playerBodySize = new Vector2(0.78f, 1.18f);
        [SerializeField] private Vector2 marginSize = new Vector2(0.98f, 1.38f);
        [SerializeField] private float floorTopY;
        [SerializeField] private float minimumVerticalClearance = 1.55f;
        [SerializeField] private Collider2D supportingFloor;

        public string OpeningId => openingId;
        public Vector2 CastStart => castStart;
        public Vector2 CastEnd => castEnd;
        public Vector2 PlayerBodySize => playerBodySize;
        public Vector2 MarginSize => marginSize;
        public float FloorTopY => floorTopY;
        public float MinimumVerticalClearance => minimumVerticalClearance;
        public Collider2D SupportingFloor => supportingFloor;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string editorOpeningId,
            Vector2 editorCastStart,
            Vector2 editorCastEnd,
            Vector2 editorPlayerBodySize,
            Vector2 editorMarginSize,
            float editorFloorTopY,
            float editorMinimumVerticalClearance,
            Collider2D editorSupportingFloor)
        {
            openingId = editorOpeningId;
            castStart = editorCastStart;
            castEnd = editorCastEnd;
            playerBodySize = editorPlayerBodySize;
            marginSize = editorMarginSize;
            floorTopY = editorFloorTopY;
            minimumVerticalClearance = editorMinimumVerticalClearance;
            supportingFloor = editorSupportingFloor;
        }
#endif

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.95f, 1f, 0.75f);
            Gizmos.DrawLine(castStart, castEnd);
            Gizmos.DrawWireCube(castStart, marginSize);
            Gizmos.DrawWireCube(castEnd, marginSize);
        }
    }
}
