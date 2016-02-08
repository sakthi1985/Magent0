using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Text;
using System.IO;
using System.Configuration;

namespace MagentoTesting
{
    public partial class About : System.Web.UI.Page
    {

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

        private void BeginAuthorization()
        {
            string CallbackUrl = Server.UrlEncode(Request.Url.AbsoluteUri);
            var uri = new Uri(MagentoServer + "/oauth/initiate?oauth_callback=" + CallbackUrl);

            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string parameters;
            string normalizedUrl;
            string signature = oAuth.GenerateSignature(uri, ConsumerKey, ConsumerSecret,
            String.Empty, String.Empty, "GET", timeStamp, nonce, OAuthBase.SignatureTypes.PLAINTEXT,
            out normalizedUrl, out parameters);

            StringBuilder sb = new StringBuilder("OAuth ");
            sb.AppendFormat("oauth_callback=\"{0}\",", CallbackUrl);
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
            request.Method = "GET";

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
                        Dictionary<String, string> dic = GetDictionaryFromQueryString(text);

                        string oauth_token = dic.First(q => q.Key == "oauth_token").Value;
                        string oauth_token_secret = dic.First(q => q.Key == "oauth_token_secret").Value;
                        Session["oauth_token_secret"] = oauth_token_secret;
                        string redirectUrl = MagentoServer + "/index.php/admin/oauth_authorize?oauth_token=" + oauth_token + "&oauth_verifier=" +
                            oauth_token_secret;
                        Response.Redirect(redirectUrl);

                    }
                    catch (Exception ex)
                    {
                        errorLabel.Text = "Parsing request token failed.<br>Response text = " + text + "<br>Exception = " + ex.Message;
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

        #region Settings

        string MagentoServer
        {
            get
            {
                return "http://smeitproducts.com/magento-beta";
            }
        }

        string ConsumerKey
        {
            get
            {
                return "a47a1f116d74f7c0608b2ac904dec6d1";
            }
        }

        string ConsumerSecret
        {
            get
            {
                return "4ba049c97fd17cf0fbb275ee39f59fc9";
            }
        }

        #endregion
    }


  


    public class ApiFilter
    {
        public ApiFilter()
        {
            filterDescriptions = new List<FilterDescription>();
        }

        public int? Page { get; set; }
        public int? Limit { get; set; }
        public List<FilterDescription> filterDescriptions;

        public const string Type = "rest";

        public void AddFilter(string column, FilterType filterType, string value)
        {
            filterDescriptions.Add(new FilterDescription()
            {
                Column = column,
                FilterType = filterType,
                Value = value
            });
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("type={0}", Type);

            if (Page.HasValue)
                sb.AppendFormat("&page={0}", Page.Value);
            if (Limit.HasValue)
                sb.AppendFormat("&limit={0}", Limit.Value);

            int counter = 1;
            foreach (var filter in filterDescriptions)
            {

                sb.AppendFormat("&filter[{0}][attribute]={1}&filter[{2}][{3}]={4}", counter, filter.Column, counter, filter.FilterType, filter.Value);
                counter++;
            }

            return sb.ToString();
        }
    }

    public class FilterDescription
    {
        public string Column { get; set; }
        public FilterType FilterType { get; set; }
        public string Value { get; set; }
    }

    public enum FilterType
    {
        /// <summary>
        /// Not Equal To
        /// </summary>
        neq,
        /// <summary>
        /// equals any of
        /// </summary>
        @in,
        /// <summary>
        /// not equals any of
        /// </summary>
        nin,
        /// <summary>
        /// greater than
        /// </summary>
        gt,
        /// <summary>
        /// less than
        /// </summary>
        lt

    }

    public class MagentoApiException : Exception
    {

        public MagentoApiException(string responseText, WebExceptionStatus status)
        {
            ResponseText = responseText;
            Status = status;
        }

        public string ResponseText { get; set; }
        public WebExceptionStatus Status { get; set; }
    }

}
