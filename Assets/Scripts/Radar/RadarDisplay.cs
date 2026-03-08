using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TorpedoState { Normal, Abnormal, Unloaded } // 정상 탑재, 비정상 탑재, 탑재되지 않음

public class RadarDisplay : MonoBehaviour
{
    [SerializeField] private GameObject lockedUI; // 잠금 UI
    [SerializeField] private GameObject radarUI; // 레이더 UI
    [SerializeField] private GameObject statUI; // 스탯 UI
    [SerializeField] private TextMeshProUGUI lockedUIHeaderText; // 잠금 UI 헤더 텍스트
    [SerializeField] private TextMeshProUGUI codeInputText; // 코드 입력 텍스트
    [SerializeField] private Image enterImg; // 엔터 이미지
    [SerializeField] private Image lockedTickPart; // 잠겼을 때 눈금 부분 이미지
    [SerializeField] private RectTransform originDot; // 원점(현재 잠수함 점)
    public RectTransform OriginDot => originDot; // 원점(현재 잠수함 점)
    [SerializeField] private RectTransform selectionIndicator; // 선택 표시
    [SerializeField] private Image explosionRangeImg; // 폭발 범위 이미지
    [SerializeField] private RectTransform distanceLine; // 거리 선
    [SerializeField] private RectTransform firingLine; // 발사 중 선
    [SerializeField] private RectTransform heightContent; // 높이 눈금 부분
    [SerializeField] private RectTransform torpedoGroup; // 어뢰 수 나타내는 어뢰 이미지 그룹
    [SerializeField] private RectTransform fireTorpedo; // 발사 어뢰
    [SerializeField] private RectTransform realExplosionRange; // 실제 폭발 범위
    [SerializeField] private RectTransform animExplosionRange; // 애니메이션 폭발 범위

    public GameObject heightLever; // 높이 레버
    public GameObject fireButton; // 발사 버튼

    private RadarController _radarController;

    private Coroutine _showExplosionCoroutine = null;

    private void Awake()
    {
        _radarController = GetComponent<RadarController>();

        SetLockedUIActive(false);
        SetRadarUIActive(false);

        ResetHeaderText();
        SetEnterImageHighlight(false);
    }

    public void SetLockedUIActive(bool isActive)
    {
        lockedUI.SetActive(isActive);
    }

    /// <summary>
    /// 레이더 UI 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetRadarUIActive(bool isActive)
    {
        radarUI.SetActive(isActive);
        statUI.SetActive(!isActive);

        if (isActive)
        {
            selectionIndicator.anchoredPosition = Vector2.zero;

            distanceLine.gameObject.SetActive(false);

            // 실제 어뢰 발사 UI 요소 숨기기
            firingLine.gameObject.SetActive(false);
            fireTorpedo.gameObject.SetActive(false);
            realExplosionRange.gameObject.SetActive(false);

            // 애니메이션 어뢰 발사 UI 요소 숨기기
            // animTargetDot.gameObject.SetActive(false);
            // animTorpedo.gameObject.SetActive(false);
            // animDistanceLine.gameObject.SetActive(false);
            // animExplosionRange.gameObject.SetActive(false);
        }
        else
        {
            fireButton.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        }
    }

    public void SetCodeInputText(string text)
    {
        SetEnterImageHighlight(text.Length == RadarController.MAX_LENGTH);

        for (int i = text.Length; i < RadarController.MAX_LENGTH; i++)
        {
            text += '_';
        }
        codeInputText.text = text;
    }

    public void UpdateUIAfterSubmit(bool isSuccess)
    {
        if (isSuccess)
        {
            lockedUIHeaderText.text = "UNLOCKED";
            lockedUIHeaderText.color = Color.green;

            // 배경 색 어두운 초록색으로 변경
            lockedUI.transform.GetChild(0).GetComponent<Image>().color = new Color(0f / 255f, 90f / 255f, 20f / 255f, 200f / 255f);
            lockedTickPart.color = Color.black;

            // 줄무늬 라인 숨기기
            lockedUI.transform.GetChild(1).gameObject.SetActive(false);
            lockedUI.transform.GetChild(2).gameObject.SetActive(false);

            // 코드 입력 부분 숨기기
            codeInputText.gameObject.SetActive(false);

            // 중앙 약간 어두운 부분 투명도 0으로 만들기
            codeInputText.transform.parent.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        }
        else
        {
            lockedUIHeaderText.text = "[INVALID CODE]";
        }
    }

    public void ResetHeaderText()
    {
        lockedUIHeaderText.text = "MANUAL MODE UNLOCKED";
    }

