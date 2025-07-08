using UnityEngine;
using System.Collections;

public class FireSpawner : MonoBehaviour
{
    public GameObject firePrefab;
    public float spawnInterval = 5f;

    void Start()
    {
        StartCoroutine(SpawnFireRoutine());
    }

    IEnumerator SpawnFireRoutine()
    {
        while (true)
        {
            SpawnFire();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnFire()
    {
        SoundManager.Instance.PlayFireTrap();
        Instantiate(firePrefab, transform.position, Quaternion.identity);
    }
}
