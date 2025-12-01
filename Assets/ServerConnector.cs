using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text; // ★ JSON 전송을 위해 인코딩 기능 추가

// ---------------------------------------------------------
// [1] 데이터 클래스 정의
// ---------------------------------------------------------

// 1-1. 분석 결과 수신용 (/analyze 응답)
[System.Serializable]
public class ServerResponse
{
    public string status;
    public string image_url; // 서버가 저장한 이미지 경로
    public ResponseData data;
}

[System.Serializable]
public class ResponseData
{
    public List<RecognitionItem> recognition;
}

[System.Serializable]
public class RecognitionItem
{
    public string label;
    public float confidence;
    public List<string> colors;
}

// 1-2. ★ [신규] 등록 요청용 (/register 요청)
[System.Serializable]
public class RegisterRequest
{
    public string user_name;
    public string image_url;     // 분석 단계에서 받은 URL을 그대로 보냄
    public string object_label;  // 사용자가 수정한 이름
    public List<string> object_colors;
    public float anchor_x;
    public float anchor_y;
    public float anchor_z;
    public string visibility;
    // ★ [추가] 상세 설명을 담을 변수 추가!
    public string description;
}
// [신규] 서버에서 받아올 물건 1개의 정보
[System.Serializable]
public class ObjectItem
{
    public int id;                // 아이디
    public string user_name;      // 유저 이름
    public string image_path;     // 이미지 경로
    public string object_label;   // 물건 이름
    public string created_at;     // 생성 날짜
    
    // ★ [추가됨] 색상과 설명
    public List<string> object_colors; 
    public string description;
    
    // 위치 정보 (나중에 쓸 수 있음)
    public float anchor_x;
    public float anchor_y;
    public float anchor_z;
}

[System.Serializable]
public class ObjectListResponse
{
    // ★ [수정] "items" -> "data" (서버가 "data": [...] 로 보내줌)
    public List<ObjectItem> data; 
}

// ---------------------------------------------------------
// [2] 서버 통신 메인 클래스
// ---------------------------------------------------------
public class ServerConnector : MonoBehaviour
{
    // 기본 주소 (본인 서버 IP 확인 필수)
    private string baseUrl = "https://fusiform-immemorially-randell.ngrok-free.dev"; 
    
    // 경로 설정
    private string analyzePath = "/analyze";
    private string registerPath = "/register"; // ★ 등록 경로 추가
    private string objectsPath = "/objects"; // [추가] 목록 조회 경로


    // =================================================================================
    // 기능 1: 사진 분석 요청 (이미지 -> 이름, 색상, 사진, URL 반환)
    // =================================================================================
    // ★ 콜백 변경됨: Action<이름, 색상, 다운로드된_이미지, 이미지URL_문자열>
    public IEnumerator SendImage(Texture2D localPhoto, System.Action<string, string, Texture2D, string> onComplete)
    {
        // 1. 사진을 바이트로 변환
        byte[] imageBytes = localPhoto.EncodeToJPG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "lost_item.jpg", "image/jpeg");

        // 2. 분석 요청 (/analyze)
        string fullAnalyzeUrl = baseUrl + analyzePath;
        Debug.Log("서버 분석 요청: " + fullAnalyzeUrl);

        using (UnityWebRequest www = UnityWebRequest.Post(fullAnalyzeUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("통신 에러: " + www.error);
                onComplete("에러", "확인불가", null, ""); // 실패 시 빈 문자열 전달
                yield break;
            }

            Debug.Log("서버 응답: " + www.downloadHandler.text);

            // 3. JSON 파싱
            ServerResponse response = null;
            string detectedName = "인식 실패";
            string detectedColor = "없음";
            string serverImageUrl = "";

            try
            {
                response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);

                if (response != null)
                {
                    // 물체 정보
                    if (response.data != null && response.data.recognition.Count > 0)
                    {
                        RecognitionItem item = response.data.recognition[0];
                        detectedName = item.label;
                        detectedColor = (item.colors != null && item.colors.Count > 0) ? item.colors[0] : "Unknown";
                    }
                    // 이미지 URL (나중에 등록할 때 써야 함!)
                    serverImageUrl = response.image_url;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("파싱 실패: " + e.Message);
            }

            // 4. 결과 이미지 다운로드 (URL이 있을 때만)
            Texture2D finalImage = null;

            if (!string.IsNullOrEmpty(serverImageUrl))
            {
                string fullImageUrl = baseUrl + serverImageUrl;
                Debug.Log("이미지 다운로드: " + fullImageUrl);

                using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(fullImageUrl))
                {
                    yield return imageRequest.SendWebRequest();

                    if (imageRequest.result == UnityWebRequest.Result.Success)
                    {
                        finalImage = DownloadHandlerTexture.GetContent(imageRequest);
                    }
                }
            }

            // 5. 결과 전달 (★ serverImageUrl도 같이 넘겨줌)
            onComplete(detectedName, detectedColor, finalImage, serverImageUrl);
        }
    }


    // =================================================================================
    // 기능 2: ★ [신규] 최종 등록 요청 (JSON -> DB 저장)
    // =================================================================================
    public IEnumerator RegisterItem(RegisterRequest data, System.Action<bool> onComplete)
    {
        string url = baseUrl + registerPath;

        // 1. 데이터를 JSON 문자열로 변환
        string jsonBody = JsonUtility.ToJson(data);
        
        // 2. 바이트 배열로 변환 (한글 깨짐 방지 UTF8)
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // 3. POST 요청 설정 (application/json)
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"등록 요청 전송: {jsonBody}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("등록 성공: " + request.downloadHandler.text);
                onComplete(true);
            }
            else
            {
                Debug.LogError("등록 실패: " + request.error);
                onComplete(false);
            }
        }
    }
    // =================================================================================
    // 기능 3: ★ [신규] 물건 목록 가져오기 (GET)
    // =================================================================================
    public IEnumerator GetObjectList(System.Action<List<ObjectItem>> onComplete)
    {
        string url = baseUrl + objectsPath + "?skip=0&limit=100";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            Debug.Log("목록 조회 요청: " + url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("목록 조회 실패: " + www.error);
                onComplete(null);
            }
            else
            {
                // Debug.Log("목록 응답: " + www.downloadHandler.text);

                try
                {
                    // JSON 파싱
                    ObjectListResponse response = JsonUtility.FromJson<ObjectListResponse>(www.downloadHandler.text);
                    
                    // ★ [수정] response.items 가 아니라 response.data 입니다!
                    if (response != null && response.data != null)
                    {
                        onComplete(response.data); // data를 넘겨줌
                       // print("가져오는 data 정보 : " + response.data.Count);
                    }
                    else
                    {
                        Debug.LogWarning("데이터가 없습니다.");
                        onComplete(new List<ObjectItem>());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("파싱 에러: " + e.Message);
                    onComplete(null);
                }
            }
        }
    }
}