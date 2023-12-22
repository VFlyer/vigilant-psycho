using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using Debug = UnityEngine.Debug;
using System;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

public class VigilantPsychoScript : MonoBehaviour {
	public KMAudio mAudio;
	public KMSelectable[] keyboardSelectable;
	public KMSelectable submitSelectable;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public TextMesh[] displayMeshes, inputMeshes, keyboardMeshes;
	public TextMesh submitTxt;
	public MeshRenderer[] statusRenderers, glitchRenderers;
	public MeshRenderer submitRenderer;
	public Material[] statusMats;
	const string initialKeyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM", engAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	string keyboardLayout, inputtedLetters;
	List<string> decodedWords, alphabetUsed;
	static readonly Color[] coloursTransforms = new[] { new Color(1, 1, 1), new Color(1, 0, 0), new Color(1, 0, 1), new Color(1, 1, 0), new Color(0, 1, 0), new Color(0, 1, 1), new Color(0.5f, 0, 1) };
	static readonly string[] coloursNames = new[] { "White", "Red", "Magenta", "Yellow", "Green", "Cyan", "Violet", },
        lettersRequiredFromColour = new[] { "ABCDEFG", "HIJKLMNO", "PQRSTUV", "WXYZABCD", "EFGHIJK", "LMNOPQRS", "TUVWXYZ" };
	int completedStages, idxForbidTransformStg3, idxColourLetter;
	List<int> idxLetterTransform;
	List<int>[] possibleTransformsKeyboard;
	int[] curIdxTransformKeyboard;
	List<int> colorIdxSubCycle;
	char letterGlitch = '-';
	public ObjectMesserScript messerScript;

	private static int moduleIDCounter;
	private int moduleID;
	private bool moduleSolved, graceStage = false, interactable, clearOnEmptyPressed = false;

	readonly static int stage3TimeSec = 900;

	Stopwatch timer;

