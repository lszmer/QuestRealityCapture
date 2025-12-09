#nullable enable

using UnityEngine;

namespace RealityLog.UI.Coverage
{
    /// <summary>
    /// Controls the large fog sphere that surrounds the player and clears fog
    /// as the user looks around.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FogSphereController : MonoBehaviour
    {
        private static readonly int FogMaskId = Shader.PropertyToID("_MaskTex");
        private static readonly int BottomClearLatitudeId = Shader.PropertyToID("_BottomClearLatitude");
        private static readonly int BottomClearEnabledId = Shader.PropertyToID("_BottomClearEnabled");
        private static readonly int TopClearLatitudeId = Shader.PropertyToID("_TopClearLatitude");
        private static readonly int TopClearEnabledId = Shader.PropertyToID("_TopClearEnabled");

        [Header("References")]
        [SerializeField] private Transform headTransform = default!;
        [SerializeField] private Transform sphereTransform = default!;
        [SerializeField] private Renderer? sphereRenderer = null;
        [SerializeField] private Material fogMaterial = default!;
        [SerializeField] private bool instantiateMaterial = true;
        [SerializeField] private ComputeShader fogMaskCompute = default!;
        [SerializeField] private bool startVisible = true;

        [Header("Fog Parameters")]
        [SerializeField, Min(0.5f)] private float sphereRadius = 2.0f;
        [SerializeField] private Vector2Int maskResolution = new(256, 128);
        [SerializeField, Range(1f, 45f)] private float brushAngleDegrees = 15f;
        [SerializeField, Range(0.5f, 30f)] private float brushFeatherDegrees = 5f;
        [SerializeField, Range(0f, 90f)] private float bottomClearAngleDegrees = 0f;
        [SerializeField, Range(0f, 90f)] private float topClearAngleDegrees = 0f;
        [SerializeField] private Vector2 viewOffsetDegrees = Vector2.zero;
        [SerializeField, Min(0.01f)] private float maskUpdateInterval = 0.03f;

        private RenderTexture? maskTexture;
        private Material? runtimeMaterial;
        private int kernelId;
        private int bottomClearKernelId;
        private float nextUpdateTime;
        private bool sphereVisible;
        private bool forceVisible;

        private void Awake()
        {
            // Try to find CenterEyeAnchor first
            if (headTransform == null)
            {
                GameObject cameraRig = GameObject.Find("OVRCameraRig");
                if (cameraRig != null)
                {
                    Transform centerEye = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
                    if (centerEye != null)
                    {
                        headTransform = centerEye;
                    }
                }
            }
            
            // Fallback to Camera.main
            if (headTransform == null)
            {
                var mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    headTransform = mainCamera.transform;
                }
            }
            
            // Ensure this GameObject is parented to CenterEyeAnchor if it's FogSphereHUD
            if (gameObject.name == "FogSphereHUD" && headTransform != null && transform.parent != headTransform)
            {
                transform.SetParent(headTransform);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            if (sphereTransform == null)
            {
                sphereTransform = transform;
            }

            if (sphereRenderer == null)
            {
                sphereRenderer = sphereTransform.GetComponent<Renderer>();
            }

            if (sphereRenderer == null)
            {
                Debug.LogError("FogSphereController requires a MeshRenderer reference.", this);
                enabled = false;
                return;
            }

            SetupMaterialInstance();

            if (fogMaskCompute == null)
            {
                Debug.LogError("FogSphereController requires a compute shader reference.", this);
                enabled = false;
                return;
            }

            maskResolution = new Vector2Int(
                Mathf.Max(16, maskResolution.x),
                Mathf.Max(8, maskResolution.y));

            maskTexture = new RenderTexture(maskResolution.x, maskResolution.y, 0, RenderTextureFormat.R8)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "FogSphereMask"
            };
            maskTexture.Create();

            kernelId = fogMaskCompute.FindKernel("CSMain");
            bottomClearKernelId = fogMaskCompute.FindKernel("CSBottomClear");
            fogMaskCompute.SetInts("_TextureSize", maskResolution.x, maskResolution.y);
            fogMaskCompute.SetTexture(kernelId, "Result", maskTexture);
            fogMaskCompute.SetTexture(bottomClearKernelId, "Result", maskTexture);

            ApplyMaskToMaterial();
            ResetFog();
            UpdateSphereScale();
            SetSphereVisible(startVisible);
        }

