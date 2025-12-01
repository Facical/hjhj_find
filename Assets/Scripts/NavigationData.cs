using UnityEngine;

/// <summary>
/// 씬 간 네비게이션 목적지 데이터 전달용 static 클래스
/// </summary>
public static class NavigationData
{
    public static bool hasTargetPosition = false;
    public static Vector3 targetPosition = Vector3.zero;

    public static void SetTargetPosition(float x, float y, float z)
    {
        targetPosition = new Vector3(x, y, z);
        hasTargetPosition = true;
        Debug.Log($"[NavigationData] 목적지 저장됨: {targetPosition}");
    }

    public static void Clear()
    {
        hasTargetPosition = false;
        targetPosition = Vector3.zero;
    }
}
