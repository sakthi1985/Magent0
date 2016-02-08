using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using RestSharp;
using System.Net;
using System.Web.Configuration;

namespace MagentoTesting
{
    public partial class GetData : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string ConsumerKey = "a47a1f116d74f7c0608b2ac904dec6d1";
            string ConsumerSecret = "4ba049c97fd17cf0fbb275ee39f59fc9";

            string oauth_token = WebConfigurationManager.AppSettings["Magento.Token"];
            string oauth_token_secret = WebConfigurationManager.AppSettings["Magento.TokenSecret"];

            var uri = new Uri("http://smeitproducts.com/magento-beta" + "/api/rest/products");
           
            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string parameters;
            string normalizedUrl;
            string signature = oAuth.GenerateSignature(uri, ConsumerKey, ConsumerSecret,
            oauth_token, oauth_token_secret, "GET", timeStamp, nonce, OAuthBase.SignatureTypes.PLAINTEXT,
            out normalizedUrl, out parameters);

            StringBuilder requestUri = new StringBuilder("OAuth ");
            requestUri.AppendFormat("oauth_token=\"{0}\",", oauth_token);
            requestUri.AppendFormat("oauth_version=\"{0}\",", "1.0");
            requestUri.AppendFormat("oauth_signature_method=\"{0}\",", "PLAINTEXT");
            requestUri.AppendFormat("oauth_nonce=\"{0}\",", nonce);
            requestUri.AppendFormat("oauth_timestamp=\"{0}\",", timeStamp);
            requestUri.AppendFormat("oauth_consumer_key=\"{0}\",", ConsumerKey);
            requestUri.AppendFormat("oauth_signature=\"{0}\"", signature);

            string BASE_URL = "http://smeitproducts.com/magento-beta";
            RestClient restClient =new RestClient(BASE_URL);                    

            RestRequest restRequest = new RestRequest("/api/rest/products", Method.GET);
            restRequest.AddHeader("Authorization", requestUri.ToString());
            restRequest.RequestFormat = DataFormat.Json;       
         

            var response = restClient.Execute(restRequest);
            var data = response.Content;

        }
    }
}