        private void LateUpdate()
        {
            if (headTransform == null || maskTexture == null)
            {
                return;
            }

            FollowHeadTransform();

            if (!sphereVisible)
            {
                return;
            }

            if (Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            StampCurrentViewDirection();
            nextUpdateTime = Time.unscaledTime + maskUpdateInterval;
        }

        private void FollowHeadTransform()
        {
            sphereTransform.position = headTransform.position;
            // Keep the fog sphere aligned with world axes so it does not rotate with the head.
            sphereTransform.rotation = Quaternion.identity;
        }

        private void UpdateSphereScale()
        {
            if (sphereTransform != null)
            {
                var diameter = sphereRadius * 2f;
                sphereTransform.localScale = Vector3.one * diameter;
            }
        }

        private void StampCurrentViewDirection()
        {
            if (fogMaskCompute == null || maskTexture == null || headTransform == null)
            {
                return;
            }

            var forward = headTransform.forward.normalized;
            forward = ApplyViewOffset(forward);

            fogMaskCompute.SetFloats("_ForwardDir", forward.x, forward.y, forward.z);

            var outerRad = Mathf.Deg2Rad * Mathf.Max(brushAngleDegrees, 0.1f);
            var featherRad = Mathf.Min(Mathf.Deg2Rad * brushFeatherDegrees, outerRad * 0.95f);
            var innerRad = Mathf.Max(outerRad - featherRad, 0.005f);

            fogMaskCompute.SetFloat("_CosOuterAngle", Mathf.Cos(outerRad));
            fogMaskCompute.SetFloat("_CosInnerAngle", Mathf.Cos(innerRad));

            var groupsX = Mathf.CeilToInt(maskResolution.x / 8f);
            var groupsY = Mathf.CeilToInt(maskResolution.y / 8f);
            fogMaskCompute.Dispatch(kernelId, groupsX, groupsY, 1);
        }

        private Vector3 ApplyViewOffset(Vector3 direction)
        {
            if (viewOffsetDegrees == Vector2.zero)
            {
                return direction;
            }

            // Apply pitch offset around a horizontal axis perpendicular to the forward direction
            // This keeps the vertical offset constant in world space regardless of head rotation
            var horizontalAxis = Vector3.Cross(direction, Vector3.up).normalized;
            if (horizontalAxis.sqrMagnitude < 0.01f)
            {
                // If direction is parallel to up/down, use world right as fallback
                horizontalAxis = Vector3.right;
            }
            var pitch = Quaternion.AngleAxis(viewOffsetDegrees.y, horizontalAxis);
            
            // Apply yaw offset around world up to keep horizontal offset constant
            var yaw = Quaternion.AngleAxis(viewOffsetDegrees.x, Vector3.up);
            
            // Apply pitch first (vertical), then yaw (horizontal)
            return yaw * pitch * direction;
        }

        private void SetupMaterialInstance()
        {
            if (sphereRenderer == null)
            {
                return;
            }

            if (fogMaterial == null)
            {
                fogMaterial = instantiateMaterial
                    ? sphereRenderer.material
                    : sphereRenderer.sharedMaterial;
            }
            else if (instantiateMaterial)
            {
                runtimeMaterial = new Material(fogMaterial);
                fogMaterial = runtimeMaterial;
            }

            if (instantiateMaterial)
            {
                sphereRenderer.material = fogMaterial;
            }
            else
            {
                sphereRenderer.sharedMaterial = fogMaterial;
            }
        }

        private void ApplyMaskToMaterial()
        {
            if (fogMaterial != null && maskTexture != null)
            {
                fogMaterial.SetTexture(FogMaskId, maskTexture);
            }
            UpdateClearShaderProperties();
        }

        /// <summary>
        /// Returns whether the fog sphere visual is currently visible.
        /// </summary>
        public bool IsSphereVisible => sphereVisible;
        public bool IsForcedVisible => forceVisible;

        /// <summary>
        /// Toggle the fog sphere visibility state.
        /// </summary>
        public void ToggleSphereVisible()
        {
            if (forceVisible)
            {
                // Ignore toggles while forced visible (e.g., during recording).
                SetSphereVisible(true);
                return;
            }

            SetSphereVisible(!sphereVisible);
        }

        /// <summary>
        /// Explicitly show or hide the fog sphere mesh and related effects.
        /// </summary>
        public void SetSphereVisible(bool visible)
        {
            sphereVisible = forceVisible ? true : visible;
            ApplyVisibility();
        }

        /// <summary>
        /// Lock the sphere visible (e.g., while recording). Passing false releases the lock.
        /// </summary>
        public void SetForcedVisible(bool forced)
        {
            forceVisible = forced;

            if (forceVisible)
            {
                SetSphereVisible(true);
            }
        }

        private void ApplyVisibility()
        {
            if (sphereTransform != null)
            {
                sphereTransform.gameObject.SetActive(sphereVisible);
            }

            if (sphereRenderer != null)
            {
                sphereRenderer.enabled = sphereVisible;
            }
        }

        private void UpdateClearShaderProperties()
        {
            if (fogMaterial == null)
            {
                return;
            }

            if (bottomClearAngleDegrees <= 0f)
            {
                fogMaterial.SetInt(BottomClearEnabledId, 0);
            }
            else
            {
                fogMaterial.SetInt(BottomClearEnabledId, 1);
                var latitudeRadians = Mathf.Deg2Rad * (-90f + Mathf.Clamp(bottomClearAngleDegrees, 0f, 90f));
                fogMaterial.SetFloat(BottomClearLatitudeId, latitudeRadians);
            }

            if (topClearAngleDegrees <= 0f)
            {
                fogMaterial.SetInt(TopClearEnabledId, 0);
            }
            else
            {
                fogMaterial.SetInt(TopClearEnabledId, 1);
                var latitudeRadians = Mathf.Deg2Rad * (90f - Mathf.Clamp(topClearAngleDegrees, 0f, 90f));
                fogMaterial.SetFloat(TopClearLatitudeId, latitudeRadians);
            }
        }

        public void ResetFog()
        {
            if (maskTexture == null)
            {
                return;
            }

            var active = RenderTexture.active;
            RenderTexture.active = maskTexture;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = active;
            ApplyClearMaskSettings();

            nextUpdateTime = 0f;
        }

        private void OnDestroy()
        {
            if (maskTexture != null)
            {
                maskTexture.Release();
                maskTexture = null;
            }

            if (instantiateMaterial && runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            sphereRadius = Mathf.Max(0.5f, sphereRadius);
            brushAngleDegrees = Mathf.Clamp(brushAngleDegrees, 1f, 90f);
            brushFeatherDegrees = Mathf.Clamp(brushFeatherDegrees, 0.1f, brushAngleDegrees);
            bottomClearAngleDegrees = Mathf.Clamp(bottomClearAngleDegrees, 0f, 90f);
            topClearAngleDegrees = Mathf.Clamp(topClearAngleDegrees, 0f, 90f);
            maskResolution = new Vector2Int(
                Mathf.Max(16, maskResolution.x),
                Mathf.Max(8, maskResolution.y));

            if (sphereTransform != null)
            {
                UpdateSphereScale();
            }
        }
#endif

        private void ApplyClearMaskSettings()
        {
            // The bottom clear mask is now handled in the fragment shader based on UV coordinates
            // We still need to set the compute shader properties for CSMain to skip the bottom area
            if (fogMaskCompute == null || maskTexture == null)
            {
                return;
            }

            if (bottomClearAngleDegrees <= 0f)
            {
                fogMaskCompute.SetInt("_BottomClearEnabled", 0);
            }
            else
            {
                fogMaskCompute.SetInt("_BottomClearEnabled", 1);
                var latitudeRadians = Mathf.Deg2Rad * (-90f + Mathf.Clamp(bottomClearAngleDegrees, 0f, 90f));
                fogMaskCompute.SetFloat("_BottomClearLatitude", latitudeRadians);
            }

            if (topClearAngleDegrees <= 0f)
            {
                fogMaskCompute.SetInt("_TopClearEnabled", 0);
            }
            else
            {
                fogMaskCompute.SetInt("_TopClearEnabled", 1);
                var latitudeRadians = Mathf.Deg2Rad * (90f - Mathf.Clamp(topClearAngleDegrees, 0f, 90f));
                fogMaskCompute.SetFloat("_TopClearLatitude", latitudeRadians);
            }
            
            // Update shader material properties as well
            UpdateClearShaderProperties();
        }
    }
}

