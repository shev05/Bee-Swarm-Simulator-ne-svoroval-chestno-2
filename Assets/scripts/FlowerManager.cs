using UnityEngine;
using System.Collections.Generic;

public class FlowerManager : MonoBehaviour
{
    public List<Transform> flowers = new List<Transform>();

    public Transform GetRandomFlower()
    {
        if (flowers.Count == 0) return null;
        return flowers[Random.Range(0, flowers.Count)];
    }

    public bool HasFlowers()
    {
        return flowers.Count > 0;
    }

    // Для отладки - визуализация в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Transform flower in flowers)
        {
            if (flower != null)
                Gizmos.DrawWireSphere(flower.position, 0.3f);
        }
    }
}