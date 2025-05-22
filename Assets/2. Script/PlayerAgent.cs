using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayerMLAgent : Agent
{
    private playerController controller; // 기존 playerController 참조
    private Rigidbody rb;
    private Animator animator;
    private Transform hitboxController; // playerController의 hitboxController 참조

    // 에피소드당 최대 스텝 (선택 사항, Inspector에서 설정 가능)
    // public int maxStepsPerEpisode = 5000;
    // private int currentStep = 0;

    public override void Initialize()
    {
        controller = GetComponent<playerController>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (controller != null)
        {
            hitboxController = controller.hitboxController;
            // controller.moveSpeed 와 같은 값도 필요하다면 여기서 가져오거나 직접 설정할 수 있습니다.
        }
        else
        {
            Debug.LogError("PlayerController를 찾을 수 없습니다. PlayerMLAgent와 같은 GameObject에 있는지 확인해주세요.");
        }
    }

    public override void OnEpisodeBegin()
    {
        // 에피소드 시작 시 호출될 로직
        // 예: 플레이어 위치, 보스 위치, 체력 등 초기화
        // transform.localPosition = new Vector3(0, 0.5f, 0); // 예시 시작 위치
        // if (controller != null)
        // {
        //    controller.isAttack = false; // 공격 상태 초기화
        //    // 필요하다면 animator.ResetTrigger("playerAttack"); 등 애니메이션 상태도 초기화
        // }
        // currentStep = 0; // 스텝 카운터 초기화
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === 관찰 요소 정의 (다음 단계에서 상세화) ===
        // 예시:
        // 1. 플레이어 자신의 정보
        //    - 위치 (X, Z) relative to some anchor or absolute
        //    - 현재 속도 (X, Z) (rb.velocity)
        //    - 현재 바라보는 방향 (hitboxController.forward or localRotation)
        //    - 공격 가능 상태 (controller.isAttack ? 0f : 1f)
        // sensor.AddObservation(transform.localPosition);
        // sensor.AddObservation(rb.velocity.x);
        // sensor.AddObservation(rb.velocity.z);
        // sensor.AddObservation(hitboxController.forward); // 또는 각도
        // sensor.AddObservation(controller.isAttack ? 0f : 1f); // 공격 중이면 0, 아니면 1

        // 2. 보스 정보
        //    - 보스 위치 (X, Z)
        //    - 플레이어로부터 보스까지의 상대적 위치/방향 벡터
        //    - 보스 체력 (정규화된 값)
        //    - 보스의 현재 상태/행동 (예: 공격 준비 중, 이동 중 등) - 가능하다면
        // GameObject boss = GameObject.FindGameObjectWithTag("Boss"); // 예시
        // if (boss != null)
        // {
        //    sensor.AddObservation(boss.transform.localPosition);
        //    sensor.AddObservation(boss.transform.localPosition - transform.localPosition);
        //    // sensor.AddObservation(boss.GetComponent<BossScript>().currentHealth / boss.GetComponent<BossScript>().maxHealth);
        // }

        // 3. 기타 환경 정보
        //    - 벽이나 장애물과의 거리 (Raycast 사용 가능)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 에이전트가 결정한 행동을 실행
        // 이 Agent가 활성화 되어 있을 때는 playerController의 Update/FixedUpdate는 사용자 입력 처리를 하지 않는다고 가정

        // currentStep++; // 스텝 카운터 증가
        // if (maxStepsPerEpisode > 0 && currentStep >= maxStepsPerEpisode)
        // {
        //    // 최대 스텝 도달 시 에피소드 종료 (시간 초과)
        //    SetReward(-1.0f); // 예시: 시간 초과 시 음의 보상
        //    EndEpisode();
        // }

        // 연속 행동: X축 이동, Z축 이동
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        // 이산 행동: 공격 (0: 안함, 1: 함)
        int attackAction = actions.DiscreteActions[0];

        // --- 이동 처리 ---
        if (controller != null && !controller.isAttack) // playerController의 isAttack 상태를 직접 참조
        {
            Vector3 moveDirection = new Vector3(moveX, 0f, moveZ);
            // playerController의 moveSpeed 사용
            Vector3 targetVelocity = moveDirection.normalized * controller.moveSpeed;
            // 기존 playerController의 FixedUpdate 로직 적용
            targetVelocity.z *= 1.5f;
            targetVelocity.y = rb.velocity.y; // 중력 등 Y축 움직임은 유지
            rb.velocity = targetVelocity;

            // 애니메이터 및 히트박스 방향 업데이트 (playerController의 Update 로직 참조)
            float inputMagnitude = moveDirection.magnitude;
            animator.SetFloat("playerWalkSpeed", inputMagnitude);

            if (inputMagnitude > 0.01f) // 약간의 Deadzone을 두어 작은 움직임 무시
            {
                // playerController는 inputWalk.y를 playerDirectionY로 사용했으므로, moveZ를 사용
                animator.SetFloat("playerDirectionX", moveX);
                animator.SetFloat("playerDirectionY", moveZ);

                // 히트박스(캐릭터) 방향 전환
                // 기존 로직: X축 입력에 따라 좌우 180도 회전, Y축(Z축 입력)에 따라서는 전후 방향을 바라보도록 하는 것이 일반적입니다.
                // 현재 playerController는 X축 입력으로만 좌우를 결정하고, Y축 입력으로는 localRotation을 이상하게 설정하고 있습니다.
                // 3D 환경이므로, 캐릭터가 이동하는 방향을 자연스럽게 바라보도록 수정하는 것을 권장합니다.
                // 예시: if (moveDirection.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(moveDirection);
                // 우선은 기존 playerController의 X축 기반 회전 로직을 최대한 따르되, Z축에 대한 부분은 주석 처리합니다.
                if (moveX > 0.01f)
                {
                    hitboxController.localRotation = Quaternion.Euler(0, 0, 0); // 오른쪽 보도록
                }
                else if (moveX < -0.01f)
                {
                    // 기존 코드: hitboxController.rotation = Quaternion.Euler(0, -180, 0);
                    // localRotation을 사용하는 것이 일반적으로 더 예측 가능합니다.
                    hitboxController.localRotation = Quaternion.Euler(0, 180, 0); // 왼쪽 보도록
                }
                // else if (moveZ != 0) // 기존 코드: inputWalk.y != 0
                // {
                //    // hitboxController.localRotation = Quaternion.Euler(0, -moveZ * 90, 0); // 이 로직은 Z축 이동에 따라 90도 회전하는데, 3D 전투에서는 부자연스러울 수 있음
                // }
            }
        }

        // --- 공격 처리 ---
        // 공격 실행 조건: 공격 액션이 선택되었고, 현재 공격 중이 아닐 때
        if (attackAction == 1 && controller != null && !controller.isAttack)
        {
            animator.SetTrigger("playerAttack");
            // playerController의 OnAttack()은 InputSystem 콜백이라 직접 호출하지 않음.
            // 대신, playerAttack 애니메이션이 시작되면 애니메이션 이벤트가 playerController의
            // HitboxActive()를 호출하고, isAttack = true로 설정하는 로직이 있다면 그것을 따릅니다.
            // (playerController의 OnAttack 함수 내부에 isAttack = true; 가 있으므로,
            //  ML-Agent가 animator.SetTrigger("playerAttack")를 하면,
            //  playerController의 공격 애니메이션이 재생되고, 관련 상태(isAttack)와 히트박스는
            //  playerController의 애니메이션 이벤트 핸들러들이 처리할 것으로 기대합니다.)
            //  만약 playerController.OnAttack()의 isAttack = true 설정이 콜백 함수 안에만 있다면,
            //  Agent가 공격을 시작할 때 controller.isAttack = true; 를 직접 설정하거나,
            //  playerController에 Agent용 공격 시작 함수를 만들어야 할 수도 있습니다.
            //  (일단 애니메이션 이벤트가 모든것을 처리한다고 가정)
        }

        // === 보상 함수 (다음 단계에서 상세화) ===
        // 예시: 매 스텝마다 작은 음의 보상 (생존 장려 또는 빠른 행동 유도)
        // AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 개발자 테스트용 수동 조작 (기존 playerController의 입력 로직을 모방)
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // playerController의 inputActions와 유사하게 값을 읽어옵니다. (Input System 사용 예시)
        // private ActionManager inputActions; // Initialize에서 new ActionManager() 필요
        // private Vector2 currentInputWalk;
        // inputActions.playerAction.Enable(); // OnEnable 등에서
        // currentInputWalk = inputActions.playerAction.walk.ReadValue<Vector2>();

        // 여기서는 간단히 Unity의 옛날 Input Manager 사용 예시로 대체합니다.
        // 실제로는 프로젝트의 Input System 설정을 따르거나, 사용자가 제공한 playerController의 inputActions를 활용해야 합니다.
        continuousActions[0] = Input.GetAxis("Horizontal"); // 좌우 이동 (A, D 또는 화살표 좌우)
        continuousActions[1] = Input.GetAxis("Vertical");   // 상하 이동 (W, S 또는 화살표 위아래)

        discreteActions[0] = Input.GetMouseButtonDown(0) ? 1 : 0; // 마우스 좌클릭으로 공격
                                                                  // 또는 Space 키 등으로 변경 가능: Input.GetKeyDown(KeyCode.Space) ? 1 : 0;
    }
}