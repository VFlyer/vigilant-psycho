using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;

public class VigiliantPsychoScript : MonoBehaviour {
	public KMAudio mAudio;
	public KMSelectable[] keyboardSelectable;
	public KMSelectable submitSelectable;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public TextMesh[] displayMeshes, inputMeshes, keyboardMeshes;
	public MeshRenderer[] statusRenderers, glitchRenderers;
	public MeshRenderer submitRenderer;
	public Material[] statusMats;
	const string initialKeyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM", engAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	string keyboardLayout, inputtedLetters;
	List<string> decodedWords, alphabetUsed;
	static readonly Color[] coloursTransforms = new[] { new Color(1, 1, 1), new Color(1, 0, 0), new Color(1, 0, 1), new Color(1, 1, 0), new Color(0, 1, 0), new Color(0, 1, 1), new Color(0.5f, 0, 1) };
	static readonly string[] coloursNames = new[] { "White", "Red", "Magenta", "Yellow", "Green", "Cyan", "Violet", },
        lettersRequiredFromColour = new[] { "ABCD", "EFGH", "IJK", "LMNO", "PQR", "STUV", "WXYZ" };
	int completedStages, idxForbidTransformStg3, idxColourLetter;
	List<int> idxLetterTransform;
	List<int>[] possibleTransformsKeyboard;
	int[] curIdxTransformKeyboard;
	List<int> colorIdxSubCycle;
	char letterGlitch = '-';

	private static int moduleIDCounter;
	private int moduleID;
	private bool moduleSolved, graceStage = false, interactable, clearOnEmptyPressed = false;

	IEnumerator stg3Handler;

	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	// Use this for initialization
	void Start () {
		moduleID = ++moduleIDCounter;
		idxLetterTransform = new List<int>();
		decodedWords = new List<string>();
		alphabetUsed = new List<string>();
		possibleTransformsKeyboard = new List<int>[26];
		curIdxTransformKeyboard = new int[26];
		colorIdxSubCycle = new List<int>();
		for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
			possibleTransformsKeyboard[x] = new List<int>();
		modSelf.OnActivate += delegate { HandleStageTransition(0); interactable = true; };
		submitSelectable.OnInteract += delegate {
			if (interactable && !moduleSolved)
				HandleSubmit();
			return false;
		};
        for (var x = 0; x < keyboardSelectable.Length; x++)
        {
			var y = x;
			keyboardSelectable[x].OnInteract += delegate {
				if (interactable && !moduleSolved)
					HandleKeyPress(y);
				return false;
			};
        }
	}
	class PossibleCombination
    {
		public char[] letters;
		public int[] idxEncodes;
    }
	List<PossibleCombination> GetAllPossibleCombinationsCurStage(string expectedWord, string curAlphabet = engAlphabet, Dictionary<int, IEnumerable<char>> idxForbiddens = null)
    {
		var output = new List<PossibleCombination>();
		var allTargetLetterIdxes = expectedWord.Select(a => curAlphabet.IndexOf(expectedWord)).ToArray();
		var serialNo = bombInfo.GetSerialNumber();
		for (var x = 0; x < expectedWord.Length; x++)
		{
			var targetLetterIdx = curAlphabet.IndexOf(expectedWord[x]);
			for (var y = 0; y < 7; y++)
			{
				switch (y)
				{
					case 0:
						break;
					case 1:
						{
							targetLetterIdx -= x + 1;
						}
						break;
					case 2:
						{
							if (x == 0) break;
							targetLetterIdx -= allTargetLetterIdxes[x - 1] + 1;
						}
						break;
					case 3:
						{
							targetLetterIdx -= curAlphabet.Contains(serialNo[x]) ? (curAlphabet.IndexOf(serialNo[x]) + 1) : (serialNo[x] - '0');
						}
						break;
					case 4:
						{
							var digitsInSerial = bombInfo.GetSerialNumberNumbers();
							var curSerialNoDigit = digitsInSerial.ElementAt(x % digitsInSerial.Count());
							targetLetterIdx += (x + 1) % 2 == (bombInfo.GetPortCount() % 2) ? -curSerialNoDigit : curSerialNoDigit;
						}
						break;
					case 5:
						{
							targetLetterIdx = 25 - targetLetterIdx;
						}
						break;
					case 6:
						{
							targetLetterIdx += bombInfo.GetSerialNumberNumbers().Sum() + 6 - x;
						}
						break;
				}
			}
		}
		return output;
    }

