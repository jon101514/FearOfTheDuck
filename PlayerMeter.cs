/** Jonathan So, jds7523@rit.edu
 * Singleton which manages the player's meters of HP and SP.
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerMeter : MonoBehaviour {

	public static PlayerMeter instance; // Singleton design pattern.

	public int hp = 24; // Player starts out with 24 HP...
	public int sp = 0; // and no SP.

	private const int MAX_HP = 24; // Players may not overheal past 24 HP.
	private const int MAX_SP = 120; // Max is 5 bars of SP meter.

	private const float FLASHTIME = 1/20f; 

	// Use Sliders to represent our meters.
	public Slider hpMeter; 
	public Slider spMeter; 
	public Image superCount; // The image which shows the player how much meter they have.

	public Sprite zero, one, two, three, four, five; // All six possible sprites to display over the above SuperCount.
	public AudioClip oneSFX, twoSFX, threeSFX, fourSFX, fiveSFX;

	private Text display; // Previously, used for debugging purposes by printing out text-based "meters" to the screen.
	private AudioSource audi;

	// Get necessary components.
	void Awake() {
		// Set up the Singleton object.
		if (instance == null) {
			instance = this;
		}
		display = GetComponent<Text>();
		audi = GetComponent<AudioSource>();
	}

	// On the "How to Play" screen, let the player become acquainted with their supers
	// by giving them three bars of super, instantly, to try out.
	private void Start() {
		if (SceneManager.GetActiveScene().name == "How to Play") {
			SPChange(24 * 3);
		}
	}

	/** See if the player has been KO'd.
	 * If the player has been KO'd, reload the scene after 4 seconds.
	 * return - a bool stating whether or not the player has been KO'd.
	 */
	public bool IsKOd() {
		if (hp <= 0 && IntroFinishManager.instance.sceneActive) {
			IntroFinishManager.instance.CallDefeat();
			PlayerDefense.instance.StopRedFlash();
			return true;
		}
		return false;
	}

	/** Adds a specified amount to HP.
	 * param[factor] - the amount of HP to add.
	 */
	public void HPChange(int factor) {
		if (hp <= MAX_HP && IntroFinishManager.instance.sceneActive) {
			hp += factor;
		}
		if (hp > MAX_HP) {
			hp = MAX_HP;
		}
		hpMeter.value = hp;
		if (hp <= 2 && hp > 0) {
			PlayerDefense.instance.StartRedFlash();
		} else {
			PlayerDefense.instance.StopRedFlash();
		}
	}

	/** Choose the appropriate sprite for the super meter.
	 */
	private void ChooseSuperSprite(int factor) {
		int numSupers = sp / 24;
		if (numSupers >= 5) {
			if (factor > 0 && superCount.sprite != five) {
				audi.PlayOneShot(fiveSFX);
			}
			superCount.sprite = five;
		} else if (numSupers == 4) {			
			if (factor > 0 && superCount.sprite != four) {
				audi.PlayOneShot(fourSFX);
			}
			superCount.sprite = four;
		} else if (numSupers == 3) {
			if (factor > 0 && superCount.sprite != three) {
				audi.PlayOneShot(threeSFX);
			}
			superCount.sprite = three;
		} else if (numSupers == 2) {
			if (factor > 0 && superCount.sprite != two) {
				audi.PlayOneShot(twoSFX);
			}
			superCount.sprite = two;
		} else if (numSupers == 1) {
			if (factor > 0 && superCount.sprite != one) {
				audi.PlayOneShot(oneSFX);
			}
			superCount.sprite = one;

		} else {
			superCount.sprite = zero;
		}

	}

	/** Adds a specified amount to SP.
	 * param[factor] - the amount of SP to add.
	 */
	public void SPChange(int factor) {
		if (sp <= MAX_SP) {
			sp += factor;
			StopCoroutine("MeterLerp");
		}
		if (sp > MAX_SP) {
			sp = MAX_SP;
		}
		spMeter.value = (sp % 24);
		ChooseSuperSprite(factor);
	}

// During testing, BuildString was called to make text-based "meters."
//	void Update() {
//		BuildString();
//	}

	/** Deprecated; used to build the string which represents the player's meters.
	 */
	private void BuildString() {
		string result = "<Color=red>HP ";
		for (int i = 0; i < hp; i++) {
			result += "|";
		}
		result += "</Color>\n";
		result += "<Color=cyan>SP";
		int spDiv = sp / 24;
		result += spDiv;
		for (int i = 0; i < sp % 24; i++) {
			result += "|";
		}
		for (int i = 0; i < 24 - (sp % 24); i++) {
			result += " ";
		}

		result += "X</Color>";
		display.text = result;
	}
}
