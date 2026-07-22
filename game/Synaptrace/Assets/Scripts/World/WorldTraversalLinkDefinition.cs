using UnityEngine;

namespace Synaptrace.World
{
    [DisallowMultipleComponent]
    public sealed class WorldTraversalLinkDefinition : MonoBehaviour
    {
        [SerializeField] private string linkId = string.Empty;
        [SerializeField] private bool mandatory = true;
        [SerializeField] private bool optional;
        [SerializeField] private BoxCollider2D takeoffCollider;
        [SerializeField] private BoxCollider2D landingCollider;
        [SerializeField] private float expectedGap = 1.2f;
        [SerializeField] private float expectedLandingRise = 0.325f;
        [SerializeField] private float minimumTakeoffWidth = 1.8f;
        [SerializeField] private float minimumLandingWidth = 2.2f;

        public string LinkId => linkId;
        public bool Mandatory => mandatory;
        public bool Optional => optional;
        public BoxCollider2D TakeoffCollider => takeoffCollider;
        public BoxCollider2D LandingCollider => landingCollider;
        public float ExpectedGap => expectedGap;
        public float ExpectedLandingRise => expectedLandingRise;
        public float MinimumTakeoffWidth => minimumTakeoffWidth;
        public float MinimumLandingWidth => minimumLandingWidth;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string editorLinkId,
            bool editorMandatory,
            bool editorOptional,
            BoxCollider2D editorTakeoffCollider,
            BoxCollider2D editorLandingCollider,
            float editorExpectedGap,
            float editorExpectedLandingRise,
            float editorMinimumTakeoffWidth,
            float editorMinimumLandingWidth)
        {
            linkId = editorLinkId;
            mandatory = editorMandatory;
            optional = editorOptional;
            takeoffCollider = editorTakeoffCollider;
            landingCollider = editorLandingCollider;
            expectedGap = editorExpectedGap;
            expectedLandingRise = editorExpectedLandingRise;
            minimumTakeoffWidth = editorMinimumTakeoffWidth;
            minimumLandingWidth = editorMinimumLandingWidth;
        }
#endif

        private void OnDrawGizmos()
        {
            if (takeoffCollider == null || landingCollider == null)
            {
                return;
            }

            Gizmos.color = optional ? new Color(0.55f, 0.75f, 1f, 0.85f) : new Color(1f, 0.9f, 0.25f, 0.85f);
            Vector3 start = new Vector3(takeoffCollider.bounds.max.x, takeoffCollider.bounds.max.y + 0.1f, 0f);
            Vector3 end = new Vector3(landingCollider.bounds.min.x, landingCollider.bounds.max.y + 0.1f, 0f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.08f);
            Gizmos.DrawWireSphere(end, 0.08f);
        }
    }
}
