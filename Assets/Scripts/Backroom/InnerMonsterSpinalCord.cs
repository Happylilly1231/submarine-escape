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

        // 4. 연출
        Sequence seq = DOTween.Sequence();

        // // 추출기 꽂는 모습 보는 위치로 카메라 이동
        // seq.Append(Camera.main.transform.DOMove(viewPoint.position, 0.4f).SetEase(Ease.OutCubic));
        // seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 0.4f).SetEase(Ease.OutCubic));

        // // 주사기 꽂기
        // // 2. 주사기 꽂는 순간 - 강한 한 방 충격! (Punch / Shake)
        // seq.Append(_currentExtractorObj.transform.DOLocalMoveX(-0.055f, 0.15f).SetEase(Ease.InQuad));
        // // 꽂히는 순간 카메라 쿵! 자극
        // seq.Join(Camera.main.transform.DOShakePosition(0.3f, strength: 0.15f, vibrato: 20, randomness: 90));

        // // 3. 추출기 작동 & 괴물 생체 반응 (불쾌함, 발작적 흔들림)
        // // 비네트가 불길하게 확 어두워짐 + 붉은 빛 조화 (있다면 극상)
        // FXManager.instance.Vignette.color.value = Color.red; // 혹은 매우 짙은 검은색
        // seq.Append(DOTween.To(() => FXManager.instance.Vignette.intensity.value, x => FXManager.instance.Vignette.intensity.value = x, 0.65f, 0.8f));

        // // 정형화된 Yoyo 대신 불규칙하고 가파른 렌즈 왜곡 & 색수차 펄스
        // seq.Join(DOTween.To(() => FXManager.instance.Chromatic.intensity.value, x => FXManager.instance.Chromatic.intensity.value = x, 1.0f, 0.8f).SetEase(Ease.InFlash));
        // seq.Join(DOTween.To(() => FXManager.instance.Distortion.intensity.value, x => FXManager.instance.Distortion.intensity.value = x, -0.45f, 0.8f).SetEase(Ease.InExpo));

        // // 거칠고 무작위적인 카메라 떨림 (괴물의 척수가 미쳐서 출렁이는 느낌)
        // seq.Join(Camera.main.transform.DOShakeRotation(1.2f, strength: new Vector3(2f, 2f, 5f), vibrato: 30));

        // // 4. 해골(기이한 시선) 쪽으로 시선이 강제로 꺾임
        // seq.Append(Camera.main.transform.DOMove(skullViewPoint.position, 0.5f).SetEase(Ease.InBack));
        // seq.Join(Camera.main.transform.DORotateQuaternion(skullViewPoint.rotation, 0.5f).SetEase(Ease.InBack));

        // // 5. [핵심] 백룸으로 '빨려 들어가는' 시공간 왜곡 연출
        // // FOV를 순간적으로 넓혔다가(공간 팽창) -> 10 이하로 좁히며 급속 이동 (워프 효과)
        // seq.Append(Camera.main.DOFieldOfView(85f, 0.2f).SetEase(Ease.OutQuad)); // 시야 확장 (흡입 전 숨고르기)
        // seq.Append(Camera.main.DOFieldOfView(5f, 0.5f).SetEase(Ease.InExpo));   // 푹 빨려들어감!
        // seq.Join(FXManager.instance.fadeImage.DOFade(1f, 0.4f).SetEase(Ease.InQuad)); // 빠른 암전

        // // 6. 후처리 정돈 및 정적
        // seq.AppendCallback(() =>
        // {
        //     FXManager.instance.VignetteOff();
        //     FXManager.instance.Chromatic.intensity.value = 0f;
        //     FXManager.instance.Distortion.intensity.value = 0f;
        //     Camera.main.fieldOfView = 60f; // 기본 FOV 복구
        // });

        // seq.AppendInterval(1.0f); // 긴장감을 고조시키는 완전한 정적과 암전


        // 0. 초기화 (Post Exposure를 연출용으로 쓸 것이므로 확인)
        enterBackroomVolume.weight = 0f;
        ColorAdjustments colorAdjustments;
        enterBackroomVolume.profile.TryGet(out colorAdjustments);
        if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;

        // 1. 카메라가 괴물 척수로 스윽 접근 & 주사기 깊숙이 '스윽-' 꽂힘 (이질적인 속도)
        seq.Append(Camera.main.transform.DOMove(viewPoint.position, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(Camera.main.transform.DORotateQuaternion(viewPoint.rotation, 0.4f).SetEase(Ease.OutCubic));
        seq.Append(_currentExtractorObj.transform.DOLocalMoveX(-0.055f, 0.6f).SetEase(Ease.InOutSine)); // 징그럽게 스윽...

        // 2. [핵심] 주사기가 다 꽂히자마자 홀린 듯 해골로 시선이 부드럽게 넘어가며 '환각 파도' 몰아침
        // 2-1. 시선 이동 (해골을 향해 지그시)
        seq.Append(Camera.main.transform.DOMove(skullViewPoint.position, 1.2f).SetEase(Ease.InOutCubic));
        seq.Join(Camera.main.transform.DORotateQuaternion(skullViewPoint.rotation, 1.2f).SetEase(Ease.InOutCubic));

        // 2-2. (동시 진행!) 시선이 돌아가는 동안 전용 볼륨 침식 + 렌즈 왜곡 꿀렁거림
        seq.Join(DOTween.To(() => enterBackroomVolume.weight, x => enterBackroomVolume.weight = x, 1f, 1.2f).SetEase(Ease.InQuad));

        if (distortion != null)
        {
            // 렌즈 왜곡이 -0.8까지 확 왜곡됐다가 다시 돌아오는 꿀렁거림(Yoyo)
            seq.Join(DOTween.To(() => distortion.intensity.value, x => distortion.intensity.value = x, -0.8f, 0.6f)
                .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
        }

        // 2-3. (동시 진행!) 고개가 돌아가며 정신이 아득해지는 미세한 고주파 미치광이 떨림
        seq.Join(Camera.main.transform.DOShakeRotation(1.2f, strength: new Vector3(1.5f, 1.5f, 3f), vibrato: 25));

        // 3. 👁️ [해골 고정 & 과노출 흡입] 해골에 시선이 꽂힌 채 공간으로 집어삼켜짐
        // 3-1. FOV가 해골의 눈구멍/중심으로 쑥 끌려들어감
        seq.Append(Camera.main.DOFieldOfView(12f, 1.0f).SetEase(Ease.InExpo));

        // 3-2. (동시 진행!) Post Exposure가 폭발하며 눈이 하얗게 멀어버림 (Whiteout)
        if (colorAdjustments != null)
        {
            seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 8.0f, 1.0f).SetEase(Ease.InExpo));
        }

        // 3-3. (동시 진행!) 동시에 UI Fade도 1로 맞춰 완전한 빛의 심연/화이트아웃 연출 (FadeImage 색상을 White로 지정하거나 커스텀)
        seq.Append(FXManager.instance.fadeImage.DOFade(1f, 0.8f).SetEase(Ease.InQuad));

        // 페이드가 진행되는 동안 폭발했던 Post Exposure를 0(원래 값)으로 함께 내립니다.
        if (colorAdjustments != null)
        {
            seq.Join(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, 0f, 0.8f).SetEase(Ease.OutQuad));
        }

        // 4. 연출 완료 후 정리
        seq.AppendCallback(() =>
        {
            enterBackroomVolume.weight = 0f;
            Camera.main.fieldOfView = 60f;
            if (colorAdjustments != null) colorAdjustments.postExposure.value = 0f;
            if (distortion != null) distortion.intensity.value = 0f;
        });

        seq.AppendInterval(0.5f);




        // // 비네트 효과
        // FXManager.instance.Vignette.color.value = Color.black;
        // seq.Append(DOTween.To(() => FXManager.instance.Vignette.intensity.value,
        //     x => FXManager.instance.Vignette.intensity.value = x, 0.5f, 3f)
        //     .SetEase(Ease.OutQuad));

        // float waveTime = 1.5f; // 한 번 출렁이는 시간
        // int loopCount = 4;     // 0.5초 * 6번 = 총 3초

        // // 1. 카메라 Z축 회전 (좌우로 흔들흔들)
        // // 3도 정도를 2초 동안 왕복하며 무한 반복(Yoyo)
        // seq.Join(transform.DOLocalRotate(new Vector3(0, 0, 3f), waveTime)
        //     .SetEase(Ease.InOutSine)
        //     .SetLoops(loopCount, LoopType.Yoyo));
        // // 2. FOV 울렁거림 (숨쉬는 듯한 시야) ⚓
        // seq.Join(Camera.main.DOFieldOfView(50f, waveTime)
        //     .SetEase(Ease.InOutSine)
        //     .SetLoops(loopCount, LoopType.Yoyo));
        // // 3. 색수차(Chromatic Aberration) 강도 조절
        // // 0.2에서 0.8 사이를 왔다갔다 하며 정신없는 느낌 연출
        // seq.Join(DOTween.To(() => FXManager.instance.Chromatic.intensity.value, x => FXManager.instance.Chromatic.intensity.value = x, 0.8f, waveTime)
        //     .SetEase(Ease.InOutFlash)
        //     .SetLoops(loopCount, LoopType.Yoyo));
        // // 4. 렌즈 왜곡 (꿀렁거리는 느낌의 핵심!)
        // seq.Join(DOTween.To(() => FXManager.instance.Distortion.intensity.value, x => FXManager.instance.Distortion.intensity.value = x, -0.2f, waveTime)
        //     .SetEase(Ease.InOutQuad)
        //     .SetLoops(loopCount, LoopType.Yoyo));

        // // // 위의 해골들을 바라보도록 보도록 회전
        // // Vector3 currentRotation = viewPoint.rotation.eulerAngles;
        // // currentRotation.x = -20f;
        // // seq.Append(Camera.main.transform.DORotateQuaternion(Quaternion.Euler(currentRotation), 2f)
        // // .SetEase(Ease.OutQuad));

        // // 해골 모습 보는 위치로 카메라 이동
        // seq.Join(Camera.main.transform.DOMove(skullViewPoint.position, 1.5f)
        // .SetEase(Ease.OutQuad)
        // .SetDelay(3f));
        // seq.Join(Camera.main.transform.DORotateQuaternion(skullViewPoint.rotation, 1.5f)
        // .SetEase(Ease.OutQuad)
        // .SetDelay(3f));

        // // 빨려 들어가며 암전 (FOV 감소와 동시에 Fade In)
        // seq.Append(Camera.main.DOFieldOfView(20f, 3f).SetEase(Ease.InExpo));
        // seq.Join(FXManager.instance.fadeImage.DOFade(1f, 2f)); // 화면이 완전히 검게 변함

        // seq.AppendCallback(() =>
        // {
        //     FXManager.instance.VignetteOff();
        // });
        // // 잠시 정적 (완전 암전 상태)
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
