using System;
using System.Globalization; 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class doubleListeningScript : MonoBehaviour 
{

	//Audio and bomb info from the ModKit:
	public KMAudio Audio;
	public KMBombInfo Bomb;

	//Module components:
	public KMSelectable[] upArrows;
	public KMSelectable[] downArrows;
	public KMSelectable playButton;
	public KMSelectable submitButton;
	public Renderer[] bitDisplays;
	public AudioSource sound1;
	public AudioSource sound2;

	public AudioClip[] sounds;
	public String[] soundNames = new String[] {"Arcade","BallpointPenWriting","Beach","BookPageTurning","CarEngine","Casino","CensorshipBleep","Chainsaw","CompressedAir","Cow","DialupInternet","DoorClosing","ExtractorFan","FireworkExploding","GlassShattering","Helicopter","Marimba","MedievalWeapons","Oboe","PhoneRinging","PoliceRadioScanner","RattlingIronChain","ReloadingGlock19","Saxophone","ServoMotor","SewingMachine","SoccerMatch","SqueakyToy","Supermarket","TableTennis","TawnyOwl","TaxiDispatch","TearingFabric","ThroatSinging","ThrushNightingale","TibetanNuns","TrainStation","Tuba","VacuumCleaner","Waterfall","Zipper"};
	bool moduleSolved = false;
	bool soundsPlaying = false;
	int[] soundPositions;

	//Logging variables:
	static int moduleIdCounter = 1;
	int moduleId;

	//Awaken module.
	void Awake()
	{
		moduleId = moduleIdCounter++;

		KMSelectable pb = playButton;
		pb.OnInteract += delegate(){PressPlay(); return false;};

		foreach(KMSelectable up in upArrows)
		{
			KMSelectable pressedArrow = up;
			//up.OnInteract += delegate(){PressNumber(pressedNumber); return false;};
		}
		foreach(KMSelectable down in downArrows)
		{
			KMSelectable pressedArrow = down;
			//down.OnInteract += delegate(){PressNumber(pressedNumber); return false;};
		}
	}


	//Initialize module.
	void Start() 
	{
		soundPositions = Enumerable.Range(0, 41).ToList().Shuffle().Take(2).ToArray();
		Debug.LogFormat("[Double Listening #{0}] The chosen sounds are {1} and {2}.", moduleId, soundNames[soundPositions[0]], soundNames[soundPositions[1]]);
	}

	//Process the user pressing the "play" button.
	void PressPlay()
	{
		if(moduleSolved || soundsPlaying)
			return;

		StartCoroutine(PlaySounds(soundPositions));
	}

	//Play both sounds and run a cooldown of 5 seconds.
	IEnumerator PlaySounds(int[] positions)
	{
		soundsPlaying = true;
		sound1.clip = sounds[positions[0]];
		sound2.clip = sounds[positions[1]];
		sound1.Play();
		sound2.Play();
		yield return new WaitForSeconds(5f);
		soundsPlaying = false;
	}
}
