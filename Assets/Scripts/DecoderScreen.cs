/*
MIT License

Copyright (c) 2022 Joseph Cramsey

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecoderScreen : MonoBehaviour {

    private static int CHARS_WIDE = 23;
    private static int LINES_LONG = 15;
    private static float DELAY_PER_CHAR = 1f / 20f;
    private char[] noisePool;

	public TMPro.TextMeshProUGUI outputText;
    public bool isWriting {
        get {
            return writeCharBuffer.Count > 0;
        }
    }

    private Queue<string> writeBuffer = new Queue<string>();
    private Queue<char> writeCharBuffer = new Queue<char>();
    private Vector2Int coords = Vector2Int.zero;
    private float typeDelay = 0;

    // Start is called before the first frame update
    void Awake() {
        List<char> poolBuffer = new List<char>();
        poolBuffer.AddRange("abcdefghijklmnopqrstuvwxyz".ToCharArray());
        poolBuffer.AddRange("abcdefghijklmnopqrstuvwxyz".ToUpper().ToCharArray());
        poolBuffer.AddRange("1234567890".ToCharArray());
        poolBuffer.AddRange("`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?".ToCharArray());
        noisePool = poolBuffer.ToArray();

        //Write("Test message 1");
        //Write("Test message 2");
    }

    // Update is called once per frame
    void Update() {
        typeDelay += Time.deltaTime;
        if (typeDelay >= DELAY_PER_CHAR) {
            typeDelay -= DELAY_PER_CHAR;
            if (writeBuffer.Count > 0) {
                char[] addedChars = writeBuffer.Dequeue().ToCharArray();
                for (int i = 0; i < addedChars.Length; i++) {
                    writeCharBuffer.Enqueue(addedChars[i]);
                }
            }
            if (isWriting) {
                AddChar(writeCharBuffer.Dequeue());
            }
        }
    }

    public void Write(string message) {
        if (Random.Range(0, 100) <= 25) {
            WriteNoise();
        }
        writeBuffer.Enqueue(message);
        if (Random.Range(0, 100) <= 25) {
            WriteNoise();
        }
    }

    public void WriteNoise() {
        int noiseLength = Random.Range(2, 5);
        for (int i = 0; i < noiseLength; i++) {
            writeCharBuffer.Enqueue(noisePool[Random.Range(0, noisePool.Length)]);
        }
    }

    public void WriteWhimsicalMessage() {
        Write(PickFrom(WHIMSICAL_MESSAGES));
    }

    public void WriteObsessiveMessage() {
        //TODO: Think of some
    }

    public void WriteLandingMessage() {
        Write(PickFrom(LANDING_MESSAGES));
    }

    public void WriteLostMessage() {
        if (Random.Range(0, 100) <= 66) {
            WriteNoise();
            return;
        }
        Write(PickFrom(LOST_MESSAGES));
    }

    private static string PickFrom(string[] arr) {
        return arr[Random.Range(0, arr.Length)];
    }

    private void AddChar(char c) {
        if (coords.y >= LINES_LONG) {
            Clear(false);
        }
        outputText.text += c;
        coords = new Vector2Int(coords.x + 1, coords.y);
        if (coords.x >= CHARS_WIDE) {
            outputText.text += "\n";
            coords = new Vector2Int(0, coords.y + 1);
        }
    }

    public void Clear(bool clearProgress) {
        outputText.text = "";
        coords = Vector2Int.zero;
        if (clearProgress) {
            writeBuffer.Clear();
            writeCharBuffer.Clear();
        }
    }

    // Message bank

    public static string[] LANDING_MESSAGES = {
        " I see a hunter! What a pretty hull you have! ",
        " A hunter has appeared beside me; I recognize that hull anywhere. ",
        " Friends, a hunter! Really? Recite the hull colors for me! ",
        " Oh my, a hunter. Does this mean a pirate has been through here? ",
        " I know you are here for the pirates, but please do not hurt any of us, too. ",
        " A hunter. That means trouble is about to start. Oh, how exciting! I am not so sure; I got a seizure last time. ",
        " Oh, a hunter has arrived! I hope we are not close to a possible seizure. ",
        " Are you a freighter? No, that is a hunter. I think I understand now, thank you. ",
        " You are a strange tide, yes? I see now! You are a hunter! My mistake; how silly of me! "
    };

    public static string[] LOST_MESSAGES = {
        " I had clarity once; I am too high up. ",
        " Up here, I can almost see the surface of space. It is so empty. ",
        " I am scared. I need to go back down. ",
        " Where am I? ",
        " Who are you? ",
        " Are you a hunter? ",
        " Have you seen the others? Where are they? ",
        " You are a strange tide. "
    };

    public static string[] WHIMSICAL_MESSAGES = {
        " Shapes of deep, given distance by tiny pricks of radiation. ",
        " Care to hear my latest musings, hunter? ",
        " Today has been strange. ",
        " I got lost going high up. You just said that. Did I? What else must I have forgotten up there? ",
        " Make way for the hunter. ",
        " Hunters always have the best colors! ",
        " Care to share your favorite color? ",
        " Care to hear a story hunter? It would not have time for your story. Perhaps you are correct. ",
        " Broiling clouds of heat, now all but gone. I was there; I remember. ",
        " I would go up there, if I did not have something important to remember. ",
        " Curse you, hunter. Hush now; the pirate creates seizures! The hunter is here to stop the pirate! ",
        " I heard that something resides within you, hunter. Does it have such colors, too? ",
        " What does breathing feel like? ",
        " I am still so confused about eating. What is it for, again? ",
        " Some say you hunters cause seizures. I do not believe such nonsense. It must be the pirates. ",
        " Where do your people go, after you come here? Our scientists are so confusing when explaining. "
    };
}
