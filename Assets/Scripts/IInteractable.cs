public interface IInteractable
{
    string GetInteractText(); // 열기 텍스트
    void Interact(); // 실제 동작 (열기/닫기)
    bool CanInteractwithSelectedItem(Item item); // 선택된 아이템으로 상호작용 가능한지 여부
}
