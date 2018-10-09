using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/* The primary intention of the EntityHealth script is to provide a universal script for all entities that posses some form of a 
 * health system. The only two major setbacks of this script in terms of its versitality are (1) it's need to define every damage 
 * value for each script individually and (2) the need to sharply differentiate responses between the player and other entities.
 * For example, an ideal universal entity script would just play a damage sound when the player was damaged, however, in this 
 * script, a specific sound is played depending upon which entity possesses this script
 */
public class EntityHealth : MonoBehaviour {
    public float maxHealth;
    public float currentHealth;
    
    /* If the entity possesses UI for displaying its health, additional instructions are carried out in this script for
     * maintaining it 
     */
    public bool healthHasUI;

    // This Vector is only applicable in the case of players as NPCs are rarely respawned at a point after their death...
    public Vector3 spawn;
    
    public Slider healthbar;
    public DamageValues[] damageValues;

    private Animator animator;

    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();

        healthHasUI = (healthbar != null) ? true : false;

        if (healthHasUI)
            healthbar.value = CalculateHealth();
	}
	
	void OnCollisionEnter2D(Collision2D collision)
    {
        for (int i = 0; i < damageValues.Length; i++)
            if (collision.gameObject.tag == damageValues[i].tag)
                DealDamage(damageValues[i].damage);
    }

    public void DealDamage(float damageValue)
    {
        /* This complicated as hell looking ternary operator does a rather simple operation. It just makes sure that in case
         * the damage dealt is negative i.e. a healing item has been collided with, the health healed must not cause the current
         * health to exceed the maximum health. Simple
         */
        currentHealth = (damageValue >= 0) ? currentHealth - damageValue : (currentHealth - damageValue > maxHealth) ? maxHealth : currentHealth - damageValue;

        if (this.gameObject.tag == "Player" && damageValue > 0)
        {
            int randInt = (int)Random.Range(1, 3);
            AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerHit1 
                                                                : (randInt == 2) ? AudioManager.playerHit2 : AudioManager.playerHit3);

            randInt = (int)Random.Range(1, 3);
            AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerHitLayered1
                                                                : (randInt == 2) ? AudioManager.playerHitLayered2 : AudioManager.playerHitLayered3);
        }

        if (healthHasUI) healthbar.value = CalculateHealth();
        if (currentHealth <= 0)
            Die();
    }

    float CalculateHealth()
    {
        return currentHealth / maxHealth;
    }

    void Die()
    {
        /* You may have noticed that we have dangerously assumed that the animator of the entity to which this script is 
         * attached is (1) existent in the first place and (2) has a boolean parameter called "isDead"/ Regardless, even if 
         * these conditions are not met, the object is destroyed nonetheless...
         */
        animator.SetBool("isDead", true);
        Destroy(this.gameObject, 1);

        if (this.gameObject.tag == "Player")
        {
            AudioManager.audioSource.PlayOneShot(AudioManager.playerDeath);
            SceneManager.LoadScene(0);
        }
    }
}

[System.Serializable]
public class DamageValues
{
    public string tag;
    public float damage;
}
