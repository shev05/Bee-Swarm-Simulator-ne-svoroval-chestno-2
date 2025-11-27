using UnityEngine;
using System.Collections;

public class HoneycombPickup : MonoBehaviour
{
    [Header("Настройки переноса")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupDistance = 3f;
    public float holdDistance = 2f;
    public float smoothSpeed = 10f;
    
    [Header("Настройки вращения")]
    public bool allowRotation = true;
    public float rotationSpeed = 2f;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.R;
    
    [Header("Эффекты")]
    public ParticleSystem pickupParticles;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    
    // Ссылка на TimedVisualSwitcher если есть
    private TimedVisualSwitcher visualSwitcher;
    private Rigidbody rb;
    private Collider objectCollider;
    private AudioSource audioSource;
    
    // Переменные переноса
    private bool isCarrying = false;
    private Transform carriedObject;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    // Оригинальные настройки
    private bool wasKinematic;
    private bool usedGravity;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        // Получаем компоненты
        visualSwitcher = GetComponent<TimedVisualSwitcher>();
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        
        // Сохраняем оригинальные позицию и поворот
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Создаем AudioSource если нет
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        HandlePickupInput();
        
        if (isCarrying)
        {
            UpdateCarriedObject();
            HandleRotationInput();
            
            // Проверяем не вышел ли объект из зоны досягаемости
            if (ShouldDropObject())
            {
                DropObject();
            }
        }
    }
    
    void HandlePickupInput()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (!isCarrying)
            {
                TryPickup();
            }
            else
            {
                DropObject();
            }
        }
    }
    
    void TryPickup()
    {
        // Проверяем что игрок смотрит на этот объект и он достаточно близко
        if (IsPlayerLookingAtObject() && IsWithinPickupDistance())
        {
            PickupObject();
        }
    }
    
    bool IsPlayerLookingAtObject()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return false;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            return hit.collider.gameObject == gameObject;
        }
        
        return false;
    }
    
    bool IsWithinPickupDistance()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return false;
        
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        return distance <= pickupDistance;
    }
    
    void PickupObject()
    {
        isCarrying = true;
        carriedObject = transform;
        
        // Сохраняем оригинальные настройки физики
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            usedGravity = rb.useGravity;
            
            // Отключаем физику при переносе
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Отключаем коллайдер или делаем его триггером
        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }
        
        // Останавливаем визуальное переключение если есть
        if (visualSwitcher != null)
        {
            visualSwitcher.enabled = false;
        }
        
        // Эффекты
        PlayPickupEffects();
    }
    
    void UpdateCarriedObject()
    {
        if (carriedObject == null) return;
        
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;
        
        // Вычисляем целевую позицию перед камерой
        targetPosition = playerCamera.transform.position + 
                        playerCamera.transform.forward * holdDistance;
        
        // Плавное перемещение
        carriedObject.position = Vector3.Lerp(
            carriedObject.position, 
            targetPosition, 
            smoothSpeed * Time.deltaTime
        );
        
        // Сохраняем оригинальный поворот или поворачиваем к камере
        if (!allowRotation)
        {
            targetRotation = Quaternion.LookRotation(
                playerCamera.transform.forward, 
                Vector3.up
            );
            
            carriedObject.rotation = Quaternion.Slerp(
                carriedObject.rotation,
                targetRotation,
                smoothSpeed * Time.deltaTime
            );
        }
    }
    
    void HandleRotationInput()
    {
        if (!allowRotation) return;
        
        float rotationInput = 0f;
        
        if (Input.GetKey(rotateLeftKey))
            rotationInput = -1f;
        else if (Input.GetKey(rotateRightKey))
            rotationInput = 1f;
        
        if (rotationInput != 0f)
        {
            carriedObject.Rotate(
                Vector3.up, 
                rotationInput * rotationSpeed * Time.deltaTime * 100f, 
                Space.World
            );
        }
    }
    
    bool ShouldDropObject()
    {
        if (carriedObject == null) return true;
        
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return true;
        
        float distance = Vector3.Distance(carriedObject.position, playerCamera.transform.position);
        return distance > pickupDistance * 1.5f;
    }
    
    public void DropObject()
    {
        if (!isCarrying) return;
        
        isCarrying = false;
        
        // Восстанавливаем оригинальную позицию и поворот
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        // МАСШТАБ НЕ ТРОГАЕМ - оставляем как есть
        
        // Восстанавливаем физику
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = usedGravity;
        }
        
        // Восстанавливаем коллайдер
        if (objectCollider != null)
        {
            objectCollider.isTrigger = false;
        }
        
        // Включаем обратно визуальное переключение
        if (visualSwitcher != null)
        {
            visualSwitcher.enabled = true;
        }
        
        // Эффекты
        PlayDropEffects();
        
        carriedObject = null;
    }
    
    void PlayPickupEffects()
    {
        if (pickupParticles != null)
            pickupParticles.Play();
            
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);
    }
    
    void PlayDropEffects()
    {
        if (dropSound != null && audioSource != null)
            audioSource.PlayOneShot(dropSound);
    }
    
    // Метод для принудительного сохранения текущей позиции как оригинальной
    public void SetCurrentAsOriginal()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }
    
    // Методы для внешнего управления
    public bool IsCarrying()
    {
        return isCarrying;
    }
    
    public void ForceDrop()
    {
        DropObject();
    }
    
    // Визуальная подсказка в редакторе
    void OnDrawGizmosSelected()
    {
        // Показываем дистанцию поднятия
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
        
        // Показываем оригинальную позицию
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.1f);
        
        // Если несем объект - показываем точку удержания
        if (isCarrying && Camera.main != null)
        {
            Vector3 holdPoint = Camera.main.transform.position + 
                               Camera.main.transform.forward * holdDistance;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPoint, 0.1f);
            Gizmos.DrawLine(Camera.main.transform.position, holdPoint);
        }
    }
}