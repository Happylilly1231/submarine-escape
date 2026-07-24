using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HintGarbageCan : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject backroomEscapeHintMemoItemObj;
    bool isFound = false; // 아이템 찾았는지 여부

    public bool CanInteractwithSelectedItem(Item item)
    {
        return false;
    }

    public string GetInteractText()
    {
        if (!isFound)
            return LocalizationHelper.GetLocalizedInteractText("Interact/Search", "E");
        else
            return "";
    }

    public void Interact()
    {
        if (isFound)
            return;

        backroomEscapeHintMemoItemObj.SetActive(true);
        backroomEscapeHintMemoItemObj.transform.DOLocalMoveY(0.4f, 0.5f)
            .SetRelative()
            .SetEase(Ease.OutBack);

        isFound = true;
    }
}
