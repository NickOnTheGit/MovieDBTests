using OpenQA.Selenium;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using MovieDBTests.Utils;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace MovieDBTests.Pages
{
    public class DiscoverPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public DiscoverPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        public void NavigateToDiscover()
        {
            _driver.Navigate().GoToUrl("https://www.themoviedb.org/discover/movie");
            WaitForPageLoad();
        }

        public void OpenWithFilters(string genreId, int fromYear, int toYear)
        {
            var url = $"https://www.themoviedb.org/discover/movie" +
                      $"?sort_by=primary_release_date.asc" +
                      $"&primary_release_date.gte={fromYear}-01-01" +
                      $"&primary_release_date.lte={toYear}-12-31" +
                      $"&with_genres={genreId}";

            TestContext.WriteLine($"Navigating to Discover URL: {url}");
            _driver.Navigate().GoToUrl(url);

            WaitForPageLoad();
            WaitForResults();
        }

        public void ApplyFiltersManually(string genreId, int fromYear, int toYear)
        {
            try
            {
                NavigateToDiscover();

                // Set sort order
                SetSortOrder("primary_release_date.asc");

                // Set date range
                SetDateRange(fromYear, toYear);

                // Set genre
                SetGenre(genreId);

                // Apply filters
                ClickSearchButton();

                WaitForResults();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Failed to apply filters manually: {ex.Message}");
                // Fallback to URL-based filtering
                OpenWithFilters(genreId, fromYear, toYear);
            }
        }

        private void SetSortOrder(string sortBy)
        {
            try
            {
                var sortSelectors = new[]
                {
                    "select[name='sort_by']",
                    "#sort_by",
                    ".sort_by select",
                    "[data-testid='sort-select']"
                };

                foreach (var selector in sortSelectors)
                {
                    try
                    {
                        var sortElement = _driver.FindElement(By.CssSelector(selector));
                        var selectElement = new SelectElement(sortElement);
                        selectElement.SelectByValue(sortBy);
                        TestContext.WriteLine($"Sort order set using selector: {selector}");
                        return;
                    }
                    catch { continue; }
                }

                TestContext.WriteLine("Could not find sort dropdown, will rely on URL parameters");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error setting sort order: {ex.Message}");
            }
        }

        private void SetDateRange(int fromYear, int toYear)
        {
            try
            {
                // Try to find date input fields
                var dateSelectors = new[]
                {
                    "input[name='primary_release_date.gte']",
                    "input[name='release_date.gte']",
                    "[data-testid='date-from']",
                    ".date_range input[type='date']:first-of-type"
                };

                foreach (var selector in dateSelectors)
                {
                    try
                    {
                        var fromInput = _driver.FindElement(By.CssSelector(selector));
                        fromInput.Clear();
                        fromInput.SendKeys($"{fromYear}-01-01");

                        // Find corresponding "to" field
                        var toSelector = selector.Replace(".gte", ".lte").Replace("from", "to").Replace("first-of-type", "last-of-type");
                        var toInput = _driver.FindElement(By.CssSelector(toSelector));
                        toInput.Clear();
                        toInput.SendKeys($"{toYear}-12-31");

                        TestContext.WriteLine($"Date range set using selectors: {selector}");
                        return;
                    }
                    catch { continue; }
                }

                TestContext.WriteLine("Could not find date inputs, will rely on URL parameters");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error setting date range: {ex.Message}");
            }
        }

        private void SetGenre(string genreId)
        {
            try
            {
                var genreSelectors = new[]
                {
                    $"input[value='{genreId}'][type='checkbox']",
                    $"[data-genre-id='{genreId}']",
                    $"[data-value='{genreId}']",
                    ".genre_selector input[type='checkbox']"
                };

                foreach (var selector in genreSelectors)
                {
                    try
                    {
                        var elements = _driver.FindElements(By.CssSelector(selector));
                        foreach (var element in elements)
                        {
                            if (!element.Selected)
                            {
                                element.Click();
                                TestContext.WriteLine($"Genre set using selector: {selector}");
                                return;
                            }
                        }
                    }
                    catch { continue; }
                }

                TestContext.WriteLine("Could not find genre selector, will rely on URL parameters");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error setting genre: {ex.Message}");
            }
        }

        private void ClickSearchButton()
        {
            try
            {
                var searchSelectors = new[]
                {
                    "button[type='submit']",
                    ".search_button",
                    "[data-testid='search-button']",
                    "input[type='submit']",
                    "button:contains('Search')"
                };

                foreach (var selector in searchSelectors)
                {
                    try
                    {
                        var button = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(selector)));
                        button.Click();
                        TestContext.WriteLine($"Search button clicked using selector: {selector}");
                        return;
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Could not find search button: {ex.Message}");
            }
        }

        private void WaitForPageLoad()
        {
            try
            {
                _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                System.Threading.Thread.Sleep(2000); // Additional wait for dynamic content
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Page load wait failed: {ex.Message}");
            }
        }

        private void WaitForResults()
        {
            try
            {
                var resultSelectors = new[]
                {
                    ".card.style_1",
                    ".movie_card",
                    "[data-testid='movie-card']",
                    ".search_results .card",
                    ".movie-item"
                };

                foreach (var selector in resultSelectors)
                {
                    try
                    {
                        _wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector(selector)));
                        TestContext.WriteLine($"Results loaded using selector: {selector}");
                        System.Threading.Thread.Sleep(1000); // Small additional wait
                        return;
                    }
                    catch { continue; }
                }

                TestContext.WriteLine("Results may not have loaded, but continuing...");
                System.Threading.Thread.Sleep(3000); // Fallback wait
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Wait for results failed: {ex.Message}");
            }
        }

        public List<string> GetMovieTitles()
        {
            var titles = new List<string>();

            var cardSelectors = new[]
            {
                ".card.style_1",
                ".movie_card",
                "[data-testid='movie-card']",
                ".search_results .card",
                ".movie-item",
                ".card"
            };

            foreach (var cardSelector in cardSelectors)
            {
                try
                {
                    var cards = _driver.FindElements(By.CssSelector(cardSelector));
                    TestContext.WriteLine($"Found {cards.Count} cards using selector: {cardSelector}");

                    if (cards.Count > 0)
                    {
                        var titleSelectors = new[]
                        {
                            "h2 a", "h3 a", ".title a", "h2", "h3", ".title",
                            "[data-testid='movie-title']", ".movie-title",
                            "a[title]", ".card-title"
                        };

                        foreach (var card in cards.Take(20)) // Limit to first 20 for performance
                        {
                            foreach (var titleSelector in titleSelectors)
                            {
                                try
                                {
                                    var titleEl = card.FindElement(By.CssSelector(titleSelector));
                                    var title = titleEl.GetAttribute("title") ?? titleEl.Text?.Trim();

                                    if (!string.IsNullOrWhiteSpace(title))
                                    {
                                        titles.Add(title);
                                        TestContext.WriteLine($"Found movie: {title}");
                                        break; // Move to next card
                                    }
                                }
                                catch { continue; }
                            }
                        }

                        if (titles.Any())
                        {
                            TestContext.WriteLine($"Successfully extracted {titles.Count} titles using {cardSelector}");
                            return titles;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error with selector {cardSelector}: {ex.Message}");
                    continue;
                }
            }

            TestContext.WriteLine($"Total movie titles found: {titles.Count}");
            return titles;
        }

        public List<string> GetReleaseDates()
        {
            var dates = new List<string>();

            try
            {
                var cards = _driver.FindElements(By.CssSelector(".card, .movie_card, [data-testid='movie-card']"));

                foreach (var card in cards.Take(20))
                {
                    var dateSelectors = new[]
                    {
                        ".release_date", ".date", "[data-testid='release-date']",
                        ".card-date", "time", ".year"
                    };

                    foreach (var selector in dateSelectors)
                    {
                        try
                        {
                            var dateEl = card.FindElement(By.CssSelector(selector));
                            var dateText = dateEl.Text?.Trim();

                            if (!string.IsNullOrWhiteSpace(dateText))
                            {
                                dates.Add(dateText);
                                break;
                            }
                        }
                        catch { continue; }
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error extracting release dates: {ex.Message}");
            }

            return dates;
        }
    }
}