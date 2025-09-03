using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using MovieDBTests.Utils;

namespace MovieDBTests.Pages
{
    public class DiscoverPage
    {
        private readonly IWebDriver _driver;
        public DiscoverPage(IWebDriver driver) { _driver = driver; }

        // NOTE: Selectors are best-effort and may need adjustment if TMDB updates its UI.
        private By SortDropdown => By.CssSelector("select#sort_by, .k-select, [data-sort]");
        private By ResultsCards => By.CssSelector("div.card.style_1, .results > .card");
        private By GenreSection => By.XPath("//section[contains(., 'Genres') or contains(., 'Genre')]");
        private By CalendarMin => By.CssSelector("input[name='primary_release_date.gte'], input[placeholder*='from']");
        private By CalendarMax => By.CssSelector("input[name='primary_release_date.lte'], input[placeholder*='to']");
        private By UserScoreSlider => By.XPath("//section[contains(., 'User Score')]//input[@type='range' or @role='slider']");

        public void Open() => _driver.Navigate().GoToUrl(Utils.Config.DiscoverUrl);

        public void SortByReleaseDateAscending()
        {
            // Try to find a select or custom dropdown and choose release_date.asc
            try
            {
                var select = _driver.FindElements(By.CssSelector("select#sort_by")).FirstOrDefault();
                if (select != null)
                {
                    select.Click();
                    _driver.FindElement(By.CssSelector("option[value='primary_release_date.asc'], option[value='release_date.asc']")).Click();
                    return;
                }
            }
            catch { /* fallback */ }

            // Fallback: click sort widget and choose the ascending option
            var sortWidget = _driver.FindElements(By.CssSelector("[data-sort], [data-value='sort'] , [aria-label*='Sort']")).FirstOrDefault();
            sortWidget?.Click();
            var asc = _driver.FindElements(By.XPath("//li[contains(., 'Release Date Ascending') or contains(., 'release_date.asc')]")).FirstOrDefault();
            asc?.Click();
        }

        public void SelectGenres(params string[] genres)
        {
            if (genres == null || genres.Length == 0) return;

            var section = _driver.FindElements(GenreSection).FirstOrDefault()?.FindElement(By.XPath("./ancestor::section")) ?? _driver.FindElement(By.TagName("body"));
            foreach (var g in genres)
            {
                var label = _driver.FindElements(By.XPath($"//label[contains(., '{g}')]")).FirstOrDefault();
                label ??= _driver.FindElements(By.XPath($"//button[contains(., '{g}')]")).FirstOrDefault();
                label?.Click();
            }
        }

        public void SetReleaseDateRange(string fromYear, string toYear, bool useCalendar = true)
        {
            var from = _driver.FindElements(CalendarMin).FirstOrDefault();
            var to = _driver.FindElements(CalendarMax).FirstOrDefault();
            if (from == null || to == null)
            {
                // As a fallback, append query params directly
                var url = new UriBuilder(_driver.Url);
                var q = System.Web.HttpUtility.ParseQueryString(url.Query);
                q["primary_release_date.gte"] = $"{fromYear}-01-01";
                q["primary_release_date.lte"] = $"{toYear}-12-31";
                url.Query = q.ToString();
                _driver.Navigate().GoToUrl(url.ToString());
                return;
            }

            from.Clear(); from.SendKeys(f"{fromYear}-01-01");
            to.Clear(); to.SendKeys(f"{toYear}-12-31");
            to.SendKeys(Keys.Enter);
        }

        public void SetMinimumUserScore(int minScore) // 0..10 or 0..100 depending on control
        {
            var slider = _driver.FindElements(UserScoreSlider).FirstOrDefault();
            if (slider == null) return;
            try
            {
                var actions = new Actions(_driver);
                // crude approach: move slider via keyboard
                slider.Click();
                for (int i = 0; i < minScore; i++) slider.SendKeys(Keys.ArrowRight);
            }
            catch { /* ignore */ }
        }

        public List<(string Title, DateTime? ReleaseDate, List<string> Genres)> ReadResults()
        {
            var cards = _driver.FindElements(ResultsCards);
            var result = new List<(string, DateTime?, List<string>)>();
            foreach (var c in cards)
            {
                var title = c.FindElements(By.CssSelector("h2, h3, a.title")).FirstOrDefault()?.Text?.Trim() ?? "";
                DateTime? rd = null;
                var dateText = c.FindElements(By.CssSelector(".release_date, .meta .release")).FirstOrDefault()?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                    rd = dt;

                var tagEls = c.FindElements(By.CssSelector(".genre, .genres a, .genres span"));
                var gs = tagEls.Select(e => e.Text.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                result.Add((title, rd, gs));
            }
            return result;
        }
    }
}
