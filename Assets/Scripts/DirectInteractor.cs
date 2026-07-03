using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DirectInteractor : MonoBehaviour
{
    [SerializeField] private float rayDistance = 3.0f; // 상호작용 감지 거리
    [SerializeField] private Camera playerCamera; // 플레이어 카메라
    [SerializeField] private TMPro.TextMeshProUGUI interactorText; // 상호작용 UI - 감지된 오브젝트와의 상호작용 키 표시

    private RaycastHit _sphereCastHit; // SphereCast로 감지된 오브젝트 정보
    private IInteractable _currentFurniture; // 현재 감지된 가구

    void Update()
    {
        DetectObject();
    }

    private void DetectObject()
    {
        ClearDetection();
        // 플레이어 카메라의 위치와 방향을 기준으로 SphereCast를 수행
        if (Physics.SphereCast(playerCamera.transform.position, 0.1f, playerCamera.transform.forward, out _sphereCastHit, rayDistance))
        {
            if (_sphereCastHit.transform.TryGetComponent(out IInteractable furniture))
            {
                HandleInteractable(furniture);
            }
        }
    }

    /// <summary>
    /// 가구 감지 시 UI 및 상태 처리
    /// </summary>
    private void HandleInteractable(IInteractable furniture)
    {
        _currentFurniture = furniture;
        interactorText.text = furniture.GetInteractText();
    }

    /// <summary>
    /// 감지된 오브젝트가 없을 때 UI 및 상태 초기화
    /// </summary>
    private void ClearDetection()
    {
        interactorText.text = "";
        _currentFurniture = null;
    }

    /// <summary>
    /// E키 입력으로 가구/실험기구와 상호작용
    /// </summary>
    public void OnInteractor(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        _currentFurniture?.Interact();
    }
}
