using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class DieselEngine : MonoBehaviour, IInteractable
{
    [SerializeField] private Item hammer;
    [SerializeField] private GameObject repairPatch;
    [SerializeField] private Transform itemViewRoot;
    [SerializeField] private GameObject flashlight;
    [Header("Focus View Settings")]
    [SerializeField] private Vector3 focusViewPos;
    [SerializeField] private Vector3 focusViewRot;
    [SerializeField] private float duration = 1.0f;
    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem originalParticle;
    [SerializeField] private ParticleSystem blockedParticle;
    [SerializeField] private ParticleSystem sparkParticle;
    [SerializeField] private float maxEmissionRate = 50f;

    private bool _isFocused = false;
    public bool IsFocused => _isFocused;
    private Camera _mainCamera;
    private InventoryManager _inventoryManager;
    private ItemEquipController _itemEquipController;
    private PlayerCameraController _playerCameraController;
    private RepairEngineQTE _repairEngineQTE;
    private PowerSwitch _powerSwitch;
    private PlayerInput _playerInput; // 플레이어 입력 컴포넌트
    private InputAction _repairEngineAction;
    private int _currentRepairStep = 0;
    public bool IsComplete => _currentRepairStep >= 3;

    void Awake()
    {
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _playerCameraController = FindObjectOfType<PlayerCameraController>();
        _repairEngineQTE = FindObjectOfType<RepairEngineQTE>();
        _powerSwitch = FindObjectOfType<PowerSwitch>();

        _playerInput = SubmarineInGameManager.instance.player.GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            _repairEngineAction = _playerInput.actions["RepairEngine"];
        }

        _mainCamera = Camera.main;
    }

    /// <summary>
    /// 상호작용 UI에 표시할 텍스트
    /// </summary>
    public string GetInteractText()
    {
        if (_isFocused) return "";
        if (IsHammerSelected())
        {
            return "open [E]";
        }
        else
        {
            return "Need Hammer";
        }
    }

    /// <summary>
    /// 플레이어가 E키를 입력할 때 키패드 패널 확대
    /// </summary>
    public void Interact()
    {
        if (_isFocused) return;
        if (IsHammerSelected())
        {
            StartFocusMode();
        }
    }

    /// <summary>
    /// 선택된 아이템으로 상호작용 가능한지 여부
    /// </summary>
    public bool CanInteractwithSelectedItem(Item item)
    {
        return item == hammer;
    }

    /// <summary>
    /// 드라이버가 선택되었는지 여부
    /// </summary>
    private bool IsHammerSelected()
    {
        List<Item> candidateItems = new List<Item>();
        if (_itemEquipController.HeldItemData != null)
        {
            candidateItems.Add(_itemEquipController.HeldItemData);
        }
        if (_inventoryManager.SelectedSlotIndex >= 0 && _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex] != null)
        {
            Item slotItem = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].Item;
            // 손에 든 아이템과 슬롯 아이템이 중복되지 않을 때만 추가
            if (!candidateItems.Contains(slotItem))
            {
                candidateItems.Add(slotItem);
            }
        }

        bool canInteract = false;

        if (candidateItems.Count > 0)
        {
            foreach (Item item in candidateItems)
            {
                if (CanInteractwithSelectedItem(item))
                {
                    canInteract = true;
                    break;
                }
            }
        }

        return canInteract;
    }

    /// <summary>
    /// 덧댈 판 확대 모드 시작
    /// </summary>
    private void StartFocusMode()
    {
        _isFocused = true;

        SubmarineInGameManager.instance.SetPlayerGeoActive(false);
        _playerCameraController.enabled = false;
        flashlight.SetActive(true);
        repairPatch.SetActive(true);

        _playerInput.currentActionMap.Disable();
        _playerInput.actions["RepairEngine"].Enable();

        if (originalParticle != null) originalParticle.gameObject.SetActive(false);
        if (sparkParticle != null) sparkParticle.gameObject.SetActive(false);
        if (blockedParticle != null)
        {
            blockedParticle.gameObject.SetActive(true);
            UpdateSmokeIntensity();
        }

        _mainCamera.transform.DOMove(focusViewPos, duration).SetEase(Ease.InOutSine);
        _mainCamera.transform.DORotate(focusViewRot, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            _repairEngineQTE.StartQTE(0);
        });
    }

    /// <summary>
    /// 수리 단계별 연기 비율 (0단계: 100%, 1단계: 66%, 2단계: 33%, 3단계: 0%)
    /// </summary>
    private void UpdateSmokeIntensity()
    {
        if (blockedParticle == null) return;

        var emission = blockedParticle.emission;
        float newRate = maxEmissionRate * (1f - (_currentRepairStep / 3f));
        emission.rateOverTime = newRate;
    }

    void OnEnable()
    {
        if (_repairEngineAction != null) _repairEngineAction.performed += OnRepairEngine;
    }

    void OnDisable()
    {
        if (_repairEngineAction != null) _repairEngineAction.performed -= OnRepairEngine;
    }

    public void OnRepairEngine(InputAction.CallbackContext context)
    {
        if (!_isFocused || !context.performed) return;

        if (_repairEngineQTE.ExecuteHit())
        {
            _currentRepairStep++;
            Debug.Log($"성공! 현재 {_currentRepairStep}/3");
            PlayHammerSwing();

            if (_currentRepairStep >= 3)
            {
                Debug.Log("Complete!");
                Invoke("CompleteRepair", 1.0f);
            }
            else
            {
                Invoke("NextQTE", 1.0f);
            }
        }
        else
        {
            Debug.Log("Fail!");
            _currentRepairStep = 0;
            ExitMode();
        }
    }

    private void PlayHammerSwing()
    {
        if (itemViewRoot == null) return;

        // 초기 상태 저장
        Vector3 originalRot = itemViewRoot.localEulerAngles;
        Vector3 originalPos = itemViewRoot.localPosition;

        Sequence swingSeq = DOTween.Sequence();

        // --- 1회차 타격 (톡!) ---
        swingSeq.Append(itemViewRoot.DOLocalRotate(new Vector3(-15, 0, 0), 0.1f).SetRelative()) // 살짝 들기
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(20, 0, 0), 0.05f).SetRelative().SetEase(Ease.InQuad)) // 툭!
                      .AppendCallback(() => _mainCamera.transform.DOShakePosition(0.05f, 0.03f)) // 미세 흔들림
                      .Append(itemViewRoot.DOLocalRotate(originalRot, 0.1f)); // 복구

        // --- 2회차 타격 (톡!) ---
        swingSeq.AppendInterval(0.05f) // 타격 사이 아주 짧은 간격
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(-15, 0, 0), 0.1f).SetRelative())
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(20, 0, 0), 0.05f).SetRelative().SetEase(Ease.InQuad)) // 툭!
                      .AppendCallback(() => _mainCamera.transform.DOShakePosition(0.05f, 0.03f))
                      .Append(itemViewRoot.DOLocalRotate(originalRot, 0.1f));

        // --- 3회차 타격 (콰앙!) ---
        swingSeq.AppendInterval(0.1f) // 마지막 타격 전 약간의 딜레이로 강조
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(-50, 0, 0), 0.15f).SetRelative().SetEase(Ease.OutQuad)) // 크게 들기
                      .Append(itemViewRoot.DOLocalRotate(new Vector3(75, 0, 0), 0.05f).SetRelative().SetEase(Ease.InExpo)) // 콰앙!
                      .Join(itemViewRoot.DOLocalMoveZ(0.25f, 0.05f).SetRelative()) // 앞으로 깊게 찌르기
                      .OnComplete(() =>
                      {
                          // 강한 흔들림 및 최종 복구
                          _mainCamera.transform.DOShakePosition(0.15f, 0.1f);
                          itemViewRoot.DOLocalRotate(originalRot, 0.2f);
                          itemViewRoot.DOLocalMove(originalPos, 0.2f);

                          UpdateSmokeIntensity();
                      });
    }

    private void CompleteRepair()
    {
        _powerSwitch.IsCompleteDieselCnt++;
        _repairEngineQTE.HideQTE();
        ExitMode();
    }

    private void NextQTE()
    {
        if (_isFocused) _repairEngineQTE.StartQTE(_currentRepairStep);
    }

    /// <summary>
    /// 축소 및 플레이어 복귀
    /// </summary>
    private void ExitMode()
    {
        if (!_isFocused) return;

        _isFocused = false;

        _playerCameraController.enabled = true;
        SubmarineInGameManager.instance.SetPlayerGeoActive(true);
        _playerInput.currentActionMap.Enable();
        flashlight.SetActive(false);
        _repairEngineQTE.HideQTE();

        if (_currentRepairStep < 3)
        {
            repairPatch.SetActive(false);
            blockedParticle.gameObject.SetActive(false);
            originalParticle.gameObject.SetActive(true);
            sparkParticle.gameObject.SetActive(true);
        }
        else
        {
            Destroy(this);
        }
    }
}
