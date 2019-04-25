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
    public string apiKey = "AIzaSyAKi-0enymTn0cegyvPx0BvvW2RTXhALeQ";
    public AudioSource globalAudio;

    private string _defaultDevice;
    private bool _recording;
    private AudioClip _audioClip;
    private Text _buttonText;
    private string _apiUrl;
    private BattleStateMachine _bsm;

    void Start()
    {
        _apiUrl = string.Format("https://speech.googleapis.com/v1/speech:recognize?alt=json&key={0}", apiKey);

//        StartCoroutine(Upload());

        foreach (var device in Microphone.devices)
        {
            _defaultDevice = device;
        }
        _buttonText = button.transform.Find("Text").GetComponent<Text>();
        _bsm = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
    }

    void Update()
    {
        // Detects changes in recording status
        if (_recording != Microphone.IsRecording(_defaultDevice))
        {
            // If recording already
            if (_recording)
            {
                // Then analyze
                Analyze();
            }
            _recording = Microphone.IsRecording(_defaultDevice);
        }
    }

    public void RecordOrAnalyze()
    {
        if (!_recording)
        {
            _audioClip = Microphone.Start(_defaultDevice, false, 5, 16000);
            globalAudio.volume = 0.1f;
            Debug.Log("enregistre");
        }
        else
        {
            Debug.Log("analyze1");
            Analyze();
        }
    }

    private void Analyze()
    {
        Microphone.End(_defaultDevice);
        Debug.Log("analyze2");
        _buttonText.text = "Analyse en cours...";
        var filenameRand = Random.Range(0.0f, 10.0f);

        var filename = string.Format("testing{0}.wav", filenameRand);

        var filePath = Path.Combine("testing/", filename);
        filePath = Path.Combine(Application.persistentDataPath, filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        SavWav.Save(filePath, _audioClip); //Save a temporary Wav File

        StartCoroutine(UploadSound(filePath));
//        StartCoroutine(Upload());
        globalAudio.volume = 1f;
        _recording = false;
    }

    IEnumerator Upload()
    {
//        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
//        {
//            new MultipartFormDataSection("email","peter@klaven"),
////            new MultipartFormDataSection("password","cityslicka"),
////            new MultipartFormDataSection("movies", new string[]{"I Love You Man", "Role Models"}),
////            new MultipartFormDataSection("job", "leader"),
////            new MultipartFormDataSection("movies", "[I Love You Man,Role Models]"),
////            new MultipartFormFileSection("my file data", "myfile.txt")
//        };
//
//        UnityWebRequest www = UnityWebRequest.Post("https://reqres.in/api/login", formData);
//        yield return www.SendWebRequest();
// 
//        if(www.isNetworkError || www.isHttpError) {
//            Debug.Log(www.error);
//        }
//        else {
//            Debug.Log(www.downloadHandler.text);
//        }


        WWWForm form = new WWWForm();
        form.AddField("email", "peter@klaven");
        form.AddField("password", "cityslicka");

        UnityWebRequest www = UnityWebRequest.Post("https://reqres.in/api/login",
            "{\"email\":\"pater@klaven\", \"password\":\"cityslicka\" }");
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.downloadHandler.text);
        }
        else
        {
            User user = gameObject.AddComponent<User>();
            JsonUtility.FromJsonOverwrite(www.downloadHandler.text, user);
//            Debug.Log(www.downloadHandler.text);
            Debug.Log(user.token);
        }
        www.downloadHandler.Dispose();

//        User u1 = gameObject.AddComponent<User>();
//        u1.token = "QpwL5tke4Pnpja7X";
//        Debug.Log(JsonUtility.ToJson(u1));
    }

    private string json(string base64)
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

        byte[] data = Encoding.UTF8.GetBytes(json(file64));
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
            foreach (var alternative in result["alternatives"].Children)
            {
                var transcripts = alternative["transcript"].ToString();

                var toAnalyze = transcripts.Split('"')[1].Split(' ');

                HandleTurn newTurn = new HandleTurn
                {
                    Type = "Hero"
                };
                BaseHero hsm = null;
                BaseEnemy esm = null;
                foreach (var word in toAnalyze)
                {
                    GameObject hero, enemy;
                    BaseAttack attack;
                    if (_bsm.heroesToManage.HeroExists(word, out hero, ref hsm))
                    {
                        newTurn.AttackerGO = hero;
                        newTurn.Attacker = hsm.theName;
                    }
                    else if (_bsm.enemiesInBattle.EnemyExists(word, out enemy, ref esm))
                    {
                        newTurn.AttackerTarget = enemy;
                    }
                    else if (hsm != null && hsm.attacks.AttackExists(word, out attack))
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
                    if (newTurn.IsConsistent())
                    {
                        Debug.Log("Consistent !");
                        Debug.Log(newTurn);
                        break;
                    }
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

                    Debug.Log(transcripts);
                    Debug.Log(newTurn);
                    _buttonText.text = "Erreur, réessayez";
                }
            }
        }
    }
}

[Serializable]
internal class User : MonoBehaviour
{
    public string token;
    public string error;
}