using Synaptrace.Core;
using Synaptrace.Player;
using UnityEngine;

namespace Synaptrace.Systems
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hazard : MonoBehaviour
    {
        [SerializeField] private string hazardName = "Hazard";
        [SerializeField] private bool countsAsHazardHit = true;

        private void Reset()
        {
            Collider2D hazardCollider = GetComponent<Collider2D>();
            hazardCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null || LevelManager.Instance == null)
            {
                return;
            }

            LevelManager.Instance.RegisterPlayerDeath(hazardName, countsAsHazardHit);
        }

        public void Configure(string newHazardName, bool newCountsAsHazardHit)
        {
            hazardName = newHazardName;
            countsAsHazardHit = newCountsAsHazardHit;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string editorHazardName, bool editorCountsAsHazardHit)
        {
            Configure(editorHazardName, editorCountsAsHazardHit);
        }
#endif
    }
}
