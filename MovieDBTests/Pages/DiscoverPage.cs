using OpenQA.Selenium;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using MovieDBTests.Utils;

namespace MovieDBTests.Pages
{
    public class DiscoverPage
    {
        private readonly IWebDriver _driver;

        public DiscoverPage(IWebDriver driver)
        {
            _driver = driver;
        }

        /// <summary>
        /// Navighează direct la pagina Discover cu filtre aplicate prin query string.
        /// </summary>
        public void OpenWithFilters(string genreId, int fromYear, int toYear)
        {
            var url = $"{Utils.Config.ApiBaseUrl.Replace("api.", "")}/discover/movie" +
                      $"?sort_by=primary_release_date.asc" +
                      $"&primary_release_date.gte={fromYear}-01-01" +
                      $"&primary_release_date.lte={toYear}-12-31" +
                      $"&with_genres={genreId}";

            TestContext.WriteLine($"Navigating to Discover URL: {url}");
            _driver.Navigate().GoToUrl(url);

            System.Threading.Thread.Sleep(3000); // Wait for results to load
        }

        /// <summary>
        /// Citește titlurile filmelor din cardurile de pe pagină.
        /// </summary>
        public List<string> GetMovieTitles()
        {
            var titles = new List<string>();

            var cards = _driver.FindElements(By.CssSelector(".card.style_1"));
            TestContext.WriteLine($"Found {cards.Count} cards");

            foreach (var card in cards.Take(10))
            {
                try
                {
                    var titleEl = card.FindElement(By.CssSelector("h2 a, h3 a, .title a"));
                    var title = titleEl.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        titles.Add(title);
                        TestContext.WriteLine($"Found movie: {title}");
                    }
                }
                catch { }
            }

            return titles;
        }
    }
}
