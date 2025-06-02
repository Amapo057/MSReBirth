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
    private float chargeTime = 0f;
    private float attackDamage = 1f;

    // �÷��̾� ���� ������Ʈ
    public Animator playerSprite;
    public GameObject playerHitbox;
    public GameObject tyr;

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
        // Ű �Է�
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
            Debug.Log("���� ����");
            Charge();
        }


        if (isDodge)
        {
            rb.velocity = new Vector3(moveDirection.x * dodgeAcceleration, 0f, moveDirection.z * dodgeAcceleration);
            dodgeAcceleration *= 0.9f;

            if (dodgeAcceleration <= 0.15f)
            {
                invincible = false;
            }

            if (dodgeAcceleration <= 0.07f)
            {
                DodgeEnd();
            }
            return;
        }

        if (isAttack)
        {

            Debug.Log("���� ��");
            chargeTime += Time.deltaTime;
            attackDamage = chargeTime * 1.2f;
            if (chargeTime >= 3f)
            {
                attackDamage = 3f;
            }
            bool triggerAttack = false;

            if (Input.GetKeyUp(KeyCode.Mouse0) || chargeTime >= 3.5)
            {
                triggerAttack = true;
            }

            if (triggerAttack)
            {
                if (chargeTime >= 2.5)
                {
                    playerSprite.SetTrigger("playerAttack");
                }
                else if (chargeTime >= 1.5)
                {
                    playerSprite.SetTrigger("playerAttack");
                }
                else
                {
                    playerSprite.SetTrigger("playerAttack");
                }
            }
            return;
        }

        // �÷��̾� �̵�

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
            Debug.Log("ȸ�� ����");
            return;
        }
        else if (other.CompareTag("TyrAttackCollider"))
        {
            playerHp -= 1;
            StartCoroutine(ResetInvincibility());
        }
    }
    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(0.5f);
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
        Debug.Log("���� ����");
    }
}

