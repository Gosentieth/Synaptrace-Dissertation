using Synaptrace.Player;
using UnityEngine;

namespace Synaptrace.Systems
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(1.5f, 1.2f, -10f);
        [SerializeField] private float followSpeed = 6f;
        [SerializeField] private Vector2 minPosition = new Vector2(-8.5f, -1f);
        [SerializeField] private Vector2 maxPosition = new Vector2(11.5f, 4f);

        private void LateUpdate()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                target = player != null ? player.transform : null;
            }

            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);
            desiredPosition.z = offset.z;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }

        public void Configure(Transform newTarget, Vector3 newOffset, float newFollowSpeed, Vector2 newMinPosition, Vector2 newMaxPosition)
        {
            target = newTarget;
            offset = newOffset;
            followSpeed = newFollowSpeed;
            minPosition = newMinPosition;
            maxPosition = newMaxPosition;
        }
    }
}
