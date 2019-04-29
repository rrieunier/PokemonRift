using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GoogleSpeech : MonoBehaviour
{
    public Button button;
    public string apiKey;

    private string _defaultDevice;
    private bool _recording;
    private AudioClip _audioClip;
    private Text _buttonText;

    public Material notRecording, recording;

    private string _apiUrl;
    private BattleStateMachine _bsm;

    void Start()
    {
        _apiUrl = string.Format("https://speech.googleapis.com/v1/speech:recognize?alt=json&key={0}", apiKey);

        iPhoneSpeaker.ForceToSpeaker();

        foreach (var device in Microphone.devices)
        {
            _defaultDevice = device;
        }
        _buttonText = button.transform.Find("Text").GetComponent<Text>();
        _bsm = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
    }

    void Update()
    {
        // Catch changes in recording status
        if (_recording != Microphone.IsRecording(_defaultDevice))
        {
            // If recording already
            if (_recording)
            {
                // Then analyze
                Analyze();
                button.image.material = notRecording;
                _buttonText.color = Color.black;
            }
            else
            {
                button.image.material = recording;
                _buttonText.color = Color.white;
            }
            _recording = Microphone.IsRecording(_defaultDevice);
        }
    }

    public void RecordOrAnalyze()
    {
        if (!_recording)
            Record();
        else
            Analyze();
    }

    private void Record()
    {
        _audioClip = Microphone.Start(_defaultDevice, false, 5, 16000);
        _bsm.globalAudio.volume = 0.1f;
    }

    private void Analyze()
    {
        Microphone.End(_defaultDevice);
        _bsm.globalAudio.volume = 1f;
        iPhoneSpeaker.ForceToSpeaker();
        _buttonText.text = "Analyse en cours...";
        var filenameRand = Random.Range(0.0f, 10.0f);

        var filename = string.Format("testing{0}.wav", filenameRand);

        var filePath = Path.Combine("testing/", filename);
        filePath = Path.Combine(Application.persistentDataPath, filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        SavWav.Save(filePath, _audioClip); //Save a temporary Wav File

        StartCoroutine(UploadSound(filePath));
    }

    private static string Json(string base64)
    {
        var json =
            "{\"config\": {\"languageCode\": \"fr-FR\",\"model\": \"command_and_search\"},\"audio\": {\"content\": \"" +
            base64 + "\"}}";
        return json;
    }

    private IEnumerator UploadSound(string file)
    {
        ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

        var bytes = File.ReadAllBytes(file);
        var file64 = Convert.ToBase64String(bytes, Base64FormattingOptions.None);

        UnityWebRequest www = new UnityWebRequest(_apiUrl, "POST");

        byte[] data = Encoding.UTF8.GetBytes(Json(file64));
        www.uploadHandler = new UploadHandlerRaw(data) {contentType = "application/json"};
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        _buttonText.text = "Traitement";

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.downloadHandler.text);
            _buttonText.text = "Erreur, réessayez";
        }
        else
        {
            Analyze(www.downloadHandler.text);
        }
        www.downloadHandler.Dispose();

        File.Delete(file);
    }

    private void Analyze(string response)
    {
        var jsonResponse = JSON.Parse(response);

        if (jsonResponse == null) return;
        foreach (var result in jsonResponse["results"].Children)
        {
            var newTurn = new HandleTurn {Type = "Hero"};

            foreach (var alternative in result["alternatives"].Children)
            {
                var transcripts = alternative["transcript"].ToString();

                var toAnalyze = transcripts.Split('"')[1].Split(' ');

                BaseHero hsm = null;
                BaseEnemy esm = null;
                foreach (var word in toAnalyze)
                {
                    GameObject hero, enemy;
                    BaseAttack attack;
                    if (_bsm.heroesToManage.HeroExists(word, out hero, ref hsm))
                    {
                        newTurn.AttackerGO = hero;
                        newTurn.Attacker = hsm.Name;
                    }
                    else if (_bsm.enemiesInBattle.EnemyExists(word, out enemy, ref esm))
                    {
                        newTurn.AttackerTarget = enemy;
                    }
                    else if (hsm != null && hsm.Attacks.AttackExists(word, out attack))
                    {
                        newTurn.Attack = attack;
                    }
//                    else
//                    {
//                        switch (word)
//                        {
//                            case "attack":
//                            case "item":
//                            case "pokemon":
//                                break;
//                        }
//                    }
                    if (newTurn.IsConsistent()) break;
                }
                if (newTurn.IsConsistent())
                {
                    _bsm.VoiceInput(newTurn);
                    _buttonText.text = "Enregistrer";
                }
                else
                {
                    if (!newTurn.AttackerTarget)
                    {
                        newTurn.AttackerTarget = _bsm.enemiesInBattle[0];
                        if (newTurn.IsConsistent())
                        {
                            _bsm.VoiceInput(newTurn);
                            _buttonText.text = "Enregistrer";
                            return;
                        }
                    }

                    _buttonText.text = "Erreur, réessayez";
                }
            }
        }
    }
}