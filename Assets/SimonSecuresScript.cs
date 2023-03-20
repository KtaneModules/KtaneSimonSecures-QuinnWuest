using KeepCoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using RNG = UnityEngine.Random;

public class SimonSecuresScript : ModuleScript
{
    public MeshRenderer screen;
    public ButtonManager rButton, sButton, kButton;
    public TextMesh buttonText;

    private string ScreenWord;

    private float switchTime, lastMash;
    private int mashes;
    private bool turned;

    private Command expected = Command.Hold;
    private int commandVar = -1, stages = 0;
    private bool wrong;

    private Coroutine mashCounter;

    private enum Command
    {
        Hold,
        Mash,
        Turn
    }

    void Start()
    {
        rButton.OnPress += () => { RPress(); PlaySound(KMSoundOverride.SoundEffect.BigButtonPress); };
        rButton.OnRelease += () => { PlaySound(KMSoundOverride.SoundEffect.BigButtonRelease); };
        sButton.OnPress += () => { switchTime = Time.time; PlaySound(KMSoundOverride.SoundEffect.ButtonPress); };
        sButton.OnRelease += () => { SRelease(); PlaySound(KMSoundOverride.SoundEffect.ButtonRelease); };
        kButton.OnPress += () => { if(turned) return; PlaySound("Keyturn"); turned = true; KPress(); };

        Texture2D tex;
        var colors = ((Texture2D)screen.material.mainTexture).GetPixels();
        screen.material.mainTexture = tex = new Texture2D(18, 5);
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        ScreenWord = "SIMON SAYS: \"DETONATE!\" ";
        StartCoroutine(MoveScreen());

        Get<KMBombModule>().OnActivate += () => { Generate(); };
    }

    private void RPress()
    {
        if(IsSolved)
            return;
        lastMash = Time.time;
        mashes++;
        if(mashCounter == null)
            mashCounter = StartCoroutine(MashCounter());
    }

    private IEnumerator MashCounter()
    {
        while(Time.time - lastMash < 1f)
            yield return null;
        if((expected == Command.Mash && mashes == commandVar) ^ wrong)
            Generate();
        else
            Incorrect("You mashed the button {0} times.".Form(mashes));
        mashes = 0;
        mashCounter = null;
    }

    private void KPress()
    {
        if(IsSolved)
            return;
        if((expected == Command.Turn && Mathf.FloorToInt(Get<KMBombInfo>().GetTime()) % 10 == commandVar) ^ wrong)
            Generate();
        else
        {
            Incorrect("You turned the key on a {0}.".Form(Mathf.FloorToInt(Get<KMBombInfo>().GetTime()) % 10));
            if(!wrong && expected == Command.Turn && Mathf.FloorToInt(Get<KMBombInfo>().GetTime()) % 10 != commandVar)
                Generate(false);
        }
    }

    private void SRelease()
    {
        if(IsSolved)
            return;
        if((expected == Command.Hold && Mathf.FloorToInt(Time.time - switchTime) == commandVar) ^ wrong)
            Generate();
        else
            Incorrect("You held the lever for {0} seconds.".Form(Mathf.FloorToInt(Time.time - switchTime)));
    }

    private void Incorrect(string log)
    {
        Strike("You messed it up! " + log);
    }

