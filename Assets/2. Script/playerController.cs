using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class playerController : MonoBehaviour
{
    private ActionManager inputActions;
    private Rigidbody rb;
    private Animator animator;
    private Vector2 inputWalk;
    private Vector2 playerDirection;

    public GameObject attackHitBox;
    public Transform hitboxController;

    public float moveSpeed = 2f;
    private bool isAttack = false;
    void Awake()
    {
        // inputActions 불러오기
        inputActions = new ActionManager();
    }
    void OnEnable()
    {
        // inputActions 활성화
        inputActions.playerAction.Enable();

        inputActions.playerAction.attack.performed += OnAttack;
    }
    void Start()
    {
        // 프레임 고정
        Application.targetFrameRate = 60;
        // Rigidbody와 Animator 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        
    }
    void OnDisable()
    {
        // inputActions 비활성화
        inputActions.playerAction.Disable();

        inputActions.playerAction.attack.performed -= OnAttack;
    }

    // Update is called once per frame
    void Update()
    {
        // inputWalk에 inputAction으로 받은 입력값 저장
        inputWalk = inputActions.playerAction.walk.ReadValue<Vector2>();

        //히트박스 방향 전환
        Vector3 currentScale = transform.localScale;

        // magnitude로 벡터값 확인
        float inputMagnitude = inputWalk.magnitude;
        animator.SetFloat("playerWalkSpeed", inputMagnitude);
        
        if (!isAttack)
        {
            if (inputMagnitude != 0)
            {
                playerDirection = inputWalk;
                animator.SetFloat("playerDirectionX", playerDirection.x);
                animator.SetFloat("playerDirectionY", playerDirection.y);

                if (inputWalk.x > 0)
                {
                    hitboxController.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else if (inputWalk.x < 0)
                {
                    hitboxController.rotation = Quaternion.Euler(0, -180, 0);
                }
                else if (inputWalk.y != 0)
                {
                    hitboxController.localRotation = Quaternion.Euler(0, -inputWalk.y * 90, 0);
                }
            }
        }
        
        


    }

    void FixedUpdate()
    {

        if (!isAttack)
        {

            Vector3 moveDirection = new Vector3(inputWalk.x, 0f, inputWalk.y);
            Vector3 targetVelocity = moveDirection.normalized * moveSpeed;
            targetVelocity.z *= 1.5f;

            // 현재 Rigidbody의 Y축 속도는 그대로 유지하면서 XZ 평면의 속도만 변경
            // (이렇게 하면 나중에 점프나 중력 같은 Y축 움직임을 추가했을 때 서로 간섭하지 않아.)
            // 만약 Y축 움직임이 전혀 없고 바닥에만 붙어 다닌다면 targetVelocity.y = 0f; 로 해도 돼.
            targetVelocity.y = rb.velocity.y;
            rb.velocity = targetVelocity;
        }
    }
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!isAttack)
        {
            animator.SetTrigger("playerAttack");
        }
        
        isAttack = true;

    }
    public void AttackFinish()
    {
        isAttack = false;
    }

    public void HitboxActive()
    {
        attackHitBox.SetActive(true);
    }
    public void HitboxInactive()
    {
        attackHitBox.SetActive(false);
    }
}

public class Clock
{
    private float saveTime = 0f;

    public void StartTime()
    {
        saveTime = 0f;
    }

    public bool TimeUp(float aimTime)
    {
        saveTime += Time.deltaTime;

        if (saveTime >= aimTime)
        {
            return true;
        }
        return false;
    }
}