	void HandleStageTransition(int idxStage)
    {
		while (decodedWords.Count > idxStage)
			decodedWords.RemoveAt(decodedWords.Count - 1);
		while (alphabetUsed.Count > idxStage)
			alphabetUsed.RemoveAt(alphabetUsed.Count - 1);

		var serialNo = bombInfo.GetSerialNumber();
		var serialLetterFirst = bombInfo.GetSerialNumberLetters().First();
		var pickedWord = Wordlist.words.PickRandom();
		var curAlphabet = engAlphabet;

		var displayTransforms = new List<int>();

		for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
			possibleTransformsKeyboard[x].Clear();
		for (var x = 0; x < curIdxTransformKeyboard.Length; x++)
			curIdxTransformKeyboard[x] = 0;
		letterGlitch = '-';
		QuickLog("Stage {0}:", idxStage + 1);
		switch (idxStage)
        {
			default:
				alphabetUsed.Add(curAlphabet);
				decodedWords.Add(pickedWord);
			break;
			case 0:
				{
					while (!pickedWord.StartsWith(serialLetterFirst.ToString())) // Pick a different starting word starting with the first letter of the serial number.
						pickedWord = Wordlist.words.Where(a => a.StartsWith(serialLetterFirst.ToString())).PickRandom();
					for (var x = 0; x < 7; x++)
						displayTransforms.AddRange(Enumerable.Repeat(0, 5));
					keyboardLayout = initialKeyboardLayout;
					for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
						possibleTransformsKeyboard[x].Add(0);
				}
				goto default;
			case 1:
				{
					letterGlitch = pickedWord.First();
					// Alphabet for Stage 2.
					var stg1DecodedWord = decodedWords.First();
					var lettersIncluded = new List<char>();
					for (var x = 0; lettersIncluded.Count < 26; x = (x + 1) % stg1DecodedWord.Length)
					{
						var curLetter = stg1DecodedWord[x];
						if (lettersIncluded.Contains(curLetter))
						{
							var idxLetter = engAlphabet.IndexOf(curLetter);
							while (lettersIncluded.Contains(engAlphabet[idxLetter]))
								idxLetter = (idxLetter + 1) % engAlphabet.Length;
							curLetter = engAlphabet[idxLetter];
						}
						lettersIncluded.Add(curLetter);
					}
					curAlphabet = lettersIncluded.Join("");
					// Display Transforms for Stage 2.
					var pickedIdxTransformDisplay = Random.Range(1, 7);
					displayTransforms.AddRange(Enumerable.Repeat(pickedIdxTransformDisplay, 5));
					// Alter the keyboard.
					keyboardLayout = engAlphabet.ToCharArray().Shuffle().Join("");
					var pickedIdxTransformKeyboard = Random.Range(1, 7);
					for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
						possibleTransformsKeyboard[x].Add(pickedIdxTransformKeyboard);
					idxColourLetter = Enumerable.Range(0, 7).Where(a => a != pickedIdxTransformKeyboard).PickRandom();
				}
				goto default;
			case 2:
				{
					colorIdxSubCycle.Clear();
					decodedWords.Add(pickedWord.ToString());
					// Alphabet for Stage 3.
					curAlphabet = keyboardLayout.ToString();
					alphabetUsed.Add(curAlphabet);
					// Alter the decoded word with the word on stage 2.
					var alteredWord = "";
					for (var x = 0; x < pickedWord.Length; x++)
						alteredWord += curAlphabet[(curAlphabet.IndexOf(pickedWord[x]) + curAlphabet.IndexOf(decodedWords[1][x])) % 26];
					pickedWord = alteredWord;
					// Display Transforms for Stage 3.
					for (var x = 0; x < 5; x++)
						displayTransforms.Add(Random.Range(0, 7));
					// Alter the keyboard.
					keyboardLayout = engAlphabet.ToCharArray().Shuffle().Join("");
					for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
						possibleTransformsKeyboard[x].AddRange(Enumerable.Range(0, 7).ToArray().Shuffle());
					var idxKeyboard1stLetter = keyboardLayout.IndexOf(alteredWord.First());
					var pickedIdxExclude = possibleTransformsKeyboard[idxKeyboard1stLetter].PickRandom();
					possibleTransformsKeyboard[idxKeyboard1stLetter].Remove(pickedIdxExclude);
					idxColourLetter = pickedIdxExclude;
					// Generate a binary corresponding to each value.
					var binary = "";
					for (var x = 0; x < 7; x++)
						binary += Random.value < 0.5f ? "0" : "1";
					colorIdxSubCycle.AddRange(Enumerable.Range(0, 14).Select(a => a % 2 == 0 ? (binary[a / 2] == '1' ? 1 : 0) : 2));
					colorIdxSubCycle.AddRange(Enumerable.Repeat(2, 5));
				}
				break;
        }
		var distancesBetweenWords = new int[5];
		for (var x = 0; x < 5; x++)
        {
			var idxFirstLetter = curAlphabet.IndexOf(pickedWord[x]);
			var idxNextLetter = curAlphabet.IndexOf(pickedWord[x + 1]);
			distancesBetweenWords[x] = idxFirstLetter > idxNextLetter ? 26 + (idxNextLetter - idxFirstLetter) : idxNextLetter - idxFirstLetter;
        }
		QuickLog("The alphabet used on this stage is {0}", curAlphabet);
		QuickLog("The decoded word is {0}", decodedWords.Last());
		if (idxStage < 2)
			QuickLog("The first letter of the decoded word is {0}", pickedWord.First());
		else
		{
			QuickLog("After applying Vigenère Cipher with stage 2's decoded word, the result should be {0}", pickedWord);
			QuickLog("The missing colour from the letter with 6 different colourings is {0}, corresponding to at least 1 of these letters required: {1}", coloursNames[idxColourLetter], lettersRequiredFromColour[idxColourLetter]);
		}
		QuickLog("Only moving forwards in the alphabet, the gaps between consecutive letters of the word are {0}", distancesBetweenWords.Join(", "));
		if (possibleTransformsKeyboard.All(a => a.Count == 1 && a.Single() == 0))
			QuickLog("No transformation applied on keyboard.");
		else if (possibleTransformsKeyboard.All(a => a.Count == 1) && possibleTransformsKeyboard.Select(a => a.Single()).Distinct().Count() == 1)
			QuickLog("All letters on the keyboard will have the {0} transformation applied.", coloursNames[possibleTransformsKeyboard.Select(a => a.Single()).Distinct().Single()]);
		if (displayTransforms.All(a => a == 0))
			QuickLog("No transformation applied on displayed letters.");
		else if (displayTransforms.Distinct().Count() == 1)
			QuickLog("Displayed letters will all apply the {0} transform.", coloursNames[displayTransforms.Distinct().Single()]);
		else
			QuickLog("The displayed letters will each have the following transforms: {0}", displayTransforms.Select(a => coloursNames[a]).Join(", "));
		var lettersToDisplay = new List<char>();
		for (var x = 0; x < 5; x++)
        {
			var y = x;
			var curDistance = distancesBetweenWords[x];
			switch (displayTransforms[x])
            {
				case 0:
					break;
				case 1:
					curDistance -= y + 1;
					break;
				case 2:
					if (x == 0) goto case 0;
					curDistance -= curAlphabet.IndexOf(lettersToDisplay[y - 1]) + 1;
					break;
				case 3:
					curDistance -= curAlphabet.Contains(serialNo[y]) ? (curAlphabet.IndexOf(serialNo[y]) + 1) : (serialNo[y] - '0');
					break;
				case 4:
                    {
						var digitsInSerial = bombInfo.GetSerialNumberNumbers();
						var curSerialNoDigit = digitsInSerial.ElementAt(y % digitsInSerial.Count());
						curDistance += (y + 1) % 2 == (bombInfo.GetPortCount() % 2) ? -curSerialNoDigit : curSerialNoDigit;
                    }
					break;
				case 5:
					curDistance = 25 - curDistance;
					break;
				case 6:
					curDistance += bombInfo.GetSerialNumberNumbers().Sum() + 5 - y;
					break;
            }

			lettersToDisplay.Add(curAlphabet[PMod(curDistance - 1, 26)]);
        }
		QuickLog("The displayed letters are {0}", lettersToDisplay.Join(", "));
		for (var x = 0; x < lettersToDisplay.Count; x++)
		{
			displayMeshes[x].text = lettersToDisplay[x].ToString();
			displayMeshes[x].color = coloursTransforms[displayTransforms[x]];
		}
		inputtedLetters = "";
		idxLetterTransform.Clear();
		HandleUpdateAll();
	}
	int PMod(int start, int dividend)
    {
		return ((start % dividend) + dividend) % dividend;
    }
	void HandleUpdateAll()
    {
        for (var x = 0; x < keyboardMeshes.Length; x++)
        {
			keyboardMeshes[x].text = keyboardLayout[x].ToString();
			keyboardMeshes[x].color = letterGlitch == keyboardLayout[x] ? coloursTransforms[idxColourLetter] : coloursTransforms[possibleTransformsKeyboard[x][curIdxTransformKeyboard[x]]];
        }
        for (var x = 0; x < inputMeshes.Length; x++)
        {
			inputMeshes[x].text = inputtedLetters.Length <= x ? "" : inputtedLetters[x].ToString();
			inputMeshes[x].color = idxLetterTransform.Count <= x ? Color.clear : coloursTransforms[idxLetterTransform[x]];
        }
        for (var x = 0; x < statusRenderers.Length; x++)
			statusRenderers[x].material = x < completedStages ? x + 1 == completedStages && (clearOnEmptyPressed || !graceStage) ? statusMats[2] : statusMats[1] : statusMats[0];
    }
	bool AreSubmittedLettersCorrect(string expectedWord)
    {
		var inputtedWordAfterTransform = "";
		var curAlphabet = alphabetUsed[completedStages];
		var serialNo = bombInfo.GetSerialNumber();
		for (var x = 0; x < inputtedLetters.Length; x++)
		{
			var idxInCurAlphabet = curAlphabet.IndexOf(inputtedLetters[x]);
			var y = x;
			switch (idxLetterTransform[x])
			{
				case 0:
					break;
				case 1:
					idxInCurAlphabet += y + 1;
					break;
				case 2:
					if (x == 0) goto case 0;
					idxInCurAlphabet -= curAlphabet.IndexOf(inputtedWordAfterTransform[y - 1]) + 1;
					break;
				case 3:
					idxInCurAlphabet += curAlphabet.Contains(serialNo[y]) ? (curAlphabet.IndexOf(serialNo[y]) + 1) : (serialNo[y] - '0');
					break;
				case 4:
					{
						var digitsInSerial = bombInfo.GetSerialNumberNumbers();
						var curSerialNoDigit = digitsInSerial.ElementAt(y % digitsInSerial.Count());
						idxInCurAlphabet += (y + 1) % 2 == (bombInfo.GetPortCount() % 2) ? curSerialNoDigit : -curSerialNoDigit;
					}
					break;
				case 5:
					idxInCurAlphabet = 25 - idxInCurAlphabet;
					break;
				case 6:
					idxInCurAlphabet -= bombInfo.GetSerialNumberNumbers().Sum() + 5 - y;
					break;
			}
			inputtedWordAfterTransform += curAlphabet[PMod(idxInCurAlphabet, 26)];
		}
		return inputtedWordAfterTransform == expectedWord;
	}

