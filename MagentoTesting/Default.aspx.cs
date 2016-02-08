using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using RestSharp;
using DotNetOpenAuth.OAuth;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Configuration;

namespace MagentoTesting
{
    public partial class _Default : System.Web.UI.Page
    {
        string ConsumerKey = "a47a1f116d74f7c0608b2ac904dec6d1";
        string ConsumerSecret = "4ba049c97fd17cf0fbb275ee39f59fc9";      

        protected void Page_Load(object sender, EventArgs e)
        {

            string oauth_token = Request.QueryString["oauth_token"];
            string oauth_verifier = Request.QueryString["oauth_verifier"];

            if (string.IsNullOrEmpty(oauth_token) || string.IsNullOrEmpty(oauth_verifier))
            {
                BeginAuthorization();
            }
            else
            {
                Authorize(oauth_token, oauth_verifier);
            }
        }

        private void BeginAuthorization()
        {
            var uri = new Uri("http://smeitproducts.com/magento-beta/oauth/initiate");


            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string parameters;
            string normalizedUrl;


            string signature = oAuth.GenerateSignature(uri, ConsumerKey, ConsumerSecret,
             String.Empty, String.Empty, "GET", timeStamp, nonce, OAuthBase.SignatureTypes.PLAINTEXT,
             out normalizedUrl, out parameters);


            StringBuilder requestUri = new StringBuilder(uri.ToString());
            requestUri.AppendFormat("?oauth_consumer_key={0}&", ConsumerKey);
            requestUri.AppendFormat("oauth_callback={0}&", "http://localhost:4893/Default.aspx");
            requestUri.AppendFormat("oauth_nonce={0}&", nonce);
            requestUri.AppendFormat("oauth_timestamp={0}&", timeStamp);
            requestUri.AppendFormat("oauth_signature_method={0}&", "PLAINTEXT");
            requestUri.AppendFormat("oauth_version={0}&", "1.0");
            requestUri.AppendFormat("oauth_signature={0}", signature);


            var imgclient = new RestClient(requestUri.ToString());

            var imgrequest = new RestSharp.RestRequest(RestSharp.Method.POST)
            {
                RequestFormat = RestSharp.DataFormat.Xml
            };

            var imgresponse = imgclient.Execute(imgrequest);
            var dd = imgresponse.Content;
            
            string[] res = dd.Split('&');
            string tok = res[0];
            string[] authToken = tok.Split('=');

            string tok2 = res[1];
            string[] authToken2 = tok2.Split('=');

            Session["oauth_token_secret"] = authToken2[1];       


            string redirectUrl = "http://smeitproducts.com/magento-beta/index.php/admin/oauth_authorize?oauth_token=" + authToken[1];
            Response.Redirect(redirectUrl);
          
        }
        private void Authorize(string oauth_token, string oauth_verifier)
        {
            var uri = new Uri(MagentoServer + "/oauth/token");
            string oauth_token_secret = (string)Session["oauth_token_secret"];

           OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string parameters;
            string normalizedUrl;
            string signature = oAuth.GenerateSignature(uri, ConsumerKey, ConsumerSecret,
            oauth_token, oauth_token_secret, "GET", timeStamp, nonce, OAuthBase.SignatureTypes.PLAINTEXT,
            out normalizedUrl, out parameters);

            StringBuilder sb = new StringBuilder("OAuth ");
            sb.AppendFormat("oauth_verifier=\"{0}\",", oauth_verifier);
            sb.AppendFormat("oauth_token=\"{0}\",", oauth_token);
            sb.AppendFormat("oauth_version=\"{0}\",", "1.0");
            sb.AppendFormat("oauth_signature_method=\"{0}\",", "PLAINTEXT");
            sb.AppendFormat("oauth_nonce=\"{0}\",", nonce);
            sb.AppendFormat("oauth_timestamp=\"{0}\",", timeStamp);
            sb.AppendFormat("oauth_consumer_key=\"{0}\",", ConsumerKey);
            sb.AppendFormat("oauth_signature=\"{0}\"", signature);



            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers[HttpRequestHeader.Authorization] = sb.ToString();
            request.ContentType = "text/xml";
            request.Accept = "text/xml";
            request.KeepAlive = true;
            request.Method = "POST";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {

                    Stream responseStream = response.GetResponseStream();
                    StreamReader responseReader = new StreamReader(responseStream);
                    string text = responseReader.ReadToEnd();
                    try
                    {
                        Dictionary<String, string> responseDic = GetDictionaryFromQueryString(text);

                        string token = responseDic.First(q => q.Key == "oauth_token").Value;
                        string secret = responseDic.First(q => q.Key == "oauth_token_secret").Value;

                        Configuration objConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
                        AppSettingsSection objAppsettings = (AppSettingsSection)objConfig.GetSection("appSettings");
                        //Edit
                        if (objAppsettings != null)
                        {
                            objAppsettings.Settings["Magento.Token"].Value = token;
                            objAppsettings.Settings["Magento.TokenSecret"].Value = secret;
                            objConfig.Save();


                            var Resturi = new Uri("http://smeitproducts.com/magento-beta" + "/api/rest/products");

                            OAuthBase oAuth2 = new OAuthBase();
                            string Restnonce = oAuth2.GenerateNonce();
                            string ResttimeStamp = oAuth2.GenerateTimeStamp();
                            string Restparameters;
                            string RestnormalizedUrl;
                            string signature2 = oAuth2.GenerateSignature(Resturi, ConsumerKey, ConsumerSecret,
                            token, secret, "GET", ResttimeStamp, Restnonce, OAuthBase.SignatureTypes.PLAINTEXT,
                            out RestnormalizedUrl, out Restparameters);

                            StringBuilder requestUri = new StringBuilder("OAuth ");
                            requestUri.AppendFormat("oauth_token=\"{0}\",", token);
                            requestUri.AppendFormat("oauth_version=\"{0}\",", "1.0");
                            requestUri.AppendFormat("oauth_signature_method=\"{0}\",", "PLAINTEXT");
                            requestUri.AppendFormat("oauth_nonce=\"{0}\",", Restnonce);
                            requestUri.AppendFormat("oauth_timestamp=\"{0}\",", ResttimeStamp);
                            requestUri.AppendFormat("oauth_consumer_key=\"{0}\",", ConsumerKey);
                            requestUri.AppendFormat("oauth_signature=\"{0}\"", signature2);

                            string BASE_URL = "http://smeitproducts.com/magento-beta";
                            RestClient restClient = new RestClient(BASE_URL);

                            RestRequest restRequest = new RestRequest("/api/rest/products", Method.GET);
                            restRequest.AddHeader("Authorization", requestUri.ToString());
                            restRequest.RequestFormat = DataFormat.Json;


                            var response2 = restClient.Execute(restRequest);
                            var data = response2.Content;
                           
                        }

                        errorLabel.Text = "Done";
                        errorLabel.ForeColor = System.Drawing.Color.Green;

                    }
                    catch (Exception ex)
                    {
                        errorLabel.Text = "Exchanging token failed.<br>Response text = " + text + "<br>Exception = " + ex.Message;
                    }
                }
            }
            catch (WebException ex)
            {
                var responseStream = ex.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                string resp = responseReader.ReadToEnd();
                errorLabel.Text = resp;
            }
        }
        private static Dictionary<string, string> GetDictionaryFromQueryString(string queryString)
        {
            string[] parts = queryString.Split('&');
            Dictionary<String, string> dic = new Dictionary<string, string>();
            foreach (var part in parts)
            {
                dic.Add(part.Split('=')[0], part.Split('=')[1]);
            }
            return dic;
        }
        string MagentoServer
        {
            get
            {
                return "http://smeitproducts.com/magento-beta";
            }
        }

    }
}


