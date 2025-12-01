using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class XROriginAdjuster : MonoBehaviour
{
    [Header("XR Origin 연결")]
    public Transform xrOrigin;

    [Header("이동 설정")]
    public float moveSpeed = 0.5f;

    [Header("회전 설정")]
    public float rotateSpeed = 50f;

    [Header("터치 영역 설정")]
    [Range(0.1f, 0.5f)]
    public float sideTouchZone = 0.3f;

    [Header("디버그")]
    public bool showDebugLog = true;

    private Vector2 lastTwoFingerPos;
    private bool isTwoFingerTouch = false;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Debug.Log("[XROriginAdjuster] EnhancedTouch 활성화됨");
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        if (xrOrigin == null)
        {
            Debug.LogError("[XROriginAdjuster] XR Origin이 연결되지 않았습니다!");
        }
        else
        {
            Debug.Log("[XROriginAdjuster] 초기화 완료. XR Origin: " + xrOrigin.name);
        }
    }

    void Update()
    {
        if (xrOrigin == null) return;

        var activeTouches = Touch.activeTouches;
        int touchCount = activeTouches.Count;

        if (touchCount > 0 && showDebugLog)
        {
            Debug.Log("[XROriginAdjuster] 터치 개수: " + touchCount);
        }

        if (touchCount == 1)
        {
            var touch = activeTouches[0];
            if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
            {
                HandleSingleTouch(touch.screenPosition);
            }
            isTwoFingerTouch = false;
        }
        else if (touchCount == 2)
        {
            HandleTwoFingerRotation();
        }
        else
        {
            isTwoFingerTouch = false;
        }
    }

    void HandleSingleTouch(Vector2 touchPos)
    {
        float screenWidth = Screen.width;
        float normalizedX = touchPos.x / screenWidth;

        if (showDebugLog)
        {
            Debug.Log("[XROriginAdjuster] 터치 X: " + normalizedX.ToString("F2"));
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 right = cam.transform.right;
        right.y = 0;
        right.Normalize();

        if (normalizedX < sideTouchZone)
        {
            xrOrigin.position -= right * moveSpeed * Time.deltaTime;
            if (showDebugLog) Debug.Log("[XROriginAdjuster] 왼쪽 이동");
        }
        else if (normalizedX > (1f - sideTouchZone))
        {
            xrOrigin.position += right * moveSpeed * Time.deltaTime;
            if (showDebugLog) Debug.Log("[XROriginAdjuster] 오른쪽 이동");
        }
    }

    void HandleTwoFingerRotation()
    {
        var activeTouches = Touch.activeTouches;
        var touch0 = activeTouches[0];
        var touch1 = activeTouches[1];
        Vector2 currentCenter = (touch0.screenPosition + touch1.screenPosition) / 2f;

        if (!isTwoFingerTouch)
        {
            isTwoFingerTouch = true;
            lastTwoFingerPos = currentCenter;
            return;
        }

        float deltaX = currentCenter.x - lastTwoFingerPos.x;
        float rotationAmount = (deltaX / Screen.width) * rotateSpeed;
        xrOrigin.Rotate(0, rotationAmount, 0);
        lastTwoFingerPos = currentCenter;

        if (showDebugLog) Debug.Log("[XROriginAdjuster] 회전: " + rotationAmount.ToString("F2"));
    }
}
