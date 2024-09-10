using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace web_scraper
{
    

    class Program
    {
        private static HashSet<string> visitedUrls = new HashSet<string>(); // Eltárolja a meglátogatott URL-eket

        static async Task Main(string[] args)
        {
            string domain = "http://merlinvizsga.hu/index.php?menu=React"; // Cseréld le a kívánt domainre
            string baseUrl = domain;

            // Kezdőoldal meglátogatása
            await CrawlPage(baseUrl, domain);

            Console.WriteLine("Minden link meglátogatva.");
        }

        private static async Task CrawlPage(string url, string domain)
        {
            if (visitedUrls.Contains(url)) return; // Ha már meglátogattuk, ne menjünk újra
            visitedUrls.Add(url); // Jelöljük meg, hogy meglátogattuk

            Console.WriteLine($"Feldolgozás: {url}");

            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;

                var pageContent = await response.Content.ReadAsStringAsync();

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(pageContent);

                // Szöveg kinyerése és mentése
                string pageText = GetPageText(document);
                SaveTextToFile(pageText, url);

                // Linkek kinyerése
                var internalLinks = GetInternalLinks(document, domain);

                // Belső linkek meglátogatása
                foreach (var link in internalLinks)
                {
                    await CrawlPage(link, domain);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba történt a {url} feldolgozása során: {ex.Message}");
            }
        }

        private static string GetPageText(HtmlDocument document)
        {
            // Kinyeri a tiszta szöveget a HTML dokumentumból
            var textNodes = document.DocumentNode.SelectNodes("//body//text()[normalize-space()]");

            if (textNodes == null) return "";

            return string.Join(Environment.NewLine, textNodes.Select(node => node.InnerText.Trim()));
        }

        private static List<string> GetInternalLinks(HtmlDocument document, string domain)
        {
            var links = new List<string>();

            var anchorNodes = document.DocumentNode.SelectNodes("//a[@href]");
            if (anchorNodes == null) return links;

            foreach (var node in anchorNodes)
            {
                var hrefValue = node.GetAttributeValue("href", string.Empty);
                if (IsInternalLink(hrefValue, domain))
                {
                    // Abszolút URL-ek kezelése
                    var absoluteUrl = MakeAbsoluteUrl(hrefValue, domain);
                    links.Add(absoluteUrl);
                }
            }

            return links;
        }

        private static bool IsInternalLink(string url, string domain)
        {
            // Ellenőrzi, hogy az URL belső link-e a megadott domainen belül
            return url.StartsWith("/") || url.StartsWith(domain);
        }

        private static string MakeAbsoluteUrl(string url, string domain)
        {
            if (url.StartsWith("/"))
            {
                return domain + url; // Belső relatív URL
            }
            return url; // Már abszolút URL
        }

        private static void SaveTextToFile(string text, string url)
        {
            // Egyszerű fájl elnevezés az URL-ből (nem biztonságos, de példa)
            string fileName = url.Replace("https://", "").Replace("/", "_") + ".txt";

            try
            {
                File.WriteAllText(fileName, text);
                Console.WriteLine($"Szöveg mentve: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba történt a fájl mentése során: {ex.Message}");
            }
        }
    }

}
