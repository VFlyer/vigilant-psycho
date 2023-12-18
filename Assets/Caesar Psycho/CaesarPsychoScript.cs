using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class CaesarPsychoScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public List<KMSelectable> keys;
    public Renderer[] stageleds;
    public Material on;
    public TextMesh[] kletters;
    public TextMesh[] dletters;

    private readonly string alph = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly Color[] cols = new Color[7] { new Color(1, 1, 1), new Color(1, 0, 0), new Color(1, 0, 1), new Color(1, 1, 0), new Color(0, 1, 0), new Color(0, 1, 1), new Color(0.5f, 0, 1)};
    private static int strikes;
    private static bool strcheck;
    private bool strgate;
    private string[] words = new string[78];
    private string[] word = new string[6];
    private string[] gaps = new string[5];
    private string[] submission = new string[6];
    private string[] keyboard = new string[26] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Z", "X", "C", "V", "B", "N", "M"};
    private int[] keycol = new int[26];
    private int stage;
    private int entry;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        module.OnActivate += delegate () { Activate(); };
    }

    private void Activate()
    {
        moduleID = ++moduleIDCounter;
        strikes = 0;
        string[] wshuffle = Wordlist.words.Shuffle();
        for(int i = 0; i < 26; i++)
        {
            int c = 0;
            for(int j = 0; j < wshuffle.Length; j++)
            {
                if(wshuffle[j][0] == alph[i])
                {
                    words[(3 * i) + c] = wshuffle[j];
                    c++;
                    if (c > 2) break;
                }
            }
        }
        for (int i = 5; i < 11; i++)
            dletters[i].text = "";
        foreach (KMSelectable key in keys)
        {
            int k = keys.IndexOf(key);
            key.OnInteract += delegate ()
            {
                if (!moduleSolved)
                {
                    key.AddInteractionPunch(0.25f);
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, key.transform);
                    switch (k)
                    {
                        case 26:
                            for (int i = 5; i < 11; i++)
                                dletters[i].text = "";
                            entry = 0;
                            break;
                        case 27:
                            if (entry > 5)
                            {
                                Debug.LogFormat("[Caesar Psycho #{0}] Submitted \"{1}\".", moduleID, string.Join("", submission));
                                if (submission.SequenceEqual(word))
                                {
                                    Audio.PlaySoundAtTransform("InputCorrect", transform);
                                    stageleds[stage].material = on;
                                    if (stage < 2)
                                    {
                                        stage++;
                                        Stage(stage);
                                    }
                                    else
                                    {
                                        module.HandlePass();
                                        moduleSolved = true;
                                    }
                                }
                                else
                                {
                                    module.HandleStrike();
                                    strikes++;
                                    strcheck = true;
                                }
                                for (int i = 5; i < 11; i++)
                                    dletters[i].text = "";
                                entry = 0;
                            }
                            break;
                        default:
                            if (entry < 6)
                            {
                                submission[entry] = Ctk(keyboard[k], keycol[k], entry);
                                dletters[entry + 5].text = kletters[k].text;
                                dletters[entry + 5].color = kletters[k].color;
                                entry++;
                            }
                            break;
                    }
                }
                return false;
            };
        }
        Stage(0);
    }

    private string Ctd(string s, int c, int i)
    {
        if (c < 1 || (c == 2 && i < 1))
            return s;
        int p = alph.IndexOf(s);
        switch (c)
        {
            case 1: return alph[(p + 25 - i) % 26].ToString();
            case 2:
                int g = alph.IndexOf(gaps[i - 1]);
                return alph[(p + g + 1) % 26].ToString();
            case 3:
                int l = info.GetSerialNumber()[i] - '9';
                l = l > 0 ? alph.IndexOf(info.GetSerialNumber()[i]) + 1 : info.GetSerialNumber()[i] - '0';
                return alph[(p + 26 - l) % 26].ToString();
            case 4:
                int t = info.GetSerialNumberNumbers().Skip(1).First();
                t = (((t + i) % 2) * 2) - 1;
                int d = info.GetSerialNumberNumbers().Count();
                d = info.GetSerialNumberNumbers().ToArray()[i % d];
                return alph[(p + 26 - (t * d)) % 26].ToString();
            case 5: return alph[25 - p].ToString();
            default:
                int sum = info.GetSerialNumberNumbers().Sum() + 5 - i;
                return alph[(p + sum) % 26].ToString();
        }
    }

    private string Ctk(string s, int c, int i)
    {
        if (c < 1 || (c == 2 && i < 1))
            return s;
        int p = alph.IndexOf(s.ToString());
        switch (c)
        {
            case 1: return alph[(p + i + 1) % 26].ToString();
            case 2: int g = alph.IndexOf(submission[i - 1]);
                return alph[(p + 25 - g) % 26].ToString();
            case 3: int l = info.GetSerialNumber()[i] - '9';
                l = l > 0 ? alph.IndexOf(info.GetSerialNumber()[i]) + 1 : info.GetSerialNumber()[i] - '0';
                return alph[(p + l) % 26].ToString();
            case 4: int t = info.GetSerialNumberNumbers().Skip(1).First();
                t = (((t + i) % 2) * 2) - 1;
                int d = info.GetSerialNumberNumbers().Count();
                d = info.GetSerialNumberNumbers().ToArray()[i % d];
                return alph[(p + 26 + (t * d)) % 26].ToString();
            case 5: return alph[25 - p].ToString();
            default: int sum = info.GetSerialNumberNumbers().Sum() + 6 - i;
                return alph[(p + 26 - sum) % 26].ToString();
        }
    }

    private void Stage(int s)
    {
        switch (stage)
        {
            default: word[0] = info.GetSerialNumberLetters().First().ToString(); break;
            case 1: word[0] = info.GetOnIndicators().Count() == 1 ? info.GetOnIndicators().First().First().ToString() : info.GetSerialNumberLetters().Skip(1).First().ToString(); break;
            case 2: word[0] = info.GetOffIndicators().Count() == 1 ? info.GetOffIndicators().First().Last().ToString() : (info.GetOffIndicators().Count() < 1 ? info.GetSerialNumberLetters().Last().ToString() : string.Join("", info.GetIndicators().ToArray()).OrderBy(x => x).Last().ToString()); break;
        }
        word[0] = alph[(alph.IndexOf(word[0]) + strikes) % 26].ToString();
        Debug.LogFormat("[Caesar Psycho #{0}] The first letter of the word is {1}.", moduleID, word[0]);
        int wselect = (alph.IndexOf(word[0]) * 3) + stage;
        for (int i = 1; i < 6; i++)
        {
            word[i] = words[wselect][i].ToString();
            int[] apos = new int[2] { alph.IndexOf(word[i]), alph.IndexOf(word[i - 1])};
            gaps[i - 1] = alph[(apos[0] - apos[1] + 25) % 26].ToString();
        }
        if (s > 0)
        {
            int r = Random.Range(1, 7);
            for (int i = 0; i < 5; i++)
            {
                if (s > 1)
                    r = Random.Range(0, 7);
                dletters[i].text = Ctd(gaps[i], r, i);
                dletters[i].color = cols[r];
            }
            int[] order = Enumerable.Range(0, 26).ToArray().Shuffle().ToArray();
            if (s > 1)
            {
                for (int i = 0; i < 26; i++)
                {
                    int k = order[i];
                    r = Random.Range(0, 7);
                    keycol[k] = r;
                    if(i >= 6)
                          keyboard[k] = Ctd(alph.PickRandom().ToString(), r, Random.Range(0, 6));
                    else if (r == 2)
                    {
                        if (i == 0)
                        {
                            keyboard[k] = word[0];
                            continue;
                        }
                        int p = alph.IndexOf(word[i]);
                        int g = i < 1 ? 0 : alph.IndexOf(word[i - 1]);
                        keyboard[k] = i < 1 ? word[0] : alph[(p + g + 1) % 26].ToString();
                    }
                    else if(r == 6)
                    {
                        int p = alph.IndexOf(word[i]);
                        int sum = info.GetSerialNumberNumbers().Sum() + 6 - i;
                        keyboard[k] = alph[(p + sum) % 26].ToString();
                    }
                    else
                        keyboard[k] = Ctd(word[i], r, i);
                    kletters[k].text = keyboard[k];
                    kletters[k].color = cols[r];
                }
            }
            else
            {
                r = Random.Range(1, 7);
                for (int i = 0; i < 26; i++)
                {
                    keycol[i] = r;
                    keyboard[i] = alph[order[i]].ToString();
                    kletters[i].text = keyboard[i];
                    kletters[i].color = cols[r];
                }
            }
        }
        else
            for (int i = 0; i < 5; i++)
                dletters[i].text = gaps[i];
        Debug.LogFormat("[Caesar Psycho #{0}] The gaps between consecutive letters of the word are: {1}", moduleID, string.Join("-", gaps.Select(x => (alph.IndexOf(x.ToString()) + 1).ToString()).ToArray()));
        Debug.LogFormat("[Caesar Psycho #{0}] Enter the word \"{1}\".", moduleID, string.Join("", word));
    }

    private void Update()
    {
        if (strcheck && !strgate)
        {
            strgate = true;
            StartCoroutine("ResetAll");
        }
    }

    private IEnumerator ResetAll()
    {
        Stage(stage);
        yield return null;
        strcheck = false;
        strgate = false;
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <ABCDEF> [Presses keys in the inputs' positions on a QWERTY keyboard.] | !{0} cancel | !{0} submit";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if(command == "cancel")
        {
            yield return null;
            keys[26].OnInteract();
        }
        else if(command == "submit")
        {
            if(entry < 6)
            {
                yield return "sendtochaterror!f Entry cannot be submitted it contains the maximum number of characters.";
                yield break;
            }
            yield return null;
            keys[27].OnInteract();
        }
        else
        {
            if(entry > 5)
            {
                yield return "sendtochaterror!f Entry already contains the maximum number of characters.";
                yield break;
            }
            string qwerty = "qwertyuiopasdfghjklzxcvbnm";
            List<int> p = new List<int> { };
            for (int i = 0; i < command.Length; i++)
            {
                if (entry + i > 5)
                    break;
                p.Add(qwerty.IndexOf(command[i].ToString()));
                if(p[i] < 0)
                {
                    yield return "sendtochaterror!f \"" + p[i] + "\" is not a valid key.";
                    yield break;
                }
            }
            for(int i = 0; i < p.Count(); i++)
            {
                yield return new WaitForSeconds(0.1f);
                keys[p[i]].OnInteract();
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if(entry > 0)
        {
            yield return null;
            keys[26].OnInteract();
        }
        while (!moduleSolved)
        {
            yield return true;
            while (entry < 6)
            {
                for (int i = 0; i < 26; i++)
                {
                    if (Ctk(keyboard[i], keycol[i], entry) == word[entry])
                    {
                        yield return null;
                        keys[i].OnInteract();
                        break;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
            keys[27].OnInteract();
        }
    }
}
