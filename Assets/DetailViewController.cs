using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class DetailViewController : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage detailImage;
    public TMP_Text titleText;
    public TMP_Text colorText;
    public TMP_Text descText;
    public TMP_Text dateText;

    private string baseUrl = "https://fusiform-immemorially-randell.ngrok-free.dev"; // 서버 주소

    // 현재 표시 중인 분실물 정보 저장
    private ObjectItem currentItem;

    public void SetData(ObjectItem item)
    {
        // 현재 아이템 저장 (ARNavButton 클릭 시 사용)
        currentItem = item;

        // 1. 기본 정보
        titleText.text = item.object_label;
        dateText.text = item.created_at;

        // 2. ★ [추가됨] 상세 설명 표시
        if (!string.IsNullOrEmpty(item.description))
        {
            descText.text = item.description;
        }
        else
        {
            descText.text = "상세 설명이 없습니다.";
        }

        // 3. ★ [수정됨] 색상 정보 표시
        if (item.object_colors != null && item.object_colors.Count > 0)
        {
            // 리스트의 모든 색상을 콤마(,)로 연결해서 표시 (예: "red, blue")
            colorText.text = string.Join(", ", item.object_colors);
        }
        else
        {
            colorText.text = "색상 정보 없음";
        }

        // 4. 이미지
        if (!string.IsNullOrEmpty(item.image_path))
        {
            StartCoroutine(DownloadImage(item.image_path));
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
                detailImage.texture = DownloadHandlerTexture.GetContent(request);

                // [선택] 이미지 비율 맞추기 (Aspect Ratio Fitter가 있다면)
                // detailImage.GetComponent<AspectRatioFitter>().aspectRatio =
                //    (float)detailImage.texture.width / detailImage.texture.height;
            }
        }
    }

    // ARNavButton 클릭 시 SampleScene으로 이동
    public void OnARNavButtonClick()
    {
        // 분실물 위치 정보를 저장하고 씬 전환
        if (currentItem != null)
        {
            // 위치 + ID + 이름을 함께 전달 (회수 시 사용)
            NavigationData.SetTargetPosition(
                currentItem.anchor_x,
                currentItem.anchor_y,
                currentItem.anchor_z,
                currentItem.id,
                currentItem.object_label
            );
            Debug.Log($"[DetailView] 분실물 '{currentItem.object_label}' (ID: {currentItem.id}) 위치로 안내 시작");
        }

        SceneManager.LoadScene("SampleScene");
    }
}