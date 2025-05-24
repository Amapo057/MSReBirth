using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// ActionManager 클래스가 정의된 네임스페이스를 여기에 추가해야 할 수 있습니다.
// 예: using YourProjectName.InputSystem;

public class PlayerAgent : Agent
{
    [Header("Agent Movement Settings")]
    public float agentMoveSpeed = 5f;

    [Header("Agent Attack Settings")]
    public GameObject agentAttackHitBox;
    public Transform agentHitboxController;
    private bool agentIsAttack = false;

    [Header("Target Boss Reference")]
    public TyrController tyr; // TyrController 스크립트 컴포넌트 참조

    [Header("Player Stats")]
    public float playerMaxHP = 3f;
    public float playerHP;
    public float invincibilityDuration = 1f; // 피격 후 무적 시간 (초)
    private bool isInvincible = false;      // 현재 무적 상태인지
    private Vector3 initailPlayerPosition;

    [Header("Rewards")]
    public float rewardDefeatTyr = 1.0f;
    public float penaltyPlayerDeath = -1.0f;
    public float rewardHitTyr = 0.5f;
    public float penaltyPlayerHit = -0.2f;
    public float penaltyTimeStep = -0.001f; // 매 의사결정 스텝마다 받을 시간 패널티
    public float rewardAttemptAttack = 0.02f; // 공격 시도 시 받는 작은 보상

    public float penaltyDistanceToTyrMultiplier = -0.0001f; // Tyr와의 거리에 따른 패널티 배율 (매우 작은 음수 값)

