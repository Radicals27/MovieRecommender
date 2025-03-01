# Movie Recommender

A .NET Core web application that provides movie recommendations using The Movie Database (TMDB) API. The application allows users to discover movies based on various filters such as release year, minimum rating, and genres.

## Features

- Movie discovery with filtering options
- Genre-based filtering
- Rating-based sorting
- Release year filtering
- Clean and modern UI
- Integration with TMDB API

## Prerequisites

- .NET 6.0 or later
- TMDB API Key (get one at https://www.themoviedb.org/settings/api)

## Setup

1. Clone the repository
2. Copy `appsettings.template.json` to `appsettings.json` and `appsettings.Development.json`
3. Add your TMDB API key to both configuration files:
   ```json
   {
     "MovieDbSettings": {
       "ApiKey": "your-api-key-here"
     }
   }
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

## Configuration

The application uses `appsettings.json` for configuration. For security:

- Never commit your actual API key to version control
- Use `appsettings.template.json` as a template
- Keep your API key secure and use different keys for development and production

## Contributing

Feel free to submit issues and enhancement requests!
