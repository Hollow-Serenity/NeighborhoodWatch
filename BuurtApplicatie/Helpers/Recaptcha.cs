using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BuurtApplicatie.Helpers
{

    public class CaptchaSettings
    {
        public string Site_Key { get; set; }
        public string Secret_Key { get; set; }
    }

    public class RecaptchaService
    {
        private CaptchaSettings _setting;
        public RecaptchaService(IOptions<CaptchaSettings> options)
        {
            _setting = options.Value;
        }

        public virtual async Task<GoogleRespo> Verification(string _Token)
        {
            GoogleData _Mydata = new GoogleData
            {
                response = _Token,
                secret = _setting.Secret_Key
            };


            HttpClient client = new HttpClient();
            var url = String.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", _Mydata.secret, _Mydata.response);
            var response = await client.GetStringAsync(url);
            //var response = await client.GetStringAsync($"https://www.google.com/recaptcha/api/siteverify?=secret={_Mydata.secret}&response={_Mydata.response}");
            var capres = JsonConvert.DeserializeObject<GoogleRespo>(response);

            return capres;

        }

    }

    public class GoogleData
    {
        public string response { get; set; }
        public string secret { get; set; }
    }
    public class GoogleRespo
    {
        public bool success { get; set; }

        public double score { get; set; }
        public string action { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
    }
}
