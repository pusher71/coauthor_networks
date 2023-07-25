using System.Net;
using System.IO;

namespace coauthor_networks
{
    public static class HttpHelper
    {
        public static HttpWebResponse GetResponse(HttpWebRequest request) => (HttpWebResponse)request.GetResponse();
        public static string GetResponseString(HttpWebResponse response) => new StreamReader(response.GetResponseStream()).ReadToEnd();
    }
}
