using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic; // 리스트 사용을 위해 필수!

public class RegisterViewController : MonoBehaviour
{
    [Header("화면 그룹 (켜고 끌 것들)")]
    public GameObject cameraFrame; // 1. 초기 화면 (촬영 버튼)
    public GameObject inputGroup;  // 2. 결과 입력 화면 (폼)
    public GameObject successPopup; // ★ [추가] "등록 완료" 팝업창

    [Header("입력 필드")]
    public TMP_InputField nameField;
    public TMP_InputField colorField;
    public TMP_InputField descriptionField;
    public RawImage photoDisplay;  

    [Header("매니저 연결")]
    public WebCamController webCamController;
    public ServerConnector server; 
    public UIManager uiManager; // ★ [추가] 메인으로 돌아가기 위해 필요

    // 서버에서 받은 이미지 URL을 임시 저장할 변수
    private string currentServerImageUrl = "";

    private void OnEnable()
    {
        ResetView();
    }

    // 초기화 함수
    public void ResetView()
    {
        Debug.Log("화면 초기화 시작");

        if(cameraFrame != null) cameraFrame.SetActive(true);
        if(inputGroup != null) inputGroup.SetActive(false);
        if(successPopup != null) successPopup.SetActive(false); // ★ 팝업도 숨기기

        if(nameField != null) nameField.text = "";
        if(colorField != null) colorField.text = "";
        if(descriptionField != null) descriptionField.text = "";
        
        if (webCamController != null) webCamController.ResetUI();
        
        currentServerImageUrl = ""; 
    }

    // 사진 찍은 후 실행되는 함수
    public void OnPhotoTaken(string detectedName, string detectedColor, Texture2D photo, string imageUrl)
    {
        Debug.Log($"결과 수신: {detectedName}, {detectedColor}, URL: {imageUrl}");

        // 1. 화면 전환
        if(cameraFrame != null) cameraFrame.SetActive(false);
        if(inputGroup != null) inputGroup.SetActive(true);

        // 2. 데이터 채워넣기
        if(nameField != null) nameField.text = detectedName;
        if(colorField != null) colorField.text = detectedColor;
        
        currentServerImageUrl = imageUrl;

        // 3. 사진 표시
        if (photoDisplay == null)
        {
            Debug.LogError("🚨 비상! [Photo Display] 연결 안됨!");
            return;
        }

        if (photo != null)
        {
            photoDisplay.texture = photo;
            photoDisplay.color = Color.white; 
            photoDisplay.uvRect = new Rect(0, 0, 1, 1);
            photoDisplay.gameObject.name = "Photo_Filled_Success"; 
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(inputGroup.GetComponent<RectTransform>());
            StartCoroutine(RefreshUI());
        }
    }

    // Register 버튼을 누르면 실행될 함수
    public void OnRegisterButtonClick()
    {
        // 1. 보낼 데이터 포장하기
        RegisterRequest req = new RegisterRequest();
        
        req.user_name = "User_1"; 
        req.image_url = currentServerImageUrl; 
        req.object_label = nameField.text;     
        
        // 색상은 리스트로 변환
        req.object_colors = new List<string>();
        req.object_colors.Add(colorField.text); 

        // ★ [추가] 상세 설명 입력칸의 내용을 담기!
        if (descriptionField != null)
        {
            req.description = descriptionField.text;
        }
        else
        {
            req.description = ""; // 비어있으면 빈 문자열
        }

        // 위치 정보 (일단 0)
        req.anchor_x = 0.0f; req.anchor_y = 0.0f; req.anchor_z = 0.0f;
        req.visibility = "visible";

        Debug.Log($"등록 요청: 이름={req.object_label}, 설명={req.description}"); // 로그로 확인

        // 2. 서버로 전송
        if (server != null)
        {
            StartCoroutine(server.RegisterItem(req, (isSuccess) => 
            {
                if (isSuccess)
                {
                    Debug.Log("🎉 최종 등록 완료!");
                    StartCoroutine(ProcessSuccessSequence());
                }
                else
                {
                    Debug.LogError("등록 실패...");
                }
            }));
        }
        else
        {
            Debug.LogError("ServerConnector가 연결되지 않았습니다!");
        }
    }

    // ★ [신규] 성공 시 팝업 띄우고 메인으로 이동하는 코루틴
    IEnumerator ProcessSuccessSequence()
    {
        // 1. 팝업 켜기
        if (successPopup != null) successPopup.SetActive(true);

        // 2. 1.5초 대기 (사용자가 읽을 시간)
        yield return new WaitForSeconds(1.5f);

        // 3. 팝업 끄기
        if (successPopup != null) successPopup.SetActive(false);

        // 4. 입력창들 초기화
        ResetView();

        // 5. 메인 화면으로 이동
        if (uiManager != null)
        {
            uiManager.BackToMain();
        }
        else
        {
            Debug.LogError("UIManager 연결이 안 되어 있습니다!");
        }
    }

    IEnumerator RefreshUI()
    {
        photoDisplay.gameObject.SetActive(false);
        yield return null; 
        photoDisplay.gameObject.SetActive(true);
    }
}