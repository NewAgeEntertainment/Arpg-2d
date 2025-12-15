using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BanditsSpawn : MonoBehaviour
{
    [Header("Bandits prefab")]
    [SerializeField] private GameObject prefab;
    [Header("Maximum bandits, who can spawn at once")]
    [SerializeField] [Min(0)] private int maxBandits;
    [Header("Timer before bandits spawn")]
    [SerializeField] private float timeBeforeSpawn;

    List<GameObject> spawnedBandits = new List<GameObject>();

    private float tempTime = 0;

    private void Start()
    {
        tempTime = timeBeforeSpawn;
    }

    private void OnDisable()
    {
        foreach (GameObject GO in spawnedBandits)
        {
            if (GO != null)
                GO.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        tempTime -= Time.deltaTime;
        if (tempTime <= 0)
        {
            SpawnBandits();
        }
    }

    private void SpawnBandits()
    {
        for (int i = 0; i < maxBandits; i++)
        {
            Vector2 screenPosition = Camera.main.ScreenToWorldPoint(new Vector3(Random.Range(0, Screen.width), Random.Range(0, Screen.height), Camera.main.farClipPlane / 2));
            GameObject bandit = Instantiate(prefab, screenPosition, Quaternion.identity);
            spawnedBandits.Add(bandit);
        }

        tempTime = timeBeforeSpawn;
    }
}
