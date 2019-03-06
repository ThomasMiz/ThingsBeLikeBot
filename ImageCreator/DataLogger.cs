#if LOG_DATA
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCreator
{
    class DataLogger
    {
        const string LOG_FOLDER = "ImageCreator_logs";

        private static String[] preStrings;
        private static String file;
        private static StreamWriter stream;
        private static FileStream fileStream;

        public static void Init()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            preStrings = new string[5];
            preStrings[(int)LoggingMode.Success] = "[OK]";
            preStrings[(int)LoggingMode.Message] = "[MSG]";
            preStrings[(int)LoggingMode.RawData] = "[DATA]";
            preStrings[(int)LoggingMode.Warning] = "[WARNING]";
            preStrings[(int)LoggingMode.Error] = "[ERROR]";

            DateTime now = DateTime.Now;
            if (!Directory.Exists(LOG_FOLDER))
                Directory.CreateDirectory(LOG_FOLDER);
            file = String.Concat(LOG_FOLDER, "/", now.Day, "-", now.Month, "-", now.Year, " ", now.Hour, ".", now.Minute, ".", now.Second, ".log");
            while (File.Exists(file))
                file = file.Insert(file.Length - 4, "0");
            fileStream = File.Create(file);
            stream = new StreamWriter(fileStream, Encoding.Default);//new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.Read);
        }

        public static void End()
        {
            stream.Flush();
            fileStream.Flush();
            stream.Dispose();
            fileStream.Dispose();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log("[DataLogger] An exception has passed unhandled. Program is likely terminating", LoggingMode.Error);
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Log("[DataLogger] Exception happned. Logging data: ", LoggingMode.Warning);
            Log("Message: " + e.Exception.Message, LoggingMode.RawData);
            Log("HResult: " + e.Exception.HResult, LoggingMode.RawData);
            Log("Source: " + e.Exception.Source, LoggingMode.RawData);
            Log("StackTrace: " + e.Exception.StackTrace, LoggingMode.RawData);
            Log("[DataLogger] Note that while this exception did happen, this message does NOT mean it wasn't handled.", LoggingMode.Warning);
            Log("[DataLogger] [END OF EXCEPTION MESSAGE]", LoggingMode.Warning);
        }

        public static void Log(String log, LoggingMode mode)
        {
            /*switch (mode)
            {
                case LoggingMode.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case LoggingMode.Message:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case LoggingMode.RawData:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;

                case LoggingMode.Error:
                case LoggingMode.Warning:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }*/

            DateTime now = DateTime.Now;
            String s = String.Concat(preStrings[(int)mode], " [", now.Day, "-", now.Month, "-", now.Year, "] [", now.Hour, ":", now.Minute, ":", now.Second, "] ", log);
            stream.WriteLine(s);
            //Console.WriteLine(s);
        }
    }

    enum LoggingMode
    {
        Success = 0, Message = 1, RawData = 2, Warning = 3, Error = 4
    }
}
#endif