using System;
using System.Threading;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Assignment
{
    class Program
    {
        private const string KEY = "ac1b0b1572524640a0ecc54de453ea9f";
        private const string BASE_URL = "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/";
        private const int DEFAULT_PAGESIZE = 25;

        private static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Top 10 Makelaars (Buy category in Amsterdam):");
            PrintTop10Makelaars("koop", "/amsterdam/");
            Console.WriteLine("\nTop 10 Makelaars (Buy category in Amsterdam + balcony):");
            PrintTop10Makelaars("koop", "/amsterdam/tuin/");
            Console.WriteLine("\nPress any key to exit..");
            Console.ReadKey();
        }

        static void PrintTop10Makelaars(string type, string parameters)
        {
            List<Tuple<string, int>> makelaarsList = new List<Tuple<string, int>>();
            int page = 1;

            //While the number of objects returned from the query is not 0 - proccess the whole page and query for the next one
            while (true)
            {
                //Get one page of data from API
                JObject data = HttpGet(type, parameters, page, DEFAULT_PAGESIZE);

                if (data["Objects"].Count() > 0)
                {
                    for (int i = 0; i < data["Objects"].Count(); i++)
                    {
                        string name = data["Objects"][i]["MakelaarNaam"].Value<string>();

                        //If the makelaar is already in the list - increase his counter of ads, otherwise add new makelaar to the list
                        if (makelaarsList.Exists(x => x.Item1 == name))
                        {
                            makelaarsList[makelaarsList.FindIndex(x => x.Item1 == name)] = Tuple.Create(name, makelaarsList.Find(x => x.Item1 == name).Item2 + 1);
                        }
                        else
                        {
                            makelaarsList.Add(Tuple.Create(name, 1));
                        }
                    }
                    page++;
                }
                else
                {
                    //Order and print top 10 makelaars of the final list and break
                    makelaarsList = makelaarsList.OrderByDescending(x => x.Item2).Take(10).ToList();
                    makelaarsList.ForEach(Console.WriteLine);
                    break;
                }

            }
        }

        static JObject HttpGet(string type, string parameters, int page, int pagesize)
        {
            //Construct the url and make GET request
            string url = BASE_URL + KEY + "/?type=" + type + "&zo=" + parameters + "&page=" + page + "&pagesize=" + pagesize;
            HttpResponseMessage response = client.GetAsync(url).Result;

            //If response status is success - parse content to json and return it, otherwise try the same query again in 10 seconds (to handle API requests limit)
            if (response.IsSuccessStatusCode)
            {
                HttpContent content = response.Content;
                JObject json = JObject.Parse(content.ReadAsStringAsync().Result);
                return json;
            }
            else
            {
                Console.WriteLine("Request refused.. Retrying in 10 seconds..");
                Thread.Sleep(10000);
                return HttpGet(type, parameters, page, pagesize);
            }
        }

    }
}