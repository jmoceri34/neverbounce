using System.Configuration;

namespace NeverBounce
{
    class Program
    {
        static void Main(string[] args)
        {
            var apiKey = ConfigurationManager.AppSettings["NeverBounceApiKey"];
            var neverBounce = new NeverBounce(apiKey);
        }
    }
}
