using UnityEngine;

[RequireComponent(typeof(TimedVisualSwitcher))]
public class CombPickup : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.R;

    [Header("Параметры")]
    public float pickupDistance = 3.5f;
    public float holdDistance = 1.8f;
    public float smoothSpeed = 18f;
    public float rotationSpeed = 200f;
    public float ghostAlpha = 0.45f;

    [Header("Тестовый режим")]
    [Tooltip("Разрешить поднимать соту даже если она не полная (для тестов)")]
    public bool allowPickupWithoutFullFill = false;

    public TimedVisualSwitcher visual;
    private Rigidbody rb;
    private Collider col;
    private Transform camTransform;

    public bool isHeld = false;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;
    private CombSlot lookingAtSlot;
    public Vector3 OriginalScale => originalLocalScale;


    public GameObject ghost;

    void Awake()
    {
        visual = GetComponent<TimedVisualSwitcher>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;
    }

    void Start()
    {
        camTransform = Camera.main?.transform;
        if (!camTransform) camTransform = FindObjectOfType<AudioListener>()?.transform;
        if (!camTransform) { Debug.LogError("Камера не найдена!"); enabled = false; return; }

        CreateGhost();
    }

    void CreateGhost()
    {
        ghost = Instantiate(gameObject);
        ghost.name = "[GHOST] " + name;

        Destroy(ghost.GetComponent<CombPickup>());
        if (ghost.TryGetComponent(out Rigidbody r)) Destroy(r);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) Destroy(c);

        foreach (var rend in ghost.GetComponentsInChildren<Renderer>())
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(1f, 1f, 0.7f, ghostAlpha);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        ghost.transform.SetParent(originalParent);
        ghost.transform.localPosition = originalLocalPos;
        ghost.transform.localRotation = originalLocalRot;
        ghost.transform.localScale = originalLocalScale;

        ghost.SetActive(false);
    }

    void Update()
    {
        if (!camTransform) return;

        if (Input.GetKeyDown(pickupKey))
        {
            if (isHeld)
            {
                CheckSlot();

                if (lookingAtSlot)
                {
                    lookingAtSlot.PlaceComb(this);
                    return;  // ← ЭТО ЗАКРЫВАЕТ ИФФЫ, предотвращает Drop()
                }

                Drop();
                return;
            }
            else
            {
                TryPickup();
            }
        }

        if (isHeld)
        {
            CheckSlot();
            HandleRotation();
        }
    }

    
    RaycastHit? RaycastFirstNonSelf(Transform from, Vector3 dir, float maxDistance)
    {
        RaycastHit[] hits = Physics.RaycastAll(from.position, dir, maxDistance);
        if (hits == null || hits.Length == 0) return null;

        // сортируем по расстоянию
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // пропускаем если попали в саму соту (ее любой дочерний трансформ)
            if ((hit.transform.IsChildOf(transform) || hit.transform == transform) && isHeld)
                continue;

            // пропускаем ghost (если он есть)
            if (ghost && (hit.transform.IsChildOf(ghost.transform) || hit.transform == ghost.transform))
                continue;

            // найден подходящий хит
            return hit;
        }

        return null;
    }
    
    void CheckSlot()
    {
        lookingAtSlot = null;

        var hitNullable = RaycastFirstNonSelf(camTransform, camTransform.forward, pickupDistance);
        if (!hitNullable.HasValue) return;

        var hit = hitNullable.Value;

        // Ищем CombSlot на хитнутом коллайдере или в родителях
        var slot = hit.collider.GetComponent<CombSlot>();
        if (slot == null)
            slot = hit.collider.GetComponentInParent<CombSlot>();

        lookingAtSlot = slot;
    }



    void LateUpdate()
    {
        if (!isHeld || !camTransform) return;

        // Только позиция — без принудительного поворота!
        Vector3 targetPos = camTransform.position + camTransform.forward * holdDistance;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // ← УБРАЛИ ПРИНУДИТЕЛЬНОЕ ВРАЩЕНИЕ К КАМЕРЕ — теперь Q/R работает!
    }

    void TryPickup()
    {
        if (!IsLookingAtMe()) return;

        bool isFull = visual.GetCurrentStage() >= visual.visualStagePrefabs.Length - 1;

        if (!isFull && !allowPickupWithoutFullFill)
        {
            Debug.Log($"Сота не полная! Стадия: {visual.GetCurrentStage() + 1}/{visual.visualStagePrefabs.Length}");
            return;
        }

        if (!isFull) Debug.Log("Взята неполная сота (тестовый режим)");

        Pickup();
    }

    bool IsLookingAtMe()
    {
        Ray ray = new Ray(camTransform.position, camTransform.forward);

        var hitNullable = RaycastFirstNonSelf(camTransform, camTransform.forward, pickupDistance);
        if (!hitNullable.HasValue) return false;

        RaycastHit hit = hitNullable.Value;

        // Проверяем — попали ли именно в эту соту (в её Mesh/Collider)
        // Мы пропускали свои коллайдеры выше, но здесь хотим удостовериться, что цель — этот объект.
        if (hit.collider != null)
        {
            // если хит попал в объект, который является ребёнком этой соты => true
            if (hit.transform.IsChildOf(transform) || hit.transform == transform) return true;

            // или если попал ровно в этот объект
            if (hit.transform.gameObject == gameObject) return true;
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
            rb.constraints = RigidbodyConstraints.None; // <- важно!
        }

        if (col) col.isTrigger = true;
        visual.enabled = false;
        if (ghost) ghost.SetActive(true);

        Debug.Log("Сота взята в руки!");
    }

    public void Drop()
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
            rb.constraints = RigidbodyConstraints.None; // или что было изначально
        }

        if (col) col.isTrigger = false;
        visual.enabled = true;
        if (ghost) ghost.SetActive(false);

        Debug.Log("Сота возвращена на место");
    }

    void HandleRotation()
{
    float input = 0f;
    if (Input.GetKey(rotateLeftKey)) input -= 1f;
    if (Input.GetKey(rotateRightKey)) input += 1f;

    if (input != 0f)
    {
        // Локальная ось Y объекта, чтобы вращение было визуально ожидаемым
        transform.Rotate(Vector3.up, input * rotationSpeed * Time.deltaTime, Space.Self);
    }
}


    void OnDrawGizmosSelected()
    {
        if (camTransform)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(camTransform.position, camTransform.forward * pickupDistance);
        }
    }
}