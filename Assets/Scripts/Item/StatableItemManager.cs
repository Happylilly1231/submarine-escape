using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatableItemManager : MonoBehaviour
{
    private Dictionary<int, IItemStateData> _itemInstanceStateDict = new Dictionary<int, IItemStateData>(); // 개별 상태 저장 딕셔너리

    private int _idCounter = 1; // 번호를 계속 증가시키며 부여하기 위한 카운터

    /// <summary>
    /// 해당 번호의 아이템 인스턴스의 상태를 개별 상태 저장 딕셔너리에서 가져오기
    /// </summary>
    /// <param name="num">찾으려는 아이템 인스턴스 번호</param>
    /// <returns>해당 번호의 아이템 인스턴스 상태</returns>
    public IItemStateData FindItemState(int num)
    {
        if (_itemInstanceStateDict.TryGetValue(num, out IItemStateData data)) // 딕셔너리에서 찾으면
            return data; // 해당 상태 데이터 반환
        return null; // 없으면 null 반환
    }

    /// <summary>
    /// 해당 아이템 인스턴스 상태 데이터를 개별 상태 저장 딕셔너리에 등록
    /// <para> - 딕셔너리에 저장되어 있지 않을 때만 실행 </para>
    /// </summary>
    /// <param name="statableItem">아이템 인스턴스(개별 상태 저장 아이템)</param>
    public void RegisterItemState(IStatableItem statableItem)
    {
        // 딕셔너리에 이미 있으면 실행 X
        if (FindItemState(statableItem.ItemInstanceNum) != null)
            return;

        IItemStateData itemStateData = statableItem.GetStateData(); // 현재 아이템 인스턴스 상태 데이터 가져오기
        int newNum = _idCounter++; // id 카운터 증가
        _itemInstanceStateDict.Add(newNum, itemStateData); // 개별 상태 저장 딕셔너리에 id(번호)-상태 쌍 추가(등록)
        statableItem.ItemInstanceNum = newNum; // newNum이 해당 아이템 인스턴스의 번호라는 것을 인스턴스가 알고 있게 하기
        Debug.Log("아이템 상태 등록 완료: " + newNum);
    }

    /// <summary>
    /// 해당 아이템 오브젝트의 개별 상태가 저장되어 있다면, 저장된 상태로 복원
    /// </summary>
    /// <param name="statableItem">아이템 인스턴스(개별 상태 저장 아이템)</param>
    public void RestoreItemState(GameObject itemObj, int itemInstanceNum)
    {
        // 해당 아이템의 개별 상태가 저장되어 있다면
        IStatableItem statableItem = itemObj.GetComponent<IStatableItem>();
        if (statableItem != null && itemInstanceNum > 0)
        {
            // 저장된 상태로 복원
            IItemStateData savedData = FindItemState(itemInstanceNum); // 개별 상태 저장 딕셔너리에서 현재 아이템 인스턴스의 저장된 상태 찾아오기
            statableItem.SetStateData(savedData); // 현재 아이템 인스턴스의 상태를 저장된 상태로 설정
            Debug.Log("아이템 상태 복원 완료: " + statableItem.ItemInstanceNum);
        }
    }


}
