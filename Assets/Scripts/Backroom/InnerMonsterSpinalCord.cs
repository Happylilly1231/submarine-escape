using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 내부 괴물 척수
/// </summary>
public class InnerMonsterSpinalCord : MonoBehaviour, IInteractable
{
    [SerializeField] private Item bioDataExtractorItem; // 생체 데이터 추출기 아이템
    [SerializeField] private Transform extractPos; // 추출 위치(생체 데이터 추출기 아이템이 위치할 루트 트랜스폼)
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform skullViewPoint;

    private ItemEquipController _itemEquipController;
    private InventoryManager _inventoryManager;
    private InnerMonsterController _innerMonsterController;
    private GameObject _currentExtractorObj = null; // 현재 생체 데이터 추출기 오브젝트

    private float _monsterAnimatorSpeed;

    private void Awake()
    {
        _itemEquipController = FindObjectOfType<ItemEquipController>();
        _inventoryManager = FindObjectOfType<InventoryManager>();
        _innerMonsterController = transform.parent.GetComponent<InnerMonsterController>();
    }

    public bool CanInteractwithSelectedItem(Item item)
    {
        return item == bioDataExtractorItem;
    }

    public string GetInteractText()
    {
        if (IsBioDataExtractorSelected())
        {
            BioDataExtractor bioDataExtractor = _itemEquipController.HeldItemObject.GetComponent<BioDataExtractor>();
            if (!bioDataExtractor.CheckIsFull()) // 가득 차있지 않은 경우 -> 추출 가능
                return "Extract Bio Data [E]";
            else // 가득 차 있는 경우 -> 추출 불가능
                return "Already Full";
        }
        else
            return "";
    }

    public void Interact()
    {
        if (IsBioDataExtractorSelected())
        {
            BioDataExtractor bioDataExtractor = _itemEquipController.HeldItemObject.GetComponent<BioDataExtractor>();
            if (!bioDataExtractor.CheckIsFull()) // 가득 차있지 않은 경우 -> 추출 가능
                StartExtractingBioData();
        }
        else
        {
            Debug.Log("생체 데이터 추출기를 들고 있지 않습니다!");
        }
    }

    public bool IsBioDataExtractorSelected()
    {
        return CanInteractwithSelectedItem(_itemEquipController.HeldItemData); // 손에 든 아이템만 검사 (추출기를 손에 들고 있는가)
    }

