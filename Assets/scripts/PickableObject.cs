using UnityEngine;

public class PickableObject : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.R;

    [Header("Settings")]
    public float pickupDistance = 3.0f;
    public float holdDistance = 1.6f;
    public float smoothSpeed = 14f;
    public float rotationSpeed = 160f;

    [Header("Ghost")]
    public bool useGhost = true;
    public float ghostAlpha = 0.45f;

    [Header("Other")]
    public bool canPickup = true;

    private Rigidbody rb;
    private Collider col;
    private Transform cam;

    private bool isHeld = false;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;

    private GameObject ghost;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;
    }

    void Start()
    {
        cam = Camera.main.transform;

        if (useGhost)
            CreateGhost();
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (isHeld) Drop();
            else TryPickup();
        }

        if (isHeld)
            HandleRotation();
    }

    void LateUpdate()
    {
        if (!isHeld || !cam) return;

        Vector3 targetPos = cam.position + cam.forward * holdDistance;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    // -----------------------------------------------------------
    // PICKUP LOGIC
    // -----------------------------------------------------------
    void TryPickup()
    {
        if (!canPickup) return;
        if (!IsLookingAtMe()) return;

        Pickup();
    }

    bool IsLookingAtMe()
    {
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            // Если попали в ghost, игнорируем
            if (ghost && hit.transform.IsChildOf(ghost.transform))
                return false;

            return hit.transform.IsChildOf(transform);
        }
        return false;
    }

    void Pickup()
    {
        isHeld = true;

        transform.SetParent(null);

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (col) col.isTrigger = true;

        if (ghost) ghost.SetActive(true);
    }

    void Drop()
    {
        isHeld = false;

        transform.SetParent(originalParent);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
        transform.localScale = originalLocalScale;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (col) col.isTrigger = false;

        if (ghost) ghost.SetActive(false);
    }

    // -----------------------------------------------------------
    // ROTATION HANDLING
    // -----------------------------------------------------------
    void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(rotateLeftKey)) input -= 1f;
        if (Input.GetKey(rotateRightKey)) input += 1f;

        if (input != 0)
        {
            transform.Rotate(Vector3.up, input * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // -----------------------------------------------------------
    // GHOST CREATION
    // -----------------------------------------------------------
    void CreateGhost()
    {
        ghost = new GameObject("[GHOST] " + name);
        ghost.transform.SetParent(originalParent);
        ghost.transform.localPosition = originalLocalPos;
        ghost.transform.localRotation = originalLocalRot;
        ghost.transform.localScale = originalLocalScale;

        // Копируем только mesh-части, кроме объектов с тегом NoGhost
        CopyMeshesRecursively(transform, ghost.transform);

        ghost.SetActive(false);
    }

    void CopyMeshesRecursively(Transform src, Transform dst)
    {
        // игнорируем объекты, помеченные NoGhost
        if (src.CompareTag("NoGhost"))
            return;

        // копия объекта без компонентов
        GameObject clone = new GameObject(src.name);
        clone.transform.SetParent(dst);
        clone.transform.localPosition = src.localPosition;
        clone.transform.localRotation = src.localRotation;
        clone.transform.localScale = src.localScale;

        // если есть Renderer — клонируем
        if (src.TryGetComponent(out Renderer rend))
        {
            var newRend = clone.AddComponent<MeshRenderer>();
            var newFilter = clone.AddComponent<MeshFilter>();

            if (src.TryGetComponent(out MeshFilter mf))
                newFilter.sharedMesh = mf.sharedMesh;

            // создаём прозрачный материал
            Material m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Mode", 2);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
            m.color = new Color(1f, 1f, 1f, ghostAlpha);

            newRend.material = m;
            newRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // рекурсивно копируем детей
        foreach (Transform child in src)
            CopyMeshesRecursively(child, clone.transform);
    }

}
