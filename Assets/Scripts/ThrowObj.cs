using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObj : MonoBehaviour
{
    public float damage = 0f;
    [SerializeField] private GameObject parent;

    // 충돌
    private void OnCollisionEnter(Collision collision)
    {
        if (parent != null && collision.gameObject == parent)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("플레이어가 맞음!");
            collision.gameObject.GetComponent<PlayerStat>().Damage(damage);
            collision.transform.LookAt(parent.transform);
        }
        else
        {
            Debug.Log("다른 곳에 맞음");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        gameObject.SetActive(false); // 투사체 비활성화
    }
}
