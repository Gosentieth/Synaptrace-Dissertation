using UnityEngine;

namespace Synaptrace.Core
{
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(0.1f, 0.9f, 0.35f, 0.85f);

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1.2f, 0f));
            Gizmos.DrawLine(transform.position + Vector3.down * 0.6f, transform.position + Vector3.up * 0.6f);
        }
    }
}
