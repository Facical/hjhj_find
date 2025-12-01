using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class PlyExporter : EditorWindow
{
    // 메뉴 경로를 조금 수정했습니다.
    [MenuItem("Export/Export Selection to PLY (Recursive)")]
    static void ExportToPLY()
    {
        GameObject rootObject = Selection.activeGameObject;

        if (rootObject == null)
        {
            Debug.LogError("오브젝트를 먼저 선택해주세요.");
            return;
        }

        // [핵심 수정] 선택한 오브젝트와 그 자식들에 있는 모든 MeshFilter를 가져옵니다.
        MeshFilter[] meshFilters = rootObject.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length == 0)
        {
            Debug.LogError("선택한 오브젝트(혹은 자식)에 MeshFilter가 하나도 없습니다.");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save PLY", "", rootObject.name, "ply");
        if (string.IsNullOrEmpty(path)) return;

        StringBuilder sb = new StringBuilder();
        List<Vector3> allVertices = new List<Vector3>();

        // 모든 자식 메쉬를 순회하며 정점 수집
        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;
            
            // 정점 좌표 변환을 위해 Transform 가져오기
            Transform trans = mf.transform;

            foreach (Vector3 v in vertices)
            {
                // [중요] 각 자식의 로컬 좌표(v)를 월드 좌표로 변환해야
                // 건물의 층(1F, 2F)들이 제 자리에 정확히 배치되어 합쳐집니다.
                Vector3 worldPos = trans.TransformPoint(v);
                allVertices.Add(worldPos);
            }
        }

        // --- PLY 파일 작성 ---
        sb.AppendLine("ply");
        sb.AppendLine("format ascii 1.0");
        sb.AppendLine($"element vertex {allVertices.Count}");
        sb.AppendLine("property float x");
        sb.AppendLine("property float y");
        sb.AppendLine("property float z");
        sb.AppendLine("end_header");

        foreach (Vector3 v in allVertices)
        {
            // Unity(Y-Up) -> PLY 표준(보통 Z-Up)으로 좌표축 변경 저장
            // 필요 없다면 v.x, v.y, v.z 로 사용하세요.
            sb.AppendLine($"{v.x} {v.z} {v.y}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"PLY Export 완료! 총 {allVertices.Count}개의 정점이 저장됨: {path}");
    }
}