using UnityEngine;
using System.Collections.Generic;

public class ColmapToUnityTransformer : MonoBehaviour
{
    [Header("=== COLMAP 데이터 입력 ===")]
    [Tooltip("COLMAP translation (x, y, z)")]
    public Vector3 testColmapPosition;
    
    [Tooltip("COLMAP rotation_xyzw 쿼터니언")]
    public Vector4 testColmapRotationXYZW;
    
    [Header("=== 변환된 Unity 데이터 (읽기 전용) ===")]
    [SerializeField] private Vector3 convertedUnityPosition;
    [SerializeField] private Vector3 convertedUnityRotationEuler;
    
    [Header("=== 설정 ===")]
    public bool autoUpdate = true;
    
    [Header("=== 계산된 변환 파라미터 ===")]
    [SerializeField] private Vector3 axisMapping;
    [SerializeField] private Vector3 axisOrder;
    [SerializeField] private Vector3 calculatedScalePerAxis;
    [SerializeField] private Vector3 calculatedOffset;
    [SerializeField] private float averageError;

    // 캘리브레이션 데이터
    private List<Vector3> colmapPoints = new List<Vector3>();
    private List<Vector3> unityPoints = new List<Vector3>();
    
    // 내부 변환 파라미터
    private Vector3 _axisMapping = Vector3.one;
    private int[] _axisOrder = { 0, 1, 2 };
    private Vector3 _scalePerAxis = Vector3.one;
    private Vector3 _offset = Vector3.zero;
    private bool isCalibrated = false;

    void Start()
    {
        InitializeCalibrationData();
        FindOptimalTransform();
    }

    void Update()
    {
        if (autoUpdate && isCalibrated)
        {
            convertedUnityPosition = TransformColmapToUnity(testColmapPosition);
            transform.position = convertedUnityPosition;
            
            Quaternion unityRotation = TransformColmapRotationToUnity(testColmapRotationXYZW);
            transform.rotation = unityRotation;
            convertedUnityRotationEuler = unityRotation.eulerAngles;
        }
    }

    void InitializeCalibrationData()
    {
        colmapPoints.Clear();
        unityPoints.Clear();
        
        // COLMAP 좌표 (translation 값)
        colmapPoints.Add(new Vector3(6.29771f, 0.22048f, -1.04017f));      // 4층 계단 앞
        colmapPoints.Add(new Vector3(-0.949919f, -0.122728f, 5.93582f));   // 441 앞
        colmapPoints.Add(new Vector3(2.4229f, -0.692227f, 2.68672f));      // 329 끝자락
        colmapPoints.Add(new Vector3(-3.1063f, -1.48395f, 4.92886f));      // 3층 계단
        colmapPoints.Add(new Vector3(2.18804f, 0.0902273f, -2.45804f));    // 329 입구
        colmapPoints.Add(new Vector3(0.602709f, -0.183206f, -1.26429f));   // 포인트 6
        colmapPoints.Add(new Vector3(-1.56418f, 1.12137f, -4.51555f));     // 포인트 7
        colmapPoints.Add(new Vector3(-1.0657f, 0.504925f, -0.524857f));    // 포인트 8 (새로 추가)

        // Unity 글로벌 좌표
        unityPoints.Add(new Vector3(12.95f, 7.77f, -25.806f));      // 4층 계단 앞
        unityPoints.Add(new Vector3(-43.53f, 7.366f, -23.82f));     // 441 앞
        unityPoints.Add(new Vector3(-0.483f, 3.398f, -19.447f));    // 329 끝자락
        unityPoints.Add(new Vector3(13.1f, 3.16f, -25.74f));        // 3층 계단
        unityPoints.Add(new Vector3(-2.532f, 3.398f, -25.623f));    // 329 입구
        unityPoints.Add(new Vector3(-32.016f, 3.38f, -24.555f));    // 포인트 6
        unityPoints.Add(new Vector3(-43.54f, 7.66f, -19.28f));      // 포인트 7
        unityPoints.Add(new Vector3(-22.2f, 7.66f, -19.99f));       // 포인트 8 (새로 추가)
    }

