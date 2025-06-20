using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.AI;
using Unity.VisualScripting;
// using UnityEditor.AssetImporters;

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
    private bool invincible = false;      // 현재 무적 상태인지
    private Vector3 initialPlayerPosition;  // 플레이어 초기 위치
    private bool isDodge = false; // 회피 중인지 여부
    private Vector3 playerDirection;  // 회피 방향 (마지막 입력 벡터)
    private float dodgeAcceleration = 10;  // 회피 가속도

    private Vector3 moveDirection = new Vector3(1f, 0f, 0f);

    float moveX;
    float moveZ;
    float chargeTime = 0f;
    float targetTime = 0f; // 공격 시도 시간 (초 단위)
    public float attackDamage = 1f;
    bool isHit = false;
    bool isAttack = false;
    bool chargeEnd = false;
    bool triggerAttack = false;


    [Header("Rewards")]
    public float rewardDefeatTyr = 5.0f;
    public float penaltyPlayerDeath = -6.0f;
    public float rewardHitTyr = 1.0f;
    public float penaltyPlayerHit = -0.8f;
    public float penaltyTimeStep = -0.0005f;
    public float penaltyForEachAttackAttempt = -0.02f;
    public float rewardForWellAimedAttempt = 0.05f;
    public float penaltyDistanceToTyrMultiplier = 0f; // 기본값 0 (사용자가 0으로 설정했었음)
    public float rewardDodge = 0.05f;
    public float penaltyDodge = -0.1f;
    public float rewardWalk = 0.05f;

    // 내부 컴포넌트 참조
    private Rigidbody rb;
    public Animator playerSprite;
    private ActionManager inputActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        if (playerSprite == null) Debug.LogError("[PlayerAgent] Awake: playerSprite 컴포넌트를 찾을 수 없습니다!", this);
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
        agentIsAttack = false;
        invincible = false;
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
        }
        else
        {
            Debug.LogWarning("[PlayerAgent] OnEpisodeBegin: Tyr 참조가 null이어서 Tyr 상태를 초기화할 수 없습니다.");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float arenaHalfWidthX = 10.0f;
        float arenaHalfDepthZ = 2.5f;
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
        sensor.AddObservation(invincible ? 1f : 0f);

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
        if (tyr.tyrMaxHp > 0) { sensor.AddObservation(tyr.tyrHp / tyr.tyrMaxHp); }
        else { sensor.AddObservation(0f); }
        sensor.AddObservation(tyr.isWalk ? 1f : 0f);
        sensor.AddObservation(tyr.isAttack ? 1f : 0f);
        sensor.AddObservation(tyr.isReady ? 1f : 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        moveX = 0;
        moveZ = 0;

        if (isHit)
        {
            return;
        }

        if (Input.GetKey(KeyCode.A)) { moveX = -1; }
        else if (Input.GetKey(KeyCode.D)) { moveX = 1; }
        if (Input.GetKey(KeyCode.W)) { moveZ = 1; }
        else if (Input.GetKey(KeyCode.S)) { moveZ = -1; }

        if (Input.GetKeyDown(KeyCode.Space) && !isAttack && !isDodge)
        {
            Dodge();
        }
        if (Input.GetKeyDown(KeyCode.Mouse0) && !isAttack && !isDodge)
        {
            Charge();
        }
        if (isAttack && !chargeEnd)
        {
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                triggerAttack = true;
                chargeEnd = true;
            }
        }
        if (isDodge)
        {
            rb.velocity = new Vector3(moveDirection.x * dodgeAcceleration, 0f, moveDirection.z * dodgeAcceleration);
            dodgeAcceleration *= 0.93f;

            if (dodgeAcceleration <= 0.3f)
            {
                invincible = false;
            }

            if (dodgeAcceleration <= 0.25f)
            {
                DodgeEnd();
            }
            return;
        }

        if (isAttack)
        {
            if (chargeTime >= 3.5f)
            {
                triggerAttack = true;
            }

            if (triggerAttack)
            {

                playerSprite.SetTrigger("playerAttack");
                triggerAttack = false;
            }
            else
            {
                chargeTime += Time.deltaTime;
                attackDamage = chargeTime * 2f;
                if (chargeTime >= 2.5f)
                {
                    attackDamage = 2f;
                }
            }
            return;
        }

        float curentSpeed = new Vector2(moveX, moveZ).magnitude;
        playerSprite.SetFloat("playerWalkSpeed", curentSpeed);
        if (curentSpeed > 0)
        {
            playerSprite.SetFloat("playerDirectionX", moveX);
            playerSprite.SetFloat("playerDirectionY", moveZ);
            moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
            rb.velocity = moveDirection * 3f;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(penaltyTimeStep);
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        int attackDiscreteAction = actions.DiscreteActions[0];
        int dodgeDiscreteAction = 0;

        if (actions.DiscreteActions.Length > 1)
        {
            dodgeDiscreteAction = actions.DiscreteActions[1];
        }
        if (isHit)
        {
            return; // 피격 상태에서는 행동을 처리하지 않음
        }

        if (dodgeDiscreteAction == 1 && !isAttack && !isDodge)
        {
            Dodge();
        }
        if (attackDiscreteAction != 0 && !isAttack && !isDodge)
        {
            switch (attackDiscreteAction)
            {
                case 1:
                    Attack();
                    break;
                case 2:
                    Charge();
                    break;
                case 3:
                    Charge2();
                    break;
            }
        }
        if (isDodge)
        {
            rb.velocity = new Vector3(moveDirection.x * dodgeAcceleration, 0f, moveDirection.z * dodgeAcceleration);
            dodgeAcceleration *= 0.93f;

            if (dodgeAcceleration <= 0.25f)
            {
                DodgeEnd();
            }
            return;
        }

        if (isAttack)
        {
            if (chargeTime >= targetTime)
            {
                triggerAttack = true;
            }

            if (triggerAttack)
            {
                playerSprite.SetTrigger("playerAttack");
                triggerAttack = false;
            }
            else
            {
                chargeTime += Time.deltaTime;
                attackDamage = chargeTime * 2f;
                if (chargeTime >= 2.5f)
                {
                    attackDamage = 2f;
                }
            }
            return;
        }
    }

    //public override void OnActionReceived(ActionBuffers actions)
    //{
    //    AddReward(penaltyTimeStep);
    //    if (tyr != null && penaltyDistanceToTyrMultiplier != 0f) // 거리 패널티는 현재 0으로 설정하신 상태
    //    {
    //        float distanceToTyr = Vector3.Distance(transform.localPosition, tyr.transform.localPosition);
    //        AddReward(distanceToTyr * penaltyDistanceToTyrMultiplier);
    //    }

    //    float moveX = actions.ContinuousActions[0];
    //    float moveZ = actions.ContinuousActions[1];
    //    int attackDiscreteAction = actions.DiscreteActions[0];
    //    int dodgeDiscreteAction = 0;
    //    if (actions.DiscreteActions.Length > 1)
    //    {
    //        dodgeDiscreteAction = actions.DiscreteActions[1];
    //    }

    //    // 1. 현재 '회피 중'(`isDodge == true`)일 때의 물리 처리 (닥터의 고유 로직)
    //    if (isDodge)
    //    {
    //        // 닥터의 회피 중 물리 로직 (예: dodgeAcceleration을 사용한 움직임)
    //        // 이 블록의 코드는 닥터께서 직접 작성/관리하시는 부분입니다.
    //        // 예시 (닥터의 이전 코드 조각 기반):
    //        dodgeAcceleration *= 0.8f; 
    //        if (rb != null && playerDirection != Vector3.zero)
    //        {
    //            rb.velocity = new Vector3(playerDirection.x * dodgeAcceleration, 0f, playerDirection.z * dodgeAcceleration);
    //        }
    //        if (dodgeAcceleration < 0.1f && isDodge) // isDodge를 한번 더 체크하여 중복 호출 방지
    //        {
    //            DodgeEnd(); // 애니메이션 이벤트 외의 강제 종료 조건 (선택 사항)
    //        }
    //        return; // 회피 중에는 다른 행동 명령 처리 안 함
    //    }

    //    // (이제 isDodge가 false인 상황)

    //    // 2. 새로운 '회피 명령' 처리 (`dodgeDiscreteAction == 1`)
    //    //    (공격 중이 아닐 때만 새로운 회피 가능)
    //    if (dodgeDiscreteAction == 1 && !agentIsAttack)
    //    {
    //        // 회피 방향 결정 ("마지막 입력의 벡터 받아서 사용" 또는 현재 이동 입력)
    //        Vector3 currentMoveIntent = new Vector3(moveX, 0f, moveZ);
    //        if (currentMoveIntent.sqrMagnitude > 0.01f)
    //        {
    //            playerDirection = currentMoveIntent.normalized;
    //        }
    //        else if (agentHitboxController != null)
    //        {
    //            playerDirection = agentHitboxController.forward;
    //        }
    //        else
    //        {
    //            playerDirection = transform.forward;
    //        }
            
    //        Dodge(); // 닥터의 Dodge() 함수 호출
    //        return; 
    //    }

    //    // 3. 새로운 '공격 명령' 처리 (`attackDiscreteAction == 1`)
    //    //    (회피 명령이 없었고, 회피 중도 아니고, 이미 공격 중도 아닐 때)
    //    if (attackDiscreteAction == 1 && !agentIsAttack) 
    //    {
    //        AddReward(penaltyForEachAttackAttempt);
    //        bool aimedWell = false;
    //        if (tyr != null && agentHitboxController != null)
    //        {
    //            Vector3 attackOrigin = agentHitboxController.position;
    //            Vector3 directionToTyr = (tyr.transform.position - attackOrigin).normalized;
    //            float dotProduct = Vector3.Dot(agentHitboxController.forward, directionToTyr);
    //            float distanceToTyrActual = Vector3.Distance(attackOrigin, tyr.transform.position);

    //            if (distanceToTyrActual <= agentAttackEffectiveRange && dotProduct >= attackAngleDotThreshold)
    //            {
    //                aimedWell = true;
    //                AddReward(rewardForWellAimedAttempt);
    //            }
    //        }

    //        if (playerSprite != null)
    //        {
    //            playerSprite.SetTrigger("playerAttack");
    //            agentIsAttack = true;
    //            if (aimedWell) { Debug.Log("[PlayerAgent] OnActionReceived: 잘 조준된 공격 실행!"); }
    //            else { Debug.LogWarning("[PlayerAgent] OnActionReceived: 조준이 좋지 않은 공격 실행!"); }
    //        }
    //        else { Debug.LogWarning("[PlayerAgent] OnActionReceived: playerSprite가 null이어서 공격 애니메이션을 실행할 수 없습니다.", this); }
    //        return; 
    //    }

    //    // 4. '일반 이동' 처리 
    //    //    (위 모든 특별한 행동 조건에 해당하지 않고, 현재 공격/회피 상태도 아닐 때)
    //    //    즉, isDodge == false && agentIsAttack == false && dodgeDiscreteAction == 0 && attackDiscreteAction == 0 인 경우,
    //    //    또는 dodgeDiscreteAction !=0 이나 attackDiscreteAction !=0 이었지만 다른 조건(예: !agentIsAttack) 때문에 실행되지 않았을 경우.
    //    //    좀 더 명확히 하려면, 이 블록의 시작에 if (!agentIsAttack && !isDodge) 조건을 다시 명시할 수 있습니다.
    //    //    하지만 위의 return 문들 때문에 이 조건은 이미 만족된 상태로 여기까지 오게 됩니다.
        
    //    if (rb == null) return;
    //    Vector3 moveDirectionInput = new Vector3(moveX, 0f, moveZ);
    //    if (!agentIsAttack)
    //    {
    //        rb.velocity = new Vector3(moveDirectionInput.normalized.x * agentMoveSpeed, rb.velocity.y, moveDirectionInput.normalized.z * agentMoveSpeed);
    //    }
        
    //    // 일반 이동 시에도 playerDirection 업데이트 (다음 회피 시 "마지막 이동 입력 벡터"로 사용하기 위해)
    //        if (moveDirectionInput.sqrMagnitude > 0.01f)
    //        {
    //            playerDirection = moveDirectionInput.normalized;
    //        }

    //    if (playerSprite != null)
    //    {
    //        float currentActualSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
    //        playerSprite.SetFloat("playerWalkSpeed", currentActualSpeed);
    //        if (moveDirectionInput.sqrMagnitude > 0.01f)
    //        {
    //            AddReward(rewardWalk);
    //            playerSprite.SetFloat("playerDirectionX", moveX);
    //            playerSprite.SetFloat("playerDirectionY", moveZ);
    //        }
    //    }
    //    if (agentHitboxController != null)
    //    {
    //        if (Mathf.Abs(moveX) > 0.01f)
    //        { agentHitboxController.localRotation = Quaternion.Euler(0, (moveX > 0.01f) ? 0f : 180f, 0); }
    //        else if (Mathf.Abs(moveZ) > 0.01f)
    //        { agentHitboxController.localRotation = Quaternion.Euler(0, -moveZ * 90f, 0); }
    //    }
    //}
    

    private void OnTriggerEnter(Collider other)
    {
        if (isDodge)
        {
            AddReward(rewardDodge);
            Debug.Log("회피성공");
            return;
        }
        if (!invincible && other.CompareTag("TyrAttackCollider"))
        {
            float damageReceived = 1f; // Tyr의 공격에 의한 기본 피해량
            PlayerTookDamage(damageReceived);
            invincible = true;
            StartCoroutine(ResetInvincibility());
        }
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        invincible = false;
    }

    public void Attack()
    {
        isAttack = true;
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
        playerSprite.SetTrigger("playerAttack");
    }
    public void AttackEnd()
    {
        triggerAttack = false;
        isAttack = false;
        attackDamage = 1f;
        chargeTime = 0f;
    }
    public void PlayerTookDamage(float damageAmount)
    {
        playerHP -= damageAmount;
        AddReward(penaltyPlayerHit);
        StartCoroutine(ResetInvincibility());
        AttackEnd();
        DodgeEnd();
        if (rb != null && tyr != null) // Rigidbody와 Tyr 참조가 모두 있어야 함
        {
            Vector3 knockbackDirection = (transform.position - tyr.transform.position);

            if (knockbackDirection == Vector3.zero)
            {
                knockbackDirection = -transform.forward; // 플레이어의 등 뒤 방향
            }

            knockbackDirection.y = 0.5f;

            rb.AddForce(knockbackDirection.normalized * 100, ForceMode.Impulse);
        }
        if (playerHP <= 0)
        {
            playerHP = 0;
            Debug.LogWarning("<<< PLAYER DIED >>> 에피소드 종료 실행.", this);
            AddReward(penaltyPlayerDeath);
            EndEpisode();
        }
    }
    public void HitEnd()
    {
        isHit = false;
    }
    public void Dodge()
    {
        playerSprite.SetTrigger("playerDodge");
        AddReward(penaltyDodge);
        isDodge = true;
        Debug.Log("회피 시도");
    }
    public void DodgeEnd()
    {
        isDodge = false;
        dodgeAcceleration = 10f;
    }
    private void Charge()
    {
        isAttack = true;
        playerSprite.SetTrigger("playerCharge");
        chargeTime = 0f;
        targetTime = 1f;
        triggerAttack = false;
        attackDamage = 1f;
        chargeEnd = false;
    }

    private void Charge2()
    {
        isAttack = true;
        playerSprite.SetTrigger("playerCharge");
        chargeTime = 0f;
        targetTime = 2f;
        triggerAttack = false;
        attackDamage = 1f;
        chargeEnd = false;
    }

    public void PlayerHitTyr(float damageDealtToTyr) // TyrController에서 호출
    {
        if (tyr == null) return;
        AddReward(rewardHitTyr * damageDealtToTyr);
        Debug.Log($"[PlayerAgent] Tyr 타격! 보상: {rewardHitTyr * damageDealtToTyr}");
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