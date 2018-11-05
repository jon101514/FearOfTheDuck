/** Jonathan So, jds7523@rit.edu
 * Manages the player's defense options (parrying and blocking), working in tandem with PlayerMeter.
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerDefense : MonoBehaviour {

	public static PlayerDefense instance; // Singleton design pattern

	public KeyCode defense; // Press the Guard button for parry, hold it for block.
	public AudioClip parry, block, damaged; 
	public AudioClip hit1SFX, hit2SFX, hit3SFX, koSFX, superSFX, koSFX_t;
	public Hitspark hitspark; // Hitspark prefab that will display on the player upon hit
	public Hitspark blockspark; // Blockspark will display on parry and block. This is an instance, not a prefab.

	private List<Hitspark> hitSparkList; // Pool for the hitsparks.
	private const int LIST_SIZE = 6;

	private const float PARRY_WINDOW = 1/15f; // Amount of time the player has to parry attacks.
	private const float FLASH_TIME = 1/6f; // Amount of seconds this object will flash for.
	private const float PARRY_COOL = 5/16f; // Cooldown time from a failed parry. 
	private const float DMG_COOL = 2/3f; // Cooldown time for damage to ensure that players don't lose all of their HP within 2 seconds. Used to be 3/4f.
	private const float DAMAGE_COOLDOWN = 1/4f; // After parrying, the player is invincible for a brief moment.
	private const float XFLASH_TIME = 1/15f; // Amount of seconds this object will flash for.

	public bool blocking = false;
	public bool parrying = false;
	private bool defCheck = false; // Defense Check
	private bool parryOK = true;
	private bool damageable = true;

	private int kbdir = 0; // Knockback direction. Either -1 for left or +1 for right.

	private SpriteRenderer sr;
	private AudioSource audi;
	private Animator anim;
	private Rigidbody2D	rb;
	private Collider2D coll;

	// Get necessary components.
	private void Awake() {
		sr = GetComponent<SpriteRenderer>();
		audi = GetComponent<AudioSource>();
		anim = GetComponent<Animator>();
		rb = GetComponent<Rigidbody2D>();
		coll = GetComponent<Collider2D>();
		// Set up the Singleton design pattern.
		if (instance == null) {
			instance = this;
		}
		blockspark.gameObject.SetActive(false);
		MakeHitSparkList();
	}

	// Creates a list of pooled hitsparks that we can use and reuse.
	private void MakeHitSparkList() {
		hitSparkList = new List<Hitspark>();
		for (int i = 0; i < LIST_SIZE; i++) {
			Hitspark obj = (Hitspark) Instantiate(hitspark, transform.position, Quaternion.identity);
			obj.gameObject.SetActive(false);
			hitSparkList.Add(obj);
		}
	}

	// Finds and activates the first inactive hitspark in our object pool.
	private void ActHitSpark() {
		for (int i = 0; i < LIST_SIZE; i++) {
			Hitspark curr = hitSparkList[i];
			if (!curr.gameObject.activeInHierarchy) {
				curr.gameObject.SetActive(true);
				curr.gameObject.transform.position = transform.position;
				break;
			}
		}
	}

	/** Takes care of detecting input on the Guard button.
	 * If the Guard button is tapped, then parry.
	 * If the Guard button is held down, then block.
	 */
	private void Update() {
		if (Input.GetKeyDown(defense) && defCheck && parryOK && !anim.GetBool("ko") && PlayerMelee.instance.canAtk && !PlayerMovement.instance.walking  && PlayerMovement.instance.canMove) {
			Parry();
		} else if (Input.GetKey(defense) && !anim.GetBool("ko") && PlayerMelee.instance.canAtk && !PlayerMovement.instance.walking && PlayerMovement.instance.canMove) {
			blocking = true;
			anim.SetBool("blocking", true);
		} else {
			blocking = false;
			anim.SetBool("blocking", false);
		}
	}

	/** Parrying grants 4 SP and makes the player flash blue.
	 * Stops the DefenseCheck coroutine (which, if not stopped, doles damage to the player).
	 * Contacts the PlayerMeter singleton object to give 4 SP to the player.
	 */
	void Parry() {
		StopCoroutine("DefenseCheck");
		StartCoroutine("Flash", "blue");
		StartCoroutine("ParryDamage");
		defCheck = false;
		blockspark.gameObject.SetActive(true);
		blockspark.SetColor("red");
		PlayerMeter.instance.SPChange(4); // Add 4 SP per successful parry.
		audi.PlayOneShot(parry);
		CallFailParry();
	}

	// Randomly chooses one of three vocalized hitsounds to play.
	private void ChooseHitSound() {
		int rand = Random.Range(0, 3);
		if (rand == 0) {
			audi.PlayOneShot(hit1SFX);
		} else if (rand == 1) {
			audi.PlayOneShot(hit2SFX);
		} else { // rand == 2
			audi.PlayOneShot(hit3SFX);
		}
	}

	/** Checks to see if the player is currently blocking or attempts to parry an attack.
	 * If the player does nothing, then they take damage. 
	 * If they are blocking, they take reduced damage.
	 * If they successfully parry the incoming attack, then they take no damage.
	 */
	IEnumerator DefenseCheck() {
		PlayerMelee.instance.CancelATK(); // Cancel the melee attack so that the player has a chance of parrying/blocking.
		defCheck = true;
		yield return new WaitForSeconds(PARRY_WINDOW);
		if (blocking && damageable) { // If the player didn't parry, but they are holding down the defense button...
			StartCoroutine("Flash", "red");
			Enemy_SP.instance.SPChange(2);  // Enemy gets 2 SP instead of 4.
			PlayerMeter.instance.HPChange(-1); // Take 1 damage instead of 2.
			StartCoroutine("DamageCooldown");
			rb.velocity = new Vector2((kbdir * 6), 0); // knockback
			audi.PlayOneShot(block);
		} else if (damageable) { // The player will take damage; no defense action.
			StartCoroutine("Flash", "red");
			ActHitSpark();
			if (!anim.GetBool("ko")) { // Make sure not to play any other animations when KO'd
				anim.SetTrigger("hit");
			}
			Enemy_SP.instance.SPChange(4); // Enemy gets 4 SP.
			PlayerMeter.instance.HPChange(-2); // Player takes 2 damage.
			StartCoroutine("DamageCooldown");
			audi.PlayOneShot(damaged);
			ChooseHitSound();
			rb.velocity = new Vector2((kbdir * 6), 6); // knockback
			PlayerMovement.instance.RestrictMove();
		}
		defCheck = false;
	}

	/** Public caller for the FailParry coroutine, which enforces a cooldown on parrying to prevent spamming.
	 */
	public void CallFailParry() {
		StartCoroutine("FailParry");
	}

	/** For a short duration after parrying, the player is invincible.
	 */
	public IEnumerator ParryDamage() {
		damageable = false;
		yield return new WaitForSeconds(DAMAGE_COOLDOWN);
		damageable = true;
	}

	/** Flashes in different colors based on situation.
	 * If hit, this flashes briefly in RED.
	 * If parrying, flashes in BLUE.
	 * 
	 * param[color] - a string expressing the color we want to flash.
	 */
	IEnumerator Flash(string color) {		
		if (color == "blue") {
			sr.color = Color.cyan;
		} else {
			sr.color = Color.red;
		}
		coll.enabled = false;
		yield return new WaitForSeconds(FLASH_TIME / 2);
		coll.enabled = true;
		yield return new WaitForSeconds(FLASH_TIME / 2);
		sr.color = Color.white;
		anim.ResetTrigger("hit");
	}

	/** Momentarily apply damage cooldown for a set number of frames where the player is invincible.
	 * Change collision layers for a brief period of time and flash.
	 */
	IEnumerator IFrames() {
		this.gameObject.layer = 12; // Set layer to IFrames
		StartCyanFlash();
		yield return new WaitForSeconds(DMG_COOL);
		StopCyanFlash();
		this.gameObject.layer = 10;
	}

	public void StartRedFlash() {
		StartCoroutine("RedFlash");
	}

	public void StopRedFlash() {
		StopCoroutine("RedFlash");
		sr.color = Color.white;
	}

	/** Infinitely flashes between red and white.
	 *  This coroutine must be stopped manually.
	 */
	IEnumerator RedFlash() {
		while (true) {
			sr.color = Color.red;
			yield return new WaitForSeconds(FLASH_TIME);
			sr.color = Color.white;
			yield return new WaitForSeconds(FLASH_TIME);
		}
	}

	public void StartCyanFlash() {
		StartCoroutine("CyanFlash");
	}

	public void StopCyanFlash() {
		StopCoroutine("CyanFlash");
		sr.color = Color.white;
	}

	/** Player flashes red (and not cyan) more rapidly than with the RedFlash coroutine.
	 * Function is misnamed.
	 */
	IEnumerator CyanFlash() {
		while (true) {
			sr.color = Color.red;
			yield return new WaitForSeconds(XFLASH_TIME);
			sr.color = Color.white;
			yield return new WaitForSeconds(XFLASH_TIME);
		}
	}

	/** Prevents parrying momentarily.
	 * This is called after a successful shot to prevent the player from mashing shoot + defense at the same time.
	 * In other words, a player may only be either defending or attacking at any given time. They cannot mash buttons.
	 */
	IEnumerator FailParry() {
		parrying = true;

		parryOK = false;
		yield return new WaitForSeconds(PARRY_COOL);
		parryOK = true;

		parrying = false;
	}

	/** Apply a cooldown on taking damage. Lets the player know that they've been KO'd if they have.
	 */
	IEnumerator DamageCooldown() {
		damageable = false;
		if (PlayerMeter.instance.IsKOd()) { // KO'd; do everything relevant to being KO'd.
			anim.SetBool("ko", true);
			PlayerMovement.instance.KOd = true;
			PlayerHitbox.instance.ChangeHitbox("ko");
			audi.PlayOneShot(koSFX);
			audi.PlayOneShot(koSFX_t);
		}
		StartCoroutine("IFrames");
		yield return new WaitForSeconds(DMG_COOL);
		damageable = true;
	}

	// If we make contact with a Enemy_Melee object, start the DefenseCheck and calculate knockback direction.
	void OnCollisionEnter2D(Collision2D coll) {
		if (coll.gameObject.tag.Equals("Enemy_Melee")) {
			StartCoroutine("DefenseCheck");
			SetKnockback(coll.gameObject);
		}
	}

	// If we make contact with a Enemy_Melee object, start the DefenseCheck and calculate knockback direction.
	void OnCollisionStay2D(Collision2D coll) {
		if (coll.gameObject.tag.Equals("Enemy_Melee")) {
			StartCoroutine("DefenseCheck");
			SetKnockback(coll.gameObject);
		}
	}

	/** Determine what to do upon making contact with enemy attacks.
	 * param[coll] - the Enemy_Bullet, Enemy_Melee, or Enemy_Super that we had just made contact with.
	 */
	void OnTriggerEnter2D (Collider2D coll) {
		if (coll.gameObject.tag.Equals("Enemy_Bullet") || coll.gameObject.tag.Equals("Enemy_Melee")) {
			StartCoroutine("DefenseCheck");
			SetKnockback(coll.gameObject);
			// We used to destroy enemy projectiles. Uncomment the following code to re-implement.
			/*
			if (blocking && coll.gameObject.tag != "Enemy_Melee") {
				// coll.gameObject.SetActive(false); // "Destroy" the enemy projectile.
				// coll.gameObject.GetComponent<Collider2D>().enabled = false;
			}
			*/
		}
		if (coll.gameObject.tag.Equals("Enemy_Super") && damageable) {
			StartCoroutine("Flash", "red");
			ActHitSpark();
			if (!anim.GetBool("ko")) { // Make sure not to play any other animations when KO'd
				anim.SetTrigger("hit");
			}
			PlayerMeter.instance.HPChange(-4); // Do this amount of Damage per super hit
			StartCoroutine("DamageCooldown");
			SetKnockback(coll.gameObject);
			audi.PlayOneShot(damaged);
			audi.PlayOneShot(superSFX);
			rb.velocity = new Vector2((kbdir * 6), 6); // knockback
			PlayerMovement.instance.RestrictMove();
		}
	}

	// Deprecated; used to destroy enemy projectiles upon block. I took this out once I remembered that Dewey threw his hat,
	// and this code would make it disappear.
	/*
	void OnTriggerStay2D (Collider2D coll) {
		if (coll.gameObject.tag.Equals("Enemy_Bullet") && blocking && coll.gameObject.tag != "Enemy_Melee") { 
			// coll.gameObject.SetActive(false); // "Destroy" the enemy projectile.
			// coll.gameObject.GetComponent<Collider2D>().enabled = false;
		}
	}
	*/

	/** Sets the direction of knockback in relation to the gameObject that just hit this object.
	 * param[coll] -  the GameObject which had just made contact with this object.
	 */
	private void SetKnockback(GameObject coll) {
		if (transform.position.x < coll.transform.position.x) { // If the object is to the right...
			kbdir = -1; // We're knocked back to the left.
		} else { // The object must be to the left.
			kbdir = 1; // We are then knocked back to the right.
		}
	}
}

