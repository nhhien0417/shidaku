using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class ReadCSVFile
{
    public static CSVFile ReadCSV(string data)
    {
        var result = new CSVFile();
        var lines = data.Split("\n");

        if (lines.Length > 1)
        {
            var keys = lines[0].Split(",");
            keys[^1] = keys[^1].TrimEnd('\r');
            
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;
                
                var values = lines[i].Split(",");
                var dict = new Dictionary<string, string>();
                for (var j = 0; j < values.Length; j++)
                {
                    dict[keys[j]] = values[j].TrimEnd('\r');
                }
                result.Data.Add(dict);
            }
        }
        else
        {
            Debug.LogError("Incorrect CSV format!");
        }
        
        
        return result;
    }
    
    public static CSVFileJson ReadCSVJson(string data)
    {
        var result = new CSVFileJson();
        var lines = data.Split("\n");

        if (lines.Length > 1)
        {
            var keys = lines[0].Split(",");
            keys[^1] = keys[^1].TrimEnd('\r');
            
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                var values = lines[i].Split(",");
                var jObj = new JSONObject();
                var jObjSimplified = new JSONObject();
                for (var j = 0; j < values.Length; j++)
                {
                    var v = values[j].TrimEnd('\r');
                    jObj.Add(keys[j], v);

                    if (!string.IsNullOrEmpty(v))
                    {
                        jObjSimplified.Add(keys[j], v);
                    }
                }
                result.Data.Add(jObj);
                result.SimplifiedData.Add(jObjSimplified);
            }
        }
        else
        {
            Debug.LogError("Incorrect CSV format!");
        }
        
        return result;
    }
    
    public class CSVFile
    {
        public List<Dictionary<string,string>> Data = new List<Dictionary<string,string>>();

        public string GetValue(string key, int rowIndex)
        {
            if (rowIndex < Data.Count && rowIndex >= 0)
            {
                var d = Data[rowIndex];
                if (d.ContainsKey(key))
                {
                    return d[key];
                }
            }

            return "";
        }

        public void PrintDebug()
        {
            var finalString = "";
            foreach (var d in Data)
            {
                var s = "{";
                foreach (var pair in d)
                {
                    s += String.Format("{0}:{1}, ", pair.Key, pair.Value);
                }
                s += "}";
                finalString += s + "\n";
            }
            Debug.Log(finalString);
        }
    }

    public class CSVFileJson
    {
        public JSONArray Data = new ();
        public JSONArray SimplifiedData = new ();
        
        public void PrintDebug(bool showFullData = false)
        {
            Debug.Log(showFullData ? Data.ToString() : SimplifiedData.ToString());
        }
    }
}
