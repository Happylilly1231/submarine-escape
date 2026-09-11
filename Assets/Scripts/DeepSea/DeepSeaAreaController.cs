using UnityEngine;
using UnityEngine.Rendering;

public enum SeaZone { DeepSea, ShallowSea, Surface }

public class DeepSeaAreaController : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private AudioReverbFilter audioReverbFilter;
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    private AudioSource _audioSource;

    [Header("=== 1. Boundary Settings (XZ Circle) ===")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero; // 원형 맵의 중심점
    [SerializeField] private float maxRadius = 250f;            // 플레이 가능 최대 반경

    [Header("=== 2. Depth Y Level Settings ===")]
    [SerializeField] private float shallowSeaY = 500f; // 이 Y값 이상이면 얕은 바다 Volume 적용
    [SerializeField] private float surfaceY = 650f;    // 수면 Y값 (더 이상 위로 못 올라감)
    [SerializeField] private float bottomY = -50f;    // 이 아래로 못 내려감
    public float MaxDepthY { get; private set; } = 700f;

    [Header("=== 3. Post Process Volumes ===")]
    [SerializeField] private Volume deepSeaVolume;    // 심해 볼륨 (흑백/어두움)
    [SerializeField] private Volume shallowSeaVolume; // 얕은 바다 볼륨 (푸른빛)
    [SerializeField] private Volume surfaceVolume;    // 수면 밖/공기 볼륨

    [Header("=== 4. Audio Reverb Presets ===")]
    // 심해: Underwater/Cave 느낌 (울림 심함)
    [SerializeField] private AudioReverbPreset deepSeaReverb = AudioReverbPreset.Underwater;
    // 얕은 바다: 적당한 울림
    [SerializeField] private AudioReverbPreset shallowSeaReverb = AudioReverbPreset.Room;
    // 수면 밖: 울림 없음
    [SerializeField] private AudioReverbPreset surfaceReverb = AudioReverbPreset.Off;

    [Header("=== Particle Systems ===")]
    [SerializeField] private ParticleSystem floatingParticles; // 메인 카메라 하위 부유물 파티클

    public float SurfaceY => surfaceY;
    public float BottomY => bottomY;

    private SeaZone _currentZone = SeaZone.DeepSea;
    public SeaZone CurrentZone => _currentZone;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (audioReverbFilter == null && Camera.main != null)
        {
            audioReverbFilter = Camera.main.GetComponent<AudioReverbFilter>();
            if (audioReverbFilter == null)
            {
                audioReverbFilter = Camera.main.gameObject.AddComponent<AudioReverbFilter>();
            }
        }

        // 게임 시작 시 현재 위치 기반으로 즉시 환경 설정 적용
        UpdateZoneState(true);
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        ClampPlayerPosition();
        CheckDepthAndSwitchEnvironment();
    }

    private void OnDisable()
    {
        _audioSource.Stop();
    }

    // 초기화 및 실시간 영역 체크용 메소드
    private void UpdateZoneState(bool forceUpdate = false)
    {
        if (playerTransform == null) return;

        float playerY = playerTransform.position.y;
        SeaZone newZone;

        if (playerY >= surfaceY)
        {
            newZone = SeaZone.Surface;
        }
        else if (playerY >= shallowSeaY)
        {
            newZone = SeaZone.ShallowSea;
        }
        else
        {
            newZone = SeaZone.DeepSea;
        }

        if (forceUpdate || newZone != _currentZone)
        {
            _currentZone = newZone;
            ApplyZoneEnvironment(_currentZone);
        }
    }

    // 1. 원형 경계 제한 및 수면 위 상승 제한
    private void ClampPlayerPosition()
    {
        Vector3 pos = playerTransform.position;

        // XZ 원형 범위 제한
        Vector2 currentXZ = new Vector2(pos.x - mapCenter.x, pos.z - mapCenter.z);
        if (currentXZ.sqrMagnitude > maxRadius * maxRadius)
        {
            currentXZ = currentXZ.normalized * maxRadius;
            pos.x = mapCenter.x + currentXZ.x;
            pos.z = mapCenter.z + currentXZ.y;
        }

        // Y축 수면 및 바닥 제한 (-50 ~ 650 범위 고정)
        pos.y = Mathf.Clamp(pos.y, bottomY, surfaceY);

        playerTransform.position = pos;
    }

    // 2. 프레임별 수심 체크
    private void CheckDepthAndSwitchEnvironment()
    {
        UpdateZoneState(false);
    }

    private void ApplyZoneEnvironment(SeaZone zone)
    {
        // Volume Weight 조절 (1: 활성, 0: 비활성)
        if (deepSeaVolume != null) deepSeaVolume.weight = (zone == SeaZone.DeepSea) ? 1f : 0f;
        if (shallowSeaVolume != null) shallowSeaVolume.weight = (zone == SeaZone.ShallowSea) ? 1f : 0f;
        if (surfaceVolume != null) surfaceVolume.weight = (zone == SeaZone.Surface) ? 1f : 0f;

        // 사운드 리버브 설정
        if (audioReverbFilter != null)
        {
            switch (zone)
            {
                case SeaZone.DeepSea:
                    audioReverbFilter.reverbPreset = deepSeaReverb;
                    break;
                case SeaZone.ShallowSea:
                    audioReverbFilter.reverbPreset = shallowSeaReverb;
                    break;
                case SeaZone.Surface:
                    audioReverbFilter.reverbPreset = surfaceReverb;
                    break;
            }
        }

        // ApplyZoneEnvironment 내부
        if (lowPassFilter != null)
        {
            switch (zone)
            {
                case SeaZone.DeepSea:
                    lowPassFilter.enabled = true;
                    lowPassFilter.cutoffFrequency = 1000f; // 먹먹함 강함
                    break;
                case SeaZone.ShallowSea:
                    lowPassFilter.enabled = true;
                    lowPassFilter.cutoffFrequency = 3500f; // 약간 먹먹함
                    break;
                case SeaZone.Surface:
                    lowPassFilter.enabled = false; // 선명한 원음
                    break;
            }
        }

        // 수면 도달 시 부유물 파티클 비활성화
        if (floatingParticles != null)
        {
            if (zone == SeaZone.Surface)
            {
                floatingParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                // 다시 바다 속으로 들어오면 재생
                var emission = floatingParticles.emission;
                emission.enabled = true;

                if (!floatingParticles.isPlaying)
                    floatingParticles.Play();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터 씬 뷰에서 원형 영역 및 Y 높이 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(mapCenter, maxRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(mapCenter + new Vector3(-maxRadius, shallowSeaY, 0), mapCenter + new Vector3(maxRadius, shallowSeaY, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(mapCenter + new Vector3(-maxRadius, surfaceY, 0), mapCenter + new Vector3(maxRadius, surfaceY, 0));

        // 바닥선 (검은색/파란색)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(mapCenter + new Vector3(-maxRadius, bottomY, 0), mapCenter + new Vector3(maxRadius, bottomY, 0));
    }
}