using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class ZombieWaveSystem : MonoBehaviour
{
    public GameObject[] zombiePrefabs;
    public Transform[] spawnPoints;

    public TextMeshProUGUI waveText; 

    public float timeBetweenWaves = 10f;

    [SerializeField] private float waveTimer = 0f;

    private int waveNumber = 1;
    public int zombiesPerWave = 1;


    void Update()
    {
        /*if(waveNumber == 10){
            waveText.text = "LAST!";
            enabled = false;
            return;

        }*/
        waveTimer += Time.deltaTime;

        int intValue = Mathf.RoundToInt(waveTimer);

        if(waveTimer >= timeBetweenWaves)
        {
            Debug.Log("PasaRonda");
            StartNewWave();
        }

    }
    void StartNewWave()
    {
        waveTimer = 0f;

        zombiesPerWave += 1;

        float minDistance = 4f;

        for(int i =0; i < zombiesPerWave; i++)
        {
            int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomSpawnIndex];

            GameObject randomZombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];

            Vector3 spawnPosition = spawnPoint.position + Random.insideUnitSphere * minDistance;

            spawnPosition.y = spawnPoint.position.y;

            Instantiate(randomZombiePrefab, spawnPosition, spawnPoint.rotation);
        }

        waveNumber++;
        UpdateWaveText();
        Debug.Log($"Ola {waveNumber} iniciada con {zombiesPerWave} zombis.");

    }
    void UpdateWaveText()
    {
        waveText.text = $"{waveNumber}";
    }


}