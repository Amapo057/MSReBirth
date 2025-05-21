using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class TyrController : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI bossHP;
    private Animator anim;
    private TimeCheck timer;
    public SpriteRenderer body;
    public SpriteRenderer Leg;

    Vector3 targetPosition;
    Vector3 beforePosition;

    public Transform HitBox;

    public float speed = 1.5f;
    private bool isWalk = true;
    private bool isAttack = false;
    private bool rightDirection = true;
    private int walkNumber = 0;
    private float distanceThreshold = 1f;
    public int tyrHP = 20;
    private int stuck = 0;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        targetPosition = player.transform.position;
        timer = new TimeCheck();
    }

    void OnTriggerEnter(Collider other)
    {
        tyrHP -= 1;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("tyrWalk", isWalk);
        bossHP.SetText("boss hp : {0}", tyrHP);
        Debug.Log(isAttack + " walk" + isWalk);
        
    }
    void FixedUpdate()
    {
        if (isAttack)
        {
            Vector3 targetDirection = (transform.position - player.transform.position).normalized;
            if (Mathf.Abs(targetDirection.x) > Mathf.Abs(targetDirection.z))
            {
                if (targetDirection.x < 0)
                {
                    anim.SetTrigger("tyrBite");
                }
                else
                {
                    anim.SetTrigger("tyrSideTail");
                }
            }
            else
            {
                if (targetDirection.z < 0)
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
            float playerDistance = Vector3.Distance(transform.position, player.transform.position);
            float direction = beforePosition.x - transform.position.x;
            if (direction > 0 && !rightDirection)
            {
                body.flipX = true;
                Leg.flipX = true;
                rightDirection = true;
            }
            else if (direction < 0 && rightDirection)
            {
                body.flipX = false;
                Leg.flipX = false;
                rightDirection = false;
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
            if (targetDistance <= distanceThreshold || playerDistance <= distanceThreshold)
            {
                if (playerDistance <= 1.5f || walkNumber >= 1)
                {
                    isAttack = true;
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

    public void AttackEnd()
    {
        targetPosition = player.transform.position;
        isWalk = true;
        walkNumber = 0;
        isAttack = false;
        stuck = 0;
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
