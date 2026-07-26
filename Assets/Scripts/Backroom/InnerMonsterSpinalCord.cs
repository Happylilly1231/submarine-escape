using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private Volume enterBackroomVolume;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private AudioClip spookySound;
    Color hdrRed = new Color(1f, 0f, 0f) * 3f;
    Color originalHdrColor;
    private LensDistortion distortion;
    private ChromaticAberration chromatic;

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

        // 전용 볼륨 내부 컴포넌트 캐싱 (필요 시)
        if (enterBackroomVolume != null && enterBackroomVolume.profile != null)
        {
            enterBackroomVolume.profile.TryGet(out distortion);
            enterBackroomVolume.profile.TryGet(out chromatic);
        }

        originalHdrColor = bodyRenderer.material.GetColor("_EmissionColor");
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
                return LocalizationHelper.GetLocalizedInteractText("Interact/ExtractBioData", "E");
            else // 가득 차 있는 경우 -> 추출 불가능
                return LocalizationHelper.GetLocalizedInteractText("Interact/AlreadyFull");
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

        // // 4. 연출
        // Sequence seq = DOTween.Sequence();


        // --------------------------------------------------
        // 0. 준비 작업: 연출용 임시 Pivot 생성 및 카메라 자식화
        // --------------------------------------------------
        GameObject tempPivotObj = new GameObject("TempCameraPivot");
        Transform tempPivot = tempPivotObj.transform;

        // 피벗의 위치와 회전을 현재 카메라와 동일하게 설정
        tempPivot.position = Camera.main.transform.position;
        tempPivot.rotation = Camera.main.transform.rotation;

        // 카메라를 피벗의 자식으로 등록
        Transform cam = Camera.main.transform;
        cam.SetParent(tempPivot);

        // 0-1. Post Exposure 초기화
        enterBackroomVolume.weight = 0f;
        ColorAdjustments colorAdjustments;
        enterBackroomVolume.profile.TryGet(out colorAdjustments);
        if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;

        // 으스스한 소리 재생
        AudioManager.Instance.PlayGlobalOneShot(spookySound);


        // --------------------------------------------------
        // Sequence 시작
        // --------------------------------------------------
        Sequence seq = DOTween.Sequence();

        // 1. 카메라 피벗이 괴물 척수로 접근 (부모 Transform 이동)
        seq.Append(tempPivot.DOMove(viewPoint.position, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(tempPivot.DORotateQuaternion(viewPoint.rotation, 0.4f).SetEase(Ease.OutCubic));
        seq.Append(_currentExtractorObj.transform.DOLocalMoveX(-0.055f, 0.6f).SetEase(Ease.InOutSine));

        // 2. 해골로 시선 이동 + 환각 연출
        // 2-1. 부모 피벗이 해골 조준점 위치/회전으로 이동
        seq.Append(tempPivot.DOMove(skullViewPoint.position, 1.2f).SetEase(Ease.InOutCubic));
        seq.Join(tempPivot.DORotateQuaternion(skullViewPoint.rotation, 1.2f).SetEase(Ease.InOutCubic));

        // 2-2. 볼륨 & 렌즈 왜곡
        seq.Join(DOTween.To(() => enterBackroomVolume.weight, x => enterBackroomVolume.weight = x, 1f, 1.2f).SetEase(Ease.InQuad));
        if (distortion != null)
        {
            seq.Join(DOTween.To(() => distortion.intensity.value, x => distortion.intensity.value = x, -0.8f, 0.6f)
                .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
        }

        // 2-3. [핵심] 이동은 부모가 하고, 미친 고주파 떨림은 자식(카메라)에만 부여!
        seq.Join(cam.DOShakeRotation(1.2f, strength: new Vector3(1.5f, 1.5f, 3f), vibrato: 25));

        // // 해골 눈 빨간색으로 변경
        // seq.Join(bodyRenderer.material.DOColor(hdrRed, "_EmissionColor", 0.1f));

        // 3. FOV 끌려들어감 & 화이트아웃
        seq.Append(Camera.main.DOFieldOfView(12f, 1.0f).SetEase(Ease.InExpo));

        if (colorAdjustments != null)
        {
            seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 8.0f, 1.0f).SetEase(Ease.InExpo));
        }

        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 0.8f).SetEase(Ease.InQuad));

        if (colorAdjustments != null)
        {
            seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 0f, 0.8f).SetEase(Ease.OutQuad));
        }

        // --------------------------------------------------
        // 4. 연출 완료 후 카메라 원래대로 복원 및 임시 피벗 삭제
        // --------------------------------------------------
        seq.AppendCallback(() =>
        {
            // 자식 카메라 회전/위치 오프셋 초기화
            cam.localPosition = Vector3.zero;
            cam.localRotation = Quaternion.identity;

            // 부모 관계 해제 및 임시 오브젝트 파괴
            cam.SetParent(null);
            Destroy(tempPivotObj);

            // 기타 볼륨 및 FOV 초기화
            enterBackroomVolume.weight = 0f;
            Camera.main.fieldOfView = 60f;
            if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;
            if (distortion != null) distortion.intensity.value = 0f;

            // 해골 눈 색 원래대로 변경
            bodyRenderer.material.SetColor("_EmissionColor", originalHdrColor);
        });

        seq.AppendInterval(0.5f);


        // // 0. 초기화 (Post Exposure를 연출용으로 쓸 것이므로 확인)
        // enterBackroomVolume.weight = 0f;
        // ColorAdjustments colorAdjustments;
        // enterBackroomVolume.profile.TryGet(out colorAdjustments);
        // if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;

        // // 1. 카메라가 괴물 척수로 스윽 접근 & 주사기 깊숙이 '스윽-' 꽂힘 (이질적인 속도)
        // seq.Append(Camera.main.transform.DOMove(viewPoint.position, 0.4f).SetEase(Ease.OutCubic));
        // seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 0.4f).SetEase(Ease.OutCubic));
        // seq.Append(_currentExtractorObj.transform.DOLocalMoveX(-0.055f, 0.6f).SetEase(Ease.InOutSine)); // 징그럽게 스윽...

        // // 2. [핵심] 주사기가 다 꽂히자마자 홀린 듯 해골로 시선이 부드럽게 넘어가며 '환각 파도' 몰아침
        // // 2-1. 시선 이동 (해골을 향해 지그시)
        // seq.Append(Camera.main.transform.DOMove(skullViewPoint.position, 1.2f).SetEase(Ease.InOutCubic));
        // seq.Join(Camera.main.transform.DORotateQuaternion(skullViewPoint.rotation, 1.2f).SetEase(Ease.InOutCubic));

        // // 2-2. (동시 진행!) 시선이 돌아가는 동안 전용 볼륨 침식 + 렌즈 왜곡 꿀렁거림
        // seq.Join(DOTween.To(() => enterBackroomVolume.weight, x => enterBackroomVolume.weight = x, 1f, 1.2f).SetEase(Ease.InQuad));

        // if (distortion != null)
        // {
        //     // 렌즈 왜곡이 -0.8까지 확 왜곡됐다가 다시 돌아오는 꿀렁거림(Yoyo)
        //     seq.Join(DOTween.To(() => distortion.intensity.value, x => distortion.intensity.value = x, -0.8f, 0.6f)
        //         .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
        // }

        // // 2-3. (동시 진행!) 고개가 돌아가며 정신이 아득해지는 미세한 고주파 미치광이 떨림
        // seq.Join(Camera.main.transform.DOShakeRotation(1.2f, strength: new Vector3(1.5f, 1.5f, 3f), vibrato: 25));

        // // 3. 👁️ [해골 고정 & 과노출 흡입] 해골에 시선이 꽂힌 채 공간으로 집어삼켜짐
        // // 3-1. FOV가 해골의 눈구멍/중심으로 쑥 끌려들어감
        // seq.Append(Camera.main.DOFieldOfView(12f, 1.0f).SetEase(Ease.InExpo));

        // // 3-2. (동시 진행!) Post Exposure가 폭발하며 눈이 하얗게 멀어버림 (Whiteout)
        // if (colorAdjustments != null)
        // {
        //     seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 8.0f, 1.0f).SetEase(Ease.InExpo));
        // }

        // // 3-3. (동시 진행!) 동시에 UI Fade도 1로 맞춰 완전한 빛의 심연/화이트아웃 연출 (FadeImage 색상을 White로 지정하거나 커스텀)
        // seq.Append(FXManager.instance.fadeImage.DOFade(1f, 0.8f).SetEase(Ease.InQuad));

        // // 페이드가 진행되는 동안 폭발했던 Post Exposure를 0(원래 값)으로 함께 내립니다.
        // if (colorAdjustments != null)
        // {
        //     seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 0f, 0.8f).SetEase(Ease.OutQuad));
        // }

        // // 4. 연출 완료 후 정리
        // seq.AppendCallback(() =>
        // {
        //     enterBackroomVolume.weight = 0f;
        //     Camera.main.fieldOfView = 60f;
        //     if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;
        //     if (distortion != null) distortion.intensity.value = 0f;
        // });

        // seq.AppendInterval(0.5f);



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
        PlayerManager.Instance.SetCameraControllerEnable(false); // 플레이어 카메라 컨트롤러 비활성화
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
