using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Authenticate : MonoBehaviour
{
    // Use this for initialization
    string authURL = "https://developer.api.autodesk.com/authentication/v1/authenticate";       //Used for authentication purposes
    string sceneURL = "https://developer.api.autodesk.com/photo-to-3d/v1/photoscene";           //Used for creating a scene
    string fileURL = "https://developer.api.autodesk.com/photo-to-3d/v1/file";                  //Used for uploading files to the created scene  
    string processSceneURL = "https://developer.api.autodesk.com/photo-to-3d/v1/photoscene/";   //Used for processing scene files
    string client_id = "";
    string client_secret = "";
    string grant_type = "client_credentials";
    string scope = "data:read data:write";
    string accessToken;
    public List<Texture2D> Images = new List<Texture2D>();
    public string photoSceneId;
    int start = 0;
    int end = 0;
    bool firstTime = true;
    void Start()
    {
        StartCoroutine(oAuth());

    }

    IEnumerator oAuth()
    {
        WWWForm formData = new WWWForm();
        formData.AddField("client_id", client_id);
        formData.AddField("client_secret", client_secret);
        formData.AddField("grant_type", grant_type);
        formData.AddField("scope", scope);
        UnityWebRequest www = UnityWebRequest.Post(authURL, formData);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("Something got broken");
        }
        else
        {
            print(www.responseCode);
            Debug.Log("Upload");

        }

        AccessKey AK = new AccessKey();
        AK = JsonUtility.FromJson<AccessKey>(www.downloadHandler.text.ToString());
        accessToken = "Bearer " + AK.access_token;
        print(accessToken);

        if (accessToken != null)
            StartCoroutine(createScene());

    }


    IEnumerator createScene()
    {
        WWWForm sceneData = new WWWForm();
        sceneData.AddField("scenename", "adaw");
        sceneData.AddField("format", "obj");
        UnityWebRequest create = UnityWebRequest.Post(sceneURL, sceneData);
        print("here  " + accessToken);
        create.SetRequestHeader("Authorization", accessToken);
        yield return create.SendWebRequest();

        if (create.isNetworkError || create.isNetworkError)
        {
            Debug.Log("Could not create photoscene Id");
        }
        else
        {
            print(create.responseCode);
            Debug.Log("Operation Sucessfull");
            Debug.Log(create.downloadHandler.text);
            string sceneId = create.downloadHandler.text.ToString();
            start = sceneId.LastIndexOf(":");
            end = sceneId.LastIndexOf("}");
            sceneId = sceneId.Substring(start + 2, end - start - 4);
            print(sceneId);
            photoSceneId = sceneId;
            if (photoSceneId != null)
                StartCoroutine(uploadImages(0,0));
        }

    }
    /*
    IEnumerator uploadImages()
    {
        WWWForm uploads = new WWWForm();
        uploads.AddField("photosceneid", photoSceneId);
        uploads.AddField("type", "image");
        uploads.AddField("file[0]", "https://drive.google.com/open?id=1b3Tp2UGDLtqxn8G73JD9NxcBS5vQFncb");
        uploads.AddField("file[1]", "https://drive.google.com/open?id=1TGYfju3dH7VzHJSafaA3gDgzN7ChEncj");
        uploads.AddField("file[2]", "https://drive.google.com/open?id=1lIkqPXceopVbEuN6k3GThbKW0c65z77Z");


        UnityWebRequest create = UnityWebRequest.Post(fileURL, uploads);
        create.SetRequestHeader("Authorization", accessToken);
        yield return create.SendWebRequest();

        if (create.isNetworkError || create.isNetworkError)
        {
            Debug.Log("Could not create photoscene Id");
        }

        else
        {
            print(create.responseCode);
            print(create.downloadHandler.text);
            print("One Iteration done");
            StartCoroutine(startProcessing());

        }

    }
    */

    
        IEnumerator uploadImages(int beginIndex, int endIndex)
        {
            beginIndex = endIndex;
            if (!firstTime)
                beginIndex++;
            firstTime = false;
            endIndex += 2;
            if (endIndex >= Images.Count - 1)
                endIndex = Images.Count - 1;

            WWWForm uploads = new WWWForm();
            uploads.AddField("photosceneid", photoSceneId);
            uploads.AddField("type", "image");
            print("Range: Start = " + beginIndex + " End = " + endIndex);
            for (int i = beginIndex; i <= endIndex; i++)
            {
                byte[] bytes = Images[i].EncodeToJPG();
                string fieldName = string.Format("file[{0}]", i);
            print(fieldName);
                uploads.AddBinaryData(fieldName, bytes,fieldName + ".jpg","image/jpg");
            }
            UnityWebRequest create = UnityWebRequest.Post(fileURL, uploads);
            create.SetRequestHeader("Authorization", accessToken);
            yield return create.SendWebRequest();

            if (create.isNetworkError || create.isNetworkError)
            {
                Debug.Log("Could not create photoscene Id");
            }

            else
            {
                print(create.responseCode);
                print(create.downloadHandler.text);
                print("One Iteration done");
                beginIndex = endIndex;
                if (endIndex < Images.Count - 1)
                    StartCoroutine(uploadImages(beginIndex, endIndex));
                else
                {

                    for(int i = 0; i < 5; i++)
                {
                    print("Images uploaded sucessfully");
                }
                    StartCoroutine(startProcessing());
                }
            }

        }


    IEnumerator startProcessing()
    {
        processSceneURL += photoSceneId;
        WWWForm uploads = new WWWForm();
        UnityWebRequest create = UnityWebRequest.Post(processSceneURL, uploads);
        create.SetRequestHeader("Authorization", accessToken);
        yield return create.SendWebRequest();

        if (create.isNetworkError || create.isNetworkError)
        {
            Debug.Log("Could not create photoscene Id");
        }

        else
        {
            print(create.responseCode);
            Debug.Log("Operation Sucessfull");
            Debug.Log(create.downloadHandler.text);

            StartCoroutine(startPolling());


        }
    }

    IEnumerator startPolling()
    {
        string poll = String.Concat(processSceneURL, "/progress");
        print(poll);
        UnityWebRequest create = UnityWebRequest.Get(poll);
        create.SetRequestHeader("Authorization", accessToken);
        yield return create.SendWebRequest();
        if (create.isNetworkError || create.isNetworkError)
        {
            Debug.Log("Couldn't poll photoscene id");
        }

        else
        {
            Debug.Log(create.downloadHandler.text);
            string progressDetails = JsonHelper.GetJsonObject(create.downloadHandler.text.ToString(), "Photoscene");
            Photoscenes ps = JsonUtility.FromJson<Photoscenes>(progressDetails);
            print(ps.progress);
            if (ps.progress == "100")
                StartCoroutine(startDownloading());
            else
            {
                yield return new WaitForSeconds(5);
                StartCoroutine(startPolling());
            }

        }
       

    }

    IEnumerator startDownloading()
    {
        processSceneURL += "?format=obj";
        WWWForm www = new WWWForm();
        www.AddField("format", "obj");
        UnityWebRequest create = UnityWebRequest.Get(processSceneURL);
        create.SetRequestHeader("Authorization", accessToken);
        yield return create.SendWebRequest();
        if (create.isNetworkError || create.isNetworkError)
        {
            Debug.Log("Couldn't poll photoscene id");
        }

        else
        {
            Debug.Log(create.responseCode);
            Debug.Log(create.downloadHandler.text);
            string responseString = create.downloadHandler.text;
            Debug.Log(responseString);
            int len = responseString.Length;
            Debug.Log(len);
            int index = responseString.IndexOf("\"scenelink\":\"") + "\"scenelink\":\"".Length;
            Debug.Log(index);
            responseString = responseString.Substring(index, len - index - 1);
            Debug.Log(responseString);
            int index2 = responseString.IndexOf("\"");
            string scenelink = responseString.Substring(0, index2);
            scenelink = scenelink.Replace("\\", "");
            Debug.Log(scenelink);
            StartCoroutine(downloadFile(scenelink));
            /*
            string sceneObject = JsonHelper.GetJsonObject(create.downloadHandler.text.ToString(), "Photoscene");
            print(sceneObject);

            SceneInfo SI = JsonUtility.FromJson<SceneInfo>(sceneObject);
            print(SI.scenelink);
            StartCoroutine(downloadFile(SI.scenelink));
            */

        }

    }
    



    IEnumerator downloadFile(string URI)
    {
        UnityWebRequest dl =  UnityWebRequest.Get(URI);
        yield return dl.SendWebRequest();

        if (dl.isNetworkError || dl.isHttpError)
        {
            Debug.Log(dl.error);
        }
        else
        {
            print(dl.responseCode);
            // Show results as text
            Debug.Log(dl.downloadHandler.text);

            // Or retrieve results as binary data
            byte[] results = dl.downloadHandler.data;
            System.IO.File.WriteAllBytes(Application.persistentDataPath, results);




        }

    }




}

public class AccessKey
{
    public string token_type;
    public string expires_in;
    public string access_token;
}

public class Photoscene
{
    public string photosceneid;
}

public class RootObject
{
    public string Usage;
    public string Resource;
    public Photoscene Photoscene;
}



[Serializable]
public class Photoscenes
{
    public string photosceneid ;
    public string progressmsg ;
    public string progress ;
}
[Serializable]

public class ProgressBar
{
    public string Usage ;
    public string Resource ;
    public Photoscenes ps = new Photoscenes() ;
}


public class Urn
{
}

public class Resultmsg
{
}

public class SceneInfo
{
    public string photosceneid ;
    public string progressmsg ;
    public string progress ;
    public string scenelink ;
    public Urn urn ;
    public string filesize ;
    public Resultmsg resultmsg ;
}

public class Link
{
    public string Usage ;
    public string Resource ;
    public SceneInfo SI = new SceneInfo();
}