using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Logging
{
    public static class LogManager
    {
        public static void MoveLastLog()
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Logs"));
            }
            
            if (!File.Exists(LatestLogPath))
                return;

            var dateTimeString = DateTime.Now.ToString("yyyy-M-d");

            var paths = Directory.GetFiles(Path.Combine(Application.persistentDataPath, "Logs"), "*.log");
            var fileNames = paths.Select(x => new FileInfo(x).Name);
            var numbers = new List<int>();

            fileNames.Where(x => x.StartsWith(dateTimeString))
                .Select(x => x.Replace($"{dateTimeString}-", "").Replace(".log", "")).ToList().ForEach(x =>
                {
                    if (Int32.TryParse(x, out var number))
                    {
                        numbers.Add(number);
                    }
                });

            numbers.Sort();

            var index = numbers.Count == 0 ? 1 : numbers[^1] + 1;
            
            File.Move(Path.Combine(Application.persistentDataPath, "Logs/latest.log"),
                Path.Combine(Application.persistentDataPath, $"Logs/{dateTimeString}-{index}.log"));
        }

        public static string LatestLogPath => Path.Combine(Application.persistentDataPath, "Logs/latest.log");
    }
    
    public static class Logger
    {
        private static bool _logMoved;
        
        private static string LogTimeStamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private static void LogCore(string message, string level,
            string filePath = "",
            string memberName = "")
        {
            if (!_logMoved)
            {
                LogManager.MoveLastLog();

                _logMoved = true;
            }

            var className = Path.GetFileNameWithoutExtension(filePath);

            var logString = $"[{LogTimeStamp}] [{level}] [{className}->{memberName}] {message}";

            File.AppendAllText(LogManager.LatestLogPath, logString + Environment.NewLine);
        }

        public static void LogInfo(string message, 
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            LogCore(message, "INFO", filePath, memberName);
        }
        
        public static void LogError(string message, 
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            LogCore(message, "ERROR", filePath, memberName);
        }
    }
}