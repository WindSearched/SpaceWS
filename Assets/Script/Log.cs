using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Log
{
    private readonly object lockObj = new object();
    public StreamWriter file;


    public Log()
    {
        Start();
    }
    private void Start()
    {
        if (!Data.DirectioryExists(ct.setting.logPath))
            Data.DirectoryCreate(ct.setting.logPath);
            
        var ts = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        lock (lockObj)
        {
            file = new StreamWriter(Path.Combine(ct.setting.logPath, ts + ".log"),true);
        }
        file.AutoFlush = true;
        file.WriteLine($"Start to log in {ts} of {Application.productName}, version {Application.version}; created by {Application.companyName}");
    }

    public void Write(string text)
    {
        if (file == null) return;

        lock (lockObj) // 保证多线程安全
        {
            try
            {
                file.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to write log: " + ex.Message);
            }
        }
    }

    public void Write(string logger, string text)
    {
        Write($"[{logger}] {text}");
    }
    public void Stop()
    {
        file.Close();
    }
}