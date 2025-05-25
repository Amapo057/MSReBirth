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
    public Transform agentHitboxController; // 공격 방향의 기준이 되는 Transform
    public float agentAttackEffectiveRange = 3.0f; // "잘 조준된 공격" 판단 시 사용할 유효 사거리
    public float attackAngleDotThreshold = 0.85f;  // "잘 조준된 공격" 판단 시 사용할 정면 각도 임계값 (1에 가까울수록 정면)
    private bool agentIsAttack = false;

    [Header("Target Boss Reference")]
    public TyrController tyr; // TyrController 스크립트 컴포넌트 참조 (Inspector에서 할당)

    [Header("Player Stats")]
    public float playerMaxHP = 3f; // 플레이어 최대 체력
    public float playerHP;         // 현재 플레이어 체력
    public float invincibilityDuration = 1f; // 피격 후 무적 시간 (초)
    private bool isInvincible = false;      // 현재 무적 상태인지
    private Vector3 initialPlayerPosition;  // 플레이어 초기 위치

    [Header("Rewards")]
    public float rewardDefeatTyr = 5.0f;
    public float penaltyPlayerDeath = -6.0f;
    public float rewardHitTyr = 1.0f;
    public float penaltyPlayerHit = -0.8f;
    public float penaltyTimeStep = -0.0005f;
    public float penaltyForEachAttackAttempt = -0.02f;
    public float rewardForWellAimedAttempt = 0.05f;
    public float penaltyDistanceToTyrMultiplier = 0f; // 기본값 0 (사용자가 0으로 설정했었음)

    // 내부 컴포넌트 참조
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

        initialPlayerPosition = transform.localPosition;

        if (rb == null) Debug.LogError("[PlayerAgent] Awake: Rigidbody 컴포넌트를 찾을 수 없습니다!", this);
        if (animator == null) Debug.LogError("[PlayerAgent] Awake: Animator 컴포넌트를 찾을 수 없습니다!", this);
        if (tyr == null) Debug.LogError("[PlayerAgent] Awake: Tyr 참조가 Inspector에서 할당되지 않았습니다!", this);
        if (agentAttackHitBox == null) Debug.LogWarning("[PlayerAgent] Awake: agentAttackHitBox가 Inspector에서 할당되지 않았습니다.", this);
        if (agentHitboxController == null) Debug.LogWarning("[PlayerAgent] Awake: agentHitboxController가 Inspector에서 할당되지 않았습니다.", this);
        if (agentMoveSpeed <= 0) Debug.LogWarning($"[PlayerAgent] Awake: agentMoveSpeed가 0 이하({agentMoveSpeed})입니다.", this);
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // ML-Agents 기본 초기화 로직 실행
        if (inputActions != null)
        {
            try { inputActions.playerAction.Enable(); }
            catch (System.Exception e) { Debug.LogError($"[PlayerAgent] OnEnable: inputActions.playerAction 활성화 실패 - {e.Message}", this); }
        }
        else { Debug.LogError("[PlayerAgent] OnEnable: inputActions (ActionManager)가 null입니다.", this); }
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // ML-Agents 기본 정리 로직 실행
        if (inputActions != null)
        {
            try { inputActions.playerAction.Disable(); }
            catch (System.Exception e) { Debug.LogError($"[PlayerAgent] OnDisable: inputActions.playerAction 비활성화 실패 - {e.Message}", this); }
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("[PlayerAgent] OnEpisodeBegin: 새 에피소드 시작.");
        agentIsAttack = false;
        isInvincible = false;
        playerHP = playerMaxHP;

        transform.localPosition = initialPlayerPosition; // 저장된 초기 위치로 리셋

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (tyr != null)
        {
            // TyrController의 ResetTyrState가 playerPosition 인자를 받는다면 전달
            tyr.ResetTyrState();
            Debug.Log("[PlayerAgent] OnEpisodeBegin - tyr.ResetTyrState() 호출됨.");
        }
        else
        {
            Debug.LogWarning("[PlayerAgent] OnEpisodeBegin: Tyr 참조가 null이어서 Tyr 상태를 초기화할 수 없습니다.");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float arenaHalfWidthX = 7.0f;
        float arenaHalfDepthZ = 4.0f;
        float estimatedMaxDistance = 17.0f;

        // 플레이어 상태 (7개)
        sensor.AddObservation(transform.localPosition.x / arenaHalfWidthX);
        sensor.AddObservation(transform.localPosition.z / arenaHalfDepthZ);
        if (agentHitboxController != null)
        {
            sensor.AddObservation(agentHitboxController.forward.x);
            sensor.AddObservation(agentHitboxController.forward.z);
        }
        else { sensor.AddObservation(0f); sensor.AddObservation(1f); }
        sensor.AddObservation(agentIsAttack ? 0f : 1f);
        sensor.AddObservation(playerMaxHP > 0 ? playerHP / playerMaxHP : 0f); // playerMaxHP 0 방지
        sensor.AddObservation(isInvincible ? 1f : 0f);

        // Tyr 조준 정확도 (1개)
        if (tyr != null && agentHitboxController != null)
        {
            Vector3 directionToTyr = (tyr.transform.position - agentHitboxController.position).normalized;
            float dotProductToTyr = Vector3.Dot(agentHitboxController.forward, directionToTyr);
            sensor.AddObservation(dotProductToTyr);
        }
        else { sensor.AddObservation(0f); }

        // Tyr 상태 (9개)
        if (tyr == null)
        {
            for (int i = 0; i < 9; i++) sensor.AddObservation(0f);
            // if (Time.frameCount % 100 == 0) Debug.LogWarning(...); // 너무 잦은 로그 방지
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;
        continuousActions[0] = 0f; continuousActions[1] = 0f; discreteActions[0] = 0;

        if (inputActions != null && inputActions.playerAction.walk != null && inputActions.playerAction.Get().enabled)
        {
            Vector2 moveInput = inputActions.playerAction.walk.ReadValue<Vector2>();
            continuousActions[0] = moveInput.x;
            continuousActions[1] = moveInput.y;
        }
        if (Input.GetMouseButton(0) && !agentIsAttack) { discreteActions[0] = 1; }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(penaltyTimeStep);
        if (tyr != null && penaltyDistanceToTyrMultiplier != 0f)
        {
            float distanceToTyr = Vector3.Distance(transform.localPosition, tyr.transform.localPosition);
            AddReward(distanceToTyr * penaltyDistanceToTyrMultiplier);
        }

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        int attackAction = actions.DiscreteActions[0];

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
            AddReward(penaltyForEachAttackAttempt);
            bool aimedWell = false;
            if (tyr != null && agentHitboxController != null)
            {
                Vector3 attackOrigin = agentHitboxController.position;
                Vector3 directionToTyr = (tyr.transform.position - attackOrigin).normalized;
                float dotProduct = Vector3.Dot(agentHitboxController.forward, directionToTyr);
                float distanceToTyrActual = Vector3.Distance(attackOrigin, tyr.transform.position);

                if (distanceToTyrActual <= agentAttackEffectiveRange && dotProduct >= attackAngleDotThreshold)
                {
                    aimedWell = true;
                    AddReward(rewardForWellAimedAttempt);
                }
            }

            if (animator != null)
            {
                animator.SetTrigger("playerAttack");
                agentIsAttack = true;
                if (aimedWell) { Debug.Log("[PlayerAgent] OnActionReceived: 잘 조준된 공격 실행!"); }
                else { Debug.LogWarning("[PlayerAgent] OnActionReceived: 조준이 좋지 않은 공격 실행!"); }
            }
            else { Debug.LogWarning("[PlayerAgent] OnActionReceived: Animator가 null이어서 공격 애니메이션을 실행할 수 없습니다.", this); }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInvincible && other.CompareTag("TyrAttackCollider"))
        {
            float damageReceived = 1f; // Tyr의 공격에 의한 기본 피해량
            PlayerTookDamage(damageReceived);
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
        Debug.Log($"[PlayerAgent] 플레이어 피격! 현재 체력: {playerHP.ToString("F2")}/{playerMaxHP}, 받은 피해: {damageAmount}, 보상: {penaltyPlayerHit}");
        if (playerHP <= 0)
        {
            playerHP = 0;
            Debug.LogWarning($"[PlayerAgent] <<< PLAYER DIED >>> 체력 {playerHP.ToString("F2")}. 에피소드 종료 실행.", this);
            AddReward(penaltyPlayerDeath);
            EndEpisode();
        }
    }

    public void PlayerHitTyr(float damageDealtToTyr) // TyrController에서 호출
    {
        if (tyr == null) return;
        AddReward(rewardHitTyr); // damageDealtToTyr 값을 보상에 반영하려면 로직 수정 필요
        Debug.Log($"[PlayerAgent] Tyr 타격! 보상: {rewardHitTyr}");
    }

    public void TyrDefeated() // TyrController에서 호출
    {
        Debug.LogWarning("[PlayerAgent] <<< TYR DEFEATED >>> 에피소드 성공 종료 실행.", this);
        AddReward(rewardDefeatTyr);
        EndEpisode();
    }

    public void Agent_AttackAnimation_HitboxActive()
    {
        if (agentAttackHitBox != null) agentAttackHitBox.SetActive(true);
        // Debug.Log("[PlayerAgent] AnimationEvent: Hitbox 활성화."); // 필요시 주석 해제
    }

    public void Agent_AttackAnimation_HitboxInactive()
    {
        if (agentAttackHitBox != null) agentAttackHitBox.SetActive(false);
        // Debug.Log("[PlayerAgent] AnimationEvent: Hitbox 비활성화."); // 필요시 주석 해제
    }

    public void Agent_AttackAnimation_Finish()
    {
        agentIsAttack = false;
        // Debug.Log("[PlayerAgent] AnimationEvent: 공격 애니메이션 종료, agentIsAttack = false."); // 필요시 주석 해제
    }
}