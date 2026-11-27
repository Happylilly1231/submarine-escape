using System.Collections;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class RescueAgentMove : MonoBehaviour
{
    [Header("Target & Movement")]
    [SerializeField] private Transform rescueOperative;
    [SerializeField] private float moveDistance = 5.0f;
    [SerializeField] private float moveDuration = 4.0f;
    [SerializeField] private bool useLocalForward = true;

    [Header("Mutation")]
    [SerializeField] private GameObject[] spikes;
    [SerializeField] private AudioSource rourSound; // 포효 소리
    [SerializeField] private Animator agentAnimator;
    [SerializeField] private string agonyBoolName = "isAgony";

    private void Awake()
    {
        if (rescueOperative == null)
            rescueOperative = transform;

        if (agentAnimator == null)
            agentAnimator = rescueOperative.GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        StartCoroutine(MoveForwardRoutine());
    }

    private IEnumerator MoveForwardRoutine()
    {
        Vector3 startPos = rescueOperative.position;
        Vector3 moveDir = useLocalForward ? rescueOperative.forward.normalized : Vector3.forward;
        Vector3 endPos = startPos + moveDir * moveDistance;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            rescueOperative.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        rescueOperative.position = endPos;
    }

    private void SetBackSpikesActive(bool active)
    {
        if (spikes == null) return;

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
            {
                spikes[i].SetActive(active);
            }
        }
    }

    public void SetAgonyState(bool isAgony)
    {
        if (agentAnimator != null && !string.IsNullOrEmpty(agonyBoolName))
        {
            agentAnimator.SetBool(agonyBoolName, isAgony);
        }
    }

    public IEnumerator ShowBackSpikesRoutine()
    {
        if (spikes == null || spikes.Length == 0) yield break;

        SetAgonyState(true);

        Vector3[] targetScales = new Vector3[spikes.Length];

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] == null) continue;

            targetScales[i] = spikes[i].transform.localScale;
            spikes[i].transform.localScale = Vector3.zero;
            spikes[i].SetActive(true);
        }

        float elapsed = 0f;
        float spikePopDuration = 0.15f;

        while (elapsed < spikePopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spikePopDuration);
            // 살짝 튀어 오르듯 커지는 이징
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < spikes.Length; i++)
            {
                if (spikes[i] != null)
                {
                    spikes[i].transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScales[i], eased);
                }
            }

            yield return null;
        }

        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] != null)
            {
                spikes[i].transform.localScale = targetScales[i];
            }
        }

        rourSound.Play();
    }
}
