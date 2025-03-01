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
- TMDB API Key

## Setup

1. Clone the repository
2. Add your TMDB API key to `appsettings.json`:
   ```json
   {
     "MovieDbSettings": {
       "ApiKey": "your-api-key-here"
     }
   }
   ```
3. Run the application:
   ```bash
   dotnet run
   ```

## Configuration

The application uses `appsettings.json` for configuration. Make sure to keep your API key secure and never commit it to version control.

## Contributing

Feel free to submit issues and enhancement requests!
