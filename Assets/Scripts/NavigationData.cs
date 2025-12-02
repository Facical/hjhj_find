using UnityEngine;

/// <summary>
/// 씬 간 네비게이션 목적지 데이터 전달용 static 클래스
/// </summary>
public static class NavigationData
{
    public static bool hasTargetPosition = false;
    public static Vector3 targetPosition = Vector3.zero;

    // 분실물 회수를 위한 ID 저장
    public static int targetItemId = -1;
    public static string targetItemLabel = "";

    public static void SetTargetPosition(float x, float y, float z, int itemId = -1, string itemLabel = "")
    {
        targetPosition = new Vector3(x, y, z);
        hasTargetPosition = true;
        targetItemId = itemId;
        targetItemLabel = itemLabel;
        Debug.Log($"[NavigationData] 목적지 저장됨: {targetPosition}, ID: {itemId}, 이름: {itemLabel}");
    }

    public static void Clear()
    {
        hasTargetPosition = false;
        targetPosition = Vector3.zero;
        targetItemId = -1;
        targetItemLabel = "";
    }
}