    private void Generate(bool inc = true)
    {
        if(!inc)
        {
            Log("Since you struck, this new display will not count towards you stage count.");
            stages--;
        }
        if(stages++ >= 5)
        {
            Solve("Good job!");
            Get<Animator>().SetBool("Open", true);
            StartCoroutine(RefreshDisplay("!SOLVED!   "));
            PlaySound(KMSoundOverride.SoundEffect.CorrectChime);
            stages = 1;
            return;
        }
        Log("Generating new display...");

        if(RNG.Range(0, 5) == 0)
            wrong = true;
        else
            wrong = false;

        string start = "SIMON SAYS: ";
        string wraps = "\"";
        if(wrong)
        {
            switch(RNG.Range(0, 8))
            {
                case 0:
                    start = "BOB SAYS: ";
                    break;
                case 1:
                    start = "SIMON SAYS- ";
                    break;
                case 2:
                    start = "SIMON SAYS; ";
                    break;
                case 3:
                    start = "SIMON SAYS ";
                    break;
                case 4:
                    start = "SIMON SAYS. ";
                    break;
                case 5:
                    wraps = "";
                    break;
                case 6:
                    wraps = "'";
                    break;
                case 7:
                    start = "";
                    break;
            }
        }

        commandVar = -1;

        string command = "ERROR";
        switch(RNG.Range(0, turned ? 2 : 3))
        {
            case 0:
                commandVar = RNG.Range(2, 7);
                expected = Command.Hold;
                command = "HOLD THE LEVER FOR {0} SECONDS.".Form(commandVar);
                break;
            case 1:
                commandVar = RNG.Range(3, 8);
                expected = Command.Mash;
                command = "MASH THE BUTTON {0} TIMES.".Form(commandVar);
                break;
            case 2:
                commandVar = RNG.Range(0, 10);
                expected = Command.Turn;
                command = "UNLOCK THE LOCK ON A {0}.".Form(commandVar);
                break;
        }

        StartCoroutine(RefreshDisplay(start + wraps + command + wraps + "   "));
        Log(start + wraps + command + wraps);
    }

    private IEnumerator RefreshDisplay(string v)
    {
        ScreenWord = Enumerable.Repeat(" ", 70).Join(" ");
        yield return new WaitForSeconds(delay * 18f);
        ScreenWord = v;
    }

