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

	//Audio, bomb, and rule seed info:
	public KMAudio Audio;
	public KMBombInfo Bomb;
	public KMRuleSeedable RuleSeed;

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
	//For rule seed:
	String[] IndicatorNames = {"BOB", "CAR", "CLR", "FRK", "FRQ", "IND", "MSA", "NSA", "SIG", "SND", "TRN"};
	Port[] Ports = {Port.Parallel, Port.Serial, Port.PS2, Port.RJ45, Port.DVI, Port.StereoRCA};
	String[] PortNames = {"Parallel", "Serial", "PS/2", "RJ-45", "DVI-D", "Stereo RCA"};

	//Key module variables:
	private bool moduleSolved = false;
	String solution = null;
	bool soundsPlaying = false;
	int[] soundPositions;
	Condition[] conditions;

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
		var rnd = RuleSeed.GetRNG();

		if(rnd.Seed == 1)
			conditions = null;
		else
		{
			conditions = new Condition[3];
			int[] conditionIndicesUsed = new int[3];
			for(int i = 0; i < 3; i++)
			{
				int condIndex = -1;
				while(condIndex == -1 || conditionIndicesUsed.Contains(condIndex))
					condIndex = rnd.Next(7);
			
				var condType = (ConditionType)condIndex;
				conditionIndicesUsed[i] = condIndex;
				int parameter;

				if(condIndex < 2)
					parameter = rnd.Next(11);
				else
					parameter = rnd.Next(6);

				if(i == 0)
				{
					int[] sounds = new int[2];
					sounds[0] = rnd.Next(41);
					sounds[1] = -1;
					while(sounds[1] == -1 || sounds[1] == sounds[0])
						sounds[1] = rnd.Next(41);
					conditions[i] = new Condition{Type = condType, ConditionParam = parameter, Sounds = sounds};
				}
				else
				{
					conditions[i] = new Condition{Type = condType, ConditionParam = parameter};
				}
			}

		}

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
			mappings = new int[] {0,0,1,1};break;
			case 1 :
			mappings = new int[] {0,1,0,1};break;
			case 2 :
			mappings = new int[] {1,0,1,0};break;
			case 3 :
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
		if(conditions == null)
		{
			if(Bomb.GetBatteryCount() >= 3 && (listeningName1.Equals("Beach") || listeningName1.Equals("Waterfall") || listeningName2.Equals("Beach") || listeningName2.Equals("Waterfall")))
			{
				Debug.LogFormat("[Double Listening #{0}] Using table row 1: \"If the bomb has at least 3 batteries and at least one of the sounds was Beach or Waterfall\".", moduleId);
				return 0;
			}	
			foreach(var plate in Bomb.GetPortPlates())
			{
				if(plate.Count() == 0)
				{
					Debug.LogFormat("[Double Listening #{0}] Using table row 2: \"Otherwise, if the bomb has an empty port plate\".", moduleId);
					return 1;
				}
			}
			if(int.Parse(Bomb.GetSerialNumber().Substring(5,1)) % 2 == 1)
			{
				Debug.LogFormat("[Double Listening #{0}] Using table row 3: \"Otherwise, if the last digit of the bomb's serial number is odd\".", moduleId);
				return 2;
			}
			Debug.LogFormat("[Double Listening #{0}] Using table row 4: \"Otherwise\".", moduleId);
			return 3;
		}
		for(int i = 0; i < 3; i++)
		{
			if(i == 0 && !conditions[i].Sounds.Contains(soundPositions[0]) && !conditions[i].Sounds.Contains(soundPositions[1]))
				continue;

			switch(conditions[i].Type)
			{
				case ConditionType.LitIndicator:
					if(Bomb.IsIndicatorOn(IndicatorNames[conditions[i].ConditionParam]))
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb has a lit {3} indicator{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", IndicatorNames[conditions[i].ConditionParam], 
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
				case ConditionType.UnlitIndicator:
					if(Bomb.IsIndicatorOff(IndicatorNames[conditions[i].ConditionParam]))
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb has an unlit {3} indicator{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", IndicatorNames[conditions[i].ConditionParam],
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
				case ConditionType.Port:
					if(Bomb.GetPortCount(Ports[conditions[i].ConditionParam]) > 0)
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb has a {3} port{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", PortNames[conditions[i].ConditionParam],
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
				case ConditionType.EmptyPlate:
					foreach(var plate in Bomb.GetPortPlates())
						if(plate.Count() == 0)
						{
							Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb has an empty port plate{3}\".", 
							moduleId, i+1, (i == 0) ? "I" : "Otherwise, i",
							(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
							return i;
						}
					break;
				case ConditionType.BatteryCount:
					if(Bomb.GetBatteryCount() >= (conditions[i].ConditionParam % 3) + 2)
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb has at least {3} batteries{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", (conditions[i].ConditionParam % 3) + 2,
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
				case ConditionType.SerialParity:
					if((Bomb.GetSerialNumberNumbers().Last() % 2) == (conditions[i].ConditionParam % 2))
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the last digit of the bomb's serial number is {3}{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", (conditions[i].ConditionParam % 2 == 0) ? "even" : "odd",
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
				case ConditionType.SerialVowel:
					bool containsVowel = Bomb.GetSerialNumberLetters().Any(x => x == 'A' || x == 'E' || x == 'I' || x == 'O' || x == 'U');
					if((containsVowel && conditions[i].ConditionParam % 2 == 1) || (!containsVowel && conditions[i].ConditionParam % 2 == 0))
					{
						Debug.LogFormat("[Double Listening #{0}] Using table row {1}: \"{2}f the bomb's serial number {3} a vowel{4}\".", 
						moduleId, i+1, (i == 0) ? "I" : "Otherwise, i", (conditions[i].ConditionParam % 2 == 0) ? "does not contain" : "contains",
						(i==0) ? (" and at least one of the sounds was " + listeningName1 + " or " + listeningName2) : "");
						return i;
					}
					break;
			}
		}
		Debug.LogFormat("[Double Listening #{0}] Using table row 4: \"Otherwise\".", moduleId);
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

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Play the sounds with “!{0} play”. Set the bit displays with “!{0} set 10101”. Submit with “!{0} submit”. Set and submit with “!{0} set 10101 submit.”";
#pragma warning restore 414

	//Process command for Twitch Plays - IEnumerator method used due to length of sounds.
	IEnumerator ProcessTwitchCommand(String command)
	{
		var play = Regex.Match(command,@"^(\s)*(play){1}(\s)*$", RegexOptions.IgnoreCase);
		var set = Regex.Match(command,@"^\s*(submit|((set\s([0-1]){5})(\ssubmit)?))(\s)*$", RegexOptions.IgnoreCase);
		
		if(!(play.Success || set.Success))
			yield break;
		
		if(play.Success)
		{
			yield return null;
			playButton.OnInteract();
		}
		else if(command.ToLower().Contains("set"))
		{
			String valuesEntered = set.Groups[3].Value.ToLowerInvariant().Trim().Substring(4);
			for(int i = 0; i < valuesEntered.Length; i++)
			{
				if(valuesEntered[i] == '1' && bitDisplays[i].GetComponentInChildren<TextMesh>().text.Contains("0"))
				{
					yield return null;
					upArrows[i].OnInteract();
					yield return new WaitForSeconds(.05f);
				}
				else if(valuesEntered[i] == '0' && bitDisplays[i].GetComponentInChildren<TextMesh>().text.Contains("1"))
				{
					yield return null;
					downArrows[i].OnInteract();
					yield return new WaitForSeconds(.05f);
				}
			}
		}

		if(command.ToLower().Contains("submit"))
		{
			yield return null;
			submitButton.OnInteract();
		}
	}

	//Calls a coroutine to autosolve the module when a TP admin does !<id> solve.
	void TwitchHandleForcedSolve()
	{
		if(moduleSolved) return;
		StartCoroutine(HandleForcedSolve());
	}

	IEnumerator HandleForcedSolve()
	{
		yield return null;
		Debug.Log(solution);
		for(int i = 0; i < solution.Length; i++)
		{
			if(solution[i] == '1')
			{
				upArrows[i].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			else
			{
				downArrows[i].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}

		submitButton.OnInteract();
	}
}
