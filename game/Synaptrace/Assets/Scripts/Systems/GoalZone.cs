using Synaptrace.Core;
using Synaptrace.Player;
using UnityEngine;

namespace Synaptrace.Systems
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoalZone : MonoBehaviour
    {
        private void Reset()
        {
            Collider2D goalCollider = GetComponent<Collider2D>();
            goalCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null || LevelManager.Instance == null)
            {
                return;
            }

            LevelManager.Instance.CompleteLevel();
        }
    }
}
