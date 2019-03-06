using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Net;

namespace ImageCreator
{
    class CseManager : IDisposable
    {
        private CustomsearchService service;

        public CseManager()
        {
            service = new CustomsearchService(new BaseClientService.Initializer() { ApiKey = Constants.CSE_API_KEY });
#if LOG_DATA
            DataLogger.Log("[CseManager] Initiated CustomsearchService", LoggingMode.Success);
#endif
        }

        /// <summary>
        /// Tries to search for images in google of a specified query and returns the first 10. Returns whether successfull
        /// </summary>
        /// <param name="query"></param>
        /// <param name="tries"></param>
        /// <returns></returns>
        public bool GetImagesFor(String query, int tries, out List<ImageData> result)
        {
#if LOG_DATA
            DataLogger.Log(String.Concat("[CseManager] Getting images for query \"", query, "\""), LoggingMode.Message);
#endif
            for (int t = 0; t < tries; t++)
            {
                try
                {
#if LOG_DATA
                    DataLogger.Log(String.Concat("[CseManager] Attempting to get images (", t, ")"), LoggingMode.Message);
#endif
                    CseResource.ListRequest req = service.Cse.List(query);
                    req.Cx = Constants.CSE_CX;
                    req.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
                    req.FileType = "png, jpg, jpeg, bmp";
                    req.Rights = "cc_publicdomain, cc_attribute, cc_sharealike, cc_noncommercial";
                    req.Safe = CseResource.ListRequest.SafeEnum.Active;
                    Search s = req.Execute();

#if LOG_DATA
                    DataLogger.Log("[CseManager] Recieved images. Processing them into data list", LoggingMode.Message);
#endif
                    result = new List<ImageData>(10);
                    for (int i = 0; i < s.Items.Count; i++)
                    {
                        Result r = s.Items[i];
                        if (r.Image.Width.HasValue && r.Image.Height.HasValue && EndsWithProperImageFormat(r.Link))
                            result.Add(new ImageData(r));
                    }
#if LOG_DATA
                    DataLogger.Log(String.Concat("[CseManager] Done processing images into data list. The ", result.Count, " results are:"), LoggingMode.Success);
                    for (int i = 0; i < result.Count; i++)
                        DataLogger.Log(String.Concat("Width=", result[i].Width, " Height=", result[i].Height, " Extension: ", result[i].Extension, " Link: \"", result[i].Link, "\""), LoggingMode.RawData);
                    DataLogger.Log("[CseManager] [END OF IMAGE DATA LIST]", LoggingMode.Message);
#endif
                    return true;
                }
                catch
                {

                }
            }

            result = null;
            return false;
        }

        private static bool EndsWithProperImageFormat(String link)
        {
            return link.EndsWith(".png") || link.EndsWith(".jpg") || link.EndsWith(".jpeg") || link.EndsWith(".bmp");
        }

        public void Dispose()
        {
            service.Dispose();
        }
    }

    class ImageData
    {
        private int _width, _height;
        private String _link, _displayLink, _ext;

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        
        public String Extension { get { return _ext; } }
        public String DisplayLink { get { return _displayLink; } }
        public String Link { get { return _link; } }

        public ImageData(Result r)
        {
            _width = r.Image.Width.Value;
            _height = r.Image.Height.Value;

            _link = r.Link;
            _displayLink = r.DisplayLink;

            _ext = _link.Substring(_link.LastIndexOf('.'));
        }

        /// <summary>
        /// Tries to download an image and store it in the given filename a specified amount of times. Returns whether successfull
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="tries"></param>
        public bool TryDownloadAs(String fileName, int tries)
        {
            for (int t = 0; t < tries; t++)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(_link, fileName);
                        return true;
                    }
                }
                catch
                {

                }
            }
            return false;
        }
    }
}
