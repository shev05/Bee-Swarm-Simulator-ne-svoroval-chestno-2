using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeeAI : MonoBehaviour
{
    public enum BeeState { Idle, FlyToFlower, Collecting, ReturnToHive, EnterHive, ExitHive, FollowPath }
    public BeeState currentState = BeeState.Idle;

    [Header("References")]
    public Transform hiveEntrance;
    public Transform hiveExit;
    public Transform[] hiveEntryPoints; // МАССИВ точек ПУТИ к улью
    private FlowerManager flowerManager;
    
    [Header("Movement Settings")]
    public float flightSpeed = 8f;
    public float rotationSpeed = 5f;
    public float idleWanderRadius = 1.5f;
    public float minHeight = 1f;
    public float maxHeight = 3f;

    [Header("State Timers")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float collectionTime = 3f;

    [Header("Collision Settings")]
    public List<GameObject> passThroughObjects = new List<GameObject>();
    public LayerMask obstacleLayers = -1;

    private Transform targetFlower;
    private Vector3 targetPosition;
    private float stateTimer = 0f;
    private float currentIdleTime = 0f;
    
    // Для следования по пути
    private int currentPathIndex = 0;
    private bool isFollowingPath = false;

    // Для плавного движения
    private Vector3 currentVelocity;
    private float smoothTime = 0.1f;

    // Кэш коллайдеров объектов сквозь которые можно летать
    private HashSet<Collider> passThroughColliders = new HashSet<Collider>();

    // Для системы заполнения сот
    private bool hasCollectedNectar = false;

    void Start()
    {
        flowerManager = FindObjectOfType<FlowerManager>();
        if (flowerManager == null)
        {
            Debug.LogError("FlowerManager not found!");
        }

        if (hiveExit == null) hiveExit = hiveEntrance;
        
        // Если нет точек пути, используем вход как единственную точку
        if (hiveEntryPoints == null || hiveEntryPoints.Length == 0)
        {
            hiveEntryPoints = new Transform[] { hiveEntrance };
        }

        InitializePassThroughColliders();
        
        // Игнорируем коллизии с другими пчелами
        IgnoreBeeCollisions();
        
        transform.position = GetRandomHivePosition();
        SwitchState(BeeState.Idle);
    }

    void InitializePassThroughColliders()
    {
        passThroughColliders.Clear();
        foreach (GameObject obj in passThroughObjects)
        {
            if (obj != null)
            {
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    passThroughColliders.Add(collider);
                }
            }
        }
    }

    void IgnoreBeeCollisions()
    {
        // Находим всех пчел на сцене и игнорируем коллизии между ними
        BeeAI[] allBees = FindObjectsOfType<BeeAI>();
        Collider myCollider = GetComponent<Collider>();
        
        foreach (BeeAI otherBee in allBees)
        {
            if (otherBee != this)
            {
                Collider otherCollider = otherBee.GetComponent<Collider>();
                if (myCollider != null && otherCollider != null)
                {
                    Physics.IgnoreCollision(myCollider, otherCollider);
                }
            }
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case BeeState.Idle:
                IdleBehaviour();
                break;
            case BeeState.ExitHive:
                ExitHiveBehaviour();
                break;
            case BeeState.FlyToFlower:
                FlyToFlowerBehaviour();
                break;
            case BeeState.Collecting:
                CollectingBehaviour();
                break;
            case BeeState.ReturnToHive:
                ReturnToHiveBehaviour();
                break;
            case BeeState.FollowPath:
                FollowPathBehaviour();
                break;
            case BeeState.EnterHive:
                EnterHiveBehaviour();
                break;
        }
    }

    void SwitchState(BeeState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        currentVelocity = Vector3.zero;

        switch (newState)
        {
            case BeeState.Idle:
                currentIdleTime = Random.Range(minIdleTime, maxIdleTime);
                targetPosition = GetRandomHivePosition();
                hasCollectedNectar = false; // Сбрасываем статус сбора при выходе из улья
                break;

            case BeeState.ExitHive:
                targetPosition = hiveExit.position;
                break;

            case BeeState.FlyToFlower:
                targetFlower = flowerManager.GetRandomFlower();
                if (targetFlower != null)
                {
                    targetPosition = GetFlightPosition(targetFlower.position);
                }
                else
                {
                    SwitchState(BeeState.ReturnToHive);
                }
                break;

            case BeeState.Collecting:
                stateTimer = collectionTime;
                break;

            case BeeState.ReturnToHive:
                // Начинаем следовать по пути точек
                StartFollowingPath();
                break;

            case BeeState.FollowPath:
                // Устанавливаем первую точку пути как цель
                if (hiveEntryPoints.Length > 0)
                {
                    targetPosition = hiveEntryPoints[currentPathIndex].position;
                }
                break;

            case BeeState.EnterHive:
                targetPosition = hiveEntrance.position;
                break;
        }
    }

    void StartFollowingPath()
    {
        currentPathIndex = 0;
        isFollowingPath = true;
        if (hiveEntryPoints.Length > 0)
        {
            SwitchState(BeeState.FollowPath);
        }
        else
        {
            SwitchState(BeeState.EnterHive);
        }
    }

    void FollowPathBehaviour()
    {
        if (!isFollowingPath || hiveEntryPoints.Length == 0) return;

        // Двигаемся к текущей точке пути
        MoveToTarget(targetPosition);

        // Проверяем достижение текущей точки
        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            // Переходим к следующей точке
            currentPathIndex++;
            
            if (currentPathIndex < hiveEntryPoints.Length)
            {
                // Есть еще точки - летим к следующей
                targetPosition = hiveEntryPoints[currentPathIndex].position;
            }
            else
            {
                // Достигли конца пути - влетаем в улей
                isFollowingPath = false;
                SwitchState(BeeState.EnterHive);
            }
        }
    }

    void IdleBehaviour()
    {
        stateTimer += Time.deltaTime;
        
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, flightSpeed * 0.3f * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || stateTimer >= currentIdleTime)
        {
            if (Random.Range(0f, 1f) < 0.4f && flowerManager.HasFlowers())
            {
                SwitchState(BeeState.ExitHive);
            }
            else
            {
                targetPosition = GetRandomHivePosition();
                stateTimer = 0f;
            }
        }
    }

    void ExitHiveBehaviour()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, flightSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SwitchState(BeeState.FlyToFlower);
        }
    }

    void FlyToFlowerBehaviour()
    {
        if (targetFlower == null)
        {
            SwitchState(BeeState.ReturnToHive);
            return;
        }

        MoveToTarget(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            SwitchState(BeeState.Collecting);
        }
    }

    void CollectingBehaviour()
    {
        stateTimer -= Time.deltaTime;
        
        // Минимальное покачивание на месте
        transform.position += new Vector3(
            Mathf.Sin(Time.time * 5f) * 0.002f,
            Mathf.Cos(Time.time * 4f) * 0.002f,
            Mathf.Sin(Time.time * 3f) * 0.002f
        );

        if (stateTimer <= 0)
        {
            hasCollectedNectar = true; // Пчела успешно собрала нектар
            SwitchState(BeeState.ReturnToHive);
        }
    }

    void ReturnToHiveBehaviour()
    {
        // Этот метод теперь не используется для движения
        // Вся логика перемещения в FollowPathBehaviour
    }

    void EnterHiveBehaviour()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, flightSpeed * 1.5f * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            // УВЕДОМЛЯЕМ СИСТЕМУ СОТ О ВОЗВРАЩЕНИИ ПЧЕЛЫ С НЕКТАРОМ
            if (hasCollectedNectar)
            {
                NotifyHoneyCollection();
            }
            
            transform.position = GetRandomHivePosition();
            SwitchState(BeeState.Idle);
        }
    }

    void MoveToTarget(Vector3 target)
    {
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, target, ref currentVelocity, smoothTime, flightSpeed);
        transform.position = newPosition;
        
        if (currentVelocity.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    Vector3 GetRandomHivePosition()
    {
        Vector3 randomPos = hiveEntrance.position + Random.insideUnitSphere * idleWanderRadius;
        randomPos.y = Mathf.Clamp(randomPos.y, hiveEntrance.position.y - 0.3f, hiveEntrance.position.y + 0.3f);
        return randomPos;
    }

    Vector3 GetFlightPosition(Vector3 target)
    {
        Vector3 offset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(minHeight, maxHeight),
            Random.Range(-0.3f, 0.3f)
        );
        return target + offset;
    }

    // Обработка триггеров
    void OnTriggerEnter(Collider other)
    {
        // Игнорируем объекты сквозь которые можно летать
        if (IsPassThroughObject(other))
        {
            return;
        }

        if (currentState == BeeState.FlyToFlower && other.CompareTag("Flower"))
        {
            if (targetFlower != null && other.transform == targetFlower)
            {
                SwitchState(BeeState.Collecting);
            }
        }
    }

    bool IsPassThroughObject(Collider collider)
    {
        return passThroughColliders.Contains(collider);
    }

    // === СИСТЕМА ЗАПОЛНЕНИЯ СОТ ===
    
    void NotifyHoneyCollection()
    {
        // Уведомляем все соты о возвращении пчелы с нектаром
        TimedVisualSwitcher[] allCombs = FindObjectsOfType<TimedVisualSwitcher>();
        foreach (TimedVisualSwitcher comb in allCombs)
        {
            comb.AddBeeReturn();
        }
        
        Debug.Log($"Пчела вернулась с нектаром! Уведомлено {allCombs.Length} сот");
        
        // Сбрасываем статус сбора
        hasCollectedNectar = false;
    }

    // Методы для управления списком объектов в runtime
    public void AddPassThroughObject(GameObject obj)
    {
        if (obj != null && !passThroughObjects.Contains(obj))
        {
            passThroughObjects.Add(obj);
            UpdatePassThroughCollidersForObject(obj);
        }
    }

    public void RemovePassThroughObject(GameObject obj)
    {
        if (passThroughObjects.Contains(obj))
        {
            passThroughObjects.Remove(obj);
            RemovePassThroughCollidersForObject(obj);
        }
    }

    public void ClearPassThroughObjects()
    {
        passThroughObjects.Clear();
        passThroughColliders.Clear();
    }

    private void UpdatePassThroughCollidersForObject(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            passThroughColliders.Add(collider);
        }
    }

    private void RemovePassThroughCollidersForObject(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            passThroughColliders.Remove(collider);
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        // Визуализация выхода (СИНИЙ)
        if (hiveExit != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hiveExit.position, 0.2f);
        }

        // Визуализация точек пути (КРАСНЫЙ с линиями)
        if (hiveEntryPoints != null && hiveEntryPoints.Length > 0)
        {
            Gizmos.color = Color.red;
            
            // Рисуем точки и соединяем их линиями
            for (int i = 0; i < hiveEntryPoints.Length; i++)
            {
                if (hiveEntryPoints[i] != null)
                {
                    // Точка
                    Gizmos.DrawWireSphere(hiveEntryPoints[i].position, 0.15f);
                    
                    // Номер точки
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(hiveEntryPoints[i].position + Vector3.up * 0.2f, (i + 1).ToString());
                    #endif
                    
                    // Линия к следующей точке
                    if (i < hiveEntryPoints.Length - 1 && hiveEntryPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(hiveEntryPoints[i].position, hiveEntryPoints[i + 1].position);
                    }
                }
            }
            
            // Линия от последней точки к улью
            if (hiveEntrance != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(hiveEntryPoints[hiveEntryPoints.Length - 1].position, hiveEntrance.position);
            }
        }

        // Визуализация общей зоны улья (ЗЕЛЕНЫЙ)
        if (hiveEntrance != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hiveEntrance.position, idleWanderRadius);
        }
    }

    // Методы для отладки
    [ContextMenu("Принудительно уведомить о сборе меда")]
    public void ForceNotifyHoneyCollection()
    {
        NotifyHoneyCollection();
    }

    [ContextMenu("Переключить в состояние сбора")]
    public void SetHasCollectedNectar()
    {
        hasCollectedNectar = true;
    }
}