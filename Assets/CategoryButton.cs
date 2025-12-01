using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    // 버튼 종류 (Inspector에서 선택)
    public enum FilterType { ALL, CATEGORY, COLOR }
    public FilterType myType;

    // 검색할 키워드 (Inspector에서 직접 입력: wallet, red 등)
    public string keyword; 

    private MainListController mainListController;
    private Button myButton;

    void Start()
    {
        // 메인 컨트롤러 찾기
        mainListController = FindObjectOfType<MainListController>();
        myButton = GetComponent<Button>();

        if (myButton != null)
        {
            myButton.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (mainListController != null)
        {
            // 컨트롤러에게 필터링 요청
            mainListController.FilterItems(myType.ToString(), keyword);
        }
    }
}