    private static readonly Dictionary<char, bool[][]> LetterDisplays = "ABCDEFGHIJKLMNOPQRSTUVWXYZ:\".! '-;0123456789".ToDictionary(c => c, c =>
    {
        const bool V = true;
        const bool F = false;
        switch(c)
        {
            case 'A':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, F }, new bool[] { V, V, V, V, V } };
            case 'B':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, V }, new bool[] { F, V, F, V, F } };
            case 'C':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V }, new bool[] { V, F, F, F, V } };
            case 'D':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V }, new bool[] { F, V, V, V, F } };
            case 'E':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, V }, new bool[] { V, F, V, F, V } };
            case 'F':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, F }, new bool[] { V, F, V, F, F } };
            case 'G':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V }, new bool[] { V, F, V, V, V } };
            case 'H':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, F, V, F, F }, new bool[] { V, V, V, V, V } };
            case 'I':
                return new bool[][] { new bool[] { V, F, F, F, V }, new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V } };
            case 'J':
                return new bool[][] { new bool[] { F, F, F, V, V }, new bool[] { F, F, F, F, V }, new bool[] { V, V, V, V, V } };
            case 'K':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, F, V, F, F }, new bool[] { V, V, F, V, V } };
            case 'L':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, F, F, F, V }, new bool[] { F, F, F, F, V } };
            case 'M':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, V, F, F, F }, new bool[] { V, V, V, V, V } };
            case 'N':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, V, V, V, F }, new bool[] { V, V, V, V, V } };
            case 'O':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V }, new bool[] { V, V, V, V, V } };
            case 'P':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, F }, new bool[] { V, V, V, F, F } };
            case 'Q':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, V, V }, new bool[] { V, V, V, V, V } };
            case 'R':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, F }, new bool[] { F, V, F, V, V } };
            case 'S':
                return new bool[][] { new bool[] { V, V, V, F, V }, new bool[] { V, F, V, F, V }, new bool[] { V, F, V, V, V } };
            case 'T':
                return new bool[][] { new bool[] { V, F, F, F, F }, new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, F } };
            case 'U':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, F, F, F, V }, new bool[] { V, V, V, V, V } };
            case 'V':
                return new bool[][] { new bool[] { V, V, V, V, F }, new bool[] { F, F, F, F, V }, new bool[] { V, V, V, V, F } };
            case 'W':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { F, F, F, V, F }, new bool[] { V, V, V, V, V } };
            case 'X':
                return new bool[][] { new bool[] { V, V, F, V, V }, new bool[] { F, F, V, F, F }, new bool[] { V, V, F, V, V } };
            case 'Y':
                return new bool[][] { new bool[] { V, V, F, F, F }, new bool[] { F, F, V, V, V }, new bool[] { V, V, F, F, F } };
            case 'Z':
                return new bool[][] { new bool[] { V, F, F, V, V }, new bool[] { V, F, V, F, V }, new bool[] { V, V, F, F, V } };
            case ':':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { F, V, F, V, F }, new bool[] { F, F, F, F, F } };
            case '"':
                return new bool[][] { new bool[] { V, V, F, F, F }, new bool[] { F, F, F, F, F }, new bool[] { V, V, F, F, F } };
            case '.':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { F, F, F, F, V }, new bool[] { F, F, F, F, F } };
            case '!':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { V, V, V, F, V }, new bool[] { F, F, F, F, F } };
            case ' ':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { F, F, F, F, F }, new bool[] { F, F, F, F, F } };
            case '\'':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { V, V, F, F, F }, new bool[] { F, F, F, F, F } };
            case '-':
                return new bool[][] { new bool[] { F, F, V, F, F }, new bool[] { F, F, V, F, F }, new bool[] { F, F, V, F, F } };
            case ';':
                return new bool[][] { new bool[] { F, F, F, F, F }, new bool[] { F, V, F, V, V }, new bool[] { F, F, F, F, F } };
            case '0':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, F, F, V }, new bool[] { V, V, V, V, V } };
            case '1':
                return new bool[][] { new bool[] { F, V, F, F, V }, new bool[] { V, V, V, V, V }, new bool[] { F, F, F, F, V } };
            case '2':
                return new bool[][] { new bool[] { V, F, V, V, V }, new bool[] { V, F, V, F, V }, new bool[] { V, V, V, F, V } };
            case '3':
                return new bool[][] { new bool[] { V, F, V, F, V }, new bool[] { V, F, V, F, V }, new bool[] { V, V, V, V, V } };
            case '4':
                return new bool[][] { new bool[] { V, V, V, F, F }, new bool[] { F, F, V, F, F }, new bool[] { V, V, V, V, V } };
            case '5':
                return new bool[][] { new bool[] { V, V, V, F, V }, new bool[] { V, F, V, F, V }, new bool[] { V, F, V, V, V } };
            case '6':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, V }, new bool[] { V, F, V, V, V } };
            case '7':
                return new bool[][] { new bool[] { V, F, F, F, F }, new bool[] { V, F, F, F, F }, new bool[] { V, V, V, V, V } };
            case '8':
                return new bool[][] { new bool[] { V, V, V, V, V }, new bool[] { V, F, V, F, V }, new bool[] { V, V, V, V, V } };
            case '9':
                return new bool[][] { new bool[] { V, V, V, F, V }, new bool[] { V, F, V, F, V }, new bool[] { V, V, V, V, V } };
        }
        throw new Exception("Invalid character found!");
    });

    private const float delay = 0.06f;

    private IEnumerator MoveScreen()
    {
        const int FlickerLen = 40;
        int i = 0;
        int k = 0;
        for(int j = 0; j < RNG.Range(0, ScreenWord.Length); j++)
        {
            char Letter = ScreenWord[i];
            UpdateScreen(LetterDisplays[Letter][0]);
            UpdateScreen(LetterDisplays[Letter][1]);
            UpdateScreen(LetterDisplays[Letter][2]);
            UpdateScreen(new[] { false, false, false, false, false });
            i++;
            i %= ScreenWord.Length;
            k++;
            k %= FlickerLen;
        }
        while(true)
        {
            i++;
            i %= ScreenWord.Length;
            k++;
            k %= FlickerLen;
            char Letter = ScreenWord[i];
            UpdateScreen(LetterDisplays[Letter][0]);
            FlickerScreen(k);
            yield return new WaitForSeconds(delay);
            k++;
            k %= FlickerLen;
            UpdateScreen(LetterDisplays[Letter][1]);
            FlickerScreen(k);
            yield return new WaitForSeconds(delay);
            k++;
            k %= FlickerLen;
            UpdateScreen(LetterDisplays[Letter][2]);
            FlickerScreen(k);
            yield return new WaitForSeconds(delay);
            k++;
            k %= FlickerLen;
            UpdateScreen(new[] { false, false, false, false, false });
            FlickerScreen(k);
            yield return new WaitForSeconds(delay);
        }
    }

    private void FlickerScreen(int k)
    {
        if(Enumerable.Range(0, stages).SelectMany(ix => new int[] { ix * 3, ix * 3 + 1 }).Contains(k))
            screen.material.color = new Color(.7f, .7f, .7f);
        else
            screen.material.color = new Color(1f, 1f, 1f);
    }

    private void UpdateScreen(bool[] Lit)
    {
        Color fColor = new Color(0.286f, 0.161f, 0.020f, 1.000f);
        Color tColor = new Color(0.973f, 0.635f, 0.247f, 1.000f);
        Texture2D tex = (Texture2D)screen.material.mainTexture;
        Color[] pixels = tex.GetPixels(1, 0, 17, 5);
        tex.SetPixels(0, 0, 17, 5, pixels);
        tex.SetPixels(17, 0, 1, 5, Lit.Select(b => b ? tColor : fColor).Reverse().ToArray());
        tex.Apply();
        screen.material.mainTexture = tex;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} hold|mash|unlock 7' to hold the lever, mash the button, or unlock the lock.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if(Regex.IsMatch(command.Trim().ToLowerInvariant(), @"\s*j\s*"))
        {
            yield return null;
            buttonText.text = "J";
            yield return "sendtochat j";
            yield break;
        }
        if(!(m = Regex.Match(command.Trim().ToLowerInvariant(), "(hold|mash|unlock)\\s+(\\d)")).Success)
            yield break;
        yield return null;
        int v = int.Parse(m.Groups[2].Value);
        switch(m.Groups[1].Value)
        {
            case "hold":
                Get<KMSelectable>().Children[1].OnInteract();
                yield return new WaitForSeconds(v + 0.5f);
                Get<KMSelectable>().Children[1].OnInteractEnded();
                break;
            case "mash":
                for(int i = 0; i < v; i++)
                {
                    Get<KMSelectable>().Children[0].OnInteract();
                    Get<KMSelectable>().Children[0].OnInteractEnded();
                    yield return new WaitForSeconds(0.2f);
                }
                break;
            case "unlock":
                while((int)Get<KMBombInfo>().GetTime() % 10 != v)
                    yield return "trycancel";
                Get<KMSelectable>().Children[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
                Get<KMSelectable>().Children[2].OnInteractEnded();
                break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while(!IsSolved)
        {
            Command rExpected = expected;
            if(wrong)
                switch(expected)
                {
                    case Command.Hold:
                        rExpected = Command.Mash;
                        break;
                    case Command.Mash:
                        rExpected = Command.Hold;
                        break;
                    case Command.Turn:
                        rExpected = Command.Hold;
                        break;
                }
            switch(rExpected)
            {
                case Command.Hold:
                    Get<KMSelectable>().Children[1].OnInteract();
                    yield return new WaitForSeconds(commandVar + 0.5f);
                    Get<KMSelectable>().Children[1].OnInteractEnded();
                    break;
                case Command.Mash:
                    for(int i = 0; i < commandVar; i++)
                    {
                        Get<KMSelectable>().Children[0].OnInteract();
                        Get<KMSelectable>().Children[0].OnInteractEnded();
                        yield return new WaitForSeconds(0.1f);
                    }
                    break;
                case Command.Turn:
                    while(Mathf.FloorToInt(Get<KMBombInfo>().GetTime()) % 10 != commandVar)
                        yield return true;
                    Get<KMSelectable>().Children[2].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    Get<KMSelectable>().Children[2].OnInteractEnded();
                    break;
            }
            yield return true;
            float time = Time.time;
            while(Time.time - time < 3f)
                yield return true;
        }
    }
}