# MovieDBTests – Selenium + HttpClient (.NET 8) Hybrid Framework

Automation framework that covers UI (Selenium) and API (TMDB /discover/movie) to filter movies by release date, genres, and user score, then validates sorting and compares API vs UI.

## Requirements (from assignment)

- UI Filters:
  - Sort by release date ascending
  - Select one or multiple Genres
  - Search by release date 1990–2005
  - Bonus: implement via calendar inputs
  - Bonus: implement user score
- Validate the filtering was done correctly
- API:
  - Use `/discover/movie`
  - Apply the same filters
  - Compare results

## Tech Stack

- C# / .NET 8
- NUnit + Microsoft.NET.Test.Sdk
- Selenium WebDriver (Chrome)
- HttpClient for APIs (no external dependency required)
- Newtonsoft.Json

## Project Structure

```
MovieDBTests
 ├─ API/                 # API client for TMDB
 ├─ Drivers/             # WebDriver setup
 ├─ Models/              # DTOs if needed
 ├─ Pages/               # Page Object Model (UI)
 ├─ Tests/API            # API tests
 ├─ Tests/UI             # UI tests
 ├─ Utils/               # Config, waits, helpers
 └─ Config/appsettings.json
```

## Prerequisites

- **Visual Studio 2022 Community** (recommended) or JetBrains Rider
- **.NET 8 SDK**
- **Google Chrome** installed
- A **TMDB API key**: https://developers.themoviedb.org/3/getting-started/introduction

## Configuration

Put your TMDB API key in one of the following:
1. Environment variable `TMDB_API_KEY` (recommended), or
2. `Config/appsettings.json` → `API.ApiKey`

`Config/appsettings.json` also contains:
- `UI.DiscoverUrl`: https://www.themoviedb.org/discover/movie
- `API.BaseUrl`: https://api.themoviedb.org/3
- `Browser.Headless`: set `true` for headless runs in CI

## How to Run

1. Open `MovieDBTests.sln` in Visual Studio.
2. Restore NuGet packages.
3. Set environment variable (Windows PowerShell example):
   ```powershell
   $env:TMDB_API_KEY="YOUR_KEY_HERE"
   ```
4. Run tests:
   - From Test Explorer in Visual Studio **or**
   - CLI:
     ```bash
     dotnet test
     ```

## Notes / Known caveats

- TMDB frequently updates selectors. `Pages/DiscoverPage.cs` uses *defensive* selectors; you might need to tweak them.
- Calendar interaction varies; the framework falls back to query-parameter navigation if inputs aren’t found.
- UI vs API full list comparison is sensitive to paging; sample compares ordering and date constraints. You can extend to intersect titles between UI and API for strict comparison.

## Screenshots

Tests attach screenshots automatically on TearDown into the test output directory (`screenshot_<testname>.png`).

## Extending

- Add more Page Objects if you navigate to detail pages.
- Add a `GenreService` using `/genre/movie/list` and map names to ids for strict API-vs-UI genre validation.
- Replace crude user-score slider with a robust action (determine min/max attributes and compute offset).
```)


### Integration Tests

- `UiVsApiComparisonTests` compares UI vs API titles for Drama 1990–2005.
