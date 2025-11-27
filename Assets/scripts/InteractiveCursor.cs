using UnityEngine;
using UnityEngine.EventSystems;

public class InteractiveCursor : MonoBehaviour
{
    [Header("Настройки курсора")]
    public Texture2D normalCursor;      // Обычный курсор
    public Texture2D interactiveCursor; // Курсор при наведении
    public Vector2 normalCursorHotspot = new Vector2(16, 16);
    public Vector2 interactiveCursorHotspot = new Vector2(16, 16);
    
    [Header("Размеры курсора")]
    public Vector2 normalSize = new Vector2(32, 32);
    public Vector2 interactiveSize = new Vector2(48, 48);
    
    [Header("Слои для взаимодействия")]
    public LayerMask interactiveLayers = -1; // Все слои по умолчанию
    
    private Camera mainCamera;
    private bool isOnInteractiveObject = false;
    private IInteractive currentInteractiveObject;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Устанавливаем обычный курсор
        SetNormalCursor();
        
        // Скрываем системный курсор
        Cursor.visible = false;
    }
    
    void Update()
    {
        CheckObjectUnderCursor();
    }
    
    void CheckObjectUnderCursor()
    {
        // Создаем луч из позиции мыши
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Проверяем попадание в объекты на интерактивных слоях
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, interactiveLayers))
        {
            // Проверяем, есть ли у объекта компоненты для взаимодействия
            IInteractive interactive = hit.collider.GetComponent<IInteractive>();
            
            if (interactive != null && interactive.IsInteractive)
            {
                if (!isOnInteractiveObject || currentInteractiveObject != interactive)
                {
                    // Уведомляем предыдущий объект о выходе курсора
                    if (currentInteractiveObject != null)
                    {
                        currentInteractiveObject.OnCursorExit();
                    }
                    
                    // Устанавливаем новый интерактивный объект
                    SetInteractiveCursor();
                    isOnInteractiveObject = true;
                    currentInteractiveObject = interactive;
                    currentInteractiveObject.OnCursorEnter();
                }
                return;
            }
        }
        
        // Если не нашли интерактивный объект
        if (isOnInteractiveObject)
        {
            SetNormalCursor();
            isOnInteractiveObject = false;
            
            // Уведомляем объект о выходе курсора
            if (currentInteractiveObject != null)
            {
                currentInteractiveObject.OnCursorExit();
                currentInteractiveObject = null;
            }
        }
    }
    
    void SetNormalCursor()
    {
        Cursor.SetCursor(normalCursor, normalCursorHotspot, CursorMode.Auto);
    }
    
    void SetInteractiveCursor()
    {
        Cursor.SetCursor(interactiveCursor, interactiveCursorHotspot, CursorMode.Auto);
    }
    
    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            Texture2D currentCursor = isOnInteractiveObject ? interactiveCursor : normalCursor;
            Vector2 currentSize = isOnInteractiveObject ? interactiveSize : normalSize;
            Vector2 cursorPos = Event.current.mousePosition;
            
            // Рисуем курсор
            Rect cursorRect = new Rect(
                cursorPos.x - currentSize.x / 2, 
                cursorPos.y - currentSize.y / 2, 
                currentSize.x, 
                currentSize.y
            );
            
            GUI.DrawTexture(cursorRect, currentCursor);
        }
    }
    
    void OnDestroy()
    {
        // При уничтожении объекта уведомляем текущий интерактивный объект
        if (currentInteractiveObject != null)
        {
            currentInteractiveObject.OnCursorExit();
        }
    }
}