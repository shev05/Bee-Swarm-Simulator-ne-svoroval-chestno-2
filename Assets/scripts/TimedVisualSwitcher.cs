using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class TimedVisualSwitcher : MonoBehaviour
{
    [Header("Настройки заполнения сот")]
    public int beesRequiredPerStage = 5; // Сколько пчел должно вернуться для смены стадии
    public bool startOnAwake = true;

    [Header("Визуальные префабы — scale может быть любой!")]
    public GameObject[] visualStagePrefabs = new GameObject[3];

    [Header("Настройки коллайдера")]
    public bool updateCollider = true;
    public ColliderType colliderType = ColliderType.MeshCollider;

    [Header("Эффекты (можно не назначать)")]
    public ParticleSystem transitionParticles;
    public AudioClip transitionSound;

    [Header("События")]
    public UnityEvent onTimerStart;
    public UnityEvent onStageChanged;
    public UnityEvent onAllStagesCompleted;

    public enum ColliderType { MeshCollider, BoxCollider, None }

    // ─────────────────────────────
    private AudioSource audioSource;
    private int currentStage =-1;
    private int beeReturnCount = 0;
    private Coroutine switchingCoroutine;

    private MeshFilter mf;
    private MeshRenderer mr;
    private SkinnedMeshRenderer smr;
    private Mesh originalMesh;

    private Vector3 originalLocalPosition;
    private Vector3 originalWorldScale;
    private Vector3[] prefabScales;

    void Awake()
    {
        // Сразу создаём AudioSource — больше никаких ошибок
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Сохраняем только масштаб из каждого префаба
        prefabScales = new Vector3[visualStagePrefabs.Length];
        for (int i = 0; i < visualStagePrefabs.Length; i++)
        {
            prefabScales[i] = visualStagePrefabs[i] != null
                ? visualStagePrefabs[i].transform.localScale
                : Vector3.one;
        }
    }

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        smr = GetComponent<SkinnedMeshRenderer>();

        // Сохраняем оригинальный меш для восстановления
        if (mf != null) originalMesh = mf.sharedMesh;

        originalLocalPosition = transform.localPosition;
        originalWorldScale = transform.lossyScale;

        // Подписываемся на события пчел
        SubscribeToBeeEvents();

        if (startOnAwake) StartHoneyFilling();
    }

    void SubscribeToBeeEvents()
    {
        // Находим всех пчел на сцене и подписываемся на их возвращения
        BeeAI[] allBees = FindObjectsOfType<BeeAI>();
        foreach (BeeAI bee in allBees)
        {
            // Добавляем компонент-слушатель к каждой пчеле
            BeeReturnListener listener = bee.gameObject.GetComponent<BeeReturnListener>();
            if (listener == null)
            {
                listener = bee.gameObject.AddComponent<BeeReturnListener>();
            }
            listener.OnBeeReturned += OnBeeReturned;
        }

        Debug.Log($"Подписались на {allBees.Length} пчел");
    }

    void OnBeeReturned()
    {
        beeReturnCount++;
        Debug.Log($"Пчела вернулась! Всего возвращений: {beeReturnCount}");

        // Проверяем, нужно ли перейти на следующую стадию
        CheckForStageAdvancement();
    }

    void CheckForStageAdvancement()
    {

        int requiredBees = (currentStage + 2) * beesRequiredPerStage;
        
        if (beeReturnCount >= requiredBees)
        {
            int nextStage = currentStage + 1;
            
            if (nextStage < visualStagePrefabs.Length)
            {
                SwitchToStage(nextStage);
            }
            else if (nextStage == visualStagePrefabs.Length)
            {
                onAllStagesCompleted?.Invoke();
                Debug.Log("Все стадии заполнения завершены!");
            }
        }
    }

    public void StartHoneyFilling()
    {
        beeReturnCount = 0;
        currentStage = -1;
        onTimerStart?.Invoke();
        Debug.Log("Начато заполнение сот");
    }

    void SwitchToStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= visualStagePrefabs.Length) return;
        if (visualStagePrefabs[stageIndex] == null) return;

        currentStage = stageIndex;

        PlayEffects();
        ApplyVisual(stageIndex);

        // ПОЗИЦИЯ ВСЕГДА ОДНА И ТА ЖЕ — НИКАКИХ ПОДЪЁМОВ/ОПУСКАНИЙ
        transform.localPosition = originalLocalPosition;

        onStageChanged?.Invoke();
        
        Debug.Log($"Перешли на стадию {stageIndex + 1}. Возвращений пчел: {beeReturnCount}");
    }

    void ApplyVisual(int stageIndex)
    {
        var prefab = visualStagePrefabs[stageIndex];
        if (prefab == null) return;

        var pMF = prefab.GetComponent<MeshFilter>();
        var pMR = prefab.GetComponent<MeshRenderer>();
        var pSMR = prefab.GetComponent<SkinnedMeshRenderer>();

        Mesh newMesh = null;

        // Меняем меш и материалы
        if (pMF && mf) 
        {
            newMesh = pMF.sharedMesh;
            mf.sharedMesh = newMesh;
        }
        if (pMR && mr) mr.sharedMaterials = pMR.sharedMaterials;
        if (pSMR && smr)
        {
            newMesh = pSMR.sharedMesh;
            smr.sharedMesh = pSMR.sharedMesh;
            smr.sharedMaterials = pSMR.sharedMaterials;
        }

        // ОБНОВЛЯЕМ КОЛЛАЙДЕР
        if (updateCollider && newMesh != null)
        {
            UpdateCollider(newMesh, stageIndex);
        }

        // МАСШТАБ — ровно тот, что ты задал в префабе
        Vector3 targetScale = prefabScales[stageIndex];

        if (transform.parent == null)
        {
            transform.localScale = targetScale;
        }
        else
        {
            // Учитываем масштаб родителя (работает идеально)
            transform.localScale = new Vector3(
                targetScale.x * originalWorldScale.x / transform.parent.lossyScale.x,
                targetScale.y * originalWorldScale.y / transform.parent.lossyScale.y,
                targetScale.z * originalWorldScale.z / transform.parent.lossyScale.z
            );
        }
    }

    void UpdateCollider(Mesh newMesh, int stageIndex)
    {
        switch (colliderType)
        {
            case ColliderType.MeshCollider:
                UpdateMeshCollider(newMesh);
                break;
            case ColliderType.BoxCollider:
                UpdateBoxCollider(newMesh);
                break;
            case ColliderType.None:
                RemoveAllColliders();
                break;
        }
    }

    void UpdateMeshCollider(Mesh newMesh)
    {
        // Получаем или добавляем MeshCollider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        // Обновляем меш в коллайдере
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = newMesh;
        meshCollider.convex = false; // Для статических объектов лучше false

        // Удаляем другие коллайдеры чтобы не было конфликтов
        RemoveOtherColliders(typeof(MeshCollider));
    }

    void UpdateBoxCollider(Mesh newMesh)
    {
        // Получаем или добавляем BoxCollider
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Настраиваем BoxCollider по границам меша
        if (newMesh != null)
        {
            boxCollider.center = newMesh.bounds.center;
            boxCollider.size = newMesh.bounds.size;
        }

        // Удаляем другие коллайдеры
        RemoveOtherColliders(typeof(BoxCollider));
    }

    void RemoveOtherColliders(System.Type keepType)
    {
        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider collider in allColliders)
        {
            if (collider.GetType() != keepType)
            {
                DestroyImmediate(collider);
            }
        }
    }

    void RemoveAllColliders()
    {
        Collider[] allColliders = GetComponents<Collider>();
        foreach (Collider collider in allColliders)
        {
            DestroyImmediate(collider);
        }
    }

    void PlayEffects()
    {
        if (transitionParticles != null) transitionParticles.Play();
        if (transitionSound != null) audioSource.PlayOneShot(transitionSound);
    }

    public void RestoreOriginal()
    {
        transform.localPosition = originalLocalPosition;

        // Восстанавливаем оригинальный меш
        if (mf != null && originalMesh != null)
        {
            mf.sharedMesh = originalMesh;
            
            // Восстанавливаем коллайдер для оригинального меша
            if (updateCollider)
            {
                UpdateCollider(originalMesh, -1);
            }
        }

        // Восстанавливаем масштаб
        if (transform.parent == null)
            transform.localScale = originalWorldScale;
        else
            transform.localScale = new Vector3(
                originalWorldScale.x / transform.parent.lossyScale.x,
                originalWorldScale.y / transform.parent.lossyScale.y,
                originalWorldScale.z / transform.parent.lossyScale.z
            );

        currentStage = -1;
        beeReturnCount = 0;
    }

    public void SkipToStage(int index)
    {
        if (index < 0 || index >= visualStagePrefabs.Length || visualStagePrefabs[index] == null) return;

        currentStage = index;
        beeReturnCount = index * beesRequiredPerStage;

        ApplyVisual(index);
        transform.localPosition = originalLocalPosition;
    }

    public void Restart()
    {
        if (switchingCoroutine != null) StopCoroutine(switchingCoroutine);
        RestoreOriginal();
        StartHoneyFilling();
    }

    public bool IsOnFinalStage()
    {
        int currentStage = GetCurrentStage();
        int maxStage = visualStagePrefabs.Length - 1;
        return currentStage == maxStage;
    }

    // Геттеры
    public int GetCurrentStage() => currentStage;
    public int GetBeeReturnCount() => beeReturnCount;
    public int GetBeesRequiredForNextStage() => (currentStage + 1) * beesRequiredPerStage;
    public float GetFillProgress() 
    {
        if (currentStage >= visualStagePrefabs.Length - 1) return 1f;
        int required = GetBeesRequiredForNextStage();
        return Mathf.Clamp01((float)beeReturnCount / required);
    }

    // Метод для отладки - показывает текущие границы коллайдера
    void OnDrawGizmosSelected()
    {
        if (updateCollider)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }

    // Метод для принудительного добавления возвращений (для тестирования)
    [ContextMenu("Добавить возвращение пчелы")]
    public void AddBeeReturn()
    {
        OnBeeReturned();
    }

    [ContextMenu("Сбросить заполнение")]
    public void ResetFilling()
    {
        Restart();
    }
}

// Дополнительный компонент для прослушивания возвращений пчел
public class BeeReturnListener : MonoBehaviour
{
    public System.Action OnBeeReturned;
    
    private BeeAI beeAI;

    void Start()
    {
        beeAI = GetComponent<BeeAI>();
        if (beeAI != null)
        {
            // Можно добавить логику отслеживания состояния пчелы
            // Пока будем использовать метод AddBeeReturn для тестирования
        }
    }

    // Этот метод будет вызываться из BeeAI когда пчела возвращается в улей
    public void NotifyBeeReturned()
    {
        OnBeeReturned?.Invoke();
    }
}