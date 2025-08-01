using UnityEngine;
using System.Collections;

public class TreeSpawner : MonoBehaviour
{
    public GameObject[] treePrefabs; // Array of different tree prefabs
    public float minSpawnRate = 1f;
    public float maxSpawnRate = 3f;
    public float minHeight = -2f;
    public float maxHeight = 5f;
    public float spawnXPosition = 10f;
    public float moveSpeed = 2f;
    public float minHorizontalSpace = 3f; // Minimum space between trees
    public float maxHorizontalSpace = 8f; // Maximum space between trees

    void Start()
    {
        StartCoroutine(SpawnTrees());
    }

    IEnumerator SpawnTrees()
    {
        while (true)
        {
            // Randomly select a tree prefab
            GameObject selectedPrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            
            // Random height for the tree
            float height = Random.Range(minHeight, maxHeight);
            Vector3 spawnPosition = new Vector3(spawnXPosition, height, 0);
            
            // Instantiate the tree
            GameObject newTree = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            
            // Make the tree move toward the player
            TreeMovement treeMovement = newTree.AddComponent<TreeMovement>();
            treeMovement.speed = moveSpeed;
            
            // Wait a variable time before spawning next tree
            float waitTime = Random.Range(minSpawnRate, maxSpawnRate);
            yield return new WaitForSeconds(waitTime);
        }
    }
}