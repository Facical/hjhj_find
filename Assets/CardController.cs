using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic; // 리스트 사용을 위해 필수

public class CardController : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage itemImage;      // 물건 사진
    public TMP_Text nameText;       // 물건 이름
    public TMP_Text locationText;   // 위치 (현재는 임시값)
    public TMP_Text dateText;       // 날짜
    public TMP_Text colorText;      // (선택사항) 카드에도 색상을 표시하고 싶다면 연결

    // 서버 기본 주소
    private string baseUrl = "https://fusiform-immemorially-randell.ngrok-free.dev"; 

    // 내부 데이터 저장용
    private ObjectItem myData;   
    private UIManager uiManager; 

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnCardClick);
        }
    }

    // ★ 데이터를 받아서 UI를 채우는 핵심 함수
    public void Setup(ObjectItem data)
    {
        myData = data; // 1. 데이터 저장 (색상, 설명 포함됨)

        // 2. 텍스트 설정
        nameText.text = data.object_label;
        
        // 날짜 포맷팅
        if(!string.IsNullOrEmpty(data.created_at) && data.created_at.Length > 10)
            dateText.text = data.created_at.Substring(0, 10);
        else
            dateText.text = data.created_at;

        // 위치 정보 (앵커 좌표가 있으면 표시, 없으면 디지털관)
        if (data.anchor_x != 0 || data.anchor_y != 0)
            locationText.text = $"위치: {data.anchor_x:F1}, {data.anchor_y:F1}";
        else
            locationText.text = "디지털관"; 

        // (선택) 카드 리스트에도 색상을 보여주고 싶다면?
        if (colorText != null && data.object_colors != null && data.object_colors.Count > 0)
        {
            colorText.text = data.object_colors[0]; // 첫 번째 색상 표시
        }

        // 3. 이미지 다운로드
        if (!string.IsNullOrEmpty(data.image_path))
        {
            StartCoroutine(DownloadImage(data.image_path));
        }
    }

    // 클릭 시 상세화면으로 데이터 전달
    void OnCardClick()
    {
        if(uiManager != null && myData != null)
        {
            Debug.Log("============== [상세 정보 확인] ==============");
            Debug.Log($"이름: {myData.object_label}");
            Debug.Log($"설명: {myData.description}"); // ★ 설명 로그 확인
            
            if(myData.object_colors != null)
                Debug.Log($"색상: {string.Join(", ", myData.object_colors)}"); // ★ 색상 로그 확인
                
            Debug.Log("============================================");

            // 상세 화면으로 모든 데이터(설명, 색상 포함) 전달
            uiManager.OpenDetail(myData);
        }
        else
        {
            Debug.LogError("데이터가 없습니다.");
        }
    }

    IEnumerator DownloadImage(string partialUrl)
    {
        string fullUrl = baseUrl + partialUrl;
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                itemImage.texture = texture;
            }
        }
    }
}