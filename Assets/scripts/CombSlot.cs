using UnityEngine;

public class CombSlot : MonoBehaviour
{
    [Header("Полупрозрачная геометрия слота (то, что видно до установки)")]
    public Transform slotGeometry;

    [Header("Префаб финальной соты")]
    public GameObject finalPrefab;

    private GameObject finalInstance;

    public void PlaceComb(CombPickup comb)
    {
        // 1. Отключаем оригинал (игроковскую соту)
        comb.gameObject.SetActive(false);
        if (comb.ghost) comb.ghost.SetActive(false);

        // 2. Создаём финальный объект
        finalInstance = Instantiate(finalPrefab, transform);

        // 3. Копируем все transform-параметры именно у геометрии
        finalInstance.transform.SetPositionAndRotation(
            slotGeometry.position,
            slotGeometry.rotation
        );
        finalInstance.transform.localScale = slotGeometry.lossyScale;
        
        Debug.Log("Сота установлена с копированием геометрии слота.");
    }

    private void DisableSlot()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        this.enabled = false;
    }
}