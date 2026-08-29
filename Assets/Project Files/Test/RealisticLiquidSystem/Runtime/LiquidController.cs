using UnityEngine;

namespace RealisticLiquidSystem
{
    /// Handles only the pouring mechanism.
    /// No filling, idle state, fill-level control, or liquid-volume animation.
    /// The liquid/stream is activated while the container is tilted past
    /// the configured pour angle.
    public class LiquidController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform container;
        [SerializeField] private GameObject pourStream;

        [Header("Outlet")]
        [Tooltip("Liquid local outlet position relative to the container pivot.")]
        [SerializeField] private Vector3 outletLocalPosition = new Vector3(0f, 0.45f, 0.45f);

        [Tooltip("Liquid local outlet direction relative to the container. Usually Vector3.forward.")]
        [SerializeField] private Vector3 outletLocalDirection = Vector3.forward;

        [Header("Pouring")]
        [Tooltip("Direction the liquid leaves the container. Usually local outlet.forward.")]
        [SerializeField] private Vector3 localPourDirection = Vector3.forward;

        [Tooltip("How far downward the pour direction must point before pouring starts.")]
        [Range(0f, 1f)]
        [SerializeField] private float pourDownThreshold = 0.35f;

        [SerializeField] private bool autoPourOnTilt = true;

        [Header("Shake")]
        [SerializeField] private bool enableShake = true;
        [SerializeField] private float shakeAmount = 0.01f;
        [SerializeField] private float shakeSpeed = 5f;
        [SerializeField] private float rotationInfluence = 0.015f;
        [SerializeField] private float maxShake = 0.035f;

        private Quaternion lastRotation;
        private Vector3 angularVelocity;
        private float shakeTime;
        private bool isPouring;

        public bool IsPouring => isPouring;

        private void Awake()
        {
            if (container == null)
                container = transform;

            lastRotation = container.rotation;

            SetPourStream(false);
        }

        private void Update()
        {
            if (container == null)
                return;

            CalculateRotationVelocity();

            if (autoPourOnTilt)
            {
                bool shouldPour = IsPouringDirectionDown();
                SetPouring(shouldPour);
            }

            if (isPouring && enableShake)
                UpdateShake();
        }

        // =========================================================
        // POURING
        // =========================================================

        /// <summary>
        /// Checks the actual pour direction against world gravity.
        /// This is independent of the container's world position and works
        /// even when the container starts with an arbitrary rotation.
        /// </summary>
        private bool IsPouringDirectionDown()
        {
            if (container == null)
                return false;

            Vector3 pourDirection;

            if (container != null)
            {
                // The pour point defines the actual local outlet direction.
                pourDirection = container.TransformDirection(outletLocalDirection);
            }
            else
            {
                // Fallback if no pour point is assigned.
                pourDirection =
                    container.TransformDirection(localPourDirection);
            }

            pourDirection.Normalize();

            // World-down is Vector3.down.
            // 1 = pointing straight down.
            // 0 = horizontal.
            // Negative = pointing upward.
            float downwardAmount =
                Vector3.Dot(pourDirection, Vector3.down);

            return downwardAmount >= pourDownThreshold;
        }

        private void SetPouring(bool pouring)
        {
            if (isPouring == pouring)
            {
                UpdatePourStreamTransform();
                return;
            }

            isPouring = pouring;
            SetPourStream(pouring);
            UpdatePourStreamTransform();
        }

        private void SetPourStream(bool visible)
        {
            if (pourStream != null && pourStream.activeSelf != visible)
                pourStream.SetActive(visible);
        }

        private void UpdatePourStreamTransform()
        {
            if (!isPouring || pourStream == null || container == null)
                return;

            Vector3 localDirection =
                outletLocalDirection.sqrMagnitude > 0.0001f
                    ? outletLocalDirection.normalized
                    : localPourDirection.normalized;

            Vector3 worldPosition =
                container.TransformPoint(outletLocalPosition);

            Vector3 worldDirection =
                container.TransformDirection(localDirection).normalized;

            pourStream.transform.position = worldPosition;

            if (worldDirection.sqrMagnitude > 0.0001f)
                pourStream.transform.rotation =
                    Quaternion.LookRotation(worldDirection, container.up);
        }

        // =========================================================
        // ROTATION VELOCITY
        // =========================================================

        private void CalculateRotationVelocity()
        {
            Quaternion currentRotation = container.rotation;

            Quaternion delta =
                currentRotation * Quaternion.Inverse(lastRotation);

            delta.ToAngleAxis(
                out float angle,
                out Vector3 axis
            );

            if (angle > 180f)
                angle -= 360f;

            if (Time.deltaTime > 0.0001f)
            {
                angularVelocity =
                    axis * (angle / Time.deltaTime);
            }

            lastRotation = currentRotation;
        }

        // =========================================================
        // SUBTLE POURING SHAKE
        // =========================================================

        private void UpdateShake()
        {
            if (pourStream == null || !pourStream.activeSelf)
                return;

            shakeTime += Time.deltaTime * shakeSpeed;

            Vector3 localDirection =
                outletLocalDirection.sqrMagnitude > 0.0001f
                    ? outletLocalDirection.normalized
                    : localPourDirection.normalized;

            Vector3 pourDirection =
                container.TransformDirection(localDirection).normalized;

            float downwardAmount =
                Mathf.Clamp01(Vector3.Dot(pourDirection, Vector3.down));

            float tiltAmount =
                Mathf.InverseLerp(
                    pourDownThreshold,
                    1f,
                    downwardAmount
                );

            float rotationShake =
                angularVelocity.magnitude *
                rotationInfluence;

            float totalShake = Mathf.Clamp(
                tiltAmount * shakeAmount + rotationShake,
                0f,
                maxShake
            );

            float waveX =
                Mathf.Sin(shakeTime * 1.7f) *
                totalShake;

            float waveZ =
                Mathf.Cos(shakeTime * 1.35f) *
                totalShake;

            float waveY =
                Mathf.Sin(shakeTime * 2.1f) *
                totalShake *
                0.25f;

            Vector3 position =
                container.TransformPoint(outletLocalPosition);

            position += container.right * waveX;
            position += container.forward * waveZ;
            position += container.up * waveY;

            pourStream.transform.position = position;
        }

        /// Returns true when the current local outlet direction points
        /// sufficiently downward to pour.
        public bool IsPourDirectionDown()
        {
            return IsPouringDirectionDown();
        }

        /// Configure the local outlet without creating or assigning a child Transform.
        public void SetOutlet(Vector3 localPosition, Vector3 localDirection)
        {
            outletLocalPosition = localPosition;
            outletLocalDirection = localDirection;

            if (isPouring)
                UpdatePourStreamTransform();
        }

        /// Returns the current local outlet position in world space.
        public Vector3 GetOutletWorldPosition()
        {
            return container != null
                ? container.TransformPoint(outletLocalPosition)
                : transform.TransformPoint(outletLocalPosition);
        }

        // =========================================================
        // PUBLIC CONTROLS
        // =========================================================

        /// Manually start pouring.
        public void StartPouring()
        {
            SetPouring(true);
        }

        /// Manually stop pouring.
        public void StopPouring()
        {
            SetPouring(false);
        }

        /// Toggle pouring manually.
        public void TogglePouring()
        {
            SetPouring(!isPouring);
        }

        /// Enable/disable automatic tilt-based pouring.
        public void SetAutoPour(bool value)
        {
            autoPourOnTilt = value;
        }
    }
}
