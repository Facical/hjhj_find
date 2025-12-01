using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;

public class NavigationManager : MonoBehaviour
{
    [Header("필수 연결 요소")]
    public NavMeshAgent agent;          // 길 안내 캐릭터
    public GuideController guideController;  // GuideController 참조
    public GameObject xrOrigin;         // AR 사용자 카메라 부모 객체
    public Transform target;            // 최종 목적지 (분실물)

    [Header("UI 요소")]
    public TMP_Dropdown floorDropdown;
    public TMP_Dropdown locationDropdown;
    
    [Header("출발지 데이터 루트")]
    public Transform startPointGroup;

    [Header("UI 패널")]
    public GameObject uiPanel;          // 안내 시작 시 숨길 UI 패널

    [Header("Agent 생성 설정")]
    public float agentSpawnDistance = 2f;  // 카메라 앞 몇 미터에 Agent 생성

    private List<Transform> currentLocations = new List<Transform>();

    void Start()
    {
        SetupFloorDropdown();
        floorDropdown.onValueChanged.AddListener(OnFloorChanged);

        // 초기화: 0번째 층 데이터 로드
        if (startPointGroup.childCount > 0)
        {
            OnFloorChanged(0);
        }

        // 다른 씬에서 전달된 목적지 위치가 있으면 Target에 적용
        if (NavigationData.hasTargetPosition)
        {
            target.position = NavigationData.targetPosition;
            Debug.Log($"[NavigationManager] Target 위치 설정됨: {target.position}");
            NavigationData.Clear(); // 사용 후 초기화
        }
    }

    void SetupFloorDropdown()
    {
        floorDropdown.ClearOptions();
        List<string> floorNames = new List<string>();

        foreach (Transform floor in startPointGroup)
        {
            floorNames.Add(floor.name);
        }
        floorDropdown.AddOptions(floorNames);
    }

    public void OnFloorChanged(int floorIndex)
    {
        locationDropdown.ClearOptions();
        currentLocations.Clear();
        
        if (floorIndex >= startPointGroup.childCount) return;

        Transform selectedFloor = startPointGroup.GetChild(floorIndex);
        List<string> locationNames = new List<string>();

        foreach (Transform location in selectedFloor)
        {
            locationNames.Add(location.name);
            currentLocations.Add(location);
        }
        locationDropdown.AddOptions(locationNames);
    }

    // ★★★ AR 환경에 맞게 수정된 버전 ★★★
    public void OnStartNavigationClicked()
    {
        Debug.Log("길 안내 시작 버튼 클릭됨!");

        int selectedLocationIndex = locationDropdown.value;

        // 리스트 범위 체크
        if (currentLocations.Count <= selectedLocationIndex)
        {
             Debug.LogError("장소 리스트가 비어있거나 인덱스 오류입니다.");
             return;
        }

        Transform startPos = currentLocations[selectedLocationIndex];

        // Agent가 꺼져있으면 켜주기
        if (!agent.gameObject.activeSelf)
        {
            agent.gameObject.SetActive(true);
        }

        // 1. AR 카메라 Transform 가져오기
        Transform cameraTransform = xrOrigin.GetComponentInChildren<Camera>().transform;

        // 2. ★ XR Origin을 startPos 위치로 이동 (실제 세계와 가상 세계 매핑) ★
        // 카메라의 로컬 위치 오프셋 계산 (XR Origin 기준 카메라가 얼마나 떨어져 있는지)
        Vector3 cameraLocalPos = cameraTransform.localPosition;
        cameraLocalPos.y = 0; // 높이는 무시

        // XR Origin 위치 설정: startPos에서 카메라 오프셋만큼 빼기
        // ★ Y값은 startPos의 Y값을 사용 (NavMesh 바닥 높이) ★
        Vector3 xrOriginPos = startPos.position - xrOrigin.transform.TransformDirection(cameraLocalPos);
        xrOriginPos.y = startPos.position.y;  // NavMesh 높이로 고정
        xrOrigin.transform.position = xrOriginPos;

        // 3. XR Origin 방향 정렬
        float currentCameraY = cameraTransform.localEulerAngles.y;
        float targetY = startPos.rotation.eulerAngles.y;
        float rotationAngle = targetY - currentCameraY;
        xrOrigin.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);

        Debug.Log($"XR Origin 이동 완료. 위치: {xrOrigin.transform.position}, 회전: {rotationAngle}");

        // 4. 이제 카메라 앞에 Agent 생성
        // XR Origin 이동 후 카메라의 새로운 월드 위치/방향 다시 계산
        Vector3 cameraWorldPos = cameraTransform.position;
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        // 카메라 앞 일정 거리에 Agent 생성 위치 계산
        // ★ Y값은 startPos 높이 사용 (NavMesh 바닥) ★
        Vector3 agentSpawnPosition = new Vector3(
            cameraWorldPos.x + cameraForward.x * agentSpawnDistance,
            startPos.position.y,  // NavMesh 높이로 고정
            cameraWorldPos.z + cameraForward.z * agentSpawnDistance
        );

        // NavMesh 위의 유효한 위치 찾기
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(agentSpawnPosition, out navHit, 5f, NavMesh.AllAreas))
        {
            agentSpawnPosition = navHit.position;
            Debug.Log($"NavMesh 위치 찾음: {agentSpawnPosition}");
        }
        else
        {
            Debug.LogWarning("카메라 앞에 유효한 NavMesh가 없어서 startPos 위치 사용");
            agentSpawnPosition = startPos.position;
        }

        // 5. Agent 이동 (Warp)
        bool warpSuccess = agent.Warp(agentSpawnPosition);

        if (warpSuccess)
        {
            // 6. Agent를 Target 방향으로 회전 (초기 방향만, 이후 GuideController에서 관리)
            Vector3 directionToTarget = (target.position - agent.transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                agent.transform.rotation = Quaternion.LookRotation(directionToTarget);
            }

            // 7. 목적지로 출발
            bool isPathFound = agent.SetDestination(target.position);

            if (isPathFound)
            {
                Debug.Log($"✅ 경로 계산 성공! Agent가 카메라 앞 {agentSpawnDistance}m 위치에서 출발합니다.");
            }
            else
            {
                Debug.LogError("❌ 경로 계산 실패! Target이 NavMesh 위에 없거나, 가는 길이 끊겨 있습니다.");
            }

            // 8. GuideController 대기 상태 리셋 (시작 후 일정 시간 자유롭게 이동)
            if (guideController != null)
            {
                guideController.ResetWaitState();
            }

            // 9. 안내 시작 성공 시에만 UI 숨기기
            if (uiPanel != null)
            {
                uiPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"Agent 이동 실패! 생성 위치가 NavMesh 위에 없습니다. 위치: {agentSpawnPosition}");
        }
    }
}