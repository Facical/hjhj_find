using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject detailPanel;
    public GameObject registerPanel;

    // DetailViewController 연결
    public DetailViewController detailViewController;

    // ★ [수정됨] 괄호 안에 (ObjectItem item)을 추가해야 데이터를 받을 수 있습니다!
    public void OpenDetail(ObjectItem item)
    {
        // 1. 화면 전환
        mainPanel.SetActive(false);
        detailPanel.SetActive(true);
        registerPanel.SetActive(false);

        // 2. 받은 데이터(item)를 상세 화면 컨트롤러에 전달
        if(detailViewController != null)
        {
            // 이제 'item'이 무엇인지 알기 때문에 에러가 사라집니다.
            detailViewController.SetData(item);
        }
        else
        {
            Debug.LogError("DetailViewController가 연결되지 않았습니다! Inspector를 확인하세요.");
        }
    }

    // 등록화면 열기
    public void OpenRegister()
    {
        mainPanel.SetActive(false);
        detailPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    // 메인으로 돌아오기
    public void BackToMain()
    {
        detailPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}