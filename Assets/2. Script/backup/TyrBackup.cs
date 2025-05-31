using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using JetBrains.Annotations;

public class TyrBackup : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI bossHP;
    private Animator anim;
    public SpriteRenderer body;
    public SpriteRenderer Leg;
    public PlayerAgent playerAgent;
    Rigidbody rb;


    Vector3 targetPosition;
    Vector3 beforePosition;
    Vector3 initialPosition;

    public Transform HitBox;

    public float speed = 1.5f;
    public bool isWalk = true;
    public bool isAttack = false;
    private bool rightDirection = true;
    private int walkNumber = 0;
    private float distanceThreshold = 1.5f;
    public float tyrMaxHP = 10f;
    public float tyrHP;
    private int stuck = 0;
    public float Force = 100f;
    public bool canMove = true; // Tyr가 움직일 수 있는지 여부
    private float maxXPosition = 7f;
    private float maxZPosition = 5f;
    private bool isInvincible = false;


    // 학습용 변수
    public bool isReady = false;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        targetPosition = player.transform.position;
        tyrHP = tyrMaxHP;

    }
    public void ResetTyrState()
    {
        Debug.Log("====== [TyrController] ResetTyrState() - START ======");
        Debug.Log("[TyrController] Tyr 상태 초기화.");
        tyrHP = tyrMaxHP; // 체력을 최대로 회복
        isWalk = true;   // 필요에 따라 초기 상태로 설정
        isAttack = false;
        isReady = false;  // 또는 게임 시작 시 Tyr의 기본 상태로 설정
        targetPosition = player.transform.position;
        ReadyEnd();
        AttackEnd();
        anim.Play("TyrIdle", 0, 0f);

        // Tyr 위치 초기화
        //transform.localPosition = initialPosition;
        float spawnX = Random.Range(-maxXPosition, maxXPosition);
        float spawnZ = Random.Range(-maxZPosition, maxZPosition);
        transform.localPosition = new Vector3(spawnX, 0f, spawnZ); // Tyr를 화면 내 임의 위치로 재배치

        // 기타 필요한 애니메이션 상태, 내부 변수 등 초기화
        // Animator animator = GetComponent<Animator>();
        // if (animator != null) animator.Rebind(); // 애니메이터 상태 초기화 (강력하지만 주의 필요)
        // else if (animator != null) animator.Play("Idle"); // 특정 Idle 상태로 전환

        // gameObject.SetActive(true); // 비활성화되었다면 다시 활성화
        Debug.Log("====== [TyrController] ResetTyrState() - END ======");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttackCollider") && !isInvincible)
        {
            tyrHP -= 1;
            isInvincible = true;
            StartCoroutine(ResetInvincibility());
            playerAgent.PlayerHitTyr(-1);
        }
        if (tyrHP <= 0)
        {
            tyrHP = 0;
            // 여기에 Tyr 사망 관련 로직 (애니메이션, 움직임 중지 등) 추가
            Debug.LogWarning("[TyrController] Tyr 사망!");

            if (playerAgent != null)
            {
                playerAgent.TyrDefeated(); // PlayerAgent에게 Tyr가 격파되었음을 알림
            }
            else
            {
                Debug.LogError("[TyrController] PlayerAgent 참조가 할당되지 않아 TyrDefeated를 호출할 수 없습니다!");
            }
            // gameObject.SetActive(false); // 예: Tyr 비활성화
        }
    }
    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(1f);
        isInvincible = false;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("tyrWalk", isWalk);
        bossHP.SetText("boss hp : {0}", tyrHP);
    }
    void FixedUpdate()
    {
        if (player == null) // 플레이어 참조가 없으면 아무것도 하지 않음
        {
            if (Time.frameCount % 100 == 0) Debug.LogWarning("[TyrController] Player 참조가 없습니다.");
            return;
        }

        // --- 플레이어 방향으로 스프라이트 뒤집기 (공격 중이 아닐 때) ---
        if (!isAttack && body != null && Leg != null) // body와 Leg SpriteRenderer가 할당되어 있다고 가정
        {
            float playerDirectionX = player.transform.position.x - transform.position.x;
            if (playerDirectionX < 0 && rightDirection) // 플레이어가 왼쪽에 있는데, Tyr가 오른쪽을 보고 있다면
            {
                // 왼쪽 보도록 뒤집기
                rightDirection = false;
                // body.flipX = true; // 또는 아래 localScale 방식 사용
                // Leg.flipX = true;
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
            else if (playerDirectionX > 0 && !rightDirection) // 플레이어가 오른쪽에 있는데, Tyr가 왼쪽을 보고 있다면
            {
                // 오른쪽 보도록 뒤집기
                rightDirection = true;
                // body.flipX = false;
                // Leg.flipX = false;
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }

        // --- 주 로직: 공격 중이거나, 이동/결정 중 ---
        if (isAttack)
        {
            // === 공격 실행 단계 ===
            isWalk = false; // 공격 중에는 걷지 않음 (애니메이터 업데이트를 위해)

            // 플레이어의 위치를 Tyr의 로컬 좌표계로 변환 (Tyr 기준 앞/뒤/좌/우 판단)
            Vector3 playerLocalPosition = transform.InverseTransformPoint(player.transform.position);
            float localPlayerX = playerLocalPosition.x;
            float localPlayerZ = playerLocalPosition.z;

            // 여기서 Debug.Log를 통해 localPlayerX, localPlayerZ 값을 확인하며 공격 트리거 조건을 조정하세요.
            // Debug.Log($"[TyrController] Attack Phase - Player Local Pos: X={localPlayerX.ToString("F2")}, Z={localPlayerZ.ToString("F2")}");

            // 로컬 좌표를 기준으로 공격 애니메이션 결정
            // (이 예시는 각 분면마다 다른 공격을 한다고 가정합니다. 실제 공격 패턴에 맞게 수정 필요)
            if (Mathf.Abs(localPlayerX) > Mathf.Abs(localPlayerZ)) // 플레이어가 Tyr의 좌우 측면에 더 가까울 때
            {
                if (localPlayerX > 0) // 플레이어가 Tyr의 로컬 오른쪽 (+X)
                {
                    anim.SetTrigger("tyrSideTail"); // 예: 오른쪽 측면 공격
                }
                else // 플레이어가 Tyr의 로컬 왼쪽 (-X)
                {
                    anim.SetTrigger("tyrBite");     // 예: 왼쪽 측면 공격
                }
            }
            else // 플레이어가 Tyr의 앞 또는 뒤에 더 가까울 때
            {
                if (localPlayerZ > 0) // 플레이어가 Tyr의 로컬 앞쪽 (+Z)
                {
                    anim.SetTrigger("tyrSideTail");   // 예: 정면 공격
                }
                else // 플레이어가 Tyr의 로컬 뒤쪽 (-Z)
                {
                    // 뒤쪽 공격 애니메이션이 있다면 설정, 없다면 다른 적절한 공격
                    anim.SetTrigger("tyrTackle"); // 예: 뒤쪽 공격 (또는 다른 공격)
                }
            }
            // isAttack 상태는 애니메이션 이벤트에서 AttackEnd()가 호출되어 false로 변경됩니다.
        }
        else // isAttack이 false일 때 (이동 또는 공격 결정 단계)
        {
            // === 이동 및 공격 결정 단계 ===

            // Stuck (제자리에 멈춤) 감지 로직
            if (transform.position == beforePosition)
            {
                stuck++;
                if (stuck >= 50) // 약 1초 (FixedUpdate 기준) 동안 움직임이 없으면
                {
                    if (player != null) targetPosition = player.transform.position; // 현재 플레이어 위치로 타겟 재설정
                    stuck = 0;
                    isWalk = true;    // 다시 이동 시도
                    walkNumber = 0;   // 이동 횟수 초기화 (공격 결정 로직에 영향)
                    Debug.Log("[TyrController] Stuck! Player 위치로 타겟 재설정.");
                }
            }
            else
            {
                stuck = 0; // 움직였으면 stuck 카운터 초기화
            }
            beforePosition = transform.position; // 현재 위치를 다음 프레임 비교를 위해 저장

            // 목표 지점 도달 여부 및 플레이어와의 거리 판단
            float targetDistance = Vector3.Distance(transform.position, targetPosition);
            float playerDistance = Vector3.Distance(transform.position, player.transform.position);

            // 목표 지점에 도달했거나, 또는 목표 지점은 아니지만 플레이어가 매우 가까이 있다면
            if (targetDistance <= distanceThreshold)
            {
                // 플레이어가 공격 범위 내에 있거나 (1.5f), 이미 한 번 이동 후 다시 목표에 도달했다면 (walkNumber >= 1) 공격 결정
                if (playerDistance <= 1.5f || walkNumber >= 1)
                {
                    isAttack = true;  // 공격 상태로 전환
                    isWalk = false;   // 걷기 중단
                    walkNumber = 0;   // 다음 교전을 위해 이동 횟수 초기화
                }
                else // 목표 지점에는 도달했지만, 플레이어가 아직 멀고, 첫 번째 이동이었다면
                {
                    walkNumber++; // 두 번째 이동 시도임을 표시
                    if (player != null) targetPosition = player.transform.position; // 현재 플레이어 위치로 타겟 업데이트
                    isWalk = true;    // 다시 걷기 상태로
                                      // timer.ResetTime(); // TimeCheck 클래스의 timer 사용 시 (현재 코드에는 직접적인 사용처 안 보임)
                }
            }
            // 목표 지점에 아직 도달하지 못했다면, 계속 이동 (isWalk가 true일 경우)

            // 실제 이동 로직 (canMove가 true이고 isWalk 상태일 때만)
            if (isWalk && canMove)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
            }
            else if (isWalk && !canMove)
            {
                // 움직일 수 없는 상태지만 isWalk가 true라면 (예: 초기화 직후), 애니메이터는 멈춰있도록 처리
                // 이 부분은 Update()의 anim.SetBool("tyrWalk", isWalk)에 의해 이미 처리될 수 있습니다.
                // 만약 정지 상태일 때 특정 애니메이션을 원한다면 여기서 추가 제어 가능.
            }
        }
    }

    public void ReadyEnd()
    {
        isReady = true;
    }

    public void AttackEnd()
    {
        targetPosition = player.transform.position;
        isWalk = true;
        walkNumber = 0;
        isAttack = false;
        stuck = 0;
        isReady = false;
    }
    public void TackleMove()
    {
        //rb.AddForce(Vector3.down.normalized * Force, ForceMode.Impulse);
        return;
    }
}
