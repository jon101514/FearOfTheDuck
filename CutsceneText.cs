/** Jonathan So, jds7523@rit.edu
 * Displays the text beneath the cutscenes, where letters draw onto the screen quickly to reveal the story.
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CutsceneText : MonoBehaviour {

	public List<string> script; // The script to be printed to the screen for the cutscene, one screen at a time.
	public List<Sprite> sprites; // The sprite to show for the cutscene. Corresponds to the Script.
	private Text textPanel; // The panel which we're printing to.
	private Image imgPanel; // The panel where we show the cutscene image.
	private float INTRO_SPEED = 1/40f; // Speed of text if we're on the intro. We're making it faster to synchronize with the song.
	private float DEF_SPEED = 1/20f; // Default speed for the text.
	private float speed = 1/20f; // The current speed of the text. We'll change this.
	private float readTime = 4f; // The amount of time granted to read text after all of it has been displayed.
	private const float INTRO_TIME = 1.5f; // Amount of time granted to read text if we're on the intro.

	public AudioSource jukebox; // Jukebox, if we're on the intro screen or Darkwing cutscenes.

	// Set up references to both the text panel we're drawing to as well as the image.
	private void Awake() {
		textPanel = GetComponent<Text>();
		imgPanel = GameObject.FindObjectOfType<Image>();
	}

	// Determine the speed and readTime of the text. Make them faster if we're on the Opening Cutscene. Begin printing.
	private void Start() {
		if (SceneManager.GetActiveScene().name.Equals("Opening_Cutscene")) {
			speed = INTRO_SPEED;
			readTime = INTRO_TIME;
		}
		StartCoroutine(PrintOut());
	}

	// Handles input, allowing the player to skip the cutscene and speed up the script.
	private void Update() {
		if (Input.GetKeyDown(KeyCode.Alpha1)) { // Skip Button
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		} else if (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.O)) { // Speed up reading
			speed = 1/75f;
		} else {
			if (SceneManager.GetActiveScene().name.Equals("Opening_Cutscene")) {
				speed = INTRO_SPEED;
				readTime = INTRO_TIME;
			} else {
				speed = DEF_SPEED;
			}
		}
	}

	/** Gradually print out each character in the script, pausing after completing each segment so that the player can read.
	 * Prints out each character of each segment of the script, and loads the corresponding cutscene image to the
	 * image panel. When we're done (either by having the song or script end), load the next scene.
	 */
	private IEnumerator PrintOut() {
		yield return new WaitForSeconds(speed);
		// Iterate through all strings in the script.
		string currString;
		for (int i = 0; i < script.Count; i++) {
			textPanel.text = "";
			imgPanel.overrideSprite = sprites[i];
			currString = script[i];
			for (int j = 0; j < currString.Length; j++) {
				textPanel.text += currString[j];
				yield return new WaitForSeconds(speed);
			}
			yield return new WaitForSeconds(readTime);
		}
		if (jukebox != null) { // If we have defined a jukebox (we're on the intro or DW's cutscene)...
			while (jukebox.isPlaying) {
				yield return new WaitForSeconds(1f);
			}
		}
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}
}
