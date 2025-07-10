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
            SoundManager.Instance.PlayFireTrap();
            SpawnFire();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnFire()
    {
        Instantiate(firePrefab, transform.position, Quaternion.identity);
    }
}
