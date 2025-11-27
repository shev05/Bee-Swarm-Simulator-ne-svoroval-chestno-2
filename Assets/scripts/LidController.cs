using UnityEngine;

public class LidControllerWithAnim : MonoBehaviour
{
    [Header("Ссылка на аниматор")]
    public Animator lidAnimator;
    
    private bool isOpen = false;
    
    void Start()
    {
        // Автопоиск аниматора если не назначен
        if (lidAnimator == null)
            lidAnimator = GetComponent<Animator>();
    }
    
    void OnMouseDown()
    {
        ToggleLid();
    }
    
    void ToggleLid()
    {
        isOpen = !isOpen;
        
        if (lidAnimator != null)
        {
            lidAnimator.SetBool("IsOpen", isOpen);
        }
    }
}