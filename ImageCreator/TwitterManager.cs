using System;
using System.Collections.Generic;
using System.IO;
using TweetSharp;

namespace ImageCreator
{
    class TwitterManager
    {
        private TwitterService service;
        
        public TwitterManager()
        {
            service = new TwitterService(Constants.TWITTER_CUSTOMER_KEY, Constants.TWITTER_CUSTOMER_KEY_SECRET, Constants.TWITTER_ACCESS_TOKEN, Constants.TWITTER_ACCESS_TOKEN_SECRET);
#if LOG_DATA
            DataLogger.Log("[TwitterService] Initiated TwitterService", LoggingMode.Success);
#endif
        }

        /// <summary>
        /// Tries to post a tweet with the given status and attached media a given amount of times. Returns whether successfull.
        /// </summary>
        /// <param name="status">Tweet status</param>
        /// <param name="mediaPath">Path to the media in this computer's file system</param>
        /// <param name="tries">How many times to try before returning false</param>
        public bool PostStatusWithMedia(String status, String mediaPath, int tries)
        {
#if LOG_DATA
            DataLogger.Log(String.Concat("[TwitterService] Posting status with media: Status=\"", status, "\", Media=\"", mediaPath, "\", Tries: ", tries), LoggingMode.Message);
#endif
            FileStream stream = new FileStream(mediaPath, FileMode.Open, FileAccess.Read);

            for (int i = 0; i < tries; i++)
            {
#if LOG_DATA
                DataLogger.Log(String.Concat("[TwitterService] Attempting to post (", i.ToString(), ")"), LoggingMode.Message);
#endif
                stream.Seek(0, SeekOrigin.Begin);
                try
                {
                    Dictionary<String, Stream> imgsDict = new Dictionary<string, Stream>(1);
                    imgsDict.Add("0", stream);
                    TwitterStatus s = service.SendTweetWithMedia(new SendTweetWithMediaOptions
                    {
                        Status = status,
                        Images = imgsDict
                    });
                    if (s != null)
                    {
#if LOG_DATA
                        DataLogger.Log(String.Concat("[TwitterService] Successfully posted status with media on attempt ", i.ToString()), LoggingMode.Success);
#endif
                        stream.Close();
                        stream.Dispose();
                        return true;
                    }
                }
                catch { }
            }
#if LOG_DATA
            DataLogger.Log("[TwitterService] Posting failed enough times. Giving up on all forms of life.", LoggingMode.Error);
#endif
            return false;
        }
    }
}
