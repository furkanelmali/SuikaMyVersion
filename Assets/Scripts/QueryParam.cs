using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryParam : MonoBehaviour
{
    void Start()
    {
        string url = Application.absoluteURL;
        Debug.Log("Current URL: " + url);

        Dictionary<string, string> queryParams = GetQueryParams(url);


        foreach (KeyValuePair<string, string> param in queryParams)
        {
            Debug.Log("Key: " + param.Key + ", Value: " + param.Value);
        }
    }


    Dictionary<string, string> GetQueryParams(string url)
    {
        Uri uri = new Uri(url);
        string query = uri.Query;
        Dictionary<string, string> queryParams = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(query))
        {
            string[] pairs = query.TrimStart('?').Split('&');
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = Uri.UnescapeDataString(keyValue[0]);
                    string value = Uri.UnescapeDataString(keyValue[1]);
                    queryParams[key] = value;
                }
            }
        }

        return queryParams;
    }
}