//ApiClient ff = new ApiClient();
////   var requests = ff.CreateAuthorizedRequest("http://smeitproducts.com/magento-beta" + "/api/rest/products", "GET", new ApiFilter() { Page = 1 }, token, secret);

////  AccessToken = act;
//// AccessTokenSecret = atsc;
//ApiFilter filter = new ApiFilter() { Page = 1 };

////OAuthBase oAuth = new OAuthBase();
//string nonceapi = oAuth.GenerateNonce();
//string timeStampapi = oAuth.GenerateTimeStamp();
//string parametersapi;
//string normalizedUrlapi;
//string signatureapi = oAuth.GenerateSignature(new Uri("http://smeitproducts.com/magento-beta/api/rest/customers"), ConsumerKey, ConsumerSecret,
//token, secret, "GET", timeStampapi, nonceapi, OAuthBase.SignatureTypes.PLAINTEXT,
//out normalizedUrlapi, out parametersapi);

////HttpWebRequest requestapi = (HttpWebRequest)WebRequest.Create("http://smeitproducts.com/magento-beta/api/rest/customers?" + filter.ToString());

////StringBuilder sb1 = new StringBuilder("OAuth ");
////sb1.AppendFormat("oauth_token=\"{0}\",", token);
////sb1.AppendFormat("oauth_version=\"{0}\",", "1.0");
////sb1.AppendFormat("oauth_signature_method=\"{0}\",", "PLAINTEXT");
////sb1.AppendFormat("oauth_nonce=\"{0}\",", nonceapi);
////sb1.AppendFormat("oauth_timestamp=\"{0}\",", timeStampapi);
////sb1.AppendFormat("oauth_consumer_key=\"{0}\",", ConsumerKey);
////sb1.AppendFormat("oauth_signature=\"{0}\"", signatureapi);

