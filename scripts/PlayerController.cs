using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public GameObject player;
    public LayerMask groundLayer;
    public GameObject weaponProjectile;
    public Vector2 heightConstraints;

    public int maxJumps;
    public float moveSpeed;
    public float jumpForce;
    public float velocityDecayRate;
    public float maxSpeed;
    public bool isArmed;
    public bool isUsingThrustMechanic;

    private Rigidbody2D playerRigidbody;
    private CapsuleCollider2D playerCollider;
    private SpriteRenderer playerSpriteRenderer;
    private Animator animator;

    private int jumpsCompleted;
    private char lastKeyHeld;
    private bool grounded;
    private float adjustableMaxSpeed;
    private float adjustableVDecayRate;
    private float nonThrustMoveSpeed;
    private float height;
    private float stepSoundPlaybackRate;
    private float nextStepPlaybackTime = 0f;

	// Use this for initialization
	void Start () {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        lastKeyHeld = 'J';
        adjustableMaxSpeed = maxSpeed;
        adjustableVDecayRate = velocityDecayRate;
        nonThrustMoveSpeed = moveSpeed * 2;
        height = heightConstraints.x - heightConstraints.y;
        stepSoundPlaybackRate = 0.2f;

        jumpsCompleted = 0;
    }

    void Update()
    {
        // isArmed is meant to be a variable that can be easily toggled using a specific key as stimulus
        isArmed = (Input.GetKeyDown(KeyCode.F)) ? ((isArmed) ? false : true) : isArmed;

        if (isArmed && Input.GetKeyDown(KeyCode.Space))
        {
            /* Here, for simplicity's sake we could've just updated the hasFired parameter and invoked the Fire() function... however, the flaw is that the fire animation WILL NOT play unless the player
             * has come to a stop due to missing transitions from the playerMovement animations. As a result, the actualy "firing" will take place when the animation hasn't even begun playing yet 
             * causing unwanted inconsistency. Also, forcing the fire animation to play while the player is moving is also undesirable as a solution because the player will appear to skid on the
             * ground...
             * The approach I've taken is to faster lessen the current velocity of the player by incrementing it's decay rate. The longer it takes to stop, the higher the decay rate will be. The 
             * FixedUpdate() function on the other hand below will use this updated decay rate and slow down the player. Even if the player tries to move while the game attempts to stop them, the 
             * decay rate will outpace the moveSpeed of the player.
             * Once the player comes to a standstill, the animator's parameter is updated and the Fire() function is invoked. The velocity decay rate is also reset to its original value i.e.
             * velocityDecayRate
             */
            if (Mathf.Abs(playerRigidbody.velocity.x) > 0) adjustableVDecayRate += 0.12f;
            else
            {
                // Sadly enough, we somehow forgot to implement a cooldown mechanic for firing. The player can shoot at any rate he desries
                animator.SetBool("hasFired", true);
                Fire();

                adjustableVDecayRate = velocityDecayRate;
            }

        } else animator.SetBool("hasFired", false);

        // The adjustableMaxSpeed is used to halve the prescribed maxSpeed whenever the player "is armed" i.e. holds a weapon.
        adjustableMaxSpeed = (isArmed) ? maxSpeed / 3 : maxSpeed;

        // To determine the direction in which the fired projectile will travel by manipulating the velocity filed
        weaponProjectile.GetComponent<Bullet>().velocity *= (playerSpriteRenderer.flipX) ? (weaponProjectile.GetComponent<Bullet>().velocity < 0) ? 1 : -1 
                                                                                         : (weaponProjectile.GetComponent<Bullet>().velocity > 0) ? 1 : -1;

        /* The multiple jump mechanic comes with a great inconsistency. However, fortunately, it comes with a reliable pattern of inconsistency. 
         * Whenever the player takes a jump from the ground itself, the jumpsCompleted variable is incremented once as expected but since it takes a while to lift of from the ground, the grounded
         * variable is immediately set to true after that therefore causing the jumpsCompleted variable to be reset incorrectly. As a result the player always seems to get ONE EXTRA jump instead of the
         * provided maxJumps. A temporary solution I have adopted is to reduce the limit by one to compensate for the extra jump...
         */
        if (grounded && jumpsCompleted > 0) jumpsCompleted = 0;

        if (Input.GetKeyDown(KeyCode.W) && grounded || Input.GetKeyDown(KeyCode.W) && jumpsCompleted < maxJumps - 1)
        {
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, jumpForce);
            jumpsCompleted++;

            AudioManager.audioSource.PlayOneShot(AudioManager.playerJump);
        }

        /* The animator has been configured to play the movement animation when the float value of the speed variable exceeds 0. So it is only natural that if you are moving, your speed will increase 
         * and also consequently trigger the movement animation */
        animator.SetFloat("speed", Mathf.Abs(playerRigidbody.velocity.x));
        animator.SetBool("grounded", grounded);
        animator.SetBool("isArmed", isArmed);
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        grounded = Physics2D.IsTouchingLayers(playerCollider, groundLayer);
        

        if (isUsingThrustMechanic)
        {
            playerSpriteRenderer.flipX = (playerRigidbody.velocity.x >= 0) ? false : true;
            if ((Input.GetKeyUp(KeyCode.K) && lastKeyHeld == 'J') || (Input.GetKeyUp(KeyCode.J) && lastKeyHeld == 'K'))
            {
                if (Input.GetKey(KeyCode.D))
                    moveSpeed *= (moveSpeed > 0) ? 1 : -1;
                else if (Input.GetKey(KeyCode.A))
                    moveSpeed *= (moveSpeed < 0) ? 1 : -1;

                /* The point is that the speed will only be updated when it is below the maxSpeed threshold.  
                 * Let us take an example for clarity's sake, where maxSpeed = 15 and moveSpeed = 4
                 * For every time the A or D key is raised after being pressed, the rigid body's x velocity is stepped up by the moveSpeed variable BUT, this happens only if upon stepping up, the resultant
                 * velocity remains below maxSpeed. If upon adding moveSpeed to velocity, the velocity exceeds maxSpeed, then the velocity is directly set to be the maxSpeed.
                 * Simply put,
                 * Input response 1: velocity.x += moveSpeed (4)
                 *                2: velocity.x += moveSpeed (8)
                 *                3: velocity.x += moveSpeed (12)
                 *                4: velocity.x += moveSpeed BUT it results in 16 which is more than maxSpeed i.e. 15, therefore, velocity is instead just directly assigned to maxSpeed which is 15 
                 */
                playerRigidbody.velocity = new Vector2((Mathf.Abs(playerRigidbody.velocity.x + moveSpeed) <= adjustableMaxSpeed) ? playerRigidbody.velocity.x + moveSpeed
                                                                                                                                 : (moveSpeed > 0) ? adjustableMaxSpeed : adjustableMaxSpeed * -1,
                                                       playerRigidbody.velocity.y);
                lastKeyHeld = (lastKeyHeld == 'J') ? 'K' : 'J';
            }

            /* Decaying x velocity to ZERO
             * You'd be surprised to know that this functions just like the snippet of code above that deals with the movement speed. Here the velocity is stepped down by the velocityDecayRate
             * variable in EVERY frame steadily. If the velocity is at the risk of being stepped down below 0, then instead of stepping down by the value of velocityDecayRate, the velocity is directly 
             * assigned to zero. It is important to note, that negative velocity WILL RESULT IN THE PLAYER MOVING TOWARDS THE LEFT (EASTWARDS direction)
             * The reverse is done if the current velocity is negative i.e in the eastwards direction
             */
            if (playerRigidbody.velocity.x > 0)
                playerRigidbody.velocity = new Vector2((playerRigidbody.velocity.x - adjustableVDecayRate > 0) ? playerRigidbody.velocity.x - adjustableVDecayRate : 0,
                                                   playerRigidbody.velocity.y);
            else
                playerRigidbody.velocity = new Vector2((playerRigidbody.velocity.x + adjustableVDecayRate < 0) ? playerRigidbody.velocity.x + adjustableVDecayRate : 0,
                                                   playerRigidbody.velocity.y);
        } else
        {
            // The player will always face the direction of the last directional key that was pressed i.e. A for left and D for right
            playerSpriteRenderer.flipX = (lastKeyHeld == 'D') ? false : true;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                playerRigidbody.velocity = new Vector2((Input.GetKey(KeyCode.A) ? nonThrustMoveSpeed * -1 : nonThrustMoveSpeed), playerRigidbody.velocity.y);
                lastKeyHeld = (Input.GetKey(KeyCode.A)) ? 'A' : 'D';
            } else playerRigidbody.velocity = new Vector2(0, playerRigidbody.velocity.y);
        }
        
        if (Time.time > nextStepPlaybackTime && grounded && Mathf.Abs(playerRigidbody.velocity.x) > 0)
        {
            int randInt = (int)Random.Range(1, 3);

            // Generic step that is to be played
            AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerStepHard1 : randInt == 2 ? AudioManager.playerStepHard2 : AudioManager.playerStepHard3);
            nextStepPlaybackTime = nextStepPlaybackTime + Time.time;

            // If the player is running at a reasonable fast pace
            if (Mathf.Abs(playerRigidbody.velocity.x) > 12.5)
            {
                // To layer step sounds with the high steps
                randInt = (int)Random.Range(1, 4);
                AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerStepHardFastHigh1
                                                                    : (randInt == 2) ? AudioManager.playerStepHardFastHigh2
                                                                                     : (randInt == 3) ? AudioManager.playerStepHardFastHigh3 : AudioManager.playerStepHardFastHigh4);
                // To layer step sounds with the low steps
                randInt = (int)Random.Range(1, 4);
                AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerStepHardFastLow1
                                                                    : (randInt == 2) ? AudioManager.playerStepHardFastLow2
                                                                                     : (randInt == 3) ? AudioManager.playerStepHardFastLow3 : AudioManager.playerStepHardFastLow4);
            } else
            {
                // To layer step sounds with the high steps
                randInt = (int)Random.Range(1, 4);
                AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerStepHardSlowHigh1
                                                                    : (randInt == 2) ? AudioManager.playerStepHardSlowHigh2
                                                                                     : (randInt == 3) ? AudioManager.playerStepHardSlowHigh3 : AudioManager.playerStepHardSlowHigh4);
                // To layer step sounds with the low steps
                randInt = (int)Random.Range(1, 4);
                AudioManager.audioSource.PlayOneShot((randInt == 1) ? AudioManager.playerStepHardSlowLow1
                                                                    : (randInt == 2) ? AudioManager.playerStepHardSlowLow2
                                                                                     : (randInt == 3) ? AudioManager.playerStepHardSlowLow3 : AudioManager.playerStepHardSlowLow4);
            }

            nextStepPlaybackTime = Time.time + ((Mathf.Abs(playerRigidbody.velocity.x) > 12.5) ? stepSoundPlaybackRate : stepSoundPlaybackRate * 2.25f); 
        }

        if (Input.GetKey(KeyCode.S)) playerRigidbody.velocity = new Vector2(0f, playerRigidbody.velocity.y);
	}

    void Fire()
    {
        /* 1. Must spawn projectile. For this a global variable of type GameObject must be declared to store the projectile to be spawned...
         * 2. Must be given a predetermined force in the direction the player faces, which for now is ONLY THE RIGHT...
         * 3. [UNIMPLEMENTED] Must have a cooldown that is correctly timed with the playerFire animation in the animator controller of the player...
         */

        Instantiate(weaponProjectile,
                    new Vector3(transform.position.x + ((playerSpriteRenderer.flipX) ? -((float) 0.75 * height) : (float) (0.75 * height)),
                                transform.position.y - ((float) 0.1 * height),
                                transform.position.z),
                    Quaternion.identity);
        weaponProjectile.transform.localScale = transform.localScale / 5;
        weaponProjectile.GetComponent<Bullet>().velocity *= (playerSpriteRenderer.flipX) ? -1 : 1f;
    }
}