	IEnumerator[] stg3Handler;
	Color originalSubmitColor;

	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
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
		originalSubmitColor = submitRenderer.material.color;
	}
	void HandleStageTransition(int idxStage)
    {
		while (decodedWords.Count > idxStage)
			decodedWords.RemoveAt(decodedWords.Count - 1);
		while (alphabetUsed.Count > idxStage)
			alphabetUsed.RemoveAt(alphabetUsed.Count - 1);

		var serialLetterFirst = bombInfo.GetSerialNumberLetters().First();
		var pickedWord = Wordlist.words.PickRandom();
		var curAlphabet = engAlphabet;
		var conditionReason = "";
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
					// Display Transforms for Stage 3.
					for (var x = 0; x < 5; x++)
						displayTransforms.Add(Random.Range(0, 7));
					// Alter the keyboard.
					keyboardLayout = engAlphabet.ToCharArray().Shuffle().Join("");
					for (var x = 0; x < possibleTransformsKeyboard.Length; x++)
						possibleTransformsKeyboard[x].AddRange(Enumerable.Range(0, 7).ToArray().Shuffle());
					// Alphabet for Stage 3.
					curAlphabet = keyboardLayout.ToString();
					alphabetUsed.Add(curAlphabet);
					// Alter the decoded word with the word on stage 2.
					var alteredWord = "";
					for (var x = 0; x < pickedWord.Length; x++)
						alteredWord += curAlphabet[(curAlphabet.IndexOf(pickedWord[x]) + curAlphabet.IndexOf(decodedWords[1][x]) + 1) % 26];
                    pickedWord = alteredWord;
					// Remove a possible colour to determine manditory letters.
					var idxKeyboard1stLetter = keyboardLayout.IndexOf(alteredWord.First());
					var pickedIdxExclude = possibleTransformsKeyboard[idxKeyboard1stLetter].PickRandom(); // Also pick a manditory colour to remove.
					possibleTransformsKeyboard[idxKeyboard1stLetter].Remove(pickedIdxExclude);
					idxColourLetter = pickedIdxExclude;
					// Generate a binary corresponding to each value, apply the condition corresponding to the binary string.
					var binary = "";
					for (var x = 0; x < 7; x++)
						binary += Random.value < 0.5f ? "0" : "1";
					colorIdxSubCycle.AddRange(Enumerable.Range(0, 14).Select(a => a % 2 == 0 ? (binary[a / 2] == '1' ? 1 : 0) : 2));
					colorIdxSubCycle.AddRange(Enumerable.Repeat(2, 5));
					var initialColourIdx = ((int)DateTime.Now.DayOfWeek + bombInfo.GetStrikes() + bombInfo.GetIndicators().Count()) % 7;
					if (binary.Distinct().Count() == 1)
                    {
						conditionReason = "All binary digits are 1s or 0s";
						idxForbidTransformStg3 = initialColourIdx;
                    }
					else if (binary.Count(a => a == '1') == 1)
                    {
						conditionReason = "Exactly 1 binary digit is a 1.";
						var idx1InBinary = binary.IndexOf('1');
						idxForbidTransformStg3 = Enumerable.Range(0, 7).Single(a =>
						Enumerable.Range(0, 4).Any(b => ((6 * b + a) % 7 == idx1InBinary && (b + a) % 7 == initialColourIdx) || ((6 * b + a) % 7 == initialColourIdx && (b + a) % 7 == idx1InBinary)));
					}
					else if (binary.Count(a => a == '0') == 1)
                    {
						conditionReason = "Exactly 1 binary digit is a 0.";
						var idx0InBinary = binary.IndexOf('0');
						idxForbidTransformStg3 = Enumerable.Range(0, 7).Single(a =>
						Enumerable.Range(0, 4).Any(b => ((6 * b + a) % 7 == idx0InBinary && (b + a) % 7 == initialColourIdx) || ((6 * b + a) % 7 == initialColourIdx && (b + a) % 7 == idx0InBinary)));
					}
					else
                    {
						var shiftedBinaries = Enumerable.Range(0, 7).Select(a => binary.Skip(a).Concat(binary.Take(a)).ToList()).ToArray();
						if (shiftedBinaries.Any(a => a.Take(4).Distinct().Count() == 1))
						{
							conditionReason = "By shifting the binary, at least 4 consecutive binary digits of 0s or 1s can be formed.";
							idxForbidTransformStg3 = (initialColourIdx + 3) % 7;
						}
						else if (shiftedBinaries.Any(a => a.SequenceEqual("1010101") || a.SequenceEqual("0101010")))
                        {
							var shiftAmountMin = Enumerable.Range(0, 7).Where(a => shiftedBinaries[a].SequenceEqual("1010101") || shiftedBinaries[a].SequenceEqual("0101010")).Min();
							conditionReason = string.Format("By shifting the binary to the left {0} time(s), a sequence where all binary digits alternate can be formed.", shiftAmountMin);
							idxForbidTransformStg3 = (initialColourIdx + shiftAmountMin) % 7;
						}
						else if (shiftedBinaries.Any(a => a.SequenceEqual("0100111") || a.SequenceEqual("1011000") || a.SequenceEqual("0001101") || a.SequenceEqual("1110010")))
                        {
							var shiftAmountMin = Enumerable.Range(0, 7).Where(a => shiftedBinaries[a].SequenceEqual("0100111") || shiftedBinaries[a].SequenceEqual("1011000") || shiftedBinaries[a].SequenceEqual("0001101") || shiftedBinaries[a].SequenceEqual("1110010")).Min();
							conditionReason = string.Format("By shifting the binary to the left {0} time(s), any one of these two following sequences can be formed: \"ABAABBB\", \"AAABBAB\"", shiftAmountMin);
							idxForbidTransformStg3 = (initialColourIdx + shiftAmountMin * 6) % 7;
						}
						else if (binary.Count(a => a == '0') == 2)
                        {
							var shiftAmountMin = Enumerable.Range(0, 7).Where(a => shiftedBinaries[a][initialColourIdx] == '0').Min();
							var binaryAfterMinShift = shiftedBinaries[shiftAmountMin];
							conditionReason = string.Format("Exactly 2 binary digits are 0s. The minimum amount of left shifts required to align a 0 with the current colour is {0}", shiftAmountMin);
							idxForbidTransformStg3 = Enumerable.Range(0, 7).Single(a => a != initialColourIdx && binaryAfterMinShift[a] == '0');
						}
						else if (binary.Count(a => a == '1') == 2)
                        {
							var shiftAmountMin = Enumerable.Range(0, 7).Where(a => shiftedBinaries[a][initialColourIdx] == '1').Min();
							var binaryAfterMinShift = shiftedBinaries[shiftAmountMin];
							conditionReason = string.Format("Exactly 2 binary digits are 1s. The minimum amount of left shifts required to align a 1 with the current colour is {0}", shiftAmountMin);
							idxForbidTransformStg3 = Enumerable.Range(0, 7).Single(a => a != initialColourIdx && binaryAfterMinShift[a] == '1');
						}
						else
                        {
							conditionReason = string.Format("No other conditions apply, binary can be shifted to form the sequence \"ABBABBA\".");
							var idxValidSequence = Enumerable.Range(0, 7).Single(a => shiftedBinaries[a].SequenceEqual("0110011") || shiftedBinaries[a].SequenceEqual("1001100"));
							idxForbidTransformStg3 = idxValidSequence;
                        }
					}
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
			var initialColourIdx = ((int)DateTime.Now.DayOfWeek + bombInfo.GetStrikes() + bombInfo.GetIndicators().Count()) % 7;
			QuickLog("Stage 3 was activated on {0} with {1} strike(s), the current colour would be {2}", DateTime.Now.DayOfWeek, bombInfo.GetStrikes(), coloursNames[initialColourIdx]);
			QuickLog("The binary generated from the submit button changing colours is {0}", colorIdxSubCycle.Where(a => a != 2).Join(""));
			QuickLog("The forbidden colour to use as the transform is {0}, with the following reason \"{1}\"", coloursNames[idxForbidTransformStg3], conditionReason);
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
		var lettersToDisplay = ApplyEncryptions(distancesBetweenWords.Select(a => curAlphabet[PMod(a - 1, 26)]).Join(""), curAlphabet, displayTransforms, true, true);
		QuickLog("The displayed letters are {0}", lettersToDisplay.Join(", "));
		for (var x = 0; x < lettersToDisplay.Count(); x++)
		{
			displayMeshes[x].text = lettersToDisplay[x].ToString();
			displayMeshes[x].color = coloursTransforms[displayTransforms[x]];
		}
		clearOnEmptyPressed = false;
		inputtedLetters = "";
		idxLetterTransform.Clear();
		HandleUpdateAll();
		submitRenderer.material.color = originalSubmitColor;
		submitTxt.text = "SUBMIT";
		messerScript.intensity = completedStages == 0 ? 0.1f : completedStages == 1 ? 1.5f : 4f;
		messerScript.mixTextures = completedStages >= 2;
		messerScript.offsetTextures = completedStages >= 2;
		messerScript.rescaleTextures = completedStages >= 2;
		messerScript.StepIntensity();
		if (completedStages >= 2)
			messerScript.Intensify(0.05f);
		else
			messerScript.StopIntensify();
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
	string ApplyEncryptions(string word, string alphabet, IEnumerable<int> encodings, bool reversed = false, bool use5Screens = false)
    {
		var output = "";
		var serialNo = bombInfo.GetSerialNumber();
		for (var x = 0; x < encodings.Count() && x < word.Length; x++)
		{
			var idxInCurAlphabet = alphabet.IndexOf(word[x]);
			var y = x;
			switch (encodings.ElementAt(x))
			{
				case 0:
					break;
				case 1:
					idxInCurAlphabet += (reversed ? -1 : 1) * (y + 1);
					break;
				case 2:
					if (x == 0) break;
					idxInCurAlphabet -= reversed ? -(alphabet.IndexOf(word[y - 1]) + 1) : (alphabet.IndexOf(output[y - 1]) + 1);
					break;
				case 3:
					idxInCurAlphabet += (reversed ? -1 : 1) * (alphabet.Contains(serialNo[y]) ? (alphabet.IndexOf(serialNo[y]) + 1) : (serialNo[y] - '0'));
					break;
				case 4:
					{
						var digitsInSerial = bombInfo.GetSerialNumberNumbers();
						var curSerialNoDigit = digitsInSerial.ElementAt(y % digitsInSerial.Count());
						idxInCurAlphabet += (reversed ? -1 : 1) * ((y + 1) % 2 == (bombInfo.GetPortCount() % 2) ? curSerialNoDigit : -curSerialNoDigit);
					}
					break;
				case 5:
					idxInCurAlphabet = 25 - idxInCurAlphabet;
					break;
				case 6:
					idxInCurAlphabet -= (reversed ? -1 : 1) * (bombInfo.GetSerialNumberNumbers().Sum() + (use5Screens ? 5 : 6) - y);
					break;
			}
			output += alphabet[PMod(idxInCurAlphabet, 26)];
		}
		return output;
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
		var inputtedWordAfterTransform = ApplyEncryptions(inputtedLetters, alphabetUsed[completedStages], idxLetterTransform);
		QuickLog("Submitted the following letters: {0}", inputtedLetters);
		QuickLog("With the transforms of each letter: {0}", idxLetterTransform.Select(a => coloursNames[a]).Join(", "));

		QuickLog("Decrypting the inputted display results in {0}.", inputtedWordAfterTransform.Join(""));
		if (completedStages >= 2)
        {
			timer.Stop();
			//timer.Reset();
			StartCoroutine(HandleStage3Transition(true, true));
			return;
        }
		if (inputtedWordAfterTransform == decodedWords[completedStages])
        {
			QuickLog("Advancing to the next stage...");
			mAudio.PlaySoundAtTransform("InputCorrect", transform);
			completedStages++;
			if (completedStages >= 2)
            {
				QuickLog("On stage 3? Alright. Here we go... The timer will be against you.");
				StartCoroutine(HandleStage3Transition());
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
					for (var x = 0; x < stg3Handler.Length; x++)
						StopCoroutine(stg3Handler[x]);
					StartCoroutine(HandleStage3Transition(true, false));
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
	IEnumerator HandleSubmitFlashingColours()
    {
		var idx = 0;
		while (completedStages == 2)
        {
			submitRenderer.material.color = new[] { Color.red, Color.green, Color.gray }[colorIdxSubCycle[idx]];
			yield return new WaitForSeconds(Random.value / 4 + 0.1f);
			idx = (idx + 1) % colorIdxSubCycle.Count;
        }
    }
	IEnumerator HandleSubmitCountdown()
    {
		while (completedStages == 2)
        {
			var timeLeft = Mathf.Clamp((float)(stage3TimeSec - timer.Elapsed.TotalSeconds), 0, stage3TimeSec);
			submitTxt.text = string.Format("SUBMIT {0}:{1}", (int)timeLeft / 60, Mathf.Floor(timeLeft % 60).ToString("00"));
			yield return null;
        }
		submitTxt.text = "SUBMIT";
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
			var inputtedWordAfterTransform = ApplyEncryptions(inputtedLetters, alphabetUsed.Last(), idxLetterTransform);
			isAllCorrect &= inputtedWordAfterTransform == decodedWords[completedStages];
			isAllCorrect &= idxLetterTransform.Distinct().Count() == 6;
			isAllCorrect &= inputtedLetters.Any(a => lettersRequiredFromColour[idxColourLetter].Contains(a));
			isAllCorrect &= !idxLetterTransform.Contains(idxForbidTransformStg3);
			if (idxLetterTransform.Distinct().Count() != 6)
				QuickLog("Number of distinct colours used is {0}, not 6.", idxLetterTransform.Distinct().Count());
			if (!inputtedLetters.Any(a => lettersRequiredFromColour[idxColourLetter].Contains(a)))
				QuickLog("Submitted letters before transforms do not contain a required letter.");
			if (idxLetterTransform.Contains(idxForbidTransformStg3))
				QuickLog("A forbidden colour, {0}, was used.", coloursNames[idxForbidTransformStg3]);
			if (isAllCorrect)
            {
				mAudio.PlaySoundAtTransform("InputCorrect", transform);
				completedStages++;
				moduleSolved = true;
				modSelf.HandlePass();
				inputtedLetters = "";
				idxLetterTransform.Clear();
				HandleUpdateAll();
				messerScript.StopIntensify();
				messerScript.RevertAllAffectedObjects();
				submitRenderer.material.color = originalSubmitColor;
				yield break;
            }
			else
			{
				modSelf.HandleStrike();
				QuickLog("Stepping back to stage 2.");
				completedStages--;
				graceStage = true;
				messerScript.StopIntensify();
				messerScript.RevertAllAffectedObjects();
				HandleStageTransition(completedStages);
			}
		}
		else if (isExiting)
        {
			messerScript.StopIntensify();
			messerScript.RevertAllAffectedObjects();
			completedStages--;
			graceStage = true;
			HandleStageTransition(completedStages);
		}
		else
        {
			graceStage = true;
			HandleStageTransition(completedStages);
			timer = Stopwatch.StartNew();
			stg3Handler = new[] { HandleSubmitFlashingColours(), HandleSubmitCountdown() };
			for (var x = 0; x < stg3Handler.Length; x++)
				StartCoroutine(stg3Handler[x]);
		}
		interactable = true;
		yield break;
    }
	void Update()
    {
		if (timer != null)
		{
			if (completedStages != 2 && timer.IsRunning)
			{
				timer.Stop();
				timer.Reset();
			}
			else if (timer.Elapsed.TotalSeconds >= stage3TimeSec)
			{
				QuickLog("{0} minutes passed. Too much time taken.", stage3TimeSec / 60);
				timer.Stop();
				timer.Reset();
				for (var x = 0; x < stg3Handler.Length; x++)
					StopCoroutine(stg3Handler[x]);
				StartCoroutine(HandleStage3Transition(true, false));
			}
		}
    }
	class PossibleCombination
	{
		public List<char> letters = new List<char>();
		public List<int> idxEncodes = new List<int>();
	}
	List<PossibleCombination> GetAllPossibleCombinations(string expectedWord, string curAlphabet = engAlphabet, IEnumerable<int> idxExcludes = null)
	{
		var output = new List<PossibleCombination>();
		var allowedLettersEach = new List<char>[expectedWord.Length];
		var allowedIdxEach = new List<int>[expectedWord.Length];
		for (var x = 0; x < expectedWord.Length; x++)
		{
			allowedLettersEach[x] = new List<char>();
			allowedIdxEach[x] = new List<int>();
		}
		for (var x = 0; x < 7; x++)
		{
			if (idxExcludes != null && idxExcludes.Contains(x)) continue;
			var y = x;
			var curEncryption = ApplyEncryptions(expectedWord, curAlphabet, Enumerable.Repeat(y, expectedWord.Length), true);
			for (var p = 0; p < expectedWord.Length; p++)
			{
				allowedLettersEach[p].Add(curEncryption[p]);
				allowedIdxEach[p].Add(y);
			}
		}
		//QuickLogDebug("[{0}], [{1}]", allowedLettersEach.Select(a => a.Join()).Join("]["), allowedIdxEach.Select(a => a.Join()).Join("]["));
		output.Add(new PossibleCombination());
		for (var x = 0; x < expectedWord.Length; x++)
		{
			var nextCombination = new List<PossibleCombination>();
			for (int i = 0; i < output.Count; i++)
			{
				PossibleCombination combo = output[i];
				for (var y = 0; y < allowedLettersEach[x].Count && y < allowedIdxEach.Length; y++)
				{
					var newCombo = new PossibleCombination();
					newCombo.letters = new List<char>();
					newCombo.letters.AddRange(combo.letters);
					newCombo.letters.Add(allowedLettersEach[x][y]);
					newCombo.idxEncodes = new List<int>();
					newCombo.idxEncodes.AddRange(combo.idxEncodes);
					newCombo.idxEncodes.Add(allowedIdxEach[x][y]);
					nextCombination.Add(newCombo);
				}
			}
			output.Clear();
			output.AddRange(nextCombination);
		}
		return output;
	}
	List<PossibleCombination> GetLayoutPossibleCombinations(string expectedWord, string curAlphabet = engAlphabet, IEnumerable<int> idxExcludes = null)
	{
		var output = new List<PossibleCombination>();
		var allowedLettersEach = new List<char>[expectedWord.Length];
		var allowedIdxEach = new List<int>[expectedWord.Length];
		for (var x = 0; x < expectedWord.Length; x++)
		{
			allowedLettersEach[x] = new List<char>();
			allowedIdxEach[x] = new List<int>();
		}
		for (var x = 0; x < 7; x++)
		{
			if (idxExcludes != null && idxExcludes.Contains(x)) continue;
			var y = x;
			var curEncryption = ApplyEncryptions(expectedWord, curAlphabet, Enumerable.Repeat(y, expectedWord.Length), true);
			for (var p = 0; p < expectedWord.Length; p++)
			{
				allowedLettersEach[p].Add(curEncryption[p]);
				allowedIdxEach[p].Add(y);
			}
		}
        for (var p = 0; p < expectedWord.Length; p++)
        {
			var newCombo = new PossibleCombination();
			newCombo.letters = allowedLettersEach[p];
			newCombo.idxEncodes = allowedIdxEach[p];
			output.Add(newCombo);
        }
		return output;
	}
#pragma warning disable 414
	private readonly string TwitchHelpMessage = "\"!{0} <ABCDEF>\" [Presses keys in the inputs' positions on a QWERTY keyboard, with * to press submit, and \"-\" to press the clear button. Prepend \"~slowpress\", \"~slowerpress\", \"~fastpress\" to adjust the speed of the buttons being pressed.]";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
    {
		if (!interactable)
        {
			yield return "sendtochaterror The module is not interactable right now. Wait for a moment until the module is ready.";
			yield break;
        }
		var intCmd = command.ToLowerInvariant();
		var rgxMatchPressAdjust = Regex.Match(command, @"^~(slow(er)|fast)press\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		var curPressDelay = 0.1f;
		if (rgxMatchPressAdjust.Success)
        {
			var matchingValue = rgxMatchPressAdjust.Value.ToLowerInvariant();
			intCmd = intCmd.Substring(rgxMatchPressAdjust.Value.Length);
			switch (matchingValue.Trim())
            {
				case "~slowerpress":
					curPressDelay = 2f;
					break;
				case "~slowpress":
					curPressDelay = 1f;
					break;
				case "~fastpress":
					curPressDelay = 0.05f;
					break;
            }
        }
		var nonWSCmdChars = intCmd.Where(a => !char.IsWhiteSpace(a)).ToList();
		var allBtnsToPress = new List<KMSelectable>();
		for (var x = 0; x < nonWSCmdChars.Count; x++)
        {
			var curChar = nonWSCmdChars[x];
			switch (curChar)
            {
				case '*':
					allBtnsToPress.Add(submitSelectable);
					break;
				case '-':
					allBtnsToPress.Add(keyboardSelectable.Last());
					break;
				default:
                    {
						var idxCurCharOnKeyboard = initialKeyboardLayout.ToLowerInvariant().IndexOf(curChar);
						if (idxCurCharOnKeyboard == -1)
                        {
							yield return "sendtochaterror I cannot type in the following character on the module: " + curChar.ToString();
							yield break;
                        }
						allBtnsToPress.Add(keyboardSelectable[idxCurCharOnKeyboard]);
                    }
					break;
            }
        }

		for (var x = 0; x < allBtnsToPress.Count; x++)
		{
			yield return null;
			allBtnsToPress[x].OnInteract();
			if (!interactable)
			{
				yield return "solve";
				yield return "strike";
			}
			yield return "trywaitcancel " + curPressDelay.ToString();
		}
		yield break;
    }
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!moduleSolved && completedStages < 3)
		{
			while (!interactable)
				yield return true;
			var lettersToInput = "";
			var idxRestrictions = new List<int>();
			switch (completedStages)
			{
				case 0:
				case 1:
					{
						var allPossibleCombinations = GetAllPossibleCombinations(decodedWords[completedStages], alphabetUsed[completedStages], completedStages == 0 ? Enumerable.Range(1, 6) : Enumerable.Range(0, 7).Where(a => !possibleTransformsKeyboard.Any(b => b.Contains(a))));
						QuickLogDebug("{0}", allPossibleCombinations.Count);
						var singleCombo = allPossibleCombinations.Single();
						lettersToInput = singleCombo.letters.Join("");
					}
					break;
				case 2:
                    {
						var layoutCombinations = GetLayoutPossibleCombinations(decodedWords.Last(), alphabetUsed.Last());
						QuickLogDebug("[{0}]", layoutCombinations.Select(a => Enumerable.Range(0, a.letters.Count).Select(b => string.Format("{0}({1})", a.letters[b], a.idxEncodes[b])).Join("")).Join("],["));

						var allCombos = PermutationGenerator.GenerateFactorialPermutations(Enumerable.Range(0, 7).Where(a => idxForbidTransformStg3 != a));
						var allowedCombos = new List<IEnumerable<int>>();
						foreach (var combo in allCombos)
                        {
							var stringEncoded = "";
							for (var x = 0; x < combo.Count(); x++)
							{
								var idxCurEncode = layoutCombinations[x].idxEncodes.IndexOf(combo.ElementAt(x));
								stringEncoded += layoutCombinations[x].letters[idxCurEncode];
							}
							var safelyAllowed = true;
							safelyAllowed &= lettersRequiredFromColour[idxColourLetter].Any(a => stringEncoded.Contains(a));
							safelyAllowed &= combo.Distinct().Count() == 6;
							for (var x = 0; x < combo.Count(); x++)
								safelyAllowed &= possibleTransformsKeyboard[keyboardLayout.IndexOf(stringEncoded[x])].Contains(combo.ElementAt(x));
							if (safelyAllowed)
								allowedCombos.Add(combo);
                        }
						if (!allowedCombos.Any())
                        {
							keyboardSelectable.Last().OnInteract();
							yield return new WaitForSeconds(.1f);
							keyboardSelectable.Last().OnInteract();
							yield return new WaitForSeconds(.1f);
							continue;
						}
						//QuickLogDebug("[{0}]", allowedCombos.Select(a => Enumerable.Range(0, a.Count()).Select(b => string.Format("{0}({1})", layoutCombinations[b].letters[a.ElementAt(b)], layoutCombinations[b].idxEncodes[a.ElementAt(b)])).Join()).Join("],["));
						var pickedCombo = allowedCombos.PickRandom();
						idxRestrictions = pickedCombo.ToList();
						QuickLogDebug("[{0}]", Enumerable.Range(0, pickedCombo.Count()).Select(b => string.Format("{0}({1})", layoutCombinations[b].letters[pickedCombo.ElementAt(b)], layoutCombinations[b].idxEncodes[pickedCombo.ElementAt(b)])).Join());
						for (var x = 0; x < pickedCombo.Count(); x++)
						{
							var idxCurEncode = layoutCombinations[x].idxEncodes.IndexOf(pickedCombo.ElementAt(x));
							lettersToInput += layoutCombinations[x].letters[idxCurEncode];
						}
					}
					break;
			}
			if (!inputtedLetters.StartsWith(lettersToInput))
			{
				keyboardSelectable.Last().OnInteract();
				yield return new WaitForSeconds(.1f);
			}
			//QuickLogDebug("{0}", lettersToInput);
			for (var x = 0; x < lettersToInput.Length; x++)
            {
				var idxCurLetter = keyboardLayout.IndexOf(lettersToInput[x]);
				//QuickLogDebug("{0} {1}", idxCurLetter, lettersToInput[x]);
				if (idxRestrictions != null && idxRestrictions.Skip(x).Any())
                {
					while (possibleTransformsKeyboard[idxCurLetter][curIdxTransformKeyboard[idxCurLetter]] != idxRestrictions[x])
                    {
						submitSelectable.OnInteract();
						yield return new WaitForSeconds(.1f);
					}
                }
				keyboardSelectable[idxCurLetter].OnInteract();
				yield return new WaitForSeconds(.1f);
            }
			submitSelectable.OnInteract();
			while (!interactable)
				yield return true;
			yield return null;
        }
    }
}
