using System.Collections.Generic;
using UnityEngine;

public class AnimatorLidController : MonoBehaviour, IInteractive
{
    [Header("Интерактивные настройки")]
    public bool isInteractive = true;
    
    public bool IsInteractive => isInteractive;
    
    [Header("Ссылка на Animator")]
    public Animator lidAnimator;
    
    [Header("Параметры аниматора")]
    public string openParameter = "IsOpen";
    
    [Header("Зависимые объекты")]
    public List<CombChoise> combChoiseObjects = new List<CombChoise>();
    
    [Header("Настройки блокировки")]
    public bool preventCloseWhenAnyCombLifted = true;
    
    private bool isOpen = false;
    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isHovered = false;
    
    [Header("Визуальные эффекты")]
    public Material hoverMaterial;
    public Material blockedMaterial;
    public AudioClip blockedSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Автопоиск аниматора
        if (lidAnimator == null)
            lidAnimator = GetComponent<Animator>();
            
        if (lidAnimator == null)
            Debug.LogError("Animator не найден на объекте!");
        
        // Получаем рендерер для смены материала при наведении
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
        
        // Автодобавление AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Проверяем начальное состояние
        UpdateLidStateBasedOnCombs();
    }
    
    void Update()
    {
        // Автоматически открываем крышку если какая-то сота поднята
        if (preventCloseWhenAnyCombLifted && !isOpen && IsAnyCombLifted())
        {
            ForceOpenLid();
        }
        
        // Обновляем визуальное состояние
        UpdateVisualState();
    }
    
    void OnMouseDown()
    {
        ToggleLid();
    }
    
    void ToggleLid()
    {
        if (lidAnimator == null) return;
        
        // Проверяем можно ли закрыть крышку
        if (isOpen && preventCloseWhenAnyCombLifted && IsAnyCombLifted())
        {
            // Нельзя закрыть крышку - есть поднятые соты
            PlayBlockedSound();
            ShowBlockedFeedback();
            Debug.Log("Нельзя закрыть крышку: есть поднятые соты!");
            return;
        }
        
        isOpen = !isOpen;
        lidAnimator.SetBool(openParameter, isOpen);
        
        Debug.Log(isOpen ? "Крышка открывается" : "Крышка закрывается");
    }
    
    // Проверяет, поднята ли хотя бы одна сота
    public bool IsAnyCombLifted()
    {
        foreach (CombChoise comb in combChoiseObjects)
        {
            if (comb != null && comb.IsLifted())
            {
                return true;
            }
        }
        return false;
    }
    
    // Получает список всех поднятых сот
    public List<CombChoise> GetLiftedCombs()
    {
        List<CombChoise> liftedCombs = new List<CombChoise>();
        foreach (CombChoise comb in combChoiseObjects)
        {
            if (comb != null && comb.IsLifted())
            {
                liftedCombs.Add(comb);
            }
        }
        return liftedCombs;
    }
    
    // Принудительно открывает крышку
    public void ForceOpenLid()
    {
        if (lidAnimator != null && !isOpen)
        {
            isOpen = true;
            lidAnimator.SetBool(openParameter, true);
            Debug.Log("Крышка принудительно открыта (есть поднятые соты)");
        }
    }
    
    // Принудительно закрывает крышку (игнорируя проверки)
    public void ForceCloseLid()
    {
        if (lidAnimator != null && isOpen)
        {
            isOpen = false;
            lidAnimator.SetBool(openParameter, false);
            Debug.Log("Крышка принудительно закрыта");
        }
    }
    
    // Обновляет состояние крышки на основе сот
    public void UpdateLidStateBasedOnCombs()
    {
        if (preventCloseWhenAnyCombLifted && IsAnyCombLifted() && !isOpen)
        {
            ForceOpenLid();
        }
    }
    
    // Визуальная обратная связь при блокировке
    void ShowBlockedFeedback()
    {
        if (objectRenderer != null && blockedMaterial != null)
        {
            objectRenderer.material = blockedMaterial;
            Invoke("RestoreOriginalMaterial", 0.5f);
        }
    }
    
    void RestoreOriginalMaterial()
    {
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = isHovered && hoverMaterial != null ? hoverMaterial : originalMaterial;
        }
    }
    
    void PlayBlockedSound()
    {
        if (blockedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(blockedSound);
        }
    }
    
    void UpdateVisualState()
    {
        // Можно добавить визуальные индикаторы состояния
        // Например, менять цвет в зависимости от возможности закрытия
    }
    
    // Реализация методов интерфейса IInteractive
    public void OnCursorEnter()
    {
        if (!isInteractive) return;
        
        isHovered = true;
        
        // Меняем материал при наведении
        if (objectRenderer != null && hoverMaterial != null)
        {
            objectRenderer.material = hoverMaterial;
        }
        
        Debug.Log("Курсор наведен на крышку: " + gameObject.name);
    }
    
    public void OnCursorExit()
    {
        if (!isInteractive) return;
        
        isHovered = false;
        
        // Возвращаем оригинальный материал
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
        
        Debug.Log("Курсор ушел с крышки: " + gameObject.name);
    }
    
    // Для внешнего управления
    public void SetLidState(bool open)
    {
        if (lidAnimator != null && isOpen != open)
        {
            // Проверяем можно ли закрыть
            if (!open && preventCloseWhenAnyCombLifted && IsAnyCombLifted())
            {
                Debug.LogWarning("Нельзя закрыть крышку: есть поднятые соты!");
                return;
            }
            
            isOpen = open;
            lidAnimator.SetBool(openParameter, isOpen);
        }
    }
    
    // Добавление соты в список отслеживания
    public void AddCombChoise(CombChoise comb)
    {
        if (comb != null && !combChoiseObjects.Contains(comb))
        {
            combChoiseObjects.Add(comb);
        }
    }
    
    // Удаление соты из списка отслеживания
    public void RemoveCombChoise(CombChoise comb)
    {
        if (combChoiseObjects.Contains(comb))
        {
            combChoiseObjects.Remove(comb);
        }
    }
    
    // Очистка списка сот
    public void ClearCombChoiseList()
    {
        combChoiseObjects.Clear();
    }
    
    // Включить/выключить интерактивность
    public void SetInteractive(bool interactive)
    {
        isInteractive = interactive;
        
        if (!interactive && isHovered)
        {
            OnCursorExit();
        }
    }
}