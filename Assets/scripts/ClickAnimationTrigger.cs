using UnityEngine;

public class ClickAnimationTrigger : MonoBehaviour
{
    [Header("Анимация на этом объекте")]
    public Animator selfAnimator;
    public string selfStateName;   // <-- имя state в Animator

    private void OnMouseDown()
    {
        PlaySafe(selfAnimator, selfStateName);
    }

    private void PlaySafe(Animator animator, string stateName)
    {
        if (animator == null)
        {
            Debug.LogError($"❌ Animator");
            return;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogError($"❌ Animator '{animator.gameObject.name}' не содержит state '{stateName}'");
            return;
        };

        // Проверяем что такой state существует в текущем контроллере
        if (!AnimatorHasState(animator, 0, stateName))
        {
            Debug.LogError($"❌ Animator '{animator.gameObject.name}' не содержит state '{stateName}'");
            return;
        }
        animator.Play(stateName, 0, 0f);
    }

    private bool AnimatorHasState(Animator animator, int layer, string state)
    {
        return animator.HasState(layer, Animator.StringToHash(state));
    }
}