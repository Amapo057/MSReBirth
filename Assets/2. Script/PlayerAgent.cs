using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayerMLAgent : Agent
{
    private playerController controller; // ���� playerController ����
    private Rigidbody rb;
    private Animator animator;
    private Transform hitboxController; // playerController�� hitboxController ����

    // ���Ǽҵ�� �ִ� ���� (���� ����, Inspector���� ���� ����)
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
            // controller.moveSpeed �� ���� ���� �ʿ��ϴٸ� ���⼭ �������ų� ���� ������ �� �ֽ��ϴ�.
        }
        else
        {
            Debug.LogError("PlayerController�� ã�� �� �����ϴ�. PlayerMLAgent�� ���� GameObject�� �ִ��� Ȯ�����ּ���.");
        }
    }

    public override void OnEpisodeBegin()
    {
        // ���Ǽҵ� ���� �� ȣ��� ����
        // ��: �÷��̾� ��ġ, ���� ��ġ, ü�� �� �ʱ�ȭ
        // transform.localPosition = new Vector3(0, 0.5f, 0); // ���� ���� ��ġ
        // if (controller != null)
        // {
        //    controller.isAttack = false; // ���� ���� �ʱ�ȭ
        //    // �ʿ��ϴٸ� animator.ResetTrigger("playerAttack"); �� �ִϸ��̼� ���µ� �ʱ�ȭ
        // }
        // currentStep = 0; // ���� ī���� �ʱ�ȭ
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === ���� ��� ���� (���� �ܰ迡�� ��ȭ) ===
        // ����:
        // 1. �÷��̾� �ڽ��� ����
        //    - ��ġ (X, Z) relative to some anchor or absolute
        //    - ���� �ӵ� (X, Z) (rb.velocity)
        //    - ���� �ٶ󺸴� ���� (hitboxController.forward or localRotation)
        //    - ���� ���� ���� (controller.isAttack ? 0f : 1f)
        // sensor.AddObservation(transform.localPosition);
        // sensor.AddObservation(rb.velocity.x);
        // sensor.AddObservation(rb.velocity.z);
        // sensor.AddObservation(hitboxController.forward); // �Ǵ� ����
        // sensor.AddObservation(controller.isAttack ? 0f : 1f); // ���� ���̸� 0, �ƴϸ� 1

        // 2. ���� ����
        //    - ���� ��ġ (X, Z)
        //    - �÷��̾�κ��� ���������� ����� ��ġ/���� ����
        //    - ���� ü�� (����ȭ�� ��)
        //    - ������ ���� ����/�ൿ (��: ���� �غ� ��, �̵� �� ��) - �����ϴٸ�
        // GameObject boss = GameObject.FindGameObjectWithTag("Boss"); // ����
        // if (boss != null)
        // {
        //    sensor.AddObservation(boss.transform.localPosition);
        //    sensor.AddObservation(boss.transform.localPosition - transform.localPosition);
        //    // sensor.AddObservation(boss.GetComponent<BossScript>().currentHealth / boss.GetComponent<BossScript>().maxHealth);
        // }

        // 3. ��Ÿ ȯ�� ����
        //    - ���̳� ��ֹ����� �Ÿ� (Raycast ��� ����)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // ������Ʈ�� ������ �ൿ�� ����
        // �� Agent�� Ȱ��ȭ �Ǿ� ���� ���� playerController�� Update/FixedUpdate�� ����� �Է� ó���� ���� �ʴ´ٰ� ����

        // currentStep++; // ���� ī���� ����
        // if (maxStepsPerEpisode > 0 && currentStep >= maxStepsPerEpisode)
        // {
        //    // �ִ� ���� ���� �� ���Ǽҵ� ���� (�ð� �ʰ�)
        //    SetReward(-1.0f); // ����: �ð� �ʰ� �� ���� ����
        //    EndEpisode();
        // }

        // ���� �ൿ: X�� �̵�, Z�� �̵�
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        // �̻� �ൿ: ���� (0: ����, 1: ��)
        int attackAction = actions.DiscreteActions[0];

        // --- �̵� ó�� ---
        if (controller != null && !controller.isAttack) // playerController�� isAttack ���¸� ���� ����
        {
            Vector3 moveDirection = new Vector3(moveX, 0f, moveZ);
            // playerController�� moveSpeed ���
            Vector3 targetVelocity = moveDirection.normalized * controller.moveSpeed;
            // ���� playerController�� FixedUpdate ���� ����
            targetVelocity.z *= 1.5f;
            targetVelocity.y = rb.velocity.y; // �߷� �� Y�� �������� ����
            rb.velocity = targetVelocity;

            // �ִϸ����� �� ��Ʈ�ڽ� ���� ������Ʈ (playerController�� Update ���� ����)
            float inputMagnitude = moveDirection.magnitude;
            animator.SetFloat("playerWalkSpeed", inputMagnitude);

            if (inputMagnitude > 0.01f) // �ణ�� Deadzone�� �ξ� ���� ������ ����
            {
                // playerController�� inputWalk.y�� playerDirectionY�� ��������Ƿ�, moveZ�� ���
                animator.SetFloat("playerDirectionX", moveX);
                animator.SetFloat("playerDirectionY", moveZ);

                // ��Ʈ�ڽ�(ĳ����) ���� ��ȯ
                // ���� ����: X�� �Է¿� ���� �¿� 180�� ȸ��, Y��(Z�� �Է�)�� ���󼭴� ���� ������ �ٶ󺸵��� �ϴ� ���� �Ϲ����Դϴ�.
                // ���� playerController�� X�� �Է����θ� �¿츦 �����ϰ�, Y�� �Է����δ� localRotation�� �̻��ϰ� �����ϰ� �ֽ��ϴ�.
                // 3D ȯ���̹Ƿ�, ĳ���Ͱ� �̵��ϴ� ������ �ڿ������� �ٶ󺸵��� �����ϴ� ���� �����մϴ�.
                // ����: if (moveDirection.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(moveDirection);
                // �켱�� ���� playerController�� X�� ��� ȸ�� ������ �ִ��� ������, Z�࿡ ���� �κ��� �ּ� ó���մϴ�.
                if (moveX > 0.01f)
                {
                    hitboxController.localRotation = Quaternion.Euler(0, 0, 0); // ������ ������
                }
                else if (moveX < -0.01f)
                {
                    // ���� �ڵ�: hitboxController.rotation = Quaternion.Euler(0, -180, 0);
                    // localRotation�� ����ϴ� ���� �Ϲ������� �� ���� �����մϴ�.
                    hitboxController.localRotation = Quaternion.Euler(0, 180, 0); // ���� ������
                }
                // else if (moveZ != 0) // ���� �ڵ�: inputWalk.y != 0
                // {
                //    // hitboxController.localRotation = Quaternion.Euler(0, -moveZ * 90, 0); // �� ������ Z�� �̵��� ���� 90�� ȸ���ϴµ�, 3D ���������� ���ڿ������� �� ����
                // }
            }
        }

        // --- ���� ó�� ---
        // ���� ���� ����: ���� �׼��� ���õǾ���, ���� ���� ���� �ƴ� ��
        if (attackAction == 1 && controller != null && !controller.isAttack)
        {
            animator.SetTrigger("playerAttack");
            // playerController�� OnAttack()�� InputSystem �ݹ��̶� ���� ȣ������ ����.
            // ���, playerAttack �ִϸ��̼��� ���۵Ǹ� �ִϸ��̼� �̺�Ʈ�� playerController��
            // HitboxActive()�� ȣ���ϰ�, isAttack = true�� �����ϴ� ������ �ִٸ� �װ��� �����ϴ�.
            // (playerController�� OnAttack �Լ� ���ο� isAttack = true; �� �����Ƿ�,
            //  ML-Agent�� animator.SetTrigger("playerAttack")�� �ϸ�,
            //  playerController�� ���� �ִϸ��̼��� ����ǰ�, ���� ����(isAttack)�� ��Ʈ�ڽ���
            //  playerController�� �ִϸ��̼� �̺�Ʈ �ڵ鷯���� ó���� ������ ����մϴ�.)
            //  ���� playerController.OnAttack()�� isAttack = true ������ �ݹ� �Լ� �ȿ��� �ִٸ�,
            //  Agent�� ������ ������ �� controller.isAttack = true; �� ���� �����ϰų�,
            //  playerController�� Agent�� ���� ���� �Լ��� ������ �� ���� �ֽ��ϴ�.
            //  (�ϴ� �ִϸ��̼� �̺�Ʈ�� ������ ó���Ѵٰ� ����)
        }

        // === ���� �Լ� (���� �ܰ迡�� ��ȭ) ===
        // ����: �� ���ܸ��� ���� ���� ���� (���� ��� �Ǵ� ���� �ൿ ����)
        // AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // ������ �׽�Ʈ�� ���� ���� (���� playerController�� �Է� ������ ���)
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // playerController�� inputActions�� �����ϰ� ���� �о�ɴϴ�. (Input System ��� ����)
        // private ActionManager inputActions; // Initialize���� new ActionManager() �ʿ�
        // private Vector2 currentInputWalk;
        // inputActions.playerAction.Enable(); // OnEnable ���
        // currentInputWalk = inputActions.playerAction.walk.ReadValue<Vector2>();

        // ���⼭�� ������ Unity�� ���� Input Manager ��� ���÷� ��ü�մϴ�.
        // �����δ� ������Ʈ�� Input System ������ �����ų�, ����ڰ� ������ playerController�� inputActions�� Ȱ���ؾ� �մϴ�.
        continuousActions[0] = Input.GetAxis("Horizontal"); // �¿� �̵� (A, D �Ǵ� ȭ��ǥ �¿�)
        continuousActions[1] = Input.GetAxis("Vertical");   // ���� �̵� (W, S �Ǵ� ȭ��ǥ ���Ʒ�)

        discreteActions[0] = Input.GetMouseButtonDown(0) ? 1 : 0; // ���콺 ��Ŭ������ ����
                                                                  // �Ǵ� Space Ű ������ ���� ����: Input.GetKeyDown(KeyCode.Space) ? 1 : 0;
    }
}