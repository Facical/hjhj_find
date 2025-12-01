using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class DrawPath : MonoBehaviour
{
    public NavMeshAgent agent;
    public LineRenderer lineRenderer;
    public float heightOffset = 0.5f;   // 바닥에서 띄울 높이
    public float curveSmoothness = 0.5f; // 곡선 부드러움 정도 (0~1)
    public float scrollSpeed = 1.0f;    // 화살표 이동 속도

    void Update()
    {
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            DrawSmoothedPath();
            AnimateLine(); // 화살표 흐르는 애니메이션
        }
    }

    void DrawSmoothedPath()
    {
        var path = agent.path;
        Vector3[] corners = path.corners;
        
        // 점이 부족하면 그냥 직선 그리기
        if (corners.Length < 2)
        {
            lineRenderer.positionCount = corners.Length;
            lineRenderer.SetPositions(corners);
            return;
        }

        // 베지에 곡선 포인트 계산
        List<Vector3> smoothPoints = new List<Vector3>();
        
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 p0 = corners[i];
            Vector3 p1 = corners[i + 1];

            // 높이 보정
            p0.y += heightOffset;
            p1.y += heightOffset;

            // 두 점 사이를 여러 개로 쪼개서 곡선처럼 보이게 함
            int segments = 10; // 점 사이를 10등분
            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                Vector3 point = Vector3.Lerp(p0, p1, t); // 간단한 선형 보간 (복잡한 베지어 대신 안정적인 방식)
                smoothPoints.Add(point);
            }
        }

        lineRenderer.positionCount = smoothPoints.Count;
        lineRenderer.SetPositions(smoothPoints.ToArray());
    }

    void AnimateLine()
    {
        // 재질(Material)의 텍스처를 움직여서 흐르는 효과를 줌
        if (lineRenderer.material != null)
        {
            float offset = Time.time * scrollSpeed;
            lineRenderer.material.mainTextureOffset = new Vector2(-offset, 0);
        }
    }
}