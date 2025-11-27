using UnityEngine;

public class CombChoise : MonoBehaviour, IInteractive
{
    [Header("Интерактивные настройки")]
    public bool isInteractive = true;
    
    [Header("Настройки подъема")]
    public float liftHeight = 2f;
    public float liftSpeed = 2f;
    public bool startLowered = false;
    
    [Header("Ссылка на крышку")]
    public AnimatorLidController linkedLid;
    
    [Header("Звуки")]
    public AudioClip liftSound;
    public AudioClip lowerSound;
    
    [Header("Визуальные эффекты")]
    public ParticleSystem liftParticles;
    public Material hoverMaterial;
    
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isLifted = false;
    private AudioSource audioSource;
    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isHovered = false;
    
    public bool IsInteractive => isInteractive;
    
    void Start()
    {
        originalPosition = transform.position;
        
        if (startLowered)
        {
            transform.position = originalPosition;
            isLifted = false;
        }
        else
        {
            transform.position = originalPosition + Vector3.up * liftHeight;
            isLifted = true;
            
            // Уведомляем крышку о начальном состоянии
            NotifyLidAboutState();
        }
        
        targetPosition = transform.position;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
        
        if (liftParticles != null)
        {
            liftParticles.Stop();
        }
    }
    
    void OnMouseDown()
    {
        if (isInteractive)
        {
            ToggleLift();
        }
    }
    
    void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, liftSpeed * Time.deltaTime);
        }
    }
    
    void ToggleLift()
    {
        if (isLifted)
        {
            Lower();
        }
        else
        {
            Lift();
        }
    }
    
    void Lift()
    {
        targetPosition = originalPosition + Vector3.up * liftHeight;
        isLifted = true;
        
        if (liftSound != null)
            audioSource.PlayOneShot(liftSound);
            
        if (liftParticles != null)
        {
            liftParticles.Play();
        }
        
        // Уведомляем крышку об изменении состояния
        NotifyLidAboutState();
            
        Debug.Log("Сота поднимается");
    }
    
    void Lower()
    {
        targetPosition = originalPosition;
        isLifted = false;
        
        if (lowerSound != null)
            audioSource.PlayOneShot(lowerSound);
        
        // Уведомляем крышку об изменении состояния
        NotifyLidAboutState();
            
        Debug.Log("Сота опускается");
    }
    
    // Уведомляет крышку об изменении состояния
    void NotifyLidAboutState()
    {
        if (linkedLid != null)
        {
            // Крышка сама проверит состояние всех сот
            linkedLid.UpdateLidStateBasedOnCombs();
        }
    }
    
    // Остальные методы остаются без изменений...
    public void OnCursorEnter()
    {
        if (!isInteractive) return;
        
        isHovered = true;
        
        if (objectRenderer != null && hoverMaterial != null)
        {
            objectRenderer.material = hoverMaterial;
        }
        
        Debug.Log("Курсор наведен на соту: " + gameObject.name);
    }
    
    public void OnCursorExit()
    {
        if (!isInteractive) return;
        
        isHovered = false;
        
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
        
        Debug.Log("Курсор ушел с соты: " + gameObject.name);
    }
    
    public void SetLiftedState(bool lifted)
    {
        if (lifted && !isLifted)
        {
            Lift();
        }
        else if (!lifted && isLifted)
        {
            Lower();
        }
    }
    
    public bool IsLifted()
    {
        return isLifted;
    }
    
    public void SetInteractive(bool interactive)
    {
        isInteractive = interactive;
        
        if (!interactive && isHovered)
        {
            OnCursorExit();
        }
    }
}