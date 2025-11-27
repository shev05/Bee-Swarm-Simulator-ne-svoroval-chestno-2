using UnityEngine;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    [Header("UI элементы курсора")]
    public Image cursorImage;
    public Sprite normalCursorSprite;
    public Sprite interactiveCursorSprite;
    
    [Header("Настройки размера")]
    public float normalScale = 1f;
    public float interactiveScale = 1.5f;
    
    [Header("Настройки взаимодействия")]
    public LayerMask interactiveLayers = -1;
    
    private Camera mainCamera;
    private RectTransform cursorRect;
    
    void Start()
    {
        mainCamera = Camera.main;
        cursorRect = cursorImage.GetComponent<RectTransform>();
        
        // Скрываем системный курсор
        Cursor.visible = false;
        
        SetNormalCursor();
    }
    
    void Update()
    {
        UpdateCursorPosition();
        CheckObjectUnderCursor();
    }
    
    void UpdateCursorPosition()
    {
        // Преобразуем позицию мыши в позицию UI
        Vector2 mousePos = Input.mousePosition;
        cursorRect.position = mousePos;
    }
    
    void CheckObjectUnderCursor()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, interactiveLayers))
        {
            if (IsObjectInteractive(hit.collider.gameObject))
            {
                SetInteractiveCursor();
                return;
            }
        }
        
        SetNormalCursor();
    }
    
    bool IsObjectInteractive(GameObject obj)
    {
        return obj.GetComponent<AnimatorLidController>() != null ||
               obj.GetComponent<IInteractive>() != null ||
               obj.CompareTag("Interactive");
    }
    
    void SetNormalCursor()
    {
        cursorImage.sprite = normalCursorSprite;
        cursorRect.localScale = Vector3.one * normalScale;
    }
    
    void SetInteractiveCursor()
    {
        cursorImage.sprite = interactiveCursorSprite;
        cursorRect.localScale = Vector3.one * interactiveScale;
    }
}