    /// <summary>
    /// 생체 데이터 추출 시작
    /// </summary>
    private void StartExtractingBioData()
    {
        Debug.Log("추출 시작!");

        // 1. 포커스 & 현재 플레이어 위치 저장 (나중에 백룸에서 탈출할 때 다시 이곳으로 옴)
        FocusManager.Instance.PushFocusState(GameFocusState.GameTimePauseSequence); // 게임 시간 정지 포커스 상태로 변경
        // SubmarineInGameManager.instance.SetFocus(true);
        BackroomManager.Instance.originalPos = SubmarineInGameManager.instance.player.transform.position;

        // 2. 괴물 정지
        _innerMonsterController.IsBeingExtracted = true;
        _innerMonsterController.CanMove(false);
        _monsterAnimatorSpeed = _innerMonsterController.Animator.speed;
        _innerMonsterController.Animator.speed = 0f;
        _innerMonsterController.audioSource.Pause();

        // 3. 추출기 아이템 관련 로직
        // 현재 들고 있는 아이템의 오브젝트(카메라 밑에 생성된 프리팹)를 현재 생체 데이터 추출기 오브젝트로 설정
        // 3-1. 추출기 아이템 소모 
        int itemInstanceNum = _inventoryManager.InventorySlots[_inventoryManager.SelectedSlotIndex].ItemInstanceNum; // 현재 선택된 슬롯의 인스턴스 번호 저장
        _inventoryManager.ConsumeItemInSlot(_itemEquipController.HeldItemData); // 손에 들고 있던 추출기 아이템을 인벤토리에서 소모(기존 들고 있던 프리팹 파괴됨)
        // 3-2. 해당 아이템 프리팹을 괴물 척수에 새로 생성(개별 데이터는 유지되어야 함)
        _currentExtractorObj = Instantiate(bioDataExtractorItem.ItemPrefab, extractPos); // 추출기 아이템 생성
        _currentExtractorObj.transform.localPosition = Vector3.zero; // 위치 초기화 (부모와 동일하게)
        _currentExtractorObj.transform.localRotation = Quaternion.identity; // 회전값 초기화 (부모와 동일하게)
        StatableItemManager.Instance.RestoreItemState(_currentExtractorObj, itemInstanceNum); // 해당 아이템 오브젝트(인스턴스)의 저장된 상태가 있다면, 저장된 상태로 복원
        // 3-3. 해당 아이템 프리팹 오브젝트는 못 줍게 변경
        _currentExtractorObj.GetComponent<BoxCollider>().enabled = false; // 아이템 못 줍게 콜라이더 컴포넌트 비활성화
        _currentExtractorObj.GetComponent<SphereCollider>().enabled = false; // 아이템 못 줍게 콜라이더 컴포넌트 비활성화

        // 4. 연출
        Sequence seq = DOTween.Sequence();

        // 추출기 꽂는 모습 보는 위치로 카메라 이동
        seq.Append(Camera.main.transform.DOMove(viewPoint.position, 1f)
        .SetEase(Ease.OutQuad));
        seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 1f)
        .SetEase(Ease.OutQuad));

        // 주사기 꽂기
        seq.Append(_currentExtractorObj.transform.DOLocalMoveX(-0.055f, 1f));

        // // 플런저(피스톤 막대 부분) 당기기
        // seq.Append(bioDataExtractorPlungerObj.transform.DOLocalMoveZ(-2f, 1f));

        // 비네트 효과
        FXManager.instance.Vignette.color.value = Color.black;
        seq.Append(DOTween.To(() => FXManager.instance.Vignette.intensity.value,
            x => FXManager.instance.Vignette.intensity.value = x, 0.5f, 3f)
            .SetEase(Ease.OutQuad));

        float waveTime = 1.5f; // 한 번 출렁이는 시간
        int loopCount = 4;     // 0.5초 * 6번 = 총 3초

        // 1. 카메라 Z축 회전 (좌우로 흔들흔들)
        // 3도 정도를 2초 동안 왕복하며 무한 반복(Yoyo)
        seq.Join(transform.DOLocalRotate(new Vector3(0, 0, 3f), waveTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(loopCount, LoopType.Yoyo));
        // 2. FOV 울렁거림 (숨쉬는 듯한 시야) ⚓
        seq.Join(Camera.main.DOFieldOfView(50f, waveTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(loopCount, LoopType.Yoyo));
        // 3. 색수차(Chromatic Aberration) 강도 조절
        // 0.2에서 0.8 사이를 왔다갔다 하며 정신없는 느낌 연출
        seq.Join(DOTween.To(() => FXManager.instance.Chromatic.intensity.value, x => FXManager.instance.Chromatic.intensity.value = x, 0.8f, waveTime)
            .SetEase(Ease.InOutFlash)
            .SetLoops(loopCount, LoopType.Yoyo));
        // 4. 렌즈 왜곡 (꿀렁거리는 느낌의 핵심!)
        seq.Join(DOTween.To(() => FXManager.instance.Distortion.intensity.value, x => FXManager.instance.Distortion.intensity.value = x, -0.2f, waveTime)
            .SetEase(Ease.InOutQuad)
            .SetLoops(loopCount, LoopType.Yoyo));

        // // 위의 해골들을 바라보도록 보도록 회전
        // Vector3 currentRotation = viewPoint.rotation.eulerAngles;
        // currentRotation.x = -20f;
        // seq.Append(Camera.main.transform.DORotateQuaternion(Quaternion.Euler(currentRotation), 2f)
        // .SetEase(Ease.OutQuad));

        // 해골 모습 보는 위치로 카메라 이동
        seq.Join(Camera.main.transform.DOMove(skullViewPoint.position, 1.5f)
        .SetEase(Ease.OutQuad)
        .SetDelay(3f));
        seq.Join(Camera.main.transform.DORotateQuaternion(skullViewPoint.rotation, 1.5f)
        .SetEase(Ease.OutQuad)
        .SetDelay(3f));

        // 빨려 들어가며 암전 (FOV 감소와 동시에 Fade In)
        seq.Append(Camera.main.DOFieldOfView(20f, 3f).SetEase(Ease.InExpo));
        seq.Join(FXManager.instance.fadeImage.DOFade(1f, 2f)); // 화면이 완전히 검게 변함

        seq.AppendCallback(() =>
        {
            FXManager.instance.VignetteOff();
        });
        // 잠시 정적 (완전 암전 상태)
        seq.AppendInterval(0.5f);

        // 5. 백룸 진입
        seq.AppendCallback(() =>
        {
            BackroomManager.Instance.EnterBackroom(); // 백룸 진입
            Camera.main.fieldOfView = 60f; // FOV 초기화
        });

        // 잠시 정적 (완전 암전 상태)
        seq.AppendInterval(2f);

        // 포커스 해제
        seq.AppendCallback(() =>
        {
            FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
            // SubmarineInGameManager.instance.SetFocus(false);
            GameTime.Instance.SetPauseInBackroom(true); // 백룸에서는 게임 시간 정지
        });

        // 완료 시 화면 밝아지기
        seq.OnComplete(() =>
        {
            FXManager.instance.fadeImage.DOFade(0f, 3f); // 다시 밝아짐
        });
    }

    /// <summary>
    /// 생체 데이터 추출 완료
    /// </summary>
    public void CompleteExtractingBioData()
    {
        // 피 충전(3칸 전부 다)
        _currentExtractorObj.GetComponent<BioDataExtractor>().Fill();

        // 연출
        Sequence seq = DOTween.Sequence();

        // 주사기 빼는 모습 보는 위치로 카메라 이동
        SubmarineInGameManager.instance.SetCameraControllerEnable(false); // 플레이어 카메라 컨트롤러 비활성화
        Camera.main.transform.position = viewPoint.position;
        Camera.main.transform.rotation = viewPoint.rotation;

        // 주사기 빼기
        seq.Append(_currentExtractorObj.transform.DOLocalMoveX(0f, 1f));

        seq.AppendCallback(() =>
        {
            // 아이템 관련
            _currentExtractorObj.GetComponent<Collider>().enabled = true; // 아이템 다시 주을 수 있게 콜라이더 컴포넌트 활성화
            _currentExtractorObj.GetComponent<SphereCollider>().enabled = true; // 아이템 다시 주을 수 있게 콜라이더 컴포넌트 활성화
            _currentExtractorObj = null; // 현재 추출기 아이템 초기화(추출 끝났으므로)

            // 상태
            FocusManager.Instance.PopFocusState(); // 이전 포커스 복구
            // SubmarineInGameManager.instance.SetFocus(false); // 포커스 해제
            GameTime.Instance.SetPauseInBackroom(false); // 백룸 나왔으니 게임 시간 정지 해제
            _innerMonsterController.IsBeingExtracted = false; // 추출 중 아님으로 설정
        });

        seq.AppendInterval(2f); // 약간의 대기 시간 (괴물이 정지한 채로 있음 - 추출기 아이템 줍기 쉽도록)

        // 완료 시 로직
        seq.OnComplete(() =>
        {
            // 괴물 정지 해제
            _innerMonsterController.CanMove(true);
            _innerMonsterController.Animator.speed = _monsterAnimatorSpeed;
            _innerMonsterController.audioSource.UnPause();

            Debug.Log("추출 완료!");
        });
    }
}
