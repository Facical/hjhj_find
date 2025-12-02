using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class WebCamController : MonoBehaviour
{
    [Header("전용 전체화면 패널")]
    public GameObject cameraFullScreenPanel;
    public RawImage fullFeed;
    public RectTransform fullFeedRect;
    public GameObject guideUI;
    
    [Header("로딩 UI")]
    public GameObject loadingPanel; 
    public Transform spinnerIcon;   

    [Header("결과 보여줄 곳")]
    public RawImage resultPhotoDisplay;

    [Header("연결")]
    public RegisterViewController viewCtrl;
    public ServerConnector server;

    private WebCamTexture camTexture;
    private bool isPreviewing = false;

    // 1. 카메라 켜기
    public void OpenCamera()
    {
        cameraFullScreenPanel.SetActive(true);
        guideUI.SetActive(true); 
        loadingPanel.SetActive(false); 
        
        StartPreview();
    }

    void StartPreview()
    {
        // 해상도 지정 없이 기본값 사용 (기기 최적 해상도)
        camTexture = new WebCamTexture(WebCamTexture.devices[0].name);
        fullFeed.texture = camTexture;
        camTexture.Play();
        isPreviewing = true;

        Debug.Log($"카메라 해상도: {camTexture.width}x{camTexture.height}");
    }

    // 2. 카메라 끄기 & UI 리셋 (필수 함수)
    void StopCamera()
    {
        if (camTexture != null && camTexture.isPlaying)
        {
            camTexture.Stop();
        }
        isPreviewing = false;
        
        if (cameraFullScreenPanel != null) cameraFullScreenPanel.SetActive(false);
    }

    public void ResetUI()
    {
        StopCamera();
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (guideUI != null) guideUI.SetActive(true);
        if (cameraFullScreenPanel != null) cameraFullScreenPanel.SetActive(false);
    }

    void Update()
    {
        // 회전 및 반전 처리
        if (isPreviewing && camTexture != null && camTexture.width > 16)
        {
            float angle = camTexture.videoRotationAngle;
            bool isMirrored = camTexture.videoVerticallyMirrored;

            // 회전 적용
            fullFeedRect.localEulerAngles = new Vector3(0, 0, -angle);

            // 반전 처리
            if (isMirrored)
            {
                fullFeed.uvRect = new Rect(0, 1, 1, -1);
            }
            else
            {
                fullFeed.uvRect = new Rect(0, 0, 1, 1);
            }

            // 화면 크기에 맞게 비율 조절
            float videoRatio;
            if (angle == 90 || angle == 270)
            {
                videoRatio = (float)camTexture.height / (float)camTexture.width;
            }
            else
            {
                videoRatio = (float)camTexture.width / (float)camTexture.height;
            }

            // 부모 크기 기준으로 꽉 채우기
            RectTransform parent = fullFeedRect.parent as RectTransform;
            if (parent != null)
            {
                float parentRatio = parent.rect.width / parent.rect.height;

                if (videoRatio > parentRatio)
                {
                    // 비디오가 더 넓음 - 높이 맞추고 가로 늘림
                    float height = parent.rect.height;
                    float width = height * videoRatio;
                    fullFeedRect.sizeDelta = new Vector2(width, height);
                }
                else
                {
                    // 비디오가 더 좁음 - 가로 맞추고 세로 늘림
                    float width = parent.rect.width;
                    float height = width / videoRatio;
                    fullFeedRect.sizeDelta = new Vector2(width, height);
                }
            }
        }

        // 로딩 아이콘 회전
        if (loadingPanel.activeSelf && spinnerIcon != null)
        {
            spinnerIcon.Rotate(0, 0, -200 * Time.deltaTime);
        }
    }

    // 3. 셔터 버튼 클릭
    public void OnShutterClick()
    {
        if (isPreviewing)
        {
            StartCoroutine(CaptureProcess());
        }
    }

    IEnumerator CaptureProcess()
    {
        yield return new WaitForEndOfFrame();

        // 1. 원본 캡처
        Texture2D originalPhoto = new Texture2D(camTexture.width, camTexture.height);
        originalPhoto.SetPixels(camTexture.GetPixels());
        originalPhoto.Apply();

        // ★ [수정 1] StopCamera()를 여기서 부르지 마세요! (패널이 꺼져버립니다)
        // 대신 카메라 하드웨어만 멈춥니다.
        if (camTexture != null) camTexture.Stop(); 
        isPreviewing = false;

        // ★ [수정 2] 로딩 상태로 UI 변경
        // 전체화면 패널(cameraFullScreenPanel)은 켜져 있는 상태여야 함!
        guideUI.SetActive(false);      // 가이드(촬영 아이콘) 숨김
        loadingPanel.SetActive(true);  // 로딩 패널("분석중...") 켜기
        
        // 3. 리사이징 (서버 전송용)
        Texture2D resizedPhoto = ResizeTexture(originalPhoto, originalPhoto.width / 2, originalPhoto.height / 2);

        // 4. 서버 전송
        StartCoroutine(server.SendImage(resizedPhoto, (name, color, finalImage, imageUrl) => 
        {
            // ★ [수정 3] 서버 응답이 왔을 때 비로소 끕니다.
            loadingPanel.SetActive(false);          // 로딩 끄기
            cameraFullScreenPanel.SetActive(false); // 전체화면 패널 닫기

            Texture2D imageToShow = (finalImage != null) ? finalImage : originalPhoto;
            resultPhotoDisplay.texture = imageToShow;

            viewCtrl.OnPhotoTaken(name, color, imageToShow, imageUrl);
        }));
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }
}