using UnityEngine;

namespace Synaptrace.World
{
    public enum WorldRouteStatus
    {
        OpenStub,
        Locked,
        Sealed
    }

    [DisallowMultipleComponent]
    public sealed class WorldRouteDefinition : MonoBehaviour
    {
        [SerializeField] private string regionId = string.Empty;
        [SerializeField] private string routeId = string.Empty;
        [SerializeField] private WorldRouteStatus status;
        [SerializeField] private BoxCollider2D routeTrigger;
        [SerializeField] private Collider2D terminalBlocker;

        public string RegionId => regionId;
        public string RouteId => routeId;
        public WorldRouteStatus Status => status;
        public BoxCollider2D RouteTrigger => routeTrigger;
        public Collider2D TerminalBlocker => terminalBlocker;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string editorRegionId,
            string editorRouteId,
            WorldRouteStatus editorStatus,
            BoxCollider2D editorRouteTrigger,
            Collider2D editorTerminalBlocker)
        {
            regionId = editorRegionId;
            routeId = editorRouteId;
            status = editorStatus;
            routeTrigger = editorRouteTrigger;
            terminalBlocker = editorTerminalBlocker;

            if (routeTrigger != null)
            {
                routeTrigger.isTrigger = true;
            }
        }
#endif

        private void OnDrawGizmos()
        {
            if (routeTrigger == null)
            {
                return;
            }

            Gizmos.color = status == WorldRouteStatus.OpenStub
                ? new Color(0.1f, 0.95f, 1f, 0.7f)
                : new Color(1f, 0.25f, 0.35f, 0.7f);
            Gizmos.matrix = routeTrigger.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(routeTrigger.offset, routeTrigger.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