    public void SetEnterImageHighlight(bool isHighlight)
    {
        Color color = Color.red;
        if (isHighlight)
            color.a = 1f;
        else
            color.a = 50f / 255f;

        enterImg.color = color;
    }

    /// <summary>
    /// 선 그리기
    /// </summary>
    /// <param name="targetPos">타겟 위치</param>
    /// <param name="line">선 이미지 RectTransform</param>
    public void DrawLineUI(RectTransform line, Vector2 targetPos)
    {
        // 선 활성화
        if (!line.gameObject.activeSelf)
            line.gameObject.SetActive(true);

        // 원점, 방향, 거리 설정
        Vector2 originPos = originDot.anchoredPosition;
        Vector2 dir = targetPos - originPos;
        float distance = dir.magnitude;
        dir = dir.normalized; // 방향 정규화

        // 선이 원점에서 타겟까지 이어지도록 함
        line.anchoredPosition = originDot.anchoredPosition;
        line.sizeDelta = new Vector2(distance, line.sizeDelta.y); // 길이

        // 해당 방향으로 선 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 현재 선택된 위치 갱신 -> 거리 선택
    /// </summary>
    /// <param name="currentPos">현재 선택 위치</param>
    public void UpdateCurrentSelectUIPos(Vector2 currentPos)
    {
        // 발사 중 -> 선택 안됨
        if (_radarController.IsFiring)
            return;

        // 현재 선택 위치를 현재 클릭 위치로 설정
        _radarController.CurrentSelectUIPos = currentPos;

        // 2차원 상 위치 선택
        _radarController.SelectPlanarPos(true);

        // 선택 표시 UI
        if (!selectionIndicator.gameObject.activeSelf)
            selectionIndicator.gameObject.SetActive(true);
        selectionIndicator.anchoredPosition = currentPos;

        // 현재 선택 위치를 타겟으로 선 그리기
        DrawLineUI(distanceLine, currentPos);

        // // 애니메이션 UI는 초기화함(거리 새로 선택될 때마다 무조건 초기화함, 높이 선택 안됨으로 설정)
        // ResetAnimUI();
    }

    /// <summary>
    /// 높이 UI 갱신(레버, 눈금)
    /// </summary>
    /// <param name="angle"></param>
    public void UpdateHeightUI(float angle, float targetY)
    {
        // 레버 위 아래 이동
        heightLever.transform.localRotation = Quaternion.Euler(angle, 0, 0);

        // 눈금 위 아래 이동
        heightContent.anchoredPosition = new Vector2(heightContent.anchoredPosition.x, targetY);
    }

    #region 발사
    /// <summary>
    /// 발사 버튼 활성화 여부 갱신
    /// </summary>
    public void UpdateFireButtonActive()
    {
        // 거리나 높이가 하나라도 선택되지 않았을 때 or 어뢰 3번 발사한 이후 or 현재 발사 중 or 현재 경보 발생 중일 때 -> 비활성화
        if (!_radarController.CheckFireAvailable())
        {
            // 비활성화
            fireButton.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        }
        else // 위 경우가 아닌 경우에만 활성화
        {
            // 활성화
            fireButton.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// 어뢰 하나 비활성화
    /// </summary>
    /// <param name="index">인덱스</param>
    public void DeactivateOneTorpedo(int index)
    {
        torpedoGroup.GetChild(index).gameObject.SetActive(false); // 해당 어뢰를 카운트 UI에서 비활성화 
    }

    /// <summary>
    /// 현재 어뢰 상태 UI 업데이트
    /// </summary>
    public void UpdateCurrentTorpedoStateUI()
    {
        Image torpedoImg = torpedoGroup.GetChild(_radarController.CurrentTorpedoIndex).GetComponent<Image>();
        switch (_radarController.CurrentTorpedoState)
        {
            case TorpedoState.Normal:
                torpedoImg.color = Color.green;
                break;
            case TorpedoState.Abnormal:
                torpedoImg.color = Color.red;
                break;
            case TorpedoState.Unloaded:
                torpedoImg.color = Color.gray;
                break;
        }
    }

    /// <summary>
    /// 발사 어뢰 활성화 여부 설정
    /// </summary>
    /// <param name="isActive">활성화 여부</param>
    public void SetFireTorpedoActive(bool isActive)
    {
        fireTorpedo.gameObject.SetActive(isActive);
    }

    public void SetColorBeforFire(FireMode fireMode)
    {
        if (fireMode == FireMode.Real)
        {
            fireTorpedo.GetComponent<Image>().color = Color.red;
            firingLine.GetComponent<Image>().color = Color.red;
        }
        else
        {
            fireTorpedo.GetComponent<Image>().color = Color.gray;
            firingLine.GetComponent<Image>().color = Color.gray;
        }
    }

    /// <summary>
    /// 발사 중인 어뢰 UI 업데이트
    /// </summary>
    public void UpdateFiringTorpedoUI(Vector2 targetPos)
    {
        // UI 좌표 구하기
        Vector2 targetUIPos = _radarController.GetUIPos(targetPos);

        // 수치 계산
        Vector2 dir = (targetUIPos - Vector2.zero).normalized; // 방향 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // 방향에 맞춘 회전 각도 계산

        // 적용
        fireTorpedo.anchoredPosition = targetUIPos;
        fireTorpedo.localRotation = Quaternion.Euler(0, 0, angle);

        DrawLineUI(firingLine, targetUIPos); // 선 위치 업데이트 - 선은 현재 어뢰 위치에 맞춰 그어짐
    }

    /// <summary>
    /// 페이드 인/아웃
    /// </summary>
    /// <param name="isFadeIn">페이드 인 여부</param>
    /// <param name="target">타겟</param>
    /// <param name="duration">페이드 시간</param>
    public IEnumerator FadeInOut(bool isFadeIn, RadarTarget target, float duration)
    {
        // 페이드 인 여부에 따른 시작, 끝 수치 설정
        float start;
        float end;
        if (isFadeIn) // 페이드 인 -> (0 -> 1), 타겟 활성화
        {
            start = 0f;
            end = 1f;
            target.dotRectTransform.gameObject.SetActive(true); // 타겟 활성화
        }
        else // 페이드 아웃 -> (1 -> 0)
        {
            start = 1f;
            end = 0f;
        }

        Vector2 retreatDir = target.dotRectTransform.anchoredPosition.normalized; // 후퇴 방향
        float retreatSpeed = 15f;

        // 불투명도 변경
        float t = 0f;
        while (t < 1f)
        {
            // 정지 중일 때 -> 아무것도 안 함
            if (SubmarineInGameManager.instance.IsPausing)
                yield return null;

            t += Time.deltaTime / duration;

            // 불투명도 변경
            float alpha = Mathf.Clamp01(Mathf.Lerp(start, end, t));
            Color c = target.dotImg.color;
            c.a = alpha;
            target.dotImg.color = c;

            // 크기 변경
            float scale = Mathf.Lerp(0.3f, 1f, alpha);
            target.dotRectTransform.localScale = Vector3.one * scale;

            // 페이드 아웃일 때만 -> 위치 변경(물러남)
            if (!isFadeIn)
            {
                // 위치 변경(물러남)
                target.dotRectTransform.anchoredPosition += retreatDir * retreatSpeed * Time.deltaTime;
            }

            yield return null;
        }

        // 페이드 아웃 -> 타겟 비활성화
        if (!isFadeIn)
            target.dotRectTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// 폭발 보여주기
    /// </summary>
    /// <param name="explosionPos">폭발 위치</param>
    public void ShowExplosion(Vector2 explosionPos, FireMode fireMode, bool isExplosionWithSomething)
    {
        // UI 좌표 구하기
        Vector2 explosionUIPos = _radarController.GetUIPos(explosionPos);

        _showExplosionCoroutine = StartCoroutine(ShowExplosionCoroutine(explosionUIPos, fireMode, isExplosionWithSomething));
    }

    private IEnumerator ShowExplosionCoroutine(Vector2 explosionUIPos, FireMode fireMode, bool isExplosionWithSomething)
    {
        Color color;

        if (isExplosionWithSomething)
        {
            if (fireMode == FireMode.Real)
                color = Color.red;
            else
                color = Color.yellow;
        }
        else
        {
            color = Color.gray;
        }
        color.a = 50f / 255f;

        realExplosionRange.gameObject.SetActive(true);
        realExplosionRange.GetComponent<Image>().color = color;
        realExplosionRange.anchoredPosition = explosionUIPos;

        yield return new WaitForSeconds(2f);

        realExplosionRange.gameObject.SetActive(false);

        _showExplosionCoroutine = null;
    }

    public void ResetUIAfterFire()
    {
        fireTorpedo.gameObject.SetActive(false); // 어뢰 끄기
        firingLine.gameObject.SetActive(false); // 선 끄기
    }

    public void ResetExplosionRange()
    {
        if (_showExplosionCoroutine != null)
        {
            StopCoroutine(_showExplosionCoroutine);
            realExplosionRange.gameObject.SetActive(false);
            _showExplosionCoroutine = null;
        }
    }
    #endregion
}
