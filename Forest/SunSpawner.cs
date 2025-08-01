using UnityEngine;
using System.Collections;

public class SunSpawner : MonoBehaviour
{
    public GameObject[] treePrefabs; // Array of different tree prefabs
    public float minSpawnRate = 1f;
    public float maxSpawnRate = 3f;
    public float minHeight = -2f;
    public float maxHeight = 5f;
    public float spawnXPosition = 10f;
    public float moveSpeed = 2f;
    public float minHorizontalSpace = 3f;
    public float maxHorizontalSpace = 8f;
    
    [Header("Fade Settings")]
    public float minLifetime = 2f;      // Shortest time before fade starts
    public float maxLifetime = 5f;      // Longest time before fade starts
    public float fadeDuration = 1f;     // How long the fade lasts

    void Start()
    {
        StartCoroutine(SpawnTrees());
    }

    IEnumerator SpawnTrees()
    {
        while (true)
        {
            GameObject selectedPrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            float height = Random.Range(minHeight, maxHeight);
            Vector3 spawnPosition = new Vector3(spawnXPosition, height, 0);
            
            GameObject newTree = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            
            // Add movement component
            TreeMovement treeMovement = newTree.AddComponent<TreeMovement>();
            treeMovement.speed = moveSpeed;
            
            // Add fade-out self-destruct
            SelfDestruct destruct = newTree.AddComponent<SelfDestruct>();
            destruct.lifetime = Random.Range(minLifetime, maxLifetime);
            destruct.fadeDuration = fadeDuration;
            
            yield return new WaitForSeconds(Random.Range(minSpawnRate, maxSpawnRate));
        }
    }
}