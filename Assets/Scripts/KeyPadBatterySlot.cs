using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KeyPadBatterySlot : MonoBehaviour
{
    [SerializeField] private GameObject battery;
    [SerializeField] private Outline outline;

    [Header("Puzzle Settings")]
    [SerializeField] private Item correctBattery; // 해당 슬롯의 정답 배터리
    [SerializeField] private MeshRenderer ledRenderer; // 해당 슬롯의 LED 렌더러
    [SerializeField] private Material greenMaterial; // sign green
    [SerializeField] private Material redMaterial;   // sign red
    [SerializeField] private GameObject sparkParticle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sparkSound;
    [SerializeField] private AudioClip ejectExplosionSound;

    private bool _isBurnt = true;
    public bool IsBurnt => _isBurnt;
    private Item _currentBatteryData = null;
    public Item CurrentBatteryData => _currentBatteryData;
    public bool IsCorret => _currentBatteryData == correctBattery;
    private Tween _ledBlinkTween;

    public void Select()
    {
        if (outline != null) outline.enabled = true;
    }

    public void DeSelect()
    {
        if (outline != null) outline.enabled = false;
    }

    public void RemoveBattery(bool spawnPhysics, System.Action onComplete = null)
    {
        if (battery == null) return;

        GameObject oldBattery = battery;
        battery = null;
        outline.enabled = false;

        oldBattery.transform.DOMove(transform.position - transform.forward * 0.1f, 1f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            if (spawnPhysics)
            {
                oldBattery.transform.SetParent(null);
                outline.enabled = false;
                Rigidbody rb = oldBattery.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.drag = 5.0f;
                Destroy(oldBattery, 3f);
            }
            else
            {
                Destroy(oldBattery);
            }
            onComplete?.Invoke();
        });
    }

    public void InstallBattery(Item batteryItem, System.Action onComplete = null)
    {
        _isBurnt = false;
        _currentBatteryData = batteryItem;

        Vector3 startLocalPos = new Vector3(0, 0, -3f);
        Vector3 startWorldPos = transform.TransformPoint(startLocalPos);

        battery = Instantiate(batteryItem.ItemPrefab, startWorldPos, transform.rotation);

        battery.transform.SetParent(this.transform);
        battery.transform.localScale = Vector3.one;

        if (battery.TryGetComponent(out Collider col)) col.enabled = false;

        Vector3 targetPos = Vector3.zero;
        battery.transform.DOLocalMove(targetPos, 0.8f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            UpdateLED();
            onComplete?.Invoke();
        });
    }

    private void UpdateLED()
    {
        if (ledRenderer == null) return;

        _ledBlinkTween?.Kill();

        if (IsCorret) ledRenderer.material = greenMaterial;
        else
        {
            ledRenderer.material = redMaterial;
            sparkParticle.SetActive(true);
            PlaySparkSound();
            StartBlinking();
        }
    }

    private void StartBlinking()
    {
        _ledBlinkTween = DOTween.Sequence()
        .AppendCallback(() => ledRenderer.material.EnableKeyword("_EMISSION"))  // 켜기
        .AppendInterval(0.2f)                                                  // 유지
        .AppendCallback(() => ledRenderer.material.DisableKeyword("_EMISSION")) // 끄기
        .AppendInterval(0.2f)                                                  // 유지
        .SetLoops(-1);
    }

    private void PlaySparkSound()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.clip = sparkSound;
            audioSource.Play();
        }
    }

    private void PlayExplosionSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.clip = ejectExplosionSound;
            audioSource.Play();
        }
    }

    public void StopAllEffects()
    {
        _ledBlinkTween?.Kill();
        if (ledRenderer != null) ledRenderer.material = redMaterial;
        sparkParticle.SetActive(false);
        PlayExplosionSound();
    }

    public void EjectBattery()
    {
        if (battery == null) return;

        _currentBatteryData = null;
        outline.enabled = false;

        GameObject ejectingBattery = battery;
        battery = null;

        ejectingBattery.transform.SetParent(null);
        ejectingBattery.transform.DOMove(transform.position - transform.forward * 0.05f, 0.1f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            if (ejectingBattery.TryGetComponent(out Collider col)) col.enabled = true;
            Rigidbody rb = ejectingBattery.AddComponent<Rigidbody>();

            Vector3 ejectDirection = -transform.forward + Random.insideUnitSphere * 0.5f;
            rb.AddForce(ejectDirection * 2f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

            if (_isBurnt)
            {
                Destroy(ejectingBattery, 8f);
                _isBurnt = false;
            }
        });
    }
}
