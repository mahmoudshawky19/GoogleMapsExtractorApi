using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Tools
{
    public class ExtractGoogleMapsTool
    {
        private readonly ILogger<ExtractGoogleMapsTool> _logger;

        public ExtractGoogleMapsTool(ILogger<ExtractGoogleMapsTool> logger)
        {
            _logger = logger;
        }

        public async Task<GoogleMapsResult> ExecuteAsync(
            GoogleMapsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var taskId = Guid.NewGuid().ToString();

            _logger.LogInformation("Starting extraction for keyword: {Keyword}, location: {Location}",
                parameters.Keyword, parameters.Location);

            IWebDriver? driver = null;
            var results = new List<GoogleMapsPlace>();

            try
            {
                driver = CreateChromeDriver(parameters.Headless);
                var js = (IJavaScriptExecutor)driver;
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(parameters.TimeoutSeconds));

                progress?.Report((0, parameters.TargetResults, "Opening browser..."));

                string searchUrl = BuildSearchUrl(parameters.Keyword, parameters.Location);
                driver.Navigate().GoToUrl(searchUrl);
                await Task.Delay(3000, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var resultsPanel = FindResultsPanel(driver, wait);
                if (resultsPanel == null)
                {
                    return new GoogleMapsResult
                    {
                        Success = false,
                        TaskId = taskId,
                        Message = "Could not find results panel",
                        Timestamp = DateTime.UtcNow
                    };
                }

                progress?.Report((0, parameters.TargetResults, "Found results panel, starting extraction..."));

                var extractedUrls = new HashSet<string>();
                int noNewResults = 0;
                const int maxNoNewResults = 15;
                int extractedCount = 0;

                while (extractedCount < parameters.TargetResults &&
                       noNewResults < maxNoNewResults &&
                       !cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var articles = driver.FindElements(By.XPath("//div[@role='article']"));
                    int extractedThisRound = 0;

                    foreach (var article in articles)
                    {
                        if (cancellationToken.IsCancellationRequested || extractedCount >= parameters.TargetResults)
                            break;

                        try
                        {
                            var place = ExtractPlaceFromArticle(article);
                            if (place != null && extractedUrls.Add(place.MapsUrl))
                            {
                                results.Add(place);
                                extractedCount++;
                                extractedThisRound++;

                                progress?.Report((extractedCount, parameters.TargetResults,
                                    $"Extracted {extractedCount}/{parameters.TargetResults} places..."));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error extracting place: {Error}", ex.Message);
                        }
                    }

                    if (extractedThisRound == 0)
                        noNewResults++;
                    else
                        noNewResults = 0;

                    await TryLoadMoreResults(driver, js, resultsPanel, wait, cancellationToken);

                    await Task.Delay(1000, cancellationToken);
                }

                stopwatch.Stop();

                var result = new GoogleMapsResult
                {
                    Success = extractedCount > 0,
                    TaskId = taskId,
                    TotalCount = extractedCount,
                    Results = results,
                    Keyword = parameters.Keyword,
                    Location = parameters.Location,
                    Message = $"Successfully extracted {extractedCount} places",
                    Timestamp = DateTime.UtcNow,
                    IsPartialResult = extractedCount < parameters.TargetResults,
                    ElapsedTime = stopwatch.Elapsed
                };

                _logger.LogInformation("Extraction completed: {Count} places in {Elapsed}",
                    extractedCount, stopwatch.Elapsed);

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Extraction cancelled by user");

                return new GoogleMapsResult
                {
                    Success = true,
                    TaskId = taskId,
                    TotalCount = results.Count,
                    Results = results,
                    Keyword = parameters.Keyword,
                    Location = parameters.Location,
                    Message = $"Task stopped by user. Extracted {results.Count} places",
                    Timestamp = DateTime.UtcNow,
                    IsPartialResult = true,
                    ElapsedTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during extraction");

                return new GoogleMapsResult
                {
                    Success = false,
                    TaskId = taskId,
                    TotalCount = results.Count,
                    Results = results,
                    Keyword = parameters.Keyword,
                    Location = parameters.Location,
                    Message = $"Error: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    ElapsedTime = stopwatch.Elapsed
                };
            }
            finally
            {
                driver?.Quit();
                driver?.Dispose();
                _logger.LogInformation("Browser closed");
            }
        }

        private ChromeDriver CreateChromeDriver(bool headless)
        {
            var options = new ChromeOptions();

            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--log-level=3");
            options.AddArgument("--silent");

            if (headless)
            {
                options.AddArgument("--headless=new");
            }

            options.AddUserProfilePreference("download.default_directory", Path.GetTempPath());
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            service.SuppressInitialDiagnosticInformation = true;

            return new ChromeDriver(service, options);
        }

        private string BuildSearchUrl(string keyword, string location)
        {
            string url = $"https://www.google.com/maps/search/{Uri.EscapeDataString(keyword)}";
            if (!string.IsNullOrEmpty(location))
            {
                url += $"+{Uri.EscapeDataString(location)}";
            }
            return url;
        }

        private IWebElement? FindResultsPanel(IWebDriver driver, WebDriverWait wait)
        {
            try
            {
                return wait.Until(d =>
                    d.FindElement(By.XPath("//div[@aria-label and (contains(@aria-label,'Results for') or contains(@aria-label,'نتائج'))]"))
                );
            }
            catch
            {
                try
                {
                    return driver.FindElement(By.XPath("//div[@role='feed']"));
                }
                catch
                {
                    return null;
                }
            }
        }

        private GoogleMapsPlace? ExtractPlaceFromArticle(IWebElement article)
        {
            var mapLink = article.FindElements(By.XPath(".//a[contains(@href,'/maps/place/')]"));
            if (mapLink.Count == 0) return null;

            string mapsUrl = mapLink[0].GetAttribute("href") ?? string.Empty;
            if (string.IsNullOrEmpty(mapsUrl)) return null;

            var place = new GoogleMapsPlace
            {
                MapsUrl = mapsUrl,
                ExtractedAt = DateTime.Now
            };

            var nameElement = article.FindElements(By.XPath(".//a[@aria-label]"));
            if (nameElement.Count > 0)
            {
                string name = nameElement[0].GetAttribute("aria-label") ?? string.Empty;
                int index = name.IndexOf('·');
                if (index >= 0)
                    name = name.Substring(0, index).Trim();
                place.Name = name;
            }

            var websiteElement = article.FindElements(By.XPath(".//a[@data-value='Website' or @data-value='الموقع الإلكتروني']"));
            if (websiteElement.Count > 0)
            {
                place.Website = websiteElement[0].GetAttribute("href") ?? string.Empty;
            }

            var phoneElement = article.FindElements(By.XPath(".//span[contains(@class,'UsdlK')]"));
            if (phoneElement.Count > 0)
            {
                place.Phone = phoneElement[0].Text;
            }

            var categoryElement = article.FindElements(By.XPath(".//span[@dir='rtl']"));
            if (categoryElement.Count > 0)
            {
                place.Category = categoryElement[0].Text;
            }

            var ratingElement = article.FindElements(By.XPath(".//span[@role='img' and @aria-label]"));
            if (ratingElement.Count > 0)
            {
                string aria = ratingElement[0].GetAttribute("aria-label") ?? string.Empty;
                var numbers = Regex.Matches(aria, @"[\d.]+")
                    .Cast<Match>()
                    .Select(x => x.Value)
                    .ToList();

                if (numbers.Count > 0)
                    place.Rating = numbers[0];
                if (numbers.Count > 1)
                    place.Reviews = numbers[1];
            }

            var addressElement = article.FindElements(By.XPath(".//div[contains(@class,'W4Efsd')]//span"));
            foreach (var el in addressElement)
            {
                string text = el.Text.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                if (Regex.IsMatch(text, @"^\d+(\.\d+)?\s*\(\d[\d,]*\)$")) continue;
                if (text.Contains("·")) continue;
                if (text.Length > 5)
                {
                    place.Address = text == "ليست هناك مراجعات" ? "-" : text;
                    break;
                }
            }

            return place;
        }

        private async Task TryLoadMoreResults(
            IWebDriver driver,
            IJavaScriptExecutor js,
            IWebElement resultsPanel,
            WebDriverWait wait,
            CancellationToken cancellationToken)
        {
            try
            {
                var loadMoreBtn = driver.FindElements(By.XPath(
                    "//button[contains(@aria-label,'Load more results') or contains(@aria-label,'تحميل المزيد')]"));

                if (loadMoreBtn.Count > 0 && loadMoreBtn[0].Displayed)
                {
                    js.ExecuteScript("arguments[0].click();", loadMoreBtn[0]);
                    await Task.Delay(2000, cancellationToken);
                    return;
                }
            }
            catch { }

            try
            {
                js.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", resultsPanel);
            }
            catch
            {
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            }
        }
    }
}