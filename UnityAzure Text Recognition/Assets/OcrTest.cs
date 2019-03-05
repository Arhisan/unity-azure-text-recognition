using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Web;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using System.IO;

public class OcrTest : MonoBehaviour
{
    public RawImage RawImage;
    public Text CameraTitle;
    public Text ResultBlock;
    private int currentCamera = 0;
    WebCamDevice[] camDevices;
    WebCamTexture currentWebCamTex;

    static string VISION_API_SUBSCRIPTION_KEY = "";
    static string VISION_API_BASE_URL = "https://northeurope.api.cognitive.microsoft.com";

    // Start is called before the first frame update
    void Start()
    {
        camDevices = WebCamTexture.devices;
        SwitchCam();
        print(camDevices.Length);
    }

    public void SwitchCam()
    {
        if(camDevices.Length > 0)
        {
            currentWebCamTex?.Stop(); 
            currentCamera = (currentCamera + 1) % camDevices.Length;
            currentWebCamTex = new WebCamTexture(camDevices[currentCamera].name);
            CameraTitle.text = "Device: " + camDevices[currentCamera].name;
            RawImage.texture = currentWebCamTex;
            RawImage.material.mainTexture = currentWebCamTex;
            currentWebCamTex.Play();
        }
        else
        {
            CameraTitle.text = "No camera found.";
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void CallOCR()
    {
        Color[] texData = currentWebCamTex.GetPixels();
        Texture2D takenPhoto = new Texture2D(currentWebCamTex.width, currentWebCamTex.height, TextureFormat.ARGB32, false);
        takenPhoto.SetPixels(texData);
        takenPhoto.Apply();

        byte[] picture = takenPhoto.EncodeToPNG();
        //StartCoroutine(MakeOCRRequest(picture, ResultBlock));
        StartCoroutine(MakeHandwritingRequest(picture, ResultBlock));

    }

    public static IEnumerator MakeHandwritingRequest(byte[] bytes, Text textComponent)
    {
        var headers = new Dictionary<string, string>() {
             {"Ocp-Apim-Subscription-Key", VISION_API_SUBSCRIPTION_KEY },
             {"Content-Type","application/octet-stream"}
        };
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["mode"] = "Printed";
        string uri = VISION_API_BASE_URL + "/vision/v1.0/recognizeText?" + queryString;
        if ((bytes != null) && (bytes.Length > 0))
        {
            WWW www = new WWW(uri, bytes, headers);
            yield return www;

            if (www.error != null)
            {
                textComponent.text = "Error 1: " + www.error;
            }
            else
            {

                string operationLocation = www.responseHeaders["Operation-Location"];
                var headers2 = new Dictionary<string, string>() {
                     {"Ocp-Apim-Subscription-Key", VISION_API_SUBSCRIPTION_KEY }
                };
                string statusRunning = "{\"status\":\"Running\"}";
                string status = "{\"status\":\"Running\"}";
                do
                {
                    System.Threading.Thread.Sleep(500);
                    WWW www2 = new WWW(operationLocation, null, headers2);
                    yield return www2;

                    if (www2.error != null)
                    {
                        textComponent.text = "Error 2: " + www.error;
                    }
                    else
                    {
                        status = www2.text;
                        textComponent.text = "Processing...";
                    }
                } while (status == statusRunning);
                HandwritingAPIResults results = JsonUtility.FromJson<HandwritingAPIResults>(status);
                status = results.ToString();
                textComponent.text = status;
                print(status);

            }
        }
    }
}

[System.Serializable]
public class HandwritingAPIResults
{
    public string status;
    public RecognitionResult recognitionResult;

    override public string ToString()
    {
        string words = "";
        foreach (Line2 line in recognitionResult.lines)
        {
            if (words.Length > 0)
            {
                words += " ";
            }
            words += line.text;
        }
        return words;
    }
}

[System.Serializable]
public class RecognitionResult
{
    public List<Line2> lines;
}

[System.Serializable]
public class Line2
{
    public string text;
}
