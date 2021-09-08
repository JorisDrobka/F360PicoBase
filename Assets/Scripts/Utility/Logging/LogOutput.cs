using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class LogOutput : MonoBehaviour
{
#if !UNITY_EDITOR
    private StreamWriter logWriter;
    void Awake()
    {
        string logsDir = Path.Combine(Application.persistentDataPath, "logs");
        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }
        //only keep a total of 64 logs on the device
        string[] existingLogs = Directory.GetFiles(logsDir);
        if (existingLogs.Length > 64)
        {
            Array.Sort(existingLogs, string.Compare);
            for (var i = 0; i < existingLogs.Length - 64; i++)
            {
                File.Delete(existingLogs[i]);
            }
        }
        logWriter = File.CreateText(Path.Combine(logsDir, "log_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".txt"));
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        logWriter.Close();
        Application.logMessageReceived -= HandleLog;
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Exception:
            case LogType.Error:
            case LogType.Assert:
            case LogType.Warning:
                logWriter.WriteLine(logString);
                logWriter.WriteLine(stackTrace);
                logWriter.WriteLine("");
                break;
            default:
                logWriter.WriteLine(logString);
                logWriter.WriteLine("");
                break;
        }
        logWriter.Flush();
    }
#endif
}
