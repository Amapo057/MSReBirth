using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public float defaultShakeDuration = 0.15f;
    public float defaultShakeMagnitude = 0.1f;
    public float defaultRotationMagnitude = 1.0f; // 회전 흔들림 강도 (선택 사항)

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    // 이 함수를 외부에서 호출하여 흔들림 시작
    public void ShakeCamera(float? duration = null, float? magnitude = null, float? rotationMagnitude = null)
    {
        // 이미 흔들리는 중이면 중복 실행 방지 (선택적)
        // StopAllCoroutines(); // 이전 흔들림을 즉시 멈추고 새로 시작하려면

        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        StartCoroutine(ShakeCoroutine(duration ?? defaultShakeDuration, 
                                     magnitude ?? defaultShakeMagnitude, 
                                     rotationMagnitude ?? defaultRotationMagnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude, float rotMagnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 위치 흔들림
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            // 필요에 따라 Z축 흔들림도 추가 가능 (보통 X, Y만으로도 충분)
            // float z = Random.Range(-1f, 1f) * magnitude; 

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0); // 또는 new Vector3(x, y, z)

            // 회전 흔들림 (선택 사항)
            float rotX = Random.Range(-1f, 1f) * rotMagnitude;
            float rotY = Random.Range(-1f, 1f) * rotMagnitude;
            float rotZ = Random.Range(-1f, 1f) * rotMagnitude;
            transform.localRotation = originalLocalRot * Quaternion.Euler(rotX, rotY, rotZ);

            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        transform.localPosition = originalLocalPos; // 원래 위치로 복원
        transform.localRotation = originalLocalRot; // 원래 회전으로 복원
    }
}