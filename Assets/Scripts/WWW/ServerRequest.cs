using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerRequest {

    public enum RequestMethod{
        get,
        post
    }
    public RequestMethod method;

    public string partialurl;
   
    public delegate void ServerCallback(ServerResponse response);
    public ServerCallback func = null;

    public Dictionary<string, object> body = null; 
    public int id;

    public ServerRequest(ServerCallback func, int id, Dictionary<string, object> body,
        RequestMethod method, string partialurl)
    {
        this.partialurl = partialurl;
        this.func = func;
        this.body = body;
        this.id = id;
        this.method = method;     
    }

    public string getBody()
    {
        if (body == null)
            return "";

        string temp = "";
        foreach(string key in body.Keys)
        {
            //Debug.Log("Key: " + key);
            if(!temp.Equals(""))
                temp += "&";
            object value = body[key];
            temp += key +"="+value.ToString();
        }
        //Debug.Log("Bodey: " + temp);
        return temp;
    }

    public string print()
    {
        return "ID:"+id+" || Url:"+partialurl+" || Method:"+method+" || Body:"+(body == null? "null value": getBody());
    }

}


public class ServerResponse {
    
    public enum ResponseError
    {
        ok = 0,
        error = 1
    }
    public int id;
    public string responsetext;
    public string errortext;
    public ResponseError responseError;
    public string rawResponse;
    
    public ServerResponse(int id)
    {
        this.id = id;        
        this.errortext = "";
        this.responsetext = "";
        this.rawResponse = "";
        this.responseError = ResponseError.error;
    }

    public void setParams(ResponseError error, string subject, string errortext, string rawResponse)
    {
        this.responsetext = subject; 
        this.responseError = error; 
        this.errortext = errortext;
        this.rawResponse = rawResponse;
    }

    public string print()
    {
        return "ID:" + id + " || Error:" + responseError + " || RawResponse:" + rawResponse + " || errortext:" + errortext+" || responseText:"+responsetext;
    }
}
