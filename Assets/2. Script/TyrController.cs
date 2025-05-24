using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using JetBrains.Annotations;

public class TyrController : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI bossHP;
    private Animator anim;
    private TimeCheck timer;
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
    public float Force = 10f;
    

    // 학습용 변수
    public bool isReady = false;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        targetPosition = player.transform.position;
        timer = new TimeCheck();
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

        // Tyr 위치 초기화 (예시)
        transform.localPosition = initialPosition;

        // 기타 필요한 애니메이션 상태, 내부 변수 등 초기화
        // Animator animator = GetComponent<Animator>();
        // if (animator != null) animator.Rebind(); // 애니메이터 상태 초기화 (강력하지만 주의 필요)
        // else if (animator != null) animator.Play("Idle"); // 특정 Idle 상태로 전환

        // gameObject.SetActive(true); // 비활성화되었다면 다시 활성화
        Debug.Log("====== [TyrController] ResetTyrState() - END ======");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttackCollider"))
        {
            tyrHP -= 1;
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

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("tyrWalk", isWalk);
        bossHP.SetText("boss hp : {0}", tyrHP);
    }
    void FixedUpdate()
    {
        if (isAttack)
        {
            // Vector3 targetDirection = (transform.position - player.transform.position).normalized;
            // if (Mathf.Abs(targetDirection.x) > Mathf.Abs(targetDirection.z))
            // {
            //     if (targetDirection.x < 0)
            //     {
            //         anim.SetTrigger("tyrBite");
            //     }
            //     else
            //     {
            //         anim.SetTrigger("tyrSideTail");
            //     }
            // }
            // else
            // {
            //     if (targetDirection.z < 0)
            //     {
            //         anim.SetTrigger("tyrSideTail");
            //     }
            //     else
            //     {
            //         anim.SetTrigger("tyrTackle");
            //     }
            // }
            float targetDirectionX = player.transform.localPosition.x - transform.localPosition.x;
            float targetDirectionZ = player.transform.localPosition.z - transform.localPosition.z;

            if (Mathf.Abs(targetDirectionX) > Mathf.Abs(targetDirectionZ))
            {

                if (targetDirectionX >= 0)
                {
                    anim.SetTrigger("tyrSideTail");

                }
                else
                {
                    anim.SetTrigger("tyrBite");
                }

            }
            else
            {
                if (targetDirectionZ >= 0)
                {
                    anim.SetTrigger("tyrSideTail");
                }
                else
                {

                    anim.SetTrigger("tyrTackle");
                }
            }
        }            

        else
        {
            // if (targetDirection.x > 0 && !rightDirection)
            // {
            //     Vector3 flipScale = new Vector3(-1f * transform.localScale.x, 1.5f, 1.5f);
            //     transform.localScale = flipScale;
            //     rightDirection = true;
            // }
            // if (targetDirection.x < 0 && rightDirection)
            // {
            //     Vector3 flipScale = new Vector3(-1f * transform.localScale.x, 1.5f, 1.5f);
            //     transform.localScale = flipScale;
            //     rightDirection = false;
            // }
            float targetDistance = Vector3.Distance(transform.position, targetPosition);
            // float targetDistanceX = Mathf.Abs(transform.localPosition.x) - Mathf.Abs(targetPosition.x);
            // float targetDistanceY = Mathf.Abs(transform.localPosition.y) - Mathf.Abs(targetPosition.y);
            float playerDistance = Vector3.Distance(transform.position, player.transform.position);
            float direction = beforePosition.x - transform.position.x;
            if (direction > 0 && !rightDirection)
            {
                // body.flipX = true;
                // Leg.flipX = true;
                rightDirection = true;
                Vector3 flipScale = new Vector3(-1f * transform.localScale.x, 2f, 2f);
                transform.localScale = flipScale;
            }
            else if (direction < 0 && rightDirection)
            {
                // body.flipX = false;
                // Leg.flipX = false;
                rightDirection = false;
                Vector3 flipScale = new Vector3(-1f * transform.localScale.x, 2f, 2f);
                transform.localScale = flipScale;
            }
            if (beforePosition == transform.position)
            {
                stuck += 1;
                if (stuck >= 50)
                {
                    targetPosition = player.transform.position;
                    stuck = 0;
                }
            }
            beforePosition = transform.position;

            if (targetDistance <= distanceThreshold)
            {
                if (playerDistance <= 1.5f || walkNumber >= 1)
                {
                    isAttack = true;
                    isReady = true;
                    isWalk = false;
                }
                else
                {
                    walkNumber += 1;
                    targetPosition = player.transform.position;
                    isWalk = true;
                    timer.ResetTime();
                    stuck = 0;
                }
            }

            //이동
            if (isWalk)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
            }

        }


    }

    public void ReadyEnd()
    {
        isReady = false;
    }

    public void AttackEnd()
    {
        targetPosition = player.transform.position;
        isWalk = true;
        walkNumber = 0;
        isAttack = false;
        stuck = 0;
    }
    public void TackleMove()
    {
        rb.AddForce(Vector3.down.normalized * Force, ForceMode.Impulse);
    }
}
public class TimeCheck
{
    private float time = 0f;
    private float time2 = 0f;

    public void ResetTime()
    {
        time = 0f;
        time2 = 0f;
    }

    public bool TimeUp(float aimTime)
    {
        time += Time.deltaTime;

        if (time >= aimTime)
        {
            return true;
        }
        return false;
    }
    public bool TimeUp2(float aimTime)
    {
        time2 += Time.deltaTime;

        if (time2 >= aimTime)
        {
            return true;
        }
        return false;
    }
}
