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

## DCS Demo Mission

To be announced.
