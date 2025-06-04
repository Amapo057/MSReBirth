using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using JetBrains.Annotations;

public class TyrController : MonoBehaviour
{
    public GameObject player;
    private Animator anim;
    public SpriteRenderer body;
    public SpriteRenderer Leg;
    // public PlayerAgent playerAgent;
    public PlayerController playerController;
    public CameraShaker cameraShaker;
    Rigidbody rb;
    public Collider tyrCollider;
    public GameObject spakePrefab;


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
    public float tyrMaxHp = 10f;
    public float tyrHp;
    private int stuck = 0;
    public float Force = 100f;
    public bool canMove = true; // Tyr가 움직일 수 있는지 여부
    private float maxXPosition = 7f;
    private float maxZPosition = 5f;
    private bool isInvincible = false;
    private bool attackMotion = false;
    public int attackNumber = 0;
    public float attackSpeed = 3f;

    // 학습용 변수
    public bool isReady = false;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        targetPosition = player.transform.position;
        tyrHp = tyrMaxHp;


    }
    public void ResetTyrState()
    {
        Debug.Log("====== [TyrController] ResetTyrState() - START ======");
        Debug.Log("[TyrController] Tyr 상태 초기화.");
        tyrHp = tyrMaxHp; // 체력을 최대로 회복
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
        Debug.Log("====== [TyrController] ResetTyrState() - END ======");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttackCollider") && !isInvincible)
        {
            float damage = playerController.attackDamage;
            tyrHp -= damage;
            if (spakePrefab != null)
            {
                Vector3 spawnPosition;

                spawnPosition = tyrCollider.ClosestPoint(other.transform.position);

                // 파티클 프리팹을 계산된 위치에 생성합니다. Quaternion.identity는 회전 없음을 의미합니다.
                Instantiate(spakePrefab, spawnPosition, Quaternion.identity);
                Debug.Log($"[TyrController] 스파크 파티클 생성 위치: {spawnPosition.ToString("F2")}");
            }
            if (damage <= 2f)
            {
                cameraShaker.ShakeCamera(0.1f, 0.05f, 0.2f);
                TriggerHitStop();
            }
            else
            {
                cameraShaker.ShakeCamera(0.15f, 0.11f, 0.6f);
                TriggerHitStop2();
            }

            isInvincible = true;
            StartCoroutine(ResetInvincibility());
            // playerAgent.PlayerHitTyr(-1);
        }
        if (tyrHp <= 0)
        {
            tyrHp = 0;
            // 여기에 Tyr 사망 관련 로직 (애니메이션, 움직임 중지 등) 추가
            Debug.LogWarning("[TyrController] Tyr 사망!");
        }
    }
    public void TriggerHitStop()
    {
        StartCoroutine(HitStopCoroutine1());
    }
    public void TriggerHitStop2()
    {
        StartCoroutine(HitStopCoroutine2());
    }

    IEnumerator HitStopCoroutine1()
    {
        float originalTimeScale = Time.timeScale; // 원래 시간 배율 저장
        Time.timeScale = 0.1f;        // 시간 느리게 만들기
        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = originalTimeScale;       // 원래 시간 배율로 복구
    }
    IEnumerator HitStopCoroutine2()
    {
        float originalTimeScale = Time.timeScale; // 원래 시간 배율 저장
        Time.timeScale = 0.1f;        // 시간 느리게 만들기

        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = originalTimeScale;       // 원래 시간 배율로 복구
    }
    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(0.7f);
        isInvincible = false;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("tyrWalk", isWalk);
    }
    void FixedUpdate()
    {
        if (!isAttack && body != null && Leg != null)
        {
            float playerDirectionX = player.transform.position.x - transform.position.x;
            if (playerDirectionX < 0 && rightDirection)
            {
                rightDirection = false;
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
            else if (playerDirectionX > 0 && !rightDirection)
            {
                rightDirection = true;
                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }
        if (isAttack)
        {
            isWalk = false;

            if (attackMotion)
            {
                isReady = true;
                Vector3 playerLocalPosition = transform.InverseTransformPoint(player.transform.position);
                float localPlayerX = playerLocalPosition.x;
                float localPlayerZ = playerLocalPosition.z;

                if (Mathf.Abs(localPlayerX) > Mathf.Abs(localPlayerZ))
                {
                    if (localPlayerX < 0)
                    {
                        anim.SetTrigger("tyrSideTail");
                        attackNumber = 1;
                    }
                    else
                    {
                        anim.SetTrigger("tyrBite");
                        attackNumber = 2;
                    }
                }
                else
                {
                    if (localPlayerZ > 0)
                    {
                        anim.SetTrigger("tyrSideTail");
                        attackNumber = 1;
                    }
                    else // 플레이어가 Tyr의 로컬 뒤쪽 (-Z)
                    {
                        anim.SetTrigger("tyrTackle");
                        attackNumber = 3;
                    }
                }
                Debug.Log("Attack");
                attackMotion = false;
            }
        }
        else
        {
            if (transform.position == beforePosition)
            {
                stuck++;
                if (stuck >= 50)
                {
                    if (player != null) targetPosition = player.transform.position;
                    stuck = 0;
                    isWalk = true;
                    walkNumber = 0;
                    Debug.Log("[TyrController] Stuck! Player 위치로 타겟 재설정.");
                }
            }
            else
            {
                stuck = 0;
            }
            beforePosition = transform.position;

            float targetDistance = Vector3.Distance(transform.position, targetPosition);
            float playerDistance = Vector3.Distance(transform.position, player.transform.position);

            if (targetDistance <= distanceThreshold)
            {
                if (playerDistance <= 1.5f || walkNumber >= 1)
                {
                    isAttack = true;
                    attackMotion = true;
                    isWalk = false;
                    walkNumber = 0;
                }
                else
                {
                    walkNumber++;
                    if (player != null) targetPosition = player.transform.position;
                    isWalk = true;
                }
            }

            if (isWalk && canMove)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
            }
        }
    }

    public void ReadyEnd()
    {
        isReady = false;
        AttackMove();
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
    public void AttackMove()
    {
        // switch (attackNumber)
        // {
        //     case 1:
        //         rb.velocity = new Vector3(0f, 0f, 1f*attackSpeed);
        //         break;
        //     case 2:
        //         rb.velocity = new Vector3(1f * attackSpeed, 0f, 0f);
        //         break;
        //     case 3:
        //         rb.velocity = new Vector3(0f, 0f, -1f * attackSpeed);
        //         break;
        // }

        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        directionToPlayer.y = 0;
        Vector3 rushDirection = directionToPlayer;

        rb.velocity = Vector3.zero; // 기존 속도 초기화 후 힘 가하기 (선택 사항)
        rb.AddForce(rushDirection * Force, ForceMode.Impulse);

        StartCoroutine(StopRushAfterTime(0.15f));
    }
    IEnumerator StopRushAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.velocity = Vector3.zero;
    }
}

