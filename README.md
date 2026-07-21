# Google Maps Extractor API

A powerful **ASP.NET Core Web API** for extracting business information and customer reviews from **Google Maps** using **Selenium WebDriver**.

**GitHub Repository:**  
https://github.com/mahmoudshawky19/GoogleMapsExtractorApi

---

# Features

## Places Extraction

- Extract business names
- Extract phone numbers
- Extract websites
- Extract addresses
- Extract ratings
- Extract review counts
- Configurable extraction limits (up to 5000 results)
- Headless browser support
- Configurable timeout

## Reviews Extraction

- Extract reviews from multiple Google Maps places
- Reviewer name
- Reviewer profile URL
- Star rating
- Review text
- Review date
- Parallel processing for improved performance

## Export

- Export places to Excel (.xlsx)
- Export places to CSV
- Export reviews to Excel

---

# Important Notice

This project relies on the current **Google Maps DOM structure**. Google frequently updates the Google Maps interface, which may change the HTML elements and selectors used by the scraper.

If this repository has not been updated for some time and the extractor no longer works correctly, you will most likely need to update the DOM selectors to match the latest Google Maps structure.

---

# Quick Start

## Prerequisites

- .NET 8 SDK
- Google Chrome

## Installation

```bash
git clone https://github.com/mahmoudshawky19/GoogleMapsExtractorApi.git

cd GoogleMapsExtractorApi

dotnet restore

dotnet run --project GoogleMapsExtractor.API
```

---

# API Endpoints

## Extract Places

**POST**

```
/api/Extraction/extract
```

Request

```json
{
  "keyword": "restaurants",
  "location": "Cairo",
  "targetResults": 100,
  "headless": true,
  "timeoutSeconds": 30
}
```

---

## Extract Reviews

**POST**

```
/api/Extraction/extract-reviews
```

Request

```json
{
  "links": [
    "https://www.google.com/maps/place/..."
  ],
  "maxReviewsPerPlace": 50,
  "headless": true,
  "timeoutSeconds": 30
}
```

---

## Export Places to Excel

**POST**

```
/api/Extraction/export/excel
```

---

## Export Reviews to Excel

**POST**

```
/api/Extraction/export/reviews-excel
```

---

## Export Places to CSV

**POST**

```
/api/Extraction/export/csv
```

---

# Technologies

- .NET 8
- ASP.NET Core Web API
- Selenium WebDriver
- ChromeDriver
- EPPlus
- Serilog
- Swagger (OpenAPI)

---


# Example Response

```json
[
  {
    "name": "Pizza Hut",
    "phone": "+20 100 000 0000",
    "website": "https://www.pizzahut.com",
    "address": "Cairo, Egypt",
    "rating": 4.5,
    "reviews": 1245
  }
]
```

---

# Request Parameters

| Parameter | Description |
|-----------|-------------|
| keyword | Search keyword |
| location | Search location |
| targetResults | Maximum number of places to extract |
| headless | Run Chrome in headless mode |
| timeoutSeconds | Maximum extraction timeout |

---

# License

This project is licensed under the **MIT License**.

---

# Author

**Mahmoud Shawky**

Email: **mahmoudshawky495@gmail.com**

GitHub: **https://github.com/mahmoudshawky19**

---

# Support

If you found this project useful, please consider giving it a **Star** on GitHub.

Contributions, issues, and feature requests are always welcome.
