using UnityEngine;
using System.Collections;
using SimpleJSON;
using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

public partial class ConnectionManager : MonoBehaviour {

    private static ConnectionManager connectionManager = null;
    
    public static ConnectionManager getInstance()
    {
        if (connectionManager == null)
        {
            GameObject go = new GameObject("_connectionManager");
            connectionManager = go.AddComponent<ConnectionManager>();
        }
        return connectionManager;
    }

    void Awake()
    {
        connectionManager = this;
        DontDestroyOnLoad(this.gameObject);
    }
   
    public void ProcessServerRequest(ServerRequest request)
    {
        StackTrace st = new StackTrace();
        StackFrame sf = st.GetFrame(3);
       // UnityEngine.Debug.Log("Caller of server request:" + sf.GetMethod() + "||" + sf.GetMethod().ReflectedType.FullName);
        StartCoroutine(generalRequest(request));
    }

    public void ProcessServerBinaryRequest(ServerRequest request)
    {
        StackTrace st = new StackTrace();
        StackFrame sf = st.GetFrame(3);
        // UnityEngine.Debug.Log("Caller of server request:" + sf.GetMethod() + "||" + sf.GetMethod().ReflectedType.FullName);
        StartCoroutine(generalRequest(request));
    }

    private IEnumerator generalRequest(ServerRequest request)
    {
        WWW www = null;
        if (request.method == ServerRequest.RequestMethod.get)
        {
            www = new WWW(appendUrls(ServerURLS.baseurl , request.partialurl)+"?"+request.getBody());
            //UnityEngine.Debug.Log("ServerRequest - requestid:"+request.id+" - Method:Get -" + appendUrls(ServerURLS.baseurl, request.partialurl) + "?" + request.getBody()+"\n"+request.print());
        }
        else
        {
            WWWForm form = new WWWForm();
            //UnityEngine.Debug.Log("ServerRequest - requestid:" + request.id + "Method:Post -" + appendUrls(ServerURLS.baseurl, request.partialurl) + "?" + request.getBody() + "\n" + request.print());
            if (request.body != null)
            {
				//UnityEngine.Debug.Log ("Keys: " + request.body.Keys.Count);
                foreach (string key in request.body.Keys)
                {
                    if (request.body[key].GetType().IsArray)
                    {
                        form.AddBinaryData(key, (byte[])request.body[key]);
                        UnityEngine.Debug.Log("I Loaded a byte array: " + key);
                    }
                    else
                    {
                        form.AddField(key, request.body[key].ToString());
                    }
                    
                    //UnityEngine.Debug.Log(key + ": " + request.body[key].ToString());
                    //In Later Stages We will Encrypt the Parameters
                    //form.AddField(key, Encrypt(request.body[key].ToString())); 
                }
            }
            //www = new WWW(appendUrls(ServerURLS.baseurl, request.partialurl), form);

            //UnityEngine.Debug.Log("URL: " + ServerURLS.baseurl + request.partialurl);

            //var headers = form.headers;
            //if (!headers.Contains("Content-Type"))
            //{
            //    headers.Add("Content-Type", "application/x-www-form-urlencoded");
            //    headers.Add("Cache-Control", "no-cache");

            //}

            //www = new WWW(ServerURLS.baseurl + request.partialurl, form.data, headers);


            www = new WWW(ServerURLS.baseurl + request.partialurl, form);

        }

        yield return www;

        ServerResponse serverresponse = new ServerResponse(request.id);

        try
        {   
            string responseText = www.text.Trim();
            JSONNode data = null;
            string error;            

            bool errfree = errorFree(www.text.Trim(), out error, out data);

            if (errfree)
            {
				//UnityEngine.Debug.Log("ServerResponse : is error free " + serverresponse.print()); 
                serverresponse.setParams(ServerResponse.ResponseError.ok, data.ToString(), "", www.text);
				//UnityEngine.Debug.Log("ServerResponse : is error free " + serverresponse.print()); 
                //Logger.info("ServerResponse : is error free " + serverresponse.print()); 
            }
            else
            {
				//UnityEngine.Debug.Log("ServerResponse : is not error free " + serverresponse.print()); 
                serverresponse.setParams(ServerResponse.ResponseError.error, "", error, "");
                //Logger.info("ServerResponse : is not error free " + serverresponse.print());         
            }           
            
        }
        catch (System.Exception ex)
        {
            //UnityEngine.Debug.Log("inside exception ");
            //Logger.error("Exception:" + ex.Message + " stacktrace:" +ex.StackTrace+ " RawResponse:"+ "");
            //UnityEngine.Debug.Log("Exception:" + ex.Message + " stacktrace:" + ex.StackTrace + " RawResponse:" + "");
            serverresponse.setParams(ServerResponse.ResponseError.error, ex.Message, "connection error" , ex.Message);
            //request.func(serverresponse);
        }
        request.func(serverresponse);
    }

    protected bool errorFree(string responseText , out string errorMsg, out JSONNode returneddata)
    {
        errorMsg = "";
        returneddata = null;

        JSONNode node = JSON.Parse(responseText);
        double errorCode = 1;

        //UnityEngine.Debug.Log("Reply: " + responseText.Trim());

        if (!string.IsNullOrEmpty(responseText.Trim()) /*&& node["status"] != null*/)
        {
            //errorCode = node["status"].AsDouble;
            //errorMsg = node["errors"].Value == null ? "" : node["errors"].Value;
            //if (errorCode == 0 /*&& (node["value"] != null && !node["value"].Value.Equals( "null"))*/ )
            {
                //string decryptedValue = Decrypt(node["value"]);
                //returneddata = JSON.ParseWithClearQuotes(decryptedValue);
                returneddata = JSON.ParseWithClearQuotes(responseText.Trim());
            }
        }
        
        bool ret = false;
        //if (errorCode == 0)
            ret = true;

        if (returneddata == null)
            returneddata = new JSONNode();

        return ret;
    }

    public string appendUrls(string url1, string url2)
    {
        string returl = "";
        if (url1.Length == 0 || url2.Length == 0)
            return url1 + url2;
        if (url1[url1.Length - 1] == '/' && url2[0] == '/')
        {
            returl = url1 + url2.Substring(1, url2.Length - 1);
        }
        else if ((url1[url1.Length - 1] == '/' && url2[0] != '/') ||
           (url1[url1.Length - 1] != '/' && url2[0] == '/'))
        {
            returl = url1 + url2;
        }
        else if (url1[url1.Length - 1] != '/' && url2[0] != '/')
        {
            returl = url1 + "/" + url2;
        } 

        return returl;
    }
    
    static byte[] bytes = ASCIIEncoding.ASCII.GetBytes("AFDO2309");
    public static string Encrypt(string originalString)
    {
        if (String.IsNullOrEmpty(originalString))
        {
            throw new ArgumentNullException("The string which needs to be encrypted can not be null.");
        }

        DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);

        StreamWriter writer = new StreamWriter(cryptoStream);
        writer.Write(originalString);
        writer.Flush();
        cryptoStream.FlushFinalBlock();
        writer.Flush();

        return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
    }

    public static string Decrypt(string cryptedString)
    {
        if (String.IsNullOrEmpty(cryptedString))
        {
            throw new ArgumentNullException("The string which needs to be decrypted can not be null.");
        }

        DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
        MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(cryptedString));
        CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateDecryptor(bytes, bytes), CryptoStreamMode.Read);
        StreamReader reader = new StreamReader(cryptoStream);

        return reader.ReadToEnd();
    }
}
