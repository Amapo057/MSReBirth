using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MinimalTestAgent : Agent // Agent 클래스 상속
{
    // Awake와 Start는 표준 MonoBehaviour 메시지이며, Agent 클래스에서 virtual로 선언되지 않았으므로 override 불필요
    void Awake()
    {
        Debug.LogError("====== [MinimalTestAgent] Awake() CALLED! ======");
    }

    void Start()
    {
        Debug.LogError("====== [MinimalTestAgent] Start() CALLED! ======");
    }

    // OnEnable은 Agent 클래스에 중요한 초기화 로직이 있으므로 override 하고 base.OnEnable() 호출
    protected override void OnEnable()
    {
        base.OnEnable(); // !!! 부모 Agent 클래스의 OnEnable()을 반드시 먼저 호출 !!!
        Debug.LogError("====== [MinimalTestAgent] OnEnable() CALLED! (After base.OnEnable) ======");
    }

    // OnDisable도 Agent 클래스에 정리 로직이 있을 수 있으므로 override 하고 base.OnDisable() 호출
    protected override void OnDisable()
    {
        base.OnDisable(); // !!! 부모 Agent 클래스의 OnDisable()을 반드시 먼저 호출 !!!
        Debug.LogError("====== [MinimalTestAgent] OnDisable() CALLED! (After base.OnDisable) ======");
    }

    public override void OnEpisodeBegin()
    {
        Debug.LogError("====== [MinimalTestAgent] OnEpisodeBegin() CALLED! ======");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.LogWarning("====== [MinimalTestAgent] CollectObservations() CALLED! ======");
        sensor.AddObservation(0f); // 관찰 공간 크기 1에 맞춤
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.LogError("====== [MinimalTestAgent] Heuristic() CALLED! ======");
        // 테스트를 위해 액션 설정은 생략
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.LogWarning("====== [MinimalTestAgent] OnActionReceived() CALLED! ======");
        // 테스트를 위해 매 스텝 의사결정 요청 (선택 사항, 너무 자주 호출하면 성능 문제 가능성)
        // RequestDecision();
    }
}