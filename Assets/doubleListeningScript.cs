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

	//Module resources:
	public AudioClip[] sounds;
	public String[] soundNames = new String[] {"Arcade","Ballpoint Pen Writing","Beach","Book Page Turning","Car Engine","Casino","Censorship Bleep","Chainsaw","Compressed Air","Cow","Dialup Internet","Door Closing","Extractor Fan","Firework Exploding","Glass Shattering","Helicopter","Marimba","Medieval Weapons","Oboe","Phone Ringing","Police Radio Scanner",
	"Rattling Iron Chain","Reloading Glock 19","Saxophone","Servo Motor","Sewing Machine","Soccer Match","Squeaky Toy","Supermarket","Table Tennis","Tawny Owl","Taxi Dispatch","Tearing Fabric","Throat Singing","Thrush Nightingale","Tibetan Nuns","Train Station","Tuba","Vacuum Cleaner","Waterfall","Zipper"};
	private Dictionary<String, String> listeningCodes = new Dictionary<String, String>()
	{
		{"Taxi Dispatch","&&&**"},{"Cow","&$#$&"},{"Extractor Fan","$#$*&"},{"Train Station","#$$**"},{"Arcade","$#$#*"},{"Casino","**$*#"},{"Supermarket","#$$&*"},{"Soccer Match","##*$*"},{"Tawny Owl","$#*$&"},{"Sewing Machine","#&&*#"},{"Thrush Nightingale","**#**"},{"Car Engine","&#**&"},{"Reloading Glock 19","$&**#"},{"Oboe","&#$$#"},
		{"Saxophone","$&&**"},{"Tuba","#&$##"},{"Marimba","&*$*$"},{"Phone Ringing","&$$&*"},{"Tibetan Nuns","#&&&&"},{"Throat Singing","**$$$"},{"Beach","*&*&&"},{"Dialup Internet","*#&*&"},{"Police Radio Scanner","**###"},{"Censorship Bleep","&&$&*"},{"Medieval Weapons","&$**&"},{"Door Closing","#$#&$"},{"Chainsaw","&#&&#"},
		{"Compressed Air","$$*$*"},{"Servo Motor","$&#$$"},{"Waterfall","&**$$"},{"Tearing Fabric","$&&*&"},{"Zipper","&$&##"},{"Vacuum Cleaner","#&$*&"},{"Ballpoint Pen Writing","$*$**"},{"Rattling Iron Chain","*#$&&"},{"Book Page Turning","###&$"},{"Table Tennis","*$$&$"},{"Squeaky Toy","$*&##"},{"Helicopter","#&$&&"},{"Firework Exploding","$&$$*"},{"Glass Shattering","*$*$*"}
	};

	//Key module variables:
	private bool moduleSolved = false;
	String solution = null;
	bool soundsPlaying = false;
	int[] soundPositions;

	//Logging variables:
	static int moduleIdCounter = 1;
	int moduleId;

	//Awaken module - assign event handlers etc.
	void Awake()
	{
		moduleId = moduleIdCounter++;

		KMSelectable pb = playButton;
		pb.OnInteract += delegate(){PressPlay(); return false;};

		KMSelectable submit = submitButton;
		submit.OnInteract += delegate(){PressSubmit(); return false;};

		for(int i = 0; i < upArrows.Length; i++)
		{
			int j = i;
			upArrows[i].OnInteract += delegate(){PressUp(j); return false;};
		}
		for(int i = 0; i < downArrows.Length; i++)
		{
			int j = i;
			downArrows[i].OnInteract += delegate(){PressDown(j); return false;};
		}
	}

	//Initialize module.
	void Start() 
	{
		soundPositions = Enumerable.Range(0, 41).ToList().Shuffle().Take(2).ToArray();
		Debug.LogFormat("[Double Listening #{0}] The chosen sounds are \"{1}\" and \"{2}\".", moduleId, soundNames[soundPositions[0]], soundNames[soundPositions[1]]);
		String listeningCode1 = listeningCodes[soundNames[soundPositions[0]]];
		String listeningCode2 = listeningCodes[soundNames[soundPositions[1]]];
		Debug.LogFormat("[Double Listening #{0}] The listening code for \"{1}\" is {2}", moduleId, soundNames[soundPositions[0]], listeningCode1);
		Debug.LogFormat("[Double Listening #{0}] The listening code for \"{1}\" is {2}", moduleId, soundNames[soundPositions[1]], listeningCode2);
		solution = CalculateSolution(listeningCode1,listeningCode2);
		Debug.LogFormat("[Double Listening #{0}] Solution: {1}.", moduleId, solution);
	}

	//Given two listening codes, calculates a String of binary to input to solve the module.
	String CalculateSolution(String symbols1, String symbols2)
	{
		int rowNum = DetermineTableRowNum();
		int[] mappings = new int[4];
		switch(rowNum)
		{
			case 0 :
			Debug.LogFormat("[Double Listening #{0}] Using table row 1: \"If the bomb has at least 3 batteries and at least one of the sounds was Beach or Waterfall\".", moduleId);
			mappings = new int[] {0,0,1,1};break;
			case 1 :
			Debug.LogFormat("[Double Listening #{0}] Using table row 2: \"Otherwise, if the bomb has an empty port plate\".", moduleId);
			mappings = new int[] {0,1,0,1};break;
			case 2 :
			Debug.LogFormat("[Double Listening #{0}] Using table row 3: \"Otherwise, if the last digit of the bomb's serial number is odd\".", moduleId);
			mappings = new int[] {1,0,1,0};break;
			case 3 :
			Debug.LogFormat("[Double Listening #{0}] Using table row 4: \"Otherwise\".", moduleId);
			mappings = new int[] {1,1,0,0};break;
		}
		
		String binary1 = ObtainBinaryOperand(symbols1, mappings);
		String binary2 = ObtainBinaryOperand(symbols2, mappings);
		Debug.LogFormat("[Double Listening #{0}] The binary code for \"{1}\" is {2}.", moduleId, symbols1, binary1);
		Debug.LogFormat("[Double Listening #{0}] The binary code for \"{1}\" is {2}.", moduleId, symbols2, binary2);

		return BitwiseXor(binary1, binary2);
	}

	//Performs edgework lookups to determine which row of the table (0-3, top to bottom) to use.
	int DetermineTableRowNum()
	{
		String listeningName1 = soundNames[soundPositions[0]];
		String listeningName2 = soundNames[soundPositions[1]];
		if(Bomb.GetBatteryCount() >= 3 && (listeningName1.Equals("Beach") || listeningName1.Equals("Waterfall") || listeningName2.Equals("Beach") || listeningName2.Equals("Waterfall")))
			return 0;
		foreach(var plate in Bomb.GetPortPlates())
		{
			if(plate.Count() == 0)
				return 1;
		}
		if(int.Parse(Bomb.GetSerialNumber().Substring(5,1)) % 2 == 1)
			return 2;
		return 3;
	}

	//Maps a String of listening characters to a String of binary.
	String ObtainBinaryOperand(String symbols, int[] mappings)
	{
		String binary = "";

		foreach(char c in symbols)
		{
			if(c == '#') binary += mappings[0].ToString();
			else if(c == '$') binary += mappings[1].ToString();
			else if(c == '&') binary += mappings[2].ToString();
			else binary += mappings[3].ToString();
		}

		return binary;
	}

	//Performs a simple bitwise XOR on strings (using this over a library method because 5 bits is awkward).
	String BitwiseXor(String op1, String op2)
	{
		String result = "";
		for(int i = 0; i < op1.Length; i++)
		{
			if(op1[i] != op2[i]) result += "1";
			else result += "0";
		}

		return result;
	}

	//Process the user pressing the "play" button.
	void PressPlay()
	{
		if(moduleSolved || soundsPlaying)
			return;

		StartCoroutine(PlaySounds(soundPositions));
	}

	//Respond to the user pressing an up arrow by changing that bit to a 1 (regardless of its current value).
	void PressUp(int arrowNum)
	{
		if(moduleSolved)
			return;

		bitDisplays[arrowNum].GetComponentInChildren<TextMesh>().text = "1\n";
		upArrows[arrowNum].AddInteractionPunch(0.5f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
	}

	//Respond to the user pressing a down arrow by changing that bit to a 0 (regardless of its current value).
	void PressDown(int arrowNum)
	{
		if(moduleSolved)
			return;
		
		bitDisplays[arrowNum].GetComponentInChildren<TextMesh>().text = "0\n";
		downArrows[arrowNum].AddInteractionPunch(0.5f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
	}

	void PressSubmit()
	{
		if(moduleSolved)
			return;
		submitButton.AddInteractionPunch(0.5f);
		String answerSubmitted = "";
		foreach(Renderer bit in bitDisplays)
		{
			answerSubmitted += (bit.GetComponentInChildren<TextMesh>().text)[0];
		}

		Debug.LogFormat("[Double Listening #{0}] You submitted {1}.", moduleId, answerSubmitted);

		if(answerSubmitted.Equals(solution))
		{
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			Debug.LogFormat("[Double Listening #{0}] Module solved.", moduleId);
			GetComponent<KMBombModule>().HandlePass();
			moduleSolved = true;
		}
		else
		{
			Debug.LogFormat("[Double Listening #{0}] That was incorrect. Strike!", moduleId);
			GetComponent<KMBombModule>().HandleStrike();
			Start();
		}
	}

	//Play both sounds and run a cooldown of 3 seconds.
	IEnumerator PlaySounds(int[] positions)
	{
		soundsPlaying = true;
		sound1.clip = sounds[positions[0]];
		sound2.clip = sounds[positions[1]];
		sound1.Play();
		sound2.Play();
		yield return new WaitForSeconds(3f);
		soundsPlaying = false;
	}
}
