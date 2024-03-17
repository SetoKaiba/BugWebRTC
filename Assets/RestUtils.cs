using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RestUtils : SingletonUtil<RestUtils>
{
    public Queue<Tuple<string, Action<Texture>, Action<string, long, string>>> textureRequests = new();
    public int maxTextureRequest = 10;
    public int currentTextureRequest = 0;

    private void Update()
    {
        if (currentTextureRequest < maxTextureRequest && textureRequests.Count > 0)
        {
            var request = textureRequests.Dequeue();
            currentTextureRequest++;
            SendWebRequestTextureInternal(request.Item1, request.Item2, request.Item3);
        }
    }

    public static void SendWebRequestTextForm(string url, Action<string> onCompleted,
        Action<string, long, string> onError,
        WWWForm form = null, Dictionary<string, string> headers = null)
    {
        var www = form == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, form);
        if (headers != null)
        {
            foreach (var keyValuePair in headers)
            {
                www.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
            }
        }

        var request = www.SendWebRequest();
        request.completed += _ => { OnComplete(url, onCompleted, onError, www); };
    }

    public static void SendWebRequestTextJson(string url, Action<string> onCompleted,
        Action<string, long, string> onError,
        string body = null, Dictionary<string, string> headers = null)
    {
        SendWebRequestText(url, onCompleted, onError, body, headers);
    }

    public static void SendWebRequestText(string url, Action<string> onCompleted, Action<string, long, string> onError,
        string body = null, Dictionary<string, string> headers = null, string contentType = "application/json")
    {
        var www = new UnityWebRequest(url, string.IsNullOrEmpty(body) ? "GET" : "POST");
        if (!string.IsNullOrEmpty(body))
        {
            UploadHandler uh = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            uh.contentType = contentType;
            www.uploadHandler = uh;
        }

        www.downloadHandler = new DownloadHandlerBuffer();
        if (headers != null)
        {
            foreach (var keyValuePair in headers)
            {
                www.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
            }
        }

        var request = www.SendWebRequest();
        request.completed += _ => { OnComplete(url, onCompleted, onError, www); };
    }

    public void SendWebRequestTexture(string url, Action<Texture> onCompleted,
        Action<string, long, string> onError)
    {
        textureRequests.Enqueue(new Tuple<string, Action<Texture>, Action<string, long, string>>(url, onCompleted,
            onError));
    }

    public void SendWebRequestTextureInternal(string url, Action<Texture> onCompleted,
        Action<string, long, string> onError)
    {
        var www = UnityWebRequestTexture.GetTexture(url);
        var asyncOperation = www.SendWebRequest();
        asyncOperation.completed += _ =>
        {
            OnComplete(url, onCompleted, onError, www);
            currentTextureRequest--;
        };
    }


    static string Escape(string s)
    {
        s = s.Replace("(", "&#40");
        s = s.Replace(")", "&#41");
        return s;
    }

    private static void OnComplete(string url, Action<Texture> onCompleted, Action<string, long, string> onError,
        UnityWebRequest www)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            onError?.Invoke(www.url, www.responseCode, www.error);
        }
        else
        {
            onCompleted?.Invoke(DownloadHandlerTexture.GetContent(www));
        }
    }

    private static void OnComplete(string url, Action<string> onCompleted, Action<string, long, string> onError,
        UnityWebRequest www)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            onError?.Invoke(www.url, www.responseCode, www.error);
        }
        else
        {
            onCompleted?.Invoke(www.downloadHandler.text);
        }
    }
}