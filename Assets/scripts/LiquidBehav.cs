using UnityEngine;

public class HoneySimulation : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float gravity = 9.8f; // Effective gravity magnitude
    public float damping = 5f; // Higher value means faster response (less viscous), lower means more viscous
    public float inputSmoothing = 10f; // Smoothing factor for input acceleration and rotation to reduce jerkiness
    public float maxTiltAngle = 30f; // Maximum tilt angle in degrees to prevent excessive tilting

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] modifiedVertices;

    private Vector3 prevPosition;
    private Vector3 prevVelocity;
    private Quaternion prevRotation;
    private float currentOmega = 0f;
    private float currentK = 0f;
    private Vector3 currentTilt = Vector3.zero; // Represents the tilt coefficients for x and z

    private Vector3 smoothedHorizontalAccel = Vector3.zero;
    private float smoothedTargetOmega = 0f;

    private Rigidbody parentRigidbody;
    private Transform parentTransform;

    private bool initializedSmoothing = false;

    void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("HoneySimulation requires a MeshFilter component.");
            return;
        }

        // Create a runtime copy of the mesh to modify
        mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.sharedMesh = mesh;

        baseVertices = mesh.vertices;
        modifiedVertices = new Vector3[baseVertices.Length];
    }

    void Start()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            Debug.LogError("HoneySimulation must be attached to a child object of the pickable jar.");
            return;
        }

        parentRigidbody = parentTransform.GetComponent<Rigidbody>();
        if (parentRigidbody == null)
        {
            Debug.LogError("Parent object must have a Rigidbody component.");
            return;
        }

        prevPosition = parentTransform.position;
        prevVelocity = Vector3.zero;
        prevRotation = parentTransform.rotation;
    }

    void Update()
    {
        if (parentRigidbody == null || !parentRigidbody.isKinematic) return; // Only simulate when held (kinematic)

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Compute linear acceleration in world space
        Vector3 currPosition = parentTransform.position;
        Vector3 velocity = (currPosition - prevPosition) / dt;
        Vector3 acceleration = (velocity - prevVelocity) / dt;
        prevVelocity = velocity;
        prevPosition = currPosition;

        // Compute angular velocity around world up (since rotation is around local up, which aligns with world up)
        Quaternion currRotation = parentTransform.rotation;
        Quaternion deltaRotation = currRotation * Quaternion.Inverse(prevRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        float sign = Vector3.Dot(axis, parentTransform.up) >= 0 ? 1f : -1f;
        float deltaAngle = sign * angle;
        float targetOmega = (deltaAngle * Mathf.Deg2Rad) / dt;
        prevRotation = currRotation;

        // Smooth the target omega to reduce jerkiness
        if (!initializedSmoothing)
        {
            smoothedTargetOmega = targetOmega;
            initializedSmoothing = true;
        }
        smoothedTargetOmega = Mathf.Lerp(smoothedTargetOmega, targetOmega, inputSmoothing * dt);

        // Damp omega
        currentOmega = Mathf.Lerp(currentOmega, smoothedTargetOmega, damping * dt);

        // Compute target parabolic coefficient k = omega^2 / (2g)
        float targetK = (currentOmega * currentOmega) / (2f * gravity);
        currentK = Mathf.Lerp(currentK, targetK, damping * dt);

        // Compute acceleration in local space
        Vector3 localAccel = parentTransform.InverseTransformDirection(acceleration);

        // Horizontal components (exclude local y)
        Vector3 horizontalAccel = localAccel - Vector3.Dot(localAccel, Vector3.up) * Vector3.up;

        // Smooth horizontal acceleration to reduce jerkiness
        smoothedHorizontalAccel = Vector3.Lerp(smoothedHorizontalAccel, horizontalAccel, inputSmoothing * dt);

        // Target tilt: -horizontalAccel / g (approximation for tan(theta))
        Vector3 targetTilt = -smoothedHorizontalAccel / gravity;

        // Clamp magnitude to max tilt
        float tiltMagnitude = targetTilt.magnitude;
        float maxTan = Mathf.Tan(maxTiltAngle * Mathf.Deg2Rad);
        if (tiltMagnitude > maxTan)
        {
            targetTilt = targetTilt.normalized * maxTan;
        }

        // Damp tilt
        currentTilt = Vector3.Lerp(currentTilt, targetTilt, damping * dt);

        // Apply deformations to vertices
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vert = baseVertices[i];

            // Parabolic deformation: dh = k * (x^2 + z^2)
            float rSquared = vert.x * vert.x + vert.z * vert.z;
            float dh = currentK * rSquared;

            // Tilt deformation: dh += tilt.x * vert.x + tilt.z * vert.z (plane tilt)
            dh += currentTilt.x * vert.x + currentTilt.z * vert.z;

            vert.y += dh;
            modifiedVertices[i] = vert;
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals(); // Update normals for proper lighting/shading
    }
}