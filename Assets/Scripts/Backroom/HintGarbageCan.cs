using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HintGarbageCan : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject backroomEscapeHintMemoItemObj;

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        return "Search [E]";
    }

    public void Interact()
    {
        backroomEscapeHintMemoItemObj.SetActive(true);
        backroomEscapeHintMemoItemObj.transform.DOLocalMoveY(0.4f, 0.5f)
            .SetRelative()
            .SetEase(Ease.OutBack);

        enabled = false; // 한 번만 조사하면 되므로 아이템 나온 후 현재 상호작용 컴포넌트 비활성화
    }
}
