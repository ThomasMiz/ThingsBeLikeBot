using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Net;

namespace ImageCreator
{
    class WordnikManager
    {
        private PluralizationService plurService;

        public WordnikManager()
        {
            plurService = PluralizationService.CreateService(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
#if LOG_DATA
            DataLogger.Log("[WordnikManager] PluralizationService created with culture: " + plurService.Culture.Name, LoggingMode.Message);
#endif
        }

        /// <summary>
        /// Gets a list of nouns using the wordnik API. These nouns aren't guaranteed to be in singular or plural form. Returns whether it was successfull.
        /// </summary>
        /// <param name="limit">How many words to return (if too many, it will likely return less)</param>
        /// <param name="nouns">The list of returned nouns</param>
        public bool GetNouns(int limit, out List<String> nouns)
        {
#if LOG_DATA
            DataLogger.Log(String.Concat("[WordnikManager] Getting ", limit, " nouns"), LoggingMode.Message);
#endif
            String req = "https://api.wordnik.com/v4/words.json/randomWords?hasDictionaryDef=true&includePartOfSpeech=noun%2C%20pronoun%2C%20proper-noun&minDictionaryCount=1&limit=" + limit + "&api_key=" + Constants.WORDNIK_API_KEY;
            String resp;

            if(!TryGetResponse(req, 10, out resp))
            {
#if LOG_DATA
                DataLogger.Log("[WordnikManager] Couldn't get a words response from Wordnik", LoggingMode.Error);
#endif
                nouns = null;
                return false;
            }

#if LOG_DATA
            DataLogger.Log("[WordnikManager] Unpacking words from JSON", LoggingMode.Message);
            DataLogger.Log("[WordnikManager] Printing raw response string:", LoggingMode.Message);
            DataLogger.Log(resp, LoggingMode.RawData);
            DataLogger.Log("[WordnikManager] [END OF RAW RESPONSE]", LoggingMode.Message);
#endif
            nouns = new List<String>(limit);
            int currentBracket = resp.IndexOf('{');
            while (currentBracket != -1)
            {
                int wordStart = resp.IndexOf("\"word\":", currentBracket);
                wordStart += 7;
                while (resp[wordStart] != '"')
                    wordStart++;
                wordStart++;
                int wordEnd = resp.IndexOf('"', wordStart + 1);
                String currentWord = resp.Substring(wordStart, wordEnd - wordStart);
                nouns.Add(currentWord);
                currentBracket = resp.IndexOf('{', wordEnd);
            }
#if LOG_DATA
            DataLogger.Log("[WordnikManager] Done unpacking words. Results:", LoggingMode.Message);
            System.Text.StringBuilder builder = new System.Text.StringBuilder(nouns.Count * 12);
            int n = 0;
            while(n < nouns.Count-1)
            {
                builder.Append(", ");
                builder.Append(nouns[n]);
                n++;
            }
            builder.Append(nouns[n]);
            DataLogger.Log(builder.ToString(), LoggingMode.RawData);
            DataLogger.Log("[WordnikManager] [END OF WORD LIST]", LoggingMode.Message);
#endif

            return true;
        }

        /// <summary>
        /// Tries to get a String response from a web request multiple times. Returns whether it was successfull.
        /// </summary>
        /// <param name="request">The URL to make the request from</param>
        /// <param name="tries">How many times to try to get the response</param>
        /// <param name="response">The response just come on i don't have to explain that</param>
        private bool TryGetResponse(String request, int tries, out String response)
        {
            using (WebClient client = new WebClient())
            {
                for (int i = 0; i < tries; i++)
                {
#if LOG_DATA
                    DataLogger.Log(String.Concat("[WordnikManager] Attempting to get words request (", tries, ")"), LoggingMode.Message);
#endif
                    try
                    {
                        response = client.DownloadString(request);
#if LOG_DATA
                        DataLogger.Log("[WordnikManager] Succesfully got a response from wordnik on try " + i, LoggingMode.Success);
#endif
                        return true;
                    }
                    catch { }
                }
            }
#if LOG_DATA
            DataLogger.Log(String.Concat("[WordnikManager] Failed to get a response for the words request after ", tries, " tries"), LoggingMode.Error);
#endif
            response = "";
            return false;
        }

        public String Singularize(String word)
        {
            return plurService.Singularize(word);
        }

        public String Pluralize(String word)
        {
            return plurService.Pluralize(word);
        }
    }
}