    private Rigidbody rb;
    private Animator animator;
    private ActionManager inputActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        try
        {
            inputActions = new ActionManager();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAgent] Awake: ActionManager 인스턴스 생성 실패 - {e.Message}. 스크립트를 비활성화합니다.", this);
            this.enabled = false;
            return;
        }

        initailPlayerPosition = transform.localPosition;

        if (rb == null) Debug.LogError("[PlayerAgent] Awake: Rigidbody 컴포넌트를 찾을 수 없습니다!", this);
        if (animator == null) Debug.LogError("[PlayerAgent] Awake: Animator 컴포넌트를 찾을 수 없습니다!", this);
        if (tyr == null) Debug.LogError("[PlayerAgent] Awake: Tyr 참조가 Inspector에서 할당되지 않았습니다!", this);
        if (agentAttackHitBox == null) Debug.LogWarning("[PlayerAgent] Awake: agentAttackHitBox가 Inspector에서 할당되지 않았습니다.", this);
        if (agentHitboxController == null) Debug.LogWarning("[PlayerAgent] Awake: agentHitboxController가 Inspector에서 할당되지 않았습니다.", this);
        if (agentMoveSpeed <= 0) Debug.LogWarning($"[PlayerAgent] Awake: agentMoveSpeed가 0 이하({agentMoveSpeed})입니다.", this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Debug.Log("[PlayerAgent] OnEnable: 에이전트 활성화 및 입력 시스템 활성화 시도.");
        if (inputActions != null)
        {
            try { inputActions.playerAction.Enable(); }
            catch (System.Exception e) { Debug.LogError($"[PlayerAgent] OnEnable: inputActions.playerAction 활성화 실패 - {e.Message}", this); }
        }
        else { Debug.LogError("[PlayerAgent] OnEnable: inputActions (ActionManager)가 null입니다.", this); }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (inputActions != null)
        {
            try { inputActions.playerAction.Disable(); }
            catch (System.Exception e) { Debug.LogError($"[PlayerAgent] OnDisable: inputActions.playerAction 비활성화 실패 - {e.Message}", this); }
        }
    }

    public override void OnEpisodeBegin()
    {
        agentIsAttack = false;
        isInvincible = false;
        playerHP = playerMaxHP;

        transform.localPosition = initailPlayerPosition;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // 플레이어 위치도 초기 위치로 리셋하는 것이 좋습니다 (필요하다면)
        // transform.localPosition = playerInitialPosition;


        // Tyr 상태 초기화 호출
        if (tyr != null)
        {
            tyr.ResetTyrState();
            Debug.Log("[PlayerAgent] OnEpisodeBegin - tyr.ResetTyrState() CALLED");
        }
        else
        {
            Debug.LogWarning("[PlayerAgent] OnEpisodeBegin: Tyr 참조가 null이어서 Tyr 상태를 초기화할 수 없습니다.");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float arenaHalfWidthX = 7.0f;
        float arenaHalfDepthZ = 2.5f;
        float estimatedMaxDistance = 15.0f;

        sensor.AddObservation(transform.localPosition.x / arenaHalfWidthX);
        sensor.AddObservation(transform.localPosition.z / arenaHalfDepthZ);
        if (agentHitboxController != null)
        {
            sensor.AddObservation(agentHitboxController.forward.x);
            sensor.AddObservation(agentHitboxController.forward.z);
        }
        else { sensor.AddObservation(0f); sensor.AddObservation(1f); }
        sensor.AddObservation(agentIsAttack ? 0f : 1f);
        sensor.AddObservation(playerHP / playerMaxHP);
        sensor.AddObservation(isInvincible ? 1f : 0f); // 무적 상태 관찰 -> Space Size +1 (현재 총 16)

        if (tyr == null)
        {
            for (int i = 0; i < 9; i++) sensor.AddObservation(0f);
            if (Time.frameCount % 100 == 0) Debug.LogWarning("[PlayerAgent] CollectObservations: Tyr 참조가 null입니다.", this);
            return;
        }
        Vector3 tyrPosition = tyr.transform.localPosition;
        sensor.AddObservation(tyrPosition.x / arenaHalfWidthX);
        sensor.AddObservation(tyrPosition.z / arenaHalfDepthZ);
        Vector3 relativePosToTyr = tyrPosition - transform.localPosition;
        sensor.AddObservation(relativePosToTyr.x / estimatedMaxDistance);
        sensor.AddObservation(relativePosToTyr.z / estimatedMaxDistance);
        sensor.AddObservation(relativePosToTyr.magnitude / estimatedMaxDistance);
        if (tyr.tyrMaxHP > 0) { sensor.AddObservation(tyr.tyrHP / tyr.tyrMaxHP); }
        else { sensor.AddObservation(0f); }
        sensor.AddObservation(tyr.isWalk ? 1f : 0f);
        sensor.AddObservation(tyr.isAttack ? 1f : 0f);
        sensor.AddObservation(tyr.isReady ? 1f : 0f);
    }

    // Heuristic 함수: 공격 입력만 예전 Input Manager 사용, 이동 입력 조건문 수정!
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Debug.LogWarning("====== [PlayerAgent] Heuristic() CALLED ======"); // 너무 자주 찍히면 주석 처리
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // 기본값 초기화
        continuousActions[0] = 0f; // X축 이동
        continuousActions[1] = 0f; // Z축 이동
        discreteActions[0] = 0;   // 공격 (0: 안함, 1: 함)

        // 이동 입력 (New Input System) - 수정된 조건문
        if (inputActions == null)
        {
            if (Time.frameCount % 100 == 0)
                Debug.LogWarning("[PlayerAgent] Heuristic: inputActions (ActionManager) is null. Skipping movement input.", this);
        }
        else if (!inputActions.playerAction.Get().enabled) // playerAction 맵이 활성화되어 있는지 확인
        {
            if (Time.frameCount % 100 == 0)
                Debug.LogWarning("[PlayerAgent] Heuristic: inputActions.playerAction map is not enabled. Skipping movement input.", this);
        }
        else if (inputActions.playerAction.walk == null) // walk 액션 자체가 null인지 확인
        {
            Debug.LogError("[PlayerAgent] Heuristic: inputActions.playerAction.walk (InputAction) is null. Check ActionManager setup.", this);
        }
        else // 모든 조건 만족 시 이동 입력 처리
        {
            Vector2 moveInput = inputActions.playerAction.walk.ReadValue<Vector2>();
            continuousActions[0] = moveInput.x;
            continuousActions[1] = moveInput.y;
        }

        // 공격 입력 (Legacy Input Manager - 마우스 왼쪽 버튼)
        if (Input.GetMouseButton(0) && !agentIsAttack) // 버튼 누르고 있고 & 현재 공격 중이 아닐 때
        {
            discreteActions[0] = 1; // 공격 실행 의도 전달
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(penaltyTimeStep); 

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        int attackAction = actions.DiscreteActions[0]; 

         if (tyr != null)
        {
            float distanceToTyr = Vector3.Distance(transform.localPosition, tyr.transform.localPosition);
            // 거리가 멀수록 더 큰 음수 보상(패널티)을 받도록 합니다.
            // estimatedMaxDistance로 나누어 거리를 정규화 한 후 배율을 곱할 수도 있습니다.
            // float normalizedDistance = distanceToTyr / estimatedMaxDistance; // estimatedMaxDistance는 CollectObservations에서 사용한 값
            // AddReward(normalizedDistance * penaltyDistanceToTyrMultiplier); 
            // 또는 단순히 거리에 직접 배율을 곱할 수도 있습니다 (배율을 매우 작게 유지).
            AddReward(distanceToTyr * penaltyDistanceToTyrMultiplier);
        }

        if (!agentIsAttack)
        {
            if (rb == null) return;
            Vector3 moveDirection = new Vector3(moveX, 0f, moveZ);
            rb.velocity = new Vector3(moveDirection.normalized.x * agentMoveSpeed, rb.velocity.y, moveDirection.normalized.z * agentMoveSpeed);
            if (animator != null)
            {
                float currentActualSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
                animator.SetFloat("playerWalkSpeed", currentActualSpeed);
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    animator.SetFloat("playerDirectionX", moveX);
                    animator.SetFloat("playerDirectionY", moveZ);
                }
            }
            if (agentHitboxController != null)
            {
                if (Mathf.Abs(moveX) > 0.01f)
                { agentHitboxController.localRotation = Quaternion.Euler(0, (moveX > 0.01f) ? 0f : 180f, 0); }
                else if (Mathf.Abs(moveZ) > 0.01f)
                { agentHitboxController.localRotation = Quaternion.Euler(0, -moveZ * 90f, 0); }
            }
        }

        if (attackAction == 1 && !agentIsAttack)
        {
            
            if (tyr != null && agentHitboxController != null)
            {
                // float distanceToTyr = Vector3.Distance(transform.localPosition, tyr.transform.localPosition);
                Vector3 directionToTyr = (tyr.transform.localPosition - transform.localPosition).normalized;
                // float dotProduct = Vector3.Dot(agentHitboxController.forward, directionToTyr); // 플레이어 정면과 Tyr 방향의 내적

            }
            if (animator != null)
            {
                AddReward(rewardAttemptAttack);
                Debug.Log("[PlayerAgent] OnActionReceived: 공격 실행!");
                animator.SetTrigger("playerAttack");
                agentIsAttack = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInvincible && other.CompareTag("TyrAttackCollider"))
        {
            Debug.Log($"[PlayerAgent] OnTriggerEnter: {other.name} 와 충돌 (태그: {other.tag})");
            PlayerTookDamage(1f);
            isInvincible = true;
            StartCoroutine(ResetInvincibility());
        }
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    public void PlayerTookDamage(float damageAmount)
    {
        playerHP -= damageAmount;
        AddReward(penaltyPlayerHit);
        if (playerHP <= 0)
        {
            playerHP = 0;
            Debug.LogWarning("[PlayerAgent] 플레이어 사망! 에피소드 종료.");
            AddReward(penaltyPlayerDeath);
            EndEpisode();
        }
    }

    public void PlayerHitTyr(float damageDealtToTyr)
    {
        if (tyr == null) return;
        AddReward(rewardHitTyr);
        Debug.Log($"[PlayerAgent] Tyr 타격! 보상: {rewardHitTyr}");
    }

    public void TyrDefeated()
    {
        Debug.LogWarning("[PlayerAgent] Tyr 격파! 에피소드 성공 종료!");
        AddReward(rewardDefeatTyr);
        EndEpisode();
    }

    public void Agent_AttackAnimation_HitboxActive()
    {
        if (agentAttackHitBox != null) agentAttackHitBox.SetActive(true);
    }

    public void Agent_AttackAnimation_HitboxInactive()
    {
        if (agentAttackHitBox != null) agentAttackHitBox.SetActive(false);
    }

    public void Agent_AttackAnimation_Finish()
    {
        agentIsAttack = false;
    }
}