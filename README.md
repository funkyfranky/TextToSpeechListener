# TextToSpeechListener

The text-to-speech listener (TTSL) is a C# console app. It will listen for data transferred via a UDP socket.
The incoming data is expected to be in JSON format. The contained text will be synthesized to speech via the Microsoft or Google TTS engines.

The common use case is that another program (or game) sends the text data via the UDP protocol. The listener receives the data and converts it into speech.

Note that this program is closely related to Ciribob's DCS-External-Audio program but it does not use any radio frequencies or SRS running.
One major advantage is, that the listener console app is constantly running. There is no "pop-up" window, each time a TTS package is received.

NOTE:
* For google you have to set the Windows environment variable `GOOGLE_APPLICATION_CREDENTIALS` to point to google crendentials file.
* The default UDP port of the listener is 11042. But it can be configured to use any other port. 
* The TTS messages are played synchronously, *i.e.* they are played one after the other and do not overlap each other.

![image](https://user-images.githubusercontent.com/28947887/234686638-79272a70-aaff-426d-9d01-6252bf779a1b.png)

## Download and Installation
Download the executable from the releases section here on github.

There is no installation required. Just run the `.exe` file. It is rather large because it should contain all necessary dependencies.

### Google Engine
In order to use the Google text-to-speech engine you first need to set it up correctly. See https://cloud.google.com/text-to-speech

Once you have a google credentials file, you have to store it as an Windows evironment variable.
Go to `System Properties` --> `Advanced` Tab --> `Environment Variables` --> `New`
and add a new variable named `GOOGLE_APPLICATION_CREDENTIALS`, which has as value the full path to your credentials file.

![Sysprop_GoogleTTS](https://user-images.githubusercontent.com/28947887/235353363-2628270d-f1cf-47c5-a73b-fca70f5cfd10.png)

If the google engine is not available, the program will fall back to the Microsoft engine.

## MOOSE
The TTS listener can be easily used with the MOOSE framework to use text-to-speech from DCS.

This is an simple demo script that you could run:
```
-- Set up a UDP socket using the default listener port 11042.
local tts=SOCKET:New(11042)

-- Default: Microsoft voice
tts:SendTextToSpeech("Hello, I am the default Microsoft voice.")

-- Use a specific Microsoft voice.
tts:SendTextToSpeech("Hello, I am Hazel from Microsoft. Hope you have me installed.", nil, "Microsoft Hazel Desktop")

-- Use any Google voice.
tts:SendTextToSpeech("Hello, I am your default Google voice. If you have not set up Google correctly, I might fall back to a Microsoft voice though.", 1)

-- Set a specific voice.
tts:SendTextToSpeech("I am the standard C Google voice. I speak English with an american accent.", 1, "en-US-Standard-C")
```

### DCS Demo Mission

A demo mission can be found as part of the release package.

## JSON
This is an example how the send JSON data could look like:
```json
{
    "command": "moose_text2speech",
    "text": "This is the text that is converted to speech.",
    "provider": 0,
    "voice": "Microsoft David Desktop",
    "culture": "en-US",
    "gender": "male",
    "volume": 100
}
```
* `command`: This must be "moose_text2speech" for the listener to know that this received data was meant to be converted to speech.
* `text`: This is the text that is converted to speech.
* `provider`: 0=Microsoft (default), 1=Google TTS engine
* `voice`: (Optional) The explicit voice to use. If not set, other `culture` and/or `gender` are used to select the voice.
* `culture`: (Optional) The languange code, *e.g.* "en-US", "en-GB", "de-DE", ...
* `gender`: (Optional) Can be "male", "female" (default) or "neutral".
* `volume`: (Optional) For Microsoft a value in [0,100] (default 100). For Google [-96, 10] as the volume gain in DB (default 0).

