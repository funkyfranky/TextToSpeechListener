using Google.Cloud.TextToSpeech.V1Beta1;
using System.Globalization;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json.Nodes;


namespace TextToSpeekListener
{
    [SupportedOSPlatform("windows")]

    public class TextToSpeekListener
    {

        // Default sample rate for creating a WAV file with google
        public static readonly int INPUT_SAMPLE_RATE = 16000;

        private static string GetText(JsonObject job)
        {
            string text = "Hello World";
            var a = job["text"];
            if (a != null)
            {
                text = a.ToString();
            }
            return text;
        }

        private static int GetProvider(JsonObject job)
        {
            int provider = 0;
            var a = job["provider"];
            if (a != null)
            {
                provider = Int16.Parse(a.ToString());
            }
            return provider;
        }

        private static int GetVolumeMicrosoft(JsonObject job)
        {
            int volume = 100;
            var a = job["volume"];
            if (a != null)
            {
                volume = Int16.Parse(a.ToString());
                volume = Math.Min(100, volume);
                volume = Math.Max(0, volume);
            }
            return volume;
        }

        private static int GetVolumeGoogle(JsonObject job)
        {
            // Google uses volume increase in DB in [-96,10] with default 0 DB.
            int volume = 0;
            var a = job["volume"];
            if (a != null)
            {
                volume = Int16.Parse(a.ToString());
                volume = Math.Min(10, volume);
                volume = Math.Max(-96, volume);
            }
            return volume;
        }


        private static string? GetVoice(JsonObject job)
        {
            string? voice = null;
            var a = job["voice"];
            if (a != null)
            {
                voice = a.ToString();
            }
            return voice;
        }

        private static string? GetCulture(JsonObject job)
        {
            string? culture = null;
            var a = job["culture"];
            if (a != null)
            {
                culture = a.ToString();
            }
            return culture;
        }

        private static VoiceGender GetGender(JsonObject job)
        {
            var a = job["gender"];
            if (a != null)
            {
                string gender = a.ToString();
                if (gender.Trim().ToLower() == "male")
                {
                    return VoiceGender.Male;
                }
                else if (gender.Trim().ToLower() == "neutral")
                {
                    return VoiceGender.Neutral;
                }
                else
                {
                    return VoiceGender.Female;
                }
            }
            else
            {
                return VoiceGender.Female;
            }
        }

