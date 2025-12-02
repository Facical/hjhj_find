using UnityEngine;

/// <summary>
/// iOS 노치/홈바 대응 Safe Area 스크립트
/// 이 스크립트가 붙은 Panel의 자식 UI들은 자동으로 안전 영역 안에 배치됩니다.
/// </summary>
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // 화면 회전 등으로 Safe Area가 변경될 수 있으므로 체크
        if (lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        // Safe Area를 앵커 값으로 변환 (0~1 범위)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // RectTransform에 적용
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log($"[SafeArea] 적용됨: {safeArea}");
    }
}