    void FindOptimalTransform()
    {
        int[,] permutations = {
            {0, 1, 2}, {0, 2, 1}, {1, 0, 2}, {1, 2, 0}, {2, 0, 1}, {2, 1, 0}
        };
        float[] signs = { 1f, -1f };
        
        float bestError = float.MaxValue;
        int bestPerm = 0;
        float bestSx = 1, bestSy = 1, bestSz = 1;
        Vector3 bestScalePerAxis = Vector3.one;
        Vector3 bestOffset = Vector3.zero;

        for (int p = 0; p < 6; p++)
        {
            int ax = permutations[p, 0];
            int ay = permutations[p, 1];
            int az = permutations[p, 2];

            foreach (float sx in signs)
            {
                foreach (float sy in signs)
                {
                    foreach (float sz in signs)
                    {
                        List<Vector3> transformedColmap = new List<Vector3>();
                        foreach (var colmap in colmapPoints)
                        {
                            float[] axes = { colmap.x, colmap.y, colmap.z };
                            transformedColmap.Add(new Vector3(
                                sx * axes[ax],
                                sy * axes[ay],
                                sz * axes[az]
                            ));
                        }

                        Vector3 scaleVal;
                        Vector3 offsetVal;
                        float error = ComputePerAxisScaleAndOffset(transformedColmap, out scaleVal, out offsetVal);

                        if (error < bestError)
                        {
                            bestError = error;
                            bestPerm = p;
                            bestSx = sx; bestSy = sy; bestSz = sz;
                            bestScalePerAxis = scaleVal;
                            bestOffset = offsetVal;
                        }
                    }
                }
            }
        }

        _axisOrder = new int[] { permutations[bestPerm, 0], permutations[bestPerm, 1], permutations[bestPerm, 2] };
        _axisMapping = new Vector3(bestSx, bestSy, bestSz);
        _scalePerAxis = bestScalePerAxis;
        _offset = bestOffset;
        isCalibrated = true;

        // Inspector 표시용
        axisOrder = new Vector3(_axisOrder[0], _axisOrder[1], _axisOrder[2]);
        axisMapping = _axisMapping;
        calculatedScalePerAxis = _scalePerAxis;
        calculatedOffset = _offset;
        averageError = bestError;

        Debug.Log("=== 최적 변환 파라미터 계산 완료 ===");
        Debug.Log($"축 순서: Unity.X←COLMAP[{_axisOrder[0]}], Unity.Y←COLMAP[{_axisOrder[1]}], Unity.Z←COLMAP[{_axisOrder[2]}]");
        Debug.Log($"축 부호: ({_axisMapping.x}, {_axisMapping.y}, {_axisMapping.z})");
        Debug.Log($"축별 스케일: X={_scalePerAxis.x:F4}, Y={_scalePerAxis.y:F4}, Z={_scalePerAxis.z:F4}");
        Debug.Log($"오프셋: {_offset}");
        Debug.Log($"평균 오차: {bestError:F3}m");
        
        // 각 포인트별 오차 출력
        Debug.Log("\n=== 포인트별 오차 ===");
        string[] pointNames = { "4층 계단 앞", "441 앞", "329 끝자락", "3층 계단", "329 입구", "포인트6", "포인트7", "포인트8" };
        for (int i = 0; i < colmapPoints.Count; i++)
        {
            Vector3 transformed = TransformColmapToUnity(colmapPoints[i]);
            float error = Vector3.Distance(transformed, unityPoints[i]);
            Debug.Log($"{pointNames[i]}: 변환={transformed}, 실제={unityPoints[i]}, 오차={error:F2}m");
        }
    }

