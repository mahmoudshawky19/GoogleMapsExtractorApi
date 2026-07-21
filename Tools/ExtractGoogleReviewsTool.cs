using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tools
{
    public class ExtractGoogleReviewsTool
    {
        private readonly ILogger<ExtractGoogleReviewsTool> _logger;
        private const int MAX_PARALLEL = 2;

        public ExtractGoogleReviewsTool(ILogger<ExtractGoogleReviewsTool> logger)
        {
            _logger = logger;
        }

        public async Task<GoogleReviewsResult> ExecuteAsync(
            GoogleReviewsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var taskId = Guid.NewGuid().ToString();

            _logger.LogInformation("Starting reviews extraction for {Count} places",
                parameters.Links.Count);

            if (parameters.Links == null || parameters.Links.Count == 0)
            {
                return new GoogleReviewsResult
                {
                    Success = false,
                    TaskId = taskId,
                    Message = "No links provided",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                var result = await ExtractReviewsParallel(
                    parameters,
                    taskId,
                    cancellationToken,
                    progress);

                stopwatch.Stop();
                result.ElapsedTime = stopwatch.Elapsed;

                _logger.LogInformation("Reviews extraction completed: {Count} reviews from {Places} places in {Elapsed}",
                    result.TotalCount, result.PlacesWithReviews, stopwatch.Elapsed);

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Reviews extraction cancelled by user");

                return new GoogleReviewsResult
                {
                    Success = true,
                    TaskId = taskId,
                    TotalCount = 0,
                    Results = new List<GoogleReview>(),
                    Message = "Task stopped by user (partial results)",
                    Timestamp = DateTime.UtcNow,
                    IsPartialResult = true,
                    ElapsedTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during reviews extraction");
                throw;
            }
        }

        private async Task<GoogleReviewsResult> ExtractReviewsParallel(
            GoogleReviewsParams parameters,
            string taskId,
            CancellationToken cancellationToken,
            IProgress<(int current, int total, string message)>? progress)
        {
            var results = new ConcurrentBag<GoogleReview>();
            var processedPlaces = new ConcurrentBag<string>();
            int processedCount = 0;
            int totalCount = parameters.Links.Count;

            progress?.Report((0, totalCount, $"Starting extraction of reviews from {totalCount} places..."));

            var semaphore = new SemaphoreSlim(MAX_PARALLEL);
            var tasks = new List<Task>();

            for (int i = 0; i < parameters.Links.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var link = parameters.Links[i];
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var reviews = await ExtractSinglePlaceReviews(
                            link,
                            parameters.Headless,
                            parameters.TimeoutSeconds,
                            parameters.MaxReviewsPerPlace,
                            cancellationToken);

                        if (reviews != null && reviews.Any())
                        {
                            foreach (var review in reviews)
                            {
                                review.PlaceUrl = link;
                                results.Add(review);
                            }
                            processedPlaces.Add(link);
                        }

                        var current = Interlocked.Increment(ref processedCount);
                        progress?.Report((current, totalCount,
                            $"Extracted reviews from {current}/{totalCount} places..."));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error extracting reviews from {Link}: {Error}", link, ex.Message);
                        Interlocked.Increment(ref processedCount);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            var resultList = results.ToList();
            int placesWithReviews = processedPlaces.Distinct().Count();

            progress?.Report((totalCount, totalCount,
                $"Completed! Extracted {resultList.Count} reviews from {placesWithReviews} places"));

            return new GoogleReviewsResult
            {
                Success = resultList.Count > 0,
                TaskId = taskId,
                TotalCount = resultList.Count,
                Results = resultList,
                PlacesProcessed = processedCount,
                PlacesWithReviews = placesWithReviews,
                Message = $"Extracted {resultList.Count} reviews from {placesWithReviews} places",
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<List<GoogleReview>> ExtractSinglePlaceReviews(
            string url,
            bool headless,
            int timeoutSeconds,
            int maxReviews,
            CancellationToken cancellationToken)
        {
            IWebDriver? driver = null;
            var allReviews = new List<GoogleReview>();

            try
            {
                driver = CreateChromeDriver(headless);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

                driver.Navigate().GoToUrl(url);
                await Task.Delay(3000, cancellationToken);

                driver.Navigate().Refresh();
                await Task.Delay(3000, cancellationToken);

                wait.Until(d => d.FindElements(By.XPath("//div[@aria-label and @role='main']//h1")).Count > 0);

                string placeName = GetPlaceName(driver);

                ClickReviewsTab(driver, wait, cancellationToken);

                wait.Until(d => d.FindElements(By.XPath("//div[@data-review-id]")).Count > 0);

                var scrollContainer = FindScrollContainer(driver);

                var extractedIds = new HashSet<string>();
                int noNewReviews = 0;
                const int maxNoNewReviews = 5;

                while (allReviews.Count < maxReviews &&
                       noNewReviews < maxNoNewReviews &&
                       !cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reviewElements = driver.FindElements(By.XPath("//div[@data-review-id]"));

                    int beforeCount = extractedIds.Count;

                    foreach (var reviewElement in reviewElements)
                    {
                        if (allReviews.Count >= maxReviews)
                            break;

                        string reviewId = reviewElement.GetAttribute("data-review-id") ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(reviewId) || !extractedIds.Add(reviewId))
                            continue;

                        var review = ExtractReviewFromElement(reviewElement);
                        review.PlaceUrl = url;
                        review.PlaceName = placeName;
                        review.ExtractedAt = DateTime.Now;

                        allReviews.Add(review);
                    }

                    if (extractedIds.Count == beforeCount)
                    {
                        noNewReviews++;
                        _logger.LogDebug("No new reviews found ({NoNew}/{MaxNoNew})",
                            noNewReviews, maxNoNewReviews);

                        if (noNewReviews >= maxNoNewReviews)
                        {
                            _logger.LogDebug("Reached end of reviews.");
                            break;
                        }
                    }
                    else
                    {
                        noNewReviews = 0;
                        _logger.LogDebug("Extracted {Count} reviews so far from {Url}",
                            allReviews.Count, url);
                    }

                    await ScrollForMoreReviews(driver, scrollContainer, cancellationToken);

                    try
                    {
                        await Task.Delay(1500, cancellationToken);
                        wait.Until(d => d.FindElements(By.XPath("//div[@data-review-id]")).Count > 0);
                    }
                    catch
                    {
                    }
                }

                _logger.LogInformation("Extracted {Count} reviews from {Url}", allReviews.Count, url);
                return allReviews;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extracting reviews from {Url}: {Error}", url, ex.Message);
                return allReviews;
            }
            finally
            {
                driver?.Quit();
                driver?.Dispose();
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

        private string GetPlaceName(IWebDriver driver)
        {
            try
            {
                var nameElement = driver.FindElement(By.XPath("//div[@aria-label and @role='main']//h1"));
                return nameElement.Text.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ClickReviewsTab(IWebDriver driver, WebDriverWait wait, CancellationToken cancellationToken)
        {
            try
            {
                var reviewsTab = driver.FindElements(By.XPath(
                    "//div[contains(text(),'Reviews') or contains(text(),'مراجعات') or contains(text(),'Recensioni')]"));

                if (reviewsTab.Count > 0 && reviewsTab[0].Displayed)
                {
                    reviewsTab[0].Click();
                    Task.Delay(2000, cancellationToken).Wait(cancellationToken);
                    return;
                }

                var reviewsButton = driver.FindElement(By.XPath(
                    "//button[contains(@aria-label, 'Reviews') or contains(@aria-label, 'مراجعات')]"));
                reviewsButton.Click();
                Task.Delay(2000, cancellationToken).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not click reviews tab: {Error}", ex.Message);
                throw;
            }
        }

        private IWebElement FindScrollContainer(IWebDriver driver)
        {
            string[] scrollXpaths = new[]
            {
                "//div[@data-review-id]/parent::div/parent::div",
                "//div[@role='feed']",
                "//div[contains(@class, 'm6QErb')]",
                "//div[contains(@class, 'DxyBCb')]",
                "//body"
            };

            foreach (var xpath in scrollXpaths)
            {
                try
                {
                    var elements = driver.FindElements(By.XPath(xpath));
                    if (elements.Count > 0)
                    {
                        _logger.LogDebug("Found scroll container using: {XPath}", xpath);
                        return elements[0];
                    }
                }
                catch { }
            }

            _logger.LogWarning("Using body as scroll container");
            return driver.FindElement(By.TagName("body"));
        }

        private GoogleReview ExtractReviewFromElement(IWebElement reviewElement)
        {
            var review = new GoogleReview
            {
                ReviewId = reviewElement.GetAttribute("data-review-id") ?? string.Empty
            };

            try
            {
                var nameElement = reviewElement.FindElements(By.XPath(".//div[@data-review-id]//button//div[contains(@class, 'fontBodyMedium')]"));
                if (nameElement.Count > 0)
                    review.ReviewerName = nameElement[0].Text.Trim();
            }
            catch { }

            try
            {
                var profileElement = reviewElement.FindElements(By.XPath(".//button[@data-href]"));
                if (profileElement.Count > 0)
                    review.ProfileUrl = profileElement[0].GetAttribute("data-href") ?? string.Empty;
            }
            catch { }

            try
            {
                var starsElement = reviewElement.FindElements(By.XPath(".//span[@role='img']"));
                if (starsElement.Count > 0)
                {
                    string ariaLabel = starsElement[0].GetAttribute("aria-label") ?? string.Empty;
                    var match = Regex.Match(ariaLabel, @"([\d.]+)");
                    review.Stars = match.Success ? match.Value : string.Empty;
                }
            }
            catch { }

            try
            {
                var textElement = reviewElement.FindElements(By.XPath(".//span[contains(@class, 'wiI7pd') or contains(@class, 'review-text')]"));
                if (textElement.Count > 0)
                    review.Text = textElement[0].Text.Trim();
                else
                {
                    var altText = reviewElement.FindElements(By.XPath(".//div[@data-review-id]//div[contains(@class, 'MyEn0d')]"));
                    if (altText.Count > 0)
                        review.Text = altText[0].Text.Trim();
                }
            }
            catch { }

            try
            {
                var timeElement = reviewElement.FindElements(By.XPath(".//span[contains(@class, 'rsqaWe')]"));
                if (timeElement.Count > 0)
                    review.Time = timeElement[0].Text.Trim();
            }
            catch { }

            return review;
        }

        private async Task ScrollForMoreReviews(
            IWebDriver driver,
            IWebElement scrollContainer,
            CancellationToken cancellationToken)
        {
            try
            {
                var js = (IJavaScriptExecutor)driver;

                js.ExecuteScript(@"
                    arguments[0].scrollBy({
                        top: window.innerHeight,
                        behavior: 'smooth'
                    });
                ", scrollContainer);

                await Task.Delay(1000, cancellationToken);
            }
            catch
            {
                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(
                        "arguments[0].scrollTop += 800;",
                        scrollContainer);
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(
                        "window.scrollTo(0, document.body.scrollHeight);");
                }
            }
        }
    }
}
