using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GuideController : MonoBehaviour
{
    [Header("연결 요소")]
    public NavMeshAgent agent;       // 안내 캐릭터
    public Transform userCamera;     // 사용자 카메라
    public Animator dogAnimator;     // ★ 여기가 추가된 부분입니다! (강아지 애니메이터)
    public ServerConnector serverConnector;  // 서버 통신용

    [Header("거리 설정")]
    public float waitDistance = 3.0f;
    public float resumeDistance = 2.0f;

    [Header("NavMeshLink 설정")]
    public float linkTraverseSpeed = 1.2f;  // Link 이동 속도 (Agent Speed와 동일하게)

    private bool isWaiting = false;
    private bool isTraversingLink = false;
    private float startGracePeriod = 2f;  // 시작 후 2초간 대기 로직 비활성화
    private float timeSinceStart = 0f;

    [Header("테스트용")]
    public Transform testTarget;  // Inspector에서 Target 연결
    [Tooltip("에디터에서 테스트 시 체크하세요. 빌드 시 자동으로 무시됩니다.")]
    public bool disableWaitLogicInEditor = true;  // 에디터에서만 대기 로직 비활성화

    [Header("회전 설정")]
    [Tooltip("Agent가 실제 이동 방향으로만 회전하도록 합니다 (모서리에서 미리 회전 방지)")]
    public bool useVelocityRotation = true;
    public float rotationSpeed = 10f;  // 회전 속도

    [Header("도착 감지 설정")]
    public float arrivalThreshold = 1.5f;  // 도착 판정 거리
    private bool hasArrived = false;

    [Header("회수 확인 팝업")]
    public GameObject recoveryPopup;      // 회수 확인 팝업 UI
    public TMP_Text popupMessageText;     // 팝업 메시지 텍스트 (TextMeshPro)

    void OnEnable()
    {
        // Agent가 활성화될 때 멈춤 상태 초기화
        ResetWaitState();

        // ★ 활성화될 때마다 회전 설정 적용 ★
        if (useVelocityRotation && agent != null)
        {
            agent.updateRotation = false;
        }
    }

    // 외부에서 호출 가능: 대기 상태 리셋
    public void ResetWaitState()
    {
        isWaiting = false;
        timeSinceStart = 0f;
        hasArrived = false;  // 도착 상태도 리셋
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    void Start()
    {
        // ★ 자동 회전 비활성화 (velocity 기반 회전 사용 시) ★
        if (useVelocityRotation && agent != null)
        {
            agent.updateRotation = false;
        }

        // 테스트: Play 누르면 바로 목적지로 이동
        if (testTarget != null && agent != null)
        {
            bool result = agent.SetDestination(testTarget.position);
            Debug.Log($"[테스트] SetDestination 결과: {result}, Target 위치: {testTarget.position}");
        }
    }

    void Update()
    {
        // NavMeshLink 위에 있는지 체크
        if (agent.isOnOffMeshLink && !isTraversingLink)
        {
            Debug.Log("[GuideController] NavMeshLink 진입! 걷기 모드로 이동 시작");
            StartCoroutine(TraverseLink());
        }

        // ★ 애니메이션 속도 제어 코드 추가 ★
        // Agent의 현재 속도(velocity) 크기를 가져옵니다.
        float currentSpeed = isTraversingLink ? linkTraverseSpeed : agent.velocity.magnitude;

        // 애니메이터의 "Speed" 파라미터에 값을 전달합니다.
        if (dogAnimator != null)
        {
            dogAnimator.SetFloat("Speed", currentSpeed);
        }

        // ★ Velocity 기반 회전 (모서리에서 미리 회전 방지) ★
        if (useVelocityRotation && !isTraversingLink && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                agent.transform.rotation = Quaternion.Slerp(
                    agent.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }

        // --- 도착 감지 로직 ---
        // Agent가 목적지에 도착했는지 확인
        if (!hasArrived && agent.hasPath && !agent.pathPending)
        {
            if (agent.remainingDistance <= arrivalThreshold)
            {
                hasArrived = true;
                agent.isStopped = true;
                Debug.Log($"[GuideController] 목적지 도착! 분실물 ID: {NavigationData.targetItemId}");
                ShowRecoveryPopup();
            }
        }

        // --- 기존 로직 (거리 계산 및 대기) ---
        // 목적지가 없거나 경로 계산 중이면 패스
        if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            return;

        // 에디터에서만 대기 로직 비활성화 (빌드에서는 항상 작동)
        #if UNITY_EDITOR
        if (disableWaitLogicInEditor)
            return;
        #endif

        // 시작 직후 일정 시간은 대기 로직 비활성화 (사용자가 준비할 시간)
        timeSinceStart += Time.deltaTime;
        if (timeSinceStart < startGracePeriod)
            return;

        // 사용자와의 거리 계산
        float distance = Vector3.Distance(
            new Vector3(agent.transform.position.x, 0, agent.transform.position.z),
            new Vector3(userCamera.position.x, 0, userCamera.position.z)
        );

        // 거리 상태에 따라 멈추거나 다시 가기
        if (!isWaiting)
        {
            if (distance > waitDistance)
            {
                Debug.Log($"[GuideController] 사용자가 너무 멀어서 대기! 거리: {distance:F2}m > {waitDistance}m");
                isWaiting = true;
                agent.isStopped = true;
            }
        }
        else
        {
            if (distance < resumeDistance)
            {
                Debug.Log($"[GuideController] 사용자가 가까워져서 재개! 거리: {distance:F2}m < {resumeDistance}m");
                isWaiting = false;
                agent.isStopped = false;
            }
        }
    }

    // NavMeshLink를 걷듯이 자연스럽게 이동
    IEnumerator TraverseLink()
    {
        isTraversingLink = true;

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = linkData.endPos + Vector3.up * agent.baseOffset;

        // 시작점에서 끝점까지의 거리 계산
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / linkTraverseSpeed;
        float elapsed = 0f;

        // 이동 방향으로 회전
        Vector3 direction = (endPos - startPos).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = targetRotation;
        }

        // 선형 보간으로 부드럽게 이동
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            agent.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 위치 보정
        agent.transform.position = endPos;

        // Link 이동 완료 알림
        agent.CompleteOffMeshLink();

        isTraversingLink = false;
    }

    // =====================================================
    // 회수 확인 팝업 관련 메서드
    // =====================================================

    // 팝업 표시
    void ShowRecoveryPopup()
    {
        if (recoveryPopup != null)
        {
            recoveryPopup.SetActive(true);

            // 메시지 텍스트가 있으면 분실물 이름 표시
            if (popupMessageText != null && !string.IsNullOrEmpty(NavigationData.targetItemLabel))
            {
                popupMessageText.text = $"'{NavigationData.targetItemLabel}'을(를) 회수하셨습니까?";
            }
        }
        else
        {
            Debug.LogWarning("[GuideController] recoveryPopup이 연결되지 않았습니다!");
        }
    }

    // "예" 버튼 클릭 시 호출
    public void OnRecoveryConfirm()
    {
        Debug.Log("[GuideController] 회수 확인됨!");

        // 팝업 숨기기
        if (recoveryPopup != null)
        {
            recoveryPopup.SetActive(false);
        }

        // 서버에 visibility 업데이트 요청
        if (serverConnector != null && NavigationData.targetItemId > 0)
        {
            StartCoroutine(serverConnector.UpdateVisibility(
                NavigationData.targetItemId,
                "hidden",
                (success) =>
                {
                    if (success)
                    {
                        Debug.Log("[GuideController] 분실물 회수 처리 완료!");
                    }
                    else
                    {
                        Debug.LogError("[GuideController] 분실물 회수 처리 실패!");
                    }

                    // 성공/실패 관계없이 목록 화면으로 돌아가기
                    NavigationData.Clear();
                    SceneManager.LoadScene("kitFind");
                }
            ));
        }
        else
        {
            Debug.LogWarning("[GuideController] ServerConnector가 없거나 유효한 ID가 없습니다.");
            NavigationData.Clear();
            SceneManager.LoadScene("kitFind");
        }
    }

    // "아니오" 버튼 클릭 시 호출
    public void OnRecoveryCancel()
    {
        Debug.Log("[GuideController] 회수 취소됨, 안내 계속");

        // 팝업 숨기기
        if (recoveryPopup != null)
        {
            recoveryPopup.SetActive(false);
        }

        // 다시 이동 가능하도록 리셋
        hasArrived = false;
        agent.isStopped = false;
    }
}