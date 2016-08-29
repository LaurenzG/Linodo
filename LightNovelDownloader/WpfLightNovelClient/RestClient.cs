using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WpfLightNovelClient
{
    class RestClient
    {

        HttpClient client;
        JavaScriptSerializer ser = new JavaScriptSerializer();

        public RestClient(string baseUrl)
        {
            client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }
        async public Task<IEnumerable<T>> GetAsync<T>(string query = "")
        {
            string url = string.Format("{0}?{1}", NameOf<T>(), query);
            string result = await client.GetStringAsync(url);
            return ser.Deserialize<IEnumerable<T>>(result);
        }

        async public Task<T> PostAsync<T>(T obj)
        {
            HttpContent content = new StringContent(
                ser.Serialize(obj),
                Encoding.ASCII,
                "application/json"
                );
            var result = await client.PostAsync(NameOf<T>(), content);
            string str = await result.Content.ReadAsStringAsync();
            return ser.Deserialize<T>(str);
        }
        async public         Task
PutAsync<T>(object key, T obj)
        {
            HttpContent content = new StringContent(
                ser.Serialize(obj),
                Encoding.ASCII,
                "application/json"
            );
            string url = $"{NameOf<T>()}/{key}";
            var result = await client.PutAsync(url, content);
        }


        async public void DeleteAsync<T>(object key)
        {
            string url = $"{NameOf<T>()}/{key}";
            var result = await client.DeleteAsync(url);
        }


        private string NameOf<T>()
        {
            return typeof(T).Name.ToLower().Replace("dto", "");
        }

    }
}


