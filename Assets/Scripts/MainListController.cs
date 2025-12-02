using UnityEngine;
using System.Collections.Generic;

public class MainListController : MonoBehaviour
{
    [Header("연결")]
    public ServerConnector server;  
    public GameObject cardPrefab;   
    public Transform gridContent;   

    // ★ [추가] 원본 데이터를 저장해둘 리스트 (필터링 기준)
    private List<ObjectItem> allItems = new List<ObjectItem>();

    private void OnEnable()
    {
        RefreshList();
    }

    // 1. 서버에서 데이터 가져오기 (처음 한 번만 실행됨)
    public void RefreshList()
    {
        StartCoroutine(server.GetObjectList((items) => 
        {
            if (items != null)
            {
                Debug.Log($"서버에서 총 {items.Count}개 가져옴");
                
                // ★ 원본 보관함에 저장!
                allItems = items; 
                
                // 처음엔 필터 없이 전부 보여주기
                UpdateUI(allItems);
            }
        }));
    }

    // 2. ★ [핵심] 화면에 카드를 그려주는 함수 (필터링된 리스트를 받아서 그림)
    void UpdateUI(List<ObjectItem> itemsToShow)
    {
        // 기존 카드 싹 지우기
        foreach (Transform child in gridContent)
        {
            Destroy(child.gameObject);
        }

        // 리스트만큼 카드 생성
        foreach (ObjectItem item in itemsToShow)
        {
            GameObject newCard = Instantiate(cardPrefab, gridContent);
            CardController card = newCard.GetComponent<CardController>();
            if (card != null) card.Setup(item);
        }
    }

    // 3. ★ [신규] 버튼이 클릭되면 호출될 필터 함수
    // type: "ALL", "CATEGORY", "COLOR"
    // keyword: "wallet", "red", "mobilephone" 등
    public void FilterItems(string type, string keyword)
    {
        List<ObjectItem> filteredList = new List<ObjectItem>();

        if (type == "ALL")
        {
            // 전부 다 보여줘
            filteredList = allItems;
        }
        else if (type == "CATEGORY")
        {
            // 이름(label)에 키워드가 포함된 것만 찾기 (대소문자 무시)
            foreach (var item in allItems)
            {
                if (item.object_label.ToLower().Contains(keyword.ToLower()))
                {
                    filteredList.Add(item);
                }
            }
        }
        else if (type == "COLOR")
        {
            // 색상 리스트에 키워드가 있는 것만 찾기
            foreach (var item in allItems)
            {
                // item.object_colors 리스트 안에 keyword가 포함되어 있는지 확인
                if (item.object_colors != null && item.object_colors.Contains(keyword.ToLower()))
                {
                    filteredList.Add(item);
                }
            }
        }

        Debug.Log($"필터({type} - {keyword}) 결과: {filteredList.Count}개 발견");
        
        // 걸러진 리스트로 화면 다시 그리기
        UpdateUI(filteredList);
    }
}