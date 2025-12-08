#nullable enable

using System.Collections;
using UnityEngine;

namespace RealityLog.UI.Coverage
{
    /// <summary>
    /// Helper MonoBehaviour that can be targeted by any UnityEvent to reset the
    /// coverage visualization whenever a recording session begins.
    /// </summary>
    public sealed class CoverageRecordingStartHandler : MonoBehaviour
    {
        [SerializeField] private FogSphereController? fogController;

        private void Awake()
        {
            if (fogController == null)
            {
                fogController = GetComponentInChildren<FogSphereController>(true)
                    ?? GetComponentInParent<FogSphereController>();
            }
        }

        public void OnRecordingStarted()
        {
            var hasTarget = false;

            if (fogController != null)
            {
                fogController.SetForcedVisible(true);  // lock visibility during recording
                fogController.SetSphereVisible(true); // ensure the sphere is shown when recording begins
                fogController.ResetFog();             // clear any previous masking
                StartCoroutine(EnsureSphereVisibleAfterFrame()); // guard against other listeners toggling it off this frame
                hasTarget = true;
            }

            if (!hasTarget)
            {
                Debug.LogWarning($"{nameof(CoverageRecordingStartHandler)} has no fog controller assigned.", this);
            }
        }

        /// <summary>
        /// Re-assert sphere visibility after all same-frame listeners run (e.g., toggle bindings).
        /// </summary>
        private IEnumerator EnsureSphereVisibleAfterFrame()
        {
            yield return null; // wait one frame to let other button listeners complete

            if (fogController != null)
            {
                fogController.SetSphereVisible(true);
            }
        }

        /// <summary>
        /// Release the forced-visible lock when recording stops.
        /// </summary>
        public void OnRecordingStopped()
        {
            fogController?.SetForcedVisible(false);
        }
    }
}

