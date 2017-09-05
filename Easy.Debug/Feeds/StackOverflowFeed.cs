using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace Easy.Debug.Feeds
{
    public class StackOverflowFeed : IFeed
    {
        public async void Execute(IDictionary<string, string> criteria)
        {
            string vslang = HttpUtility.UrlEncode(criteria["VSLANG"]);
            string search = HttpUtility.UrlEncode(criteria["VSException"]);
            string searchDetail = HttpUtility.UrlEncode(criteria["VSExceptionDetail"]);

            HttpClientHandler handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var httpClient = new HttpClient(handler))
            {
                var apiUrl = "http://api.stackexchange.com/2.2/search/advanced?order=desc&sort=activity&tagged=" + vslang + "&site=stackoverflow&accepted=True&title=" + search.Split('.')[1];

                //"http://api.stackexchange.com/2.2/search/advanced?order=desc&sort=activity&accepted=True&site=stackoverflow&tagged=" + vslang + "&intitle=" + search;
                //http://api.stackexchange.com/2.2/search/advanced?order=desc&sort=activity&accepted=True&tagged=.net&title=divide%20by%20zero&filter=default&site=stackoverflow
                //setup HttpClient
                httpClient.BaseAddress = new Uri(apiUrl);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //make request
                var response = await httpClient.GetStringAsync(apiUrl);

                ProcessResponse(response);
            }
        }

        private void ProcessResponse(string response)
        {
            dynamic payload = JsonConvert.DeserializeObject(response);
            string link = string.Empty;
            foreach (var item in payload.items)
            {
                link += item.link;
            }            
        }
    }
}