    float ComputePerAxisScaleAndOffset(List<Vector3> transformedColmap, out Vector3 scaleOut, out Vector3 offsetOut)
    {
        int n = colmapPoints.Count;
        
        float sumCx = 0, sumCy = 0, sumCz = 0;
        float sumUx = 0, sumUy = 0, sumUz = 0;
        float sumCxCx = 0, sumCyCy = 0, sumCzCz = 0;
        float sumCxUx = 0, sumCyUy = 0, sumCzUz = 0;

        for (int i = 0; i < n; i++)
        {
            Vector3 c = transformedColmap[i];
            Vector3 u = unityPoints[i];
            
            sumCx += c.x; sumCy += c.y; sumCz += c.z;
            sumUx += u.x; sumUy += u.y; sumUz += u.z;
            sumCxCx += c.x * c.x; sumCyCy += c.y * c.y; sumCzCz += c.z * c.z;
            sumCxUx += c.x * u.x; sumCyUy += c.y * u.y; sumCzUz += c.z * u.z;
        }

        float scaleX = (n * sumCxUx - sumCx * sumUx) / (n * sumCxCx - sumCx * sumCx + 0.0001f);
        float scaleY = (n * sumCyUy - sumCy * sumUy) / (n * sumCyCy - sumCy * sumCy + 0.0001f);
        float scaleZ = (n * sumCzUz - sumCz * sumUz) / (n * sumCzCz - sumCz * sumCz + 0.0001f);
        
        scaleOut = new Vector3(scaleX, scaleY, scaleZ);
        
        offsetOut = new Vector3(
            (sumUx - scaleX * sumCx) / n,
            (sumUy - scaleY * sumCy) / n,
            (sumUz - scaleZ * sumCz) / n
        );

        float totalError = 0;
        for (int i = 0; i < n; i++)
        {
            Vector3 c = transformedColmap[i];
            Vector3 predicted = new Vector3(
                c.x * scaleOut.x + offsetOut.x,
                c.y * scaleOut.y + offsetOut.y,
                c.z * scaleOut.z + offsetOut.z
            );
            totalError += Vector3.Distance(predicted, unityPoints[i]);
        }
        
        return totalError / n;
    }

    public Vector3 TransformColmapToUnity(Vector3 colmapPos)
    {
        if (!isCalibrated)
        {
            Debug.LogWarning("캘리브레이션이 완료되지 않았습니다!");
            return colmapPos;
        }
        
        float[] axes = { colmapPos.x, colmapPos.y, colmapPos.z };
        
        Vector3 mapped = new Vector3(
            _axisMapping.x * axes[_axisOrder[0]],
            _axisMapping.y * axes[_axisOrder[1]],
            _axisMapping.z * axes[_axisOrder[2]]
        );
        
        return new Vector3(
            mapped.x * _scalePerAxis.x + _offset.x,
            mapped.y * _scalePerAxis.y + _offset.y,
            mapped.z * _scalePerAxis.z + _offset.z
        );
    }

    public Quaternion TransformColmapRotationToUnity(Vector4 colmapRotXYZW)
    {
        if (!isCalibrated)
        {
            return Quaternion.identity;
        }

        float cx = colmapRotXYZW.x;
        float cy = colmapRotXYZW.y;
        float cz = colmapRotXYZW.z;
        float cw = colmapRotXYZW.w;

        float[] qAxes = { cx, cy, cz };
        
        float ux = _axisMapping.x * qAxes[_axisOrder[0]];
        float uy = _axisMapping.y * qAxes[_axisOrder[1]];
        float uz = _axisMapping.z * qAxes[_axisOrder[2]];
        
        Quaternion colmapQuat = new Quaternion(ux, uy, uz, cw);
        Quaternion correction = Quaternion.Euler(0, 180, 0);
        
        return colmapQuat * correction;
    }

    [ContextMenu("수동으로 위치/회전 업데이트")]
    void ManualUpdate()
    {
        if (!isCalibrated)
        {
            InitializeCalibrationData();
            FindOptimalTransform();
        }
        
        convertedUnityPosition = TransformColmapToUnity(testColmapPosition);
        transform.position = convertedUnityPosition;
        
        Quaternion unityRotation = TransformColmapRotationToUnity(testColmapRotationXYZW);
        transform.rotation = unityRotation;
        convertedUnityRotationEuler = unityRotation.eulerAngles;
        
        Debug.Log($"Position: {testColmapPosition} → {convertedUnityPosition}");
        Debug.Log($"Rotation: {testColmapRotationXYZW} → {convertedUnityRotationEuler}");
    }

    [ContextMenu("캘리브레이션 재실행")]
    void Recalibrate()
    {
        InitializeCalibrationData();
        FindOptimalTransform();
    }
}