////requestapi.Headers[HttpRequestHeader.Authorization] = sb.ToString();
////requestapi.Method = "GET";

//////request.ContentType = "application/json";
////requestapi.Accept = "text/html,application/xhtml+xml,application/json,application/xml;q=0.9,*/*;q=0.8";//application/json,
////requestapi.KeepAlive = true;
////HttpWebResponse responseapi = (HttpWebResponse)request.GetResponse();
////if (responseapi.StatusCode == HttpStatusCode.OK)
////{
////    //System.IO.StreamReader sr = new System.IO.StreamReader(responseapi.GetResponseStream());

////    Stream responseStream1 = responseapi.GetResponseStream();
////    StreamReader responseReader1 = new StreamReader(responseStream1);
////    string text1 = responseReader1.ReadToEnd();
////}

//StringBuilder requestUri = new StringBuilder("http://smeitproducts.com/magento-beta/api/rest/customers");
//requestUri.AppendFormat("?oauth_token={0}&", token);
//requestUri.AppendFormat("oauth_consumer_key={0}&", ConsumerKey);

////requestUri.AppendFormat("oauth_consumer_secret={0}&", ConsumerSecret);
////requestUri.AppendFormat("oauth_secret={0}&", secret);

//requestUri.AppendFormat("oauth_nonce={0}&", nonceapi);
//requestUri.AppendFormat("oauth_timestamp={0}&", timeStampapi);
//requestUri.AppendFormat("oauth_signature_method={0}&", OAuthBase.SignatureTypes.PLAINTEXT);
//requestUri.AppendFormat("oauth_version={0}&", "1.0");
//requestUri.AppendFormat("oauth_signature={0}", signatureapi);


//var imgclient = new RestClient(requestUri.ToString());

//var imgrequest = new RestSharp.RestRequest(RestSharp.Method.GET)
//{
//    RequestFormat = RestSharp.DataFormat.Json,
//};


//var imgresponse = imgclient.Execute(imgrequest);
//var dd = imgresponse.Content;





// var responseText = ff.FetchRequest(requests);
