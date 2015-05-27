using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using Microsoft.SPOT;
using System.Security.Cryptography;

using Json.NETMF;

namespace TrailCamera
{
    public class AzureBlob
    {
        public void PutBlob(Configuration config, byte[] blobContent, bool error = false)
        {
            String requestMethod = "PUT";
            String storageServiceVersion = "2009-09-19";
            DateTime currentDateTime = DateTime.UtcNow;
            String dateInRfc1123Format = currentDateTime.ToString("R");  //Please note the blog where this is changed to "s" in the WebAPI

            Int32 blobLength = blobContent.Length;

            PhotoResponse info = getuploadInfo(config);

            string authorizationHeader = info.header;

            //Uri uri = new Uri(BlobEndPoint + urlPath.ToString());

            Uri uri = new Uri(info.sasUrl);

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(uri);
            request.Method = requestMethod;
            request.Headers.Add("x-ms-blob-type", "BlockBlob");
            request.Headers.Add("x-ms-date", dateInRfc1123Format);
            request.Headers.Add("x-ms-version", storageServiceVersion);
            request.Headers.Add("Authorization", authorizationHeader);
            request.ContentLength = blobLength;

            try
            {
                Debug.Print("BEGIN: request.GetRequestStream()");
                using (Stream requestStream = request.GetRequestStream())
                {
                    Debug.Print("END: request.GetRequestStream()");
                    int blobOffset = 0;
                    byte[] buffer = new byte[1024];
                    while (blobOffset < blobContent.Length)
                    {
                        int length = System.Math.Min(buffer.Length, blobContent.Length - blobOffset);
                        Array.Copy(blobContent, blobOffset, buffer, 0, length);
                        requestStream.Write(buffer, 0, buffer.Length);
                        blobOffset += length;

                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Debug.Print("HttpWebResponse.StatusCode: " + response.StatusCode.ToString());
                    Debug.Print("HttpWebResponse.StatusCode: " + response.StatusDescription.ToString());
                }
                error = false;
            }
            catch (WebException ex)
            {
                Debug.Print("An error occured. Status code:" + ((HttpWebResponse)ex.Response).StatusCode);

                error = true;
                using (Stream stream = ex.Response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        var s = sr.ReadToEnd();
                        Debug.Print(s);
                    }
                }
            }

        }

        private static PhotoResponse getuploadInfo(Configuration config)
        {
            PhotoResponse response = new PhotoResponse();

            Debug.Print("PutBlobMobile");

            //WebRequest photoRequest = WebRequest.Create("https://trailmonitorservice.azure-mobile.net/api/getuploadinfo?deviceId=" + deviceId);
            WebRequest photoRequest = WebRequest.Create(config.sasUrl + "?deviceId=" + config.deviceId);
            photoRequest.Method = "GET";
            //photoRequest.Headers.Add("X-ZUMO-APPLICATION", "hjFCXDfKWCfiBbpIhvaasdfjbbDrSW31");
            photoRequest.Headers.Add("X-ZUMO-APPLICATION", config.sasKey);

            Debug.Print("Getting PUT URL");
            Hashtable temp;

            using (var sbPhotoResponseStream = photoRequest.GetResponse().GetResponseStream())
            {
                StreamReader sr = new StreamReader(sbPhotoResponseStream);

                string data = sr.ReadToEnd();
                temp = JsonSerializer.DeserializeString(data) as Hashtable;

                response.sasUrl = temp["sasUrl"] as string;
                response.header = temp["header"] as string;
                response.photoId = temp["photoId"] as string;
                response.expiry = temp["expiry"] as string;

                Debug.Print(response.sasUrl);
                Debug.Print(response.header);
                Debug.Print(response.photoId);
                Debug.Print(response.expiry);

            }


            return response;
        }
    }
}
