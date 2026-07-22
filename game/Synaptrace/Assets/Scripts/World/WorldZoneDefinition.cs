using UnityEngine;

namespace Synaptrace.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WorldZoneDefinition : MonoBehaviour
    {
        [SerializeField] private string regionId = string.Empty;
        [SerializeField] private string zoneId = string.Empty;
        [SerializeField] private bool optional;

        public string RegionId => regionId;
        public string ZoneId => zoneId;
        public bool Optional => optional;

        private void Reset()
        {
            BoxCollider2D zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string editorRegionId, string editorZoneId, bool editorOptional)
        {
            regionId = editorRegionId;
            zoneId = editorZoneId;
            optional = editorOptional;

            BoxCollider2D zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }
#endif

        private void OnDrawGizmos()
        {
            Gizmos.color = optional ? new Color(0.6f, 0.8f, 1f, 0.28f) : new Color(0.1f, 1f, 0.7f, 0.28f);
            BoxCollider2D zoneCollider = GetComponent<BoxCollider2D>();

            if (zoneCollider == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(zoneCollider.offset, zoneCollider.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
