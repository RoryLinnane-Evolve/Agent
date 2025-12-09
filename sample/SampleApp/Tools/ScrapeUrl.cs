using Ragent.Reflection;

namespace SampleApp.Tools;

[Tool(Description = "This tool scrapes a URL and returns the content in html.", Id = "scrape_url", Name = "Scrape URL")]
public class ScrapeUrl {
    [ToolLogic]
    public static string Logic([ToolParam(Description = "The url to scrape. this has to be a fully formed url e.g. https://google.com/test")]string url) {
        using (var client = new HttpClient()) {
            var result = client.GetAsync(url).Result;
            return result.Content.ReadAsStringAsync().Result;
        }
    }
}