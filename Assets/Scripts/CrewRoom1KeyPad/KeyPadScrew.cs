using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KeyPadScrew : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Vector3 screwdriverPos;
    [Header("Outline Settings")]
    [SerializeField] private MeshRenderer outlineRenderer;

    private float rotateAmount = 720f; // 나사 회전 각도
    private float moveAmount = 0.1f; // 나사 이동 거리
    private float rotateDuration = 1f; // 회전 애니메이션 지속 시간
    private bool _isRemoved = false;
    public bool IsRemoved => _isRemoved;

    /// <summary>
    /// 나사 선택 - 아웃라인 활성화
    /// </summary>
    public void Select()
    {
        if (_isRemoved) return;

        if (outlineRenderer != null) outlineRenderer.enabled = true;
    }

    /// <summary>
    /// 나사 선택 취소 - 아웃라인 비활성화
    /// </summary>
    public void DeSelect()
    {
        if (outlineRenderer != null) outlineRenderer.enabled = false;
    }

    /// <summary>
    /// 드라이버를 이용해 나사 없앰
    /// </summary>
    /// <param name="screwdriver"></param>
    /// <param name="onComplete"></param>
    public void RemoveWithTool(GameObject screwdriver, System.Action onComplete)
    {
        if (_isRemoved) return;
        _isRemoved = true;

        DeSelect();

        // 시작 시점의 부모 저장 (나중에 복구용)
        Transform originalParent = screwdriver != null ? screwdriver.transform.parent : null;

        if (screwdriver != null)
        {
            screwdriver.transform.SetParent(null);
            screwdriver.transform.DORotate(new Vector3(0, -90, 0), 0.1f).SetLink(gameObject);
        }

        // 1단계: 드라이버 이동
        var moveTween = screwdriver != null
            ? screwdriver.transform.DOMove(screwdriverPos, 1f)
            : DOVirtual.DelayedCall(1f, () => { }); // 드라이버 없으면 1초 대기만 함

        moveTween.SetEase(Ease.OutQuad).SetLink(gameObject).OnComplete(() =>
        {
            // 나사가 파괴되었으면 중단
            if (this == null) return;

            // 드라이버가 아직 살아있다면 자식으로 설정
            if (screwdriver != null) screwdriver.transform.SetParent(this.transform);

            // 2단계: 나사 회전
            transform.DOLocalRotate(transform.right * rotateAmount, rotateDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject);

            // 3단계: 나사 이동
            transform.DOMove(transform.position - transform.forward * moveAmount, rotateDuration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    // 나사가 끝까지 왔을 때 드라이버 상태 확인 후 복구
                    if (screwdriver != null && originalParent != null)
                    {
                        screwdriver.transform.SetParent(originalParent);
                    }

                    // 나사 물리 적용
                    ApplyPhysics();
                    onComplete?.Invoke();
                });
        });
    }

    private void ApplyPhysics()
    {
        // true를 넣어 트윈을 즉시 완료 상태로 보내고 OnComplete를 실행
        transform.DOKill(true);

        if (TryGetComponent(out Collider col))
        {
            col.enabled = false;
        }

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.mass = 0.1f;
        rb.drag = 5.0f;

        Destroy(gameObject, 2f);
    }
}