	void HandleSubmit()
	{
		submitSelectable.AddInteractionPunch(0.25f);
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitSelectable.transform);
		clearOnEmptyPressed = false;
		if (inputtedLetters.Length < 6)
		{
			for (var x = 0; x < curIdxTransformKeyboard.Length; x++)
				curIdxTransformKeyboard[x] = (curIdxTransformKeyboard[x] + 1) % possibleTransformsKeyboard[x].Count;
			HandleUpdateAll();
			return;
		}
		var inputtedWordAfterTransform = "";
		var curAlphabet = alphabetUsed[completedStages];
		var serialNo = bombInfo.GetSerialNumber();
		QuickLog("Submitted the following letters: {0}", inputtedLetters.Join(""));
		QuickLog("With the transforms of each letter: {0}", idxLetterTransform.Select(a => coloursNames[a]).Join(", "));
		for (var x = 0; x < inputtedLetters.Length; x++)
        {
			var idxInCurAlphabet = curAlphabet.IndexOf(inputtedLetters[x]);
			var y = x;
			switch (idxLetterTransform[x])
			{
				case 0:
					break;
				case 1:
					idxInCurAlphabet += y + 1;
					break;
				case 2:
					if (x == 0) goto case 0;
					idxInCurAlphabet -= curAlphabet.IndexOf(inputtedWordAfterTransform[y - 1]) + 1;
					break;
				case 3:
					idxInCurAlphabet += curAlphabet.Contains(serialNo[y]) ? (curAlphabet.IndexOf(serialNo[y]) + 1) : (serialNo[y] - '0');
					break;
				case 4:
					{
						var digitsInSerial = bombInfo.GetSerialNumberNumbers();
						var curSerialNoDigit = digitsInSerial.ElementAt(y % digitsInSerial.Count());
						idxInCurAlphabet += (y + 1) % 2 == (bombInfo.GetPortCount() % 2) ? curSerialNoDigit : -curSerialNoDigit;
					}
					break;
				case 5:
					idxInCurAlphabet = 25 - idxInCurAlphabet;
					break;
				case 6:
					idxInCurAlphabet -= bombInfo.GetSerialNumberNumbers().Sum() + 5 - y;
					break;
			}
			inputtedWordAfterTransform += curAlphabet[PMod(idxInCurAlphabet, 26)];
		}
		QuickLog("Decrypting the inputted display results in {0}.", inputtedWordAfterTransform.Join(""));
		if (completedStages >= 2)
        {
			StopCoroutine(stg3Handler);
			stg3Handler = HandleStage3Transition(true, true);
			StartCoroutine(stg3Handler);
			return;
        }
		if (AreSubmittedLettersCorrect(decodedWords[completedStages]))
        {
			QuickLog("Advancing to the next stage...");
			mAudio.PlaySoundAtTransform("InputCorrect", transform);
			completedStages++;
			if (completedStages >= 2)
            {
				QuickLog("On stage 3? Alright. Here we go...");
				stg3Handler = HandleStage3Transition();
				StartCoroutine(stg3Handler);
            }
			else
            {
				graceStage = completedStages == 1;
				HandleStageTransition(completedStages);
			}
        }
		else
        {
			modSelf.HandleStrike();
			if (graceStage)
			{
				QuickLog("Retrying current stage...");
				graceStage = false;
			}
			else
			{
				if (completedStages == 0)
					QuickLog("Retrying current stage...");
				else
					QuickLog("Stepping back due to 2 consectutive strikes on this module.");
				completedStages = Mathf.Clamp(completedStages - 1, 0, 3);
			}
			HandleStageTransition(completedStages);
		}
    }
	void HandleKeyPress(int idxPressed)
    {
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keyboardSelectable[idxPressed].transform);
		keyboardSelectable[idxPressed].AddInteractionPunch(0.25f);
		if (idxPressed >= 26)
		{
			if (inputtedLetters.Length > 0)
			{
				inputtedLetters = "";
				idxLetterTransform.Clear();
			}
			else if (clearOnEmptyPressed && completedStages > 0)
			{
				QuickLog("Stepping back...");
				if (completedStages == 2)
				{
					StopCoroutine(stg3Handler);
					stg3Handler = HandleStage3Transition(true, false);
					StartCoroutine(stg3Handler);
					return;
				}
				completedStages--;
				graceStage = completedStages == 1;
				HandleStageTransition(completedStages);
			}
			else
				clearOnEmptyPressed = true;
		}
		else
		{
			clearOnEmptyPressed = false;
			if (inputtedLetters.Length < 6)
			{
				inputtedLetters += keyboardLayout[idxPressed];
				idxLetterTransform.Add(possibleTransformsKeyboard[idxPressed][curIdxTransformKeyboard[idxPressed]]);
			}
		}
		HandleUpdateAll();
	}
	IEnumerator HandleStage3Transition(bool isExiting = false, bool checkAnswer = false)
    {
		interactable = false;
		HandleUpdateAll();
		mAudio.PlaySoundAtTransform(isExiting ? "QTCSolve" : "CryCycReveal", transform);
		for (int i = 0; i < 21; i++)
		{
			foreach (Renderer g in glitchRenderers)
			{
				int rand = Random.Range(0, 2);
				g.enabled = rand == 1 && i < 20;
            }
			yield return new WaitForSeconds(0.05f);
		}
		if (isExiting && checkAnswer)
        {
			var isAllCorrect = true;
			isAllCorrect &= AreSubmittedLettersCorrect(decodedWords[completedStages]);
			isAllCorrect &= idxLetterTransform.Distinct().Count() == 6;

			if (isAllCorrect)
            {
				completedStages++;
				moduleSolved = true;
				modSelf.HandlePass();
				HandleUpdateAll();
				yield break;
            }
			else
			{
				modSelf.HandleStrike();
				QuickLog("Stepping back to stage 2.");
				completedStages--;
				graceStage = true;
				HandleStageTransition(completedStages);
			}
		}
		else if (isExiting)
        {
			completedStages--;
			graceStage = true;
			HandleStageTransition(completedStages);
		}
		else
        {
			graceStage = false;
			HandleStageTransition(completedStages);
		}
		interactable = true;
		yield break;
    }

}
