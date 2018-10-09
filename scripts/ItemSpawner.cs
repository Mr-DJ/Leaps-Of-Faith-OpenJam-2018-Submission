using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/* The ItemSpawner script's purpose is to spawn a random item (collectable or NPC) from a set specified in the Inspector tab 
 * present in Unity's workspace. It is dreadfully syntax ugly and violates some general conventions and ethics.
 */
public class ItemSpawner : MonoBehaviour
{
    public Spawnable[] spawnables;
    public GameObject platformToSpawnOn;
  
    // This is to prevent the item from spawning within the platform. Therefore, the height offset is always positive...
    private float heightOffset;
    
	// Use this for initialization
	void Start () {
        heightOffset = platformToSpawnOn.GetComponent<BoxCollider2D>().size.y * 2;

        /* The spawnDeterminant is just a random integer value from 1 to 1000. After the probability ranges for all the items 
         * has been determined, the item with the spawnDeterminant falling within its range will be spawned.
         * In example, let's say:
         * a. Eggs : 700
         * b. None : 200
         * c. Doggo : 100 (Note that the frequency rates of all items must add upto 1000)
         * Therefore the ranges will be:
         * a. Eggs 0 to 700 (700)
         * b. None : 700 to 900 (200)
         * c. Doggo : 900 to 1000 (100)
         * So let's assume the spawnDeterminant was randomly assigned to 527. In that case, 527 falls within the 0 to 700 range, 
         * therefore an Egg is spawned.
         */
        int spawnDeterminant = UnityEngine.Random.Range((int) 1, 1000);

        for (int i = 0, currentTotal = 0; i < spawnables.Length; i++)
        {
            // Here the ranges of the item are determined...
            spawnables[i].minInRange = (float) currentTotal;
            spawnables[i].maxInRange = (float) (currentTotal + spawnables[i].spawnFrequency);

            currentTotal += (int) spawnables[i].spawnFrequency;
            
            // If the spawnDeterminant falls within the range of the item, the item is spawned. 
            if (spawnDeterminant > spawnables[i].minInRange && spawnDeterminant <= spawnables[i].maxInRange)
            {
                if (spawnables[i].spawnable != null) 
                    Instantiate(spawnables[i].spawnable, 
                                new Vector3(transform.position.x, transform.position.y + heightOffset, transform.position.z),
                                Quaternion.identity);
            }
        }


	}
}

[Serializable]
public class Spawnable
{
    public GameObject spawnable;
    public float spawnFrequency;

    public float minInRange
    {
        get; set;
    }
    public float maxInRange { get; set; }
}
