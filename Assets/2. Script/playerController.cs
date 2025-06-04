using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    // �÷��̾� �۵� ����
    public float moveSpeed = 2f;
    private Vector3 moveDirection = new Vector3(1f, 0f, 0f);
    public float playerHp = 10;

    private bool isDodge = false;
    public float dodgeAcceleration = 10f;
    private bool invincible = false;

    private bool isAttack = false;
    private bool isHit = false;
    private float chargeTime = 0f;
    public float attackDamage = 1f;
    public float playerKnockbackForce = 5f;

    // �÷��̾� ���� ������Ʈ
    public Animator playerSprite;
    public GameObject playerHitbox;
    public GameObject tyr;
    public CameraShaker cameraShaker;
    // �÷��̾� ������Ʈ
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
    }

    private void FixedUpdate()
    {
        float moveX = 0;
        float moveZ = 0;

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
            bool triggerAttack = false;

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                triggerAttack = true;
            }
            if (chargeTime >= 3.5f)
            {
                triggerAttack = true;
            }

            if (triggerAttack)
            {
                if (chargeTime >= 1.6f || chargeTime <= 2f)
                {
                    playerSprite.SetTrigger("playerAttack");
                }
                else
                {
                    playerSprite.SetTrigger("playerAttack");
                }
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
            rb.velocity = moveDirection * moveSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (invincible)
        {
            return;
        }
        else if (other.CompareTag("TyrAttackCollider"))
        {
            cameraShaker.ShakeCamera();
            
            PlayerTookDamage();

            playerSprite.SetTrigger("playerHit");
        }
    }
    public void PlayerTookDamage()
    {
        isHit = true;
        playerHp -= 1f;
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

            rb.AddForce(knockbackDirection.normalized * playerKnockbackForce, ForceMode.Impulse);
            Debug.Log($"[PlayerAgent] 플레이어 넉백! 방향: {knockbackDirection.normalized.ToString("F2")}, 힘: {playerKnockbackForce}");
        }

        if (playerHp <= 0)
        {
            playerHp = 0;
        }
    }
    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(0.6f);
        invincible = false;
    }
    

    private void Dodge()
    {
        isDodge = true;
        invincible = true;
        playerSprite.SetTrigger("playerDodge");

    }
    public void DodgeEnd()
    {
        dodgeAcceleration = 10f;
        isDodge = false;
    }
    private void Charge()
    {
        isAttack = true;
        playerSprite.SetTrigger("playerCharge");
    }
    public void AttackEnd()
    {
        isAttack = false;
        attackDamage = 1f;
        chargeTime = 0f;
    }
    public void HitEnd()
    {
        isHit = false;
    }
}

