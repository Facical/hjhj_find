using UnityEngine;

/// <summary>
/// 에디터에서 AR 카메라 대신 WebCam 배경을 보여주는 스크립트
/// XR Origin의 Main Camera에 추가하세요
/// </summary>
public class EditorCameraFallback : MonoBehaviour
{
    private WebCamTexture webCamTexture;
    private GameObject backgroundQuad;

    void Start()
    {
        // 에디터에서만 작동
        #if UNITY_EDITOR
        SetupWebCamBackground();
        #endif
    }

    void SetupWebCamBackground()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("웹캠을 찾을 수 없습니다.");
            return;
        }

        // 웹캠 시작
        webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 1920, 1080);
        webCamTexture.Play();

        // 배경용 Quad 생성
        backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundQuad.name = "EditorCameraBackground";
        backgroundQuad.transform.SetParent(transform);
        backgroundQuad.transform.localPosition = new Vector3(0, 0, 10f);
        backgroundQuad.transform.localRotation = Quaternion.identity;
        backgroundQuad.transform.localScale = new Vector3(16f, 9f, 1f);

        // 머티리얼 설정
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = webCamTexture;
        backgroundQuad.GetComponent<Renderer>().material = mat;

        // Collider 제거
        Destroy(backgroundQuad.GetComponent<Collider>());

        Debug.Log("에디터 카메라 배경 설정 완료");
    }

    void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
