using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;

namespace ImageCreator
{
    class Program
    {
        const String RESULTS_DIR = "memes";

        public static int Main(string[] args)
        {
#if LOG_DATA
            DataLogger.Init();
#endif
            int r = DoStuff(args);
#if LOG_DATA
            DataLogger.Log("END OF PROGRAM", LoggingMode.Message);
            DataLogger.End();
#endif
            return r;
        }

        /// <summary>
        /// This used to be Main but now I moved it for logging convenience reasons. Returns the application's exit code.
        /// An exit code of -1 means no internet connection. This value is used by ThingBeLikeBot.
        /// </summary>
        private static int DoStuff(string[] args)
        {
            if (!CheckInternetConnection())
                return -1;

            TwitterManager twitterManager = new TwitterManager();
            WordnikManager wordnik = new WordnikManager();
            List<String> nouns;

            if (!wordnik.GetNouns(1, out nouns))
                return -1;

            if (!Directory.Exists(RESULTS_DIR))
                Directory.CreateDirectory(RESULTS_DIR);

            String word = nouns[0];

            String singular = wordnik.Singularize(word);
            String plural = wordnik.Pluralize(word);

            using (CseManager cse = new CseManager())
            {
                List<ImageData> imgData;
                if (cse.GetImagesFor(singular, 10, out imgData))
                {
                    ImageData d = ImageManager.ChooseImage(imgData);
                    String fileName = singular + d.Extension;

                    if (d.TryDownloadAs(fileName, 10))
                    {
                        DateTime now = DateTime.Now;
                        string resPath = RESULTS_DIR + "/[" + now.Day + "-" + now.Month + "-" + now.Year + "]" + plural + " be like.jpeg";
                        ImageManager.CreateImage(singular, plural, fileName, resPath);
                        File.Delete(fileName);
                        if (twitterManager.PostStatusWithMedia("", resPath, 10))
                            return 0;
                    }
                }
            }
            return -1;
        }

        static bool CheckInternetConnection()
        {
#if LOG_DATA
            DataLogger.Log("[Program] Checking internet connection by pinging google.com", LoggingMode.Message);
#endif
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using (Ping ping = new Ping())
                    {
                        PingReply reply = ping.Send("google.com", 5000);
#if LOG_DATA
                        DataLogger.Log(String.Concat("[Program] Ping result ", i, ": status=", reply.Status, " roundupTime=", reply.RoundtripTime), LoggingMode.RawData);
#endif
                        return (reply.Status == IPStatus.Success);
                    }
                }
                catch { }
            }
#if LOG_DATA
            DataLogger.Log("[Program] Pinging failed. Assuming no internet connection", LoggingMode.Error);
#endif
            return false;
        }
    }
}
