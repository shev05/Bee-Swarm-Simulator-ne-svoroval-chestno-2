using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = -9.81f;
    
    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;
    
    [Header("References")]
    public Transform cameraTransform;
    public CharacterController characterController;
    
    // Приватные переменные
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    
    void Start()
    {
        // Блокируем и скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Если компоненты не назначены в инспекторе, находим их автоматически
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleJump();
    }
    
    void HandleMouseLook()
    {
        // Получаем ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Вращение персонажа по горизонтали (ось Y)
        transform.Rotate(Vector3.up * mouseX);
        
        // Вращение камеры по вертикали (ось X)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
        
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
    
    void HandleMovement()
    {
        // Проверяем, стоит ли персонаж на земле
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Получаем ввод с клавиатуры
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Определяем направление движения относительно поворота персонажа
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        
        // Выбираем скорость (ходьба или бег)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        
        // Двигаем персонажа
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        
        // Применяем гравитацию
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
}