        private static void StartListener(int listenPort = 11042, bool googleAvail = false)
        {
            UdpClient listener = new(listenPort);
            IPEndPoint groupEP = new(IPAddress.Any, listenPort);


            Console.WriteLine();
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($"Starting listener on port {listenPort}");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine();

            // Create Speech synthesizer instance
            SpeechSynthesizer synth = new();

            try
            {
                while (true)
                {
                    // 
                    Console.Write("Waiting for broadcast...");
                    byte[] bytes = listener.Receive(ref groupEP);

                    // Get message as text
                    string? jsonString = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                    // Check if this comes from MOOSE (string is hard coded as SOCKET.DataType.TTS="moose_text2speech")
                    bool isMoose = jsonString.Contains("moose_text2speech");

                    if (isMoose)
                    {

                        Console.WriteLine();
                        Console.WriteLine($"Received broadcast from {groupEP} :");
                        Console.WriteLine($"{jsonString}");

                        // Create a json object
                        var jsonObject = JsonNode.Parse(jsonString).AsObject();

                        //string server = "Unknown";
                        string? text = GetText(jsonObject);
                        int provider = GetProvider(jsonObject);
                        string? voice = GetVoice(jsonObject);
                        string? culture = GetCulture(jsonObject);
                        VoiceGender gender = GetGender(jsonObject);


                        // Debug output
                        Console.WriteLine($"Text to speech: {text}");


                        if (googleAvail && provider == 1)
                        {

                            // Create a google TTS client instance
                            var client = TextToSpeechClient.Create();

                            // The input to be synthesized, can be provided as text or SSML.
                            var input = new SynthesisInput
                            {
                                //Text = text
                                Ssml = text
                            };

                            VoiceSelectionParams? voiceSelection = null;

                            if (!string.IsNullOrEmpty(voice))
                            {
                                voiceSelection = new VoiceSelectionParams()
                                {
                                    Name = voice,
                                    // csharp_style_prefer_range_operator = false
                                    LanguageCode = voice.Substring(0, 5),
                                };
                            }
                            else
                            {
                                if (culture == null)
                                { culture = "en-US"; }

                                SsmlVoiceGender sgender = SsmlVoiceGender.Unspecified;
                                switch (gender)
                                {
                                    case VoiceGender.Male:
                                        sgender = SsmlVoiceGender.Male;
                                        break;
                                    case VoiceGender.Neutral:
                                        sgender = SsmlVoiceGender.Neutral;
                                        break;
                                    case VoiceGender.Female:
                                        sgender = SsmlVoiceGender.Female;
                                        break;
                                    default:
                                        sgender = SsmlVoiceGender.Male;
                                        break;
                                }

                                voiceSelection = new VoiceSelectionParams
                                {
                                    LanguageCode = culture,
                                    SsmlGender = sgender,  // This does not seem to work correctly
                                };


                            }

                            // Get volume
                            int volume = GetVolumeGoogle(jsonObject);

                            // Specify the type of audio file that is created. We do a wav file and play it.
                            var audioConfig = new AudioConfig
                            {
                                //AudioEncoding = AudioEncoding.Mp3
                                AudioEncoding = AudioEncoding.Linear16, // wav file
                                SampleRateHertz = INPUT_SAMPLE_RATE,
                                VolumeGainDb = volume
                            };

                            // Perform the text-to-speech request.
                            var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

                            // Create temmp file name
                            var tempFile = Path.GetTempFileName();

                            // Write the response to the output file.
                            using (var output = File.Create(tempFile))
                            {
                                response.AudioContent.WriteTo(output);
                            }

                            // Debug message
                            Console.WriteLine($"Speaking Google: voice={voice}, culture={culture}, gender={gender}, volumeGain={volume} DB");
                            //Console.WriteLine($"Audio content written to file \"{tempFile}\"");

                            // Play sound file
                            //SoundPlayer soundfile = new SoundPlayer(tempFile);
                            SoundPlayer soundfile = new(tempFile);
                            soundfile.PlaySync(); // PlaySync means that once sound start then no other activity if form will occur untill sound goes to finish

                            // Delete temp file
                            File.Delete(tempFile);

                        }
                        else
                        {
                            // Get volume
                            int volume = GetVolumeMicrosoft(jsonObject);

                            // Select voice
                            if (voice == null || voice.Length == 0)
                            {
                                // No explicit voice ==> select by hints (culture and/or gender)
                                if (culture == null)
                                {
                                    //Console.WriteLine($"Selecting voice from hint gender={gender}");
                                    // Get voice with given gender (no culture)
                                    synth.SelectVoiceByHints(gender, VoiceAge.Adult);
                                }
                                else
                                {
                                    // Get voice by given gender and culture
                                    synth.SelectVoiceByHints(gender, VoiceAge.Adult, 0, new CultureInfo(culture, false));
                                }
                            }
                            else
                            {
                                // Voice has been given explicitly
                                try
                                {
                                    synth.SelectVoice(voice);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Could not get specific voice {voice}: {ex.Message}");
                                }

                            }

                            // Set volume
                            synth.Volume = volume;

                            // Debug message
                            Console.WriteLine($"Speaking Microsoft: voice={voice}, culture={culture}, gender={gender}, volume={volume}");

                            // Speak text
                            synth.Speak(text);

                        }

                        // Write line
                        Console.WriteLine();
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }

        private static void ShowVoicesSystem()
        {
            // Initialize a new instance of the SpeechSynthesizer.  
            using (SpeechSynthesizer synth = new())
            {

                // Output information about all of the installed voices.   
                Console.WriteLine("Installed voices Microsoft:");
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    Console.WriteLine($"- {info.Name}, Culture: {info.Culture}, Gender: {info.Gender}, Age: {info.Age}");
                }
                Console.WriteLine();
            }

        }

        private static bool ShowVoicesGoogle()
        {
            try
            {
                var client = TextToSpeechClient.Create();
                var response = client.ListVoices("en-US");
                Console.WriteLine("Installed voices Google (only first 10 en-US):");
                int N = 0;
                foreach (var voice in response.Voices)
                {
                    Console.WriteLine($"- {voice.Name}, Gender={voice.SsmlGender}; Culture(s): {string.Join(", ", voice.LanguageCodes)}");
                    N++;
                    if (N >= 10) break;
                }
                Console.WriteLine();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with Google Text to Speech: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        public static void Main()
        {
            // Greetings
            Console.WriteLine("Text-To-Speech Listener");
            Console.WriteLine("=======================");
            Console.WriteLine();

            // Show installed system voices
            ShowVoicesSystem();

            // Show insalled google voices. This also checks if google is available at all
            bool googleAvail = ShowVoicesGoogle();

            // User input of port
            Console.Write("Listen on port (hit enter for default 11042): ");
            string? port = Console.ReadLine();
            int listenPort = 11042;
            if (port is not null && port.Length > 0)
            {
                listenPort = Int16.Parse(port);
            }

            // Start UDP listener
            StartListener(listenPort, googleAvail);
        }
    }
}