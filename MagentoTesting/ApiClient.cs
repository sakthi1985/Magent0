using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.IO;

namespace MagentoTesting
{
    public class ApiClient
    {

        public string MagentoServer = "http://smeitproducts.com/magento-beta";
        public string ConsumerKey = "a47a1f116d74f7c0608b2ac904dec6d1";
        public string ConsumerSecret = "4ba049c97fd17cf0fbb275ee39f59fc9";
        public string AccessToken = "0efb830215fada603465e96f02ae73a0";
        public string AccessTokenSecret = "78f999da754901500e615c4595b16338";       

        public HttpWebRequest CreateAuthorizedRequest(string url, string requestMethod, ApiFilter filter,string act, string atsc)
        {

            AccessToken = act;
            AccessTokenSecret = atsc;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "?" + filter.ToString());

            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string parameters;
            string normalizedUrl;
            string signature = oAuth.GenerateSignature(new Uri(url), ConsumerKey, ConsumerSecret,
            AccessToken, AccessTokenSecret, requestMethod, timeStamp, nonce, OAuthBase.SignatureTypes.PLAINTEXT,
            out normalizedUrl, out parameters);

            StringBuilder sb = new StringBuilder("OAuth ");
            sb.AppendFormat("oauth_token=\"{0}\",", AccessToken);
            sb.AppendFormat("oauth_version=\"{0}\",", "1.0");
            sb.AppendFormat("oauth_signature_method=\"{0}\",", "PLAINTEXT");
            sb.AppendFormat("oauth_nonce=\"{0}\",", nonce);
            sb.AppendFormat("oauth_timestamp=\"{0}\",", timeStamp);
            sb.AppendFormat("oauth_consumer_key=\"{0}\",", ConsumerKey);
            sb.AppendFormat("oauth_signature=\"{0}\"", signature);

            request.Headers[HttpRequestHeader.Authorization] = sb.ToString();
            request.Method = requestMethod;

            //request.ContentType = "application/json";
            request.Accept = "text/html,application/xhtml+xml,application/json,application/xml;q=0.9,*/*;q=0.8";//application/json,
            request.KeepAlive = true;

            return request;
        }

        public string FetchRequest(HttpWebRequest request)
        {
            try
            {



                string responseText = string.Empty;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader responseReader = new StreamReader(responseStream))
                        {
                            responseText = responseReader.ReadToEnd();
                            return responseText;
                        }
                    }
                }

                return responseText;
            }
            catch (WebException ex)
            {
                var responseStream = ex.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                string responseText = responseReader.ReadToEnd();
                throw new MagentoApiException(responseText, ex.Status);
            }
        }

           

   

    }
  




}