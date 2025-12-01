using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

[Serializable]
public class LocationGuideData
{
    public string locationName;      // StartPoint 이름
    [TextArea]
    public string guideMessage;      // 안내 메시지
}

public class DirectionGuide : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject uiPanel;              // 기존 위치 선택 패널
    public GameObject directionGuidePanel;  // 방향 안내 패널
    public TMP_Text guideText;              // 안내 메시지 텍스트

    [Header("연결")]
    public NavigationManager navigationManager;
    public TMP_Dropdown floorDropdown;
    public TMP_Dropdown locationDropdown;

    [Header("기본 메시지")]
    [TextArea]
    public string defaultGuideMessage = "정면의 목표물을 바라보고\n'준비 완료' 버튼을 눌러주세요";

    [Header("위치별 안내 메시지 (Inspector에서 설정)")]
    public List<LocationGuideData> locationGuides = new List<LocationGuideData>();

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<string, string> guideMessages = new Dictionary<string, string>();

    void Start()
    {
        // Inspector에서 설정한 데이터를 딕셔너리로 변환
        foreach (var guide in locationGuides)
        {
            if (!string.IsNullOrEmpty(guide.locationName))
            {
                guideMessages[guide.locationName] = guide.guideMessage;
            }
        }

        // 패널 숨기기
        if (directionGuidePanel != null)
        {
            directionGuidePanel.SetActive(false);
        }
    }

    // "길 안내 시작" 버튼에 연결 - 방향 안내 패널 표시
    public void OnStartButtonClicked()
    {
        // 현재 선택된 위치의 이름 가져오기
        string locationName = GetCurrentLocationName();

        // 해당 위치의 안내 메시지 설정
        string message = defaultGuideMessage;
        if (guideMessages.ContainsKey(locationName))
        {
            message = guideMessages[locationName];
        }

        guideText.text = message;

        // 기존 위치 선택 패널 숨기기
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // 방향 안내 패널 표시
        directionGuidePanel.SetActive(true);

        Debug.Log($"방향 안내 시작: {locationName}");
    }

    // "준비 완료" 버튼에 연결 - 실제 네비게이션 시작
    public void OnConfirmDirectionClicked()
    {
        // 방향 안내 패널 숨기기
        directionGuidePanel.SetActive(false);

        // 실제 네비게이션 시작
        navigationManager.OnStartNavigationClicked();

        Debug.Log("방향 확인 완료, 네비게이션 시작!");
    }

    // "뒤로 가기" 버튼에 연결 (선택사항)
    public void OnBackButtonClicked()
    {
        // 방향 안내 패널 숨기기
        directionGuidePanel.SetActive(false);

        // 기존 위치 선택 패널 다시 표시
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }
    }

    string GetCurrentLocationName()
    {
        int locationIndex = locationDropdown.value;
        if (locationIndex >= 0 && locationIndex < locationDropdown.options.Count)
        {
            return locationDropdown.options[locationIndex].text;
        }
        return "";
    }
}
