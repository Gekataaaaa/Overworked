using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Копия Console в файл Logs/console.log (только редактор): Editor.log на этих ПК не пишется,
    /// а так лог можно прочитать снаружи. Файл перезаписывается при каждом запуске редактора.
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleFileLog
    {
        const string FilePath = "Logs/console.log";
        static readonly object fileLock = new object();

        static ConsoleFileLog()
        {
            if (SessionState.GetBool("ConsoleFileLog.started", false) == false)
            {
                SessionState.SetBool("ConsoleFileLog.started", true);
                try { Directory.CreateDirectory("Logs"); File.WriteAllText(FilePath, ""); } catch { }
            }
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        static void OnLog(string message, string stack, LogType type)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {type}: {message}\n";
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) line += stack + "\n";
            lock (fileLock)
            {
                try { File.AppendAllText(FilePath, line); } catch { }
            }
        }
    }
}
