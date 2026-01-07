using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KeyPadScrew : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Vector3 screwdriverPos;
    [SerializeField] private float rotateAmount = 720f; // 나사 회전 각도
    [SerializeField] private float moveAmount = 0.05f; // 나사 이동 거리
    [SerializeField] private float rotateDuration = 1f; // 회전 애니메이션 지속 시간
    [Header("Outline Settings")]
    [SerializeField] private MeshRenderer outlineRenderer;
    private bool _isRemoved = false;
    public bool IsRemoved => _isRemoved;

    public void Select()
    {
        if (_isRemoved) return;

        if (outlineRenderer != null) outlineRenderer.enabled = true;
    }

    public void DeSelect()
    {
        if (outlineRenderer != null) outlineRenderer.enabled = false;
    }

    public void RemoveWithTool(GameObject screwdriver, System.Action onComplete)
    {
        if (_isRemoved) return;
        _isRemoved = true;

        DeSelect();

        Transform originalParent = screwdriver.transform.parent;
        screwdriver.transform.SetParent(null);
        screwdriver.transform.DORotate(new Vector3(0, -90, 0), 0.1f);

        screwdriver.transform.DOMove(screwdriverPos, 1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            screwdriver.transform.SetParent(this.transform);
            Vector3 rotationAxis = transform.right;
            transform.DOLocalRotate(rotationAxis * rotateAmount, rotateDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad);
            transform.DOMove(transform.position - transform.forward * moveAmount, rotateDuration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                screwdriver.transform.SetParent(originalParent);
                ApplyPhysics();
                onComplete?.Invoke();
            });
        });
    }

    private void ApplyPhysics()
    {
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.1f;
        rb.drag = 5.0f;
    }
}
