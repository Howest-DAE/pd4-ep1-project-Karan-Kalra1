# Multiplayer Dodgeball

A two-player online dodgeball game built with **Unity**, **Netcode for
GameObjects**, **Unity Gaming Services**, **PlayFab**, and a custom
**ASP.NET Core REST API** backed by **SQL Server / Azure SQL Database**.

The project combines real-time multiplayer gameplay with persistent
player data, match statistics, a match-specific crown system, virtual
currency, and a backend API.

## Highlights

-   Online two-player matches using Unity Gaming Services Sessions/Relay
    and Netcode for GameObjects.
-   Lobby creation/joining, synchronized team selection, and ready
    states.
-   Networked player spawning and movement, ball pickup, aiming,
    throwing, and hit detection.
-   Server-authoritative hit validation and score updates.
-   PlayFab integration for player login, display names, and virtual
    currency.
-   Gold rewards for scoring and an extra-ball purchase flow.
-   Match-specific crowns: each player starts with five crowns, loses
    one when hit, and sees the remaining count during gameplay and on
    the Game Over screen.
-   ASP.NET Core REST API for player records, match creation, hit
    reporting, and match summaries.
-   Entity Framework Core database access and code-first migrations.
-   Azure App Service and Azure SQL Database deployment configuration.

## Technology Stack

  -----------------------------------------------------------------------
  Area                                Technology
  ----------------------------------- -----------------------------------
  Game engine                         Unity

  Multiplayer networking              Netcode for GameObjects

  Lobby and connection setup          Unity Gaming Services Sessions and
                                      Relay

  Player identity and economy         PlayFab

  Backend                             ASP.NET Core Web API

  API documentation/testing           Swagger / OpenAPI

  Data access                         Entity Framework Core

  Database                            SQL Server / Azure SQL Database

  Hosting                             Azure App Service

  UI                                  Unity UI Toolkit and Unity game UI
  -----------------------------------------------------------------------

## Gameplay Features

### Multiplayer sessions

Players can create or join a session through the lobby interface. Unity
Gaming Services handles session and Relay connection setup, while
Netcode for GameObjects synchronizes gameplay objects and network state.

The lobby tracks each connected player's identity, display name, team
selection, and ready state. The match progresses when the required
players are connected and ready.

### Networked dodgeball

The game supports networked player spawning and movement, ball pickup,
aiming, and throwing. The server handles authoritative hit detection and
match scoring so clients do not independently decide whether a hit
should count.

When a valid hit is registered, the match state is updated and the
appropriate gameplay effects and rewards are triggered.

### PlayFab login and Gold economy

PlayFab is used for player login and virtual currency. Players can earn
Gold for scoring and spend Gold to purchase an extra ball. The purchase
flow checks currency and item inventory before consuming the purchased
item and requesting a networked extra-ball spawn.

### Match-specific crown system

Crowns belong to a **single match**, not to a player's permanent
profile.

1.  When the backend creates a match, it creates a match-player record
    for each participant and initializes `CrownsRemaining` to `5`.
2.  Unity reads the crown count from backend match data rather than
    assigning the starting count in gameplay code.
3.  When a player is hit, the backend hit logic updates the victim's
    remaining crowns.
4.  The crown visual above the player displays the current count during
    gameplay.
5.  The Game Over screen displays final match statistics, including
    remaining crowns.

Because crowns are stored on a match-player record, a new match has its
own independent crown values.

### Match timer and Game Over

The networked match timer is synchronized between players. When the
timer expires, the match transitions to Game Over. The Game Over
interface retrieves match statistics from the backend and displays the
results for both participants.

### Player names and match statistics

Player display names are shown during gameplay. Match summaries include
per-player information such as host/client role, points scored, times
hit, and remaining crowns, depending on the fields returned by the API.

## Architecture Overview

``` text
Unity Client A                         Unity Client B
     |                                      |
     +------ Unity Sessions / Relay --------+
                       |
              Netcode for GameObjects
                       |
          Server-authoritative match state
                       |
                 BackendApiClient
                       |
               HTTPS REST requests
                       |
             ASP.NET Core Web API
             /        |         \
      Controllers   DTOs     AppDbContext
                                |
                       Entity Framework Core
                                |
                       SQL Server / Azure SQL
```

PlayFab is used alongside this architecture for player identity and the
Gold economy. The custom API stores and retrieves the application's
player and match records; it is separate from PlayFab's own services.

## Backend API

The backend is an ASP.NET Core Web API. It exposes HTTP endpoints that
Unity calls through `BackendApiClient`. Controllers receive requests,
process the requested operation, use Entity Framework Core to access the
database, and return JSON responses.

### Main responsibilities

-   Create or update a player record using the player's PlayFab ID and
    display name.
-   Retrieve player information.
-   Create a match for two registered players.
-   Initialize match-player statistics, including points and starting
    crowns.
-   Record hits and update match statistics.
-   Return a match summary for the Game Over interface.

### Main endpoints


  --------------------------------------------------------------------------------
  Method                  Route                            Purpose
  ----------------------- -------------------------------- -----------------------
  `POST`                  `/Players`                       Create or upsert a
                                                           player record.

  `GET`                   `/Players/{playFabId}`           Retrieve a player by
                                                           PlayFab ID, if exposed
                                                           by the current
                                                           controller.

  `POST`                  `/Matches`                       Create a match between
                                                           host and client
                                                           players.

  `GET`                   `/Matches/{matchId}`             Retrieve a match
                                                           summary and player
                                                           statistics.

  `POST`                  `/Matches/{matchId}/PlayerHit`   Report a hit and update
                                                           match statistics.
  --------------------------------------------------------------------------------

### Example: register a player

`POST /Players`

``` json
{
  "playFabId": "PLAYFAB_PLAYER_ID",
  "displayName": "PlayerOne"
}
```

The API uses the PlayFab ID to associate the backend record with the
player identity used by Unity.

### Example: create a match

`POST /Matches`

``` json
{
  "hostPlayFabId": "PLAYFAB_HOST_ID",
  "clientPlayFabId": "PLAYFAB_CLIENT_ID"
}
```

The API finds the corresponding player records, creates a match, and
adds a match-player record for each participant. Match-player data
includes fields such as host role, points scored, and crowns remaining.

### Example: report a hit

`POST /Matches/{matchId}/PlayerHit`

``` json
{
  "attackerPlayFabId": "PLAYFAB_ATTACKER_ID",
  "victimPlayFabId": "PLAYFAB_VICTIM_ID"
}
```

The API uses the match ID and player IDs to identify the relevant
match-player records and update match statistics. The victim's crown
count is match-specific and is reduced by the hit logic.

### Example: retrieve a match summary

`GET /Matches/{matchId}`

The response contains the match and its player statistics. The exact
JSON shape depends on the DTO returned by the current implementation,
but it includes data Unity needs to show match results, such as player
identities, points, and remaining crowns.

### How Unity communicates with the API

1.  A Unity script calls a method on `BackendApiClient`.
2.  `BackendApiClient` sends an HTTP request to the configured API base
    URL.
3.  The API routes the request to the appropriate controller action.
4.  The controller validates the request and uses `AppDbContext` to
    query or modify the database.
5.  Entity Framework Core translates the data operations into SQL.
6.  The API returns an HTTP status code and, when applicable, a JSON
    response.
7.  Unity deserializes the response into a DTO and updates relevant game
    UI or networked state.


## Database Design

Entity Framework Core maps C# model classes to SQL tables. The main
domain tables are:

  -----------------------------------------------------------------------
  Table                               Purpose
  ----------------------------------- -----------------------------------
  `Players`                           Stores backend player records
                                      associated with PlayFab IDs and
                                      display names.

  `Matches`                           Stores each match record.

  `MatchPlayers`                      Joins players to a match and stores
                                      match-specific values such as host
                                      role, points scored, and remaining
                                      crowns.

  `PlayerHits`                        Stores hit records, where
                                      represented by the current
                                      model/controller implementation.

  `__EFMigrationsHistory`             Internal EF Core table that tracks
                                      which migrations have been applied.
  -----------------------------------------------------------------------



### Entity Framework Core migrations

Migrations version the database schema as the models evolve. When a
model changes, a migration describes the schema change; applying it
updates the target database.

Typical Package Manager Console commands:

``` powershell
Add-Migration AddCrownsRemaining
Update-Database
```

-   `Add-Migration` compares the current EF Core model with the previous
    model snapshot and generates migration code.
-   `Update-Database` applies pending migrations to the database
    configured by the active connection string.
-   `__EFMigrationsHistory` records which migrations have been applied.



## Configuration and Running Locally

### Prerequisites

-   Unity and the project's required Unity packages.
-   A compatible .NET SDK for the API's configured target framework.
-   SQL Server LocalDB, SQL Server Express, or another SQL Server
    database.
-   PlayFab title/configuration for the login and economy features.
-   Unity Gaming Services project configuration for Sessions/Relay.
-   Visual Studio or another .NET-compatible editor.

### Run the API

1.  Open the API solution in Visual Studio.

2.  Create a local `appsettings.json` or user-secrets configuration with
    your own database connection string.

3.  Ensure the key matches the one used by the API, typically
    `ConnectionStrings:DefaultConnection`.

4.  Apply migrations to the local database:

    ``` powershell
    Update-Database
    ```

5.  Run the ASP.NET Core project.

6.  Open the local Swagger page, typically at the `/swagger` route shown
    in the run output.

Example configuration template :

``` json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_LOCAL_SQL_SERVER_CONNECTION_STRING"
  }
}
```



### Run the Unity game

1.  Open the Unity project.
2.  Configure the required Unity Services project and PlayFab settings.
3.  Set `BackendApiClient` to the local API base URL when testing
    locally.
4.  Start the API and then run the Unity game.
5.  Use two clients to test lobby joining, team selection, gameplay
    synchronization, hits, crowns, Gold rewards, and match results.

For a deployed build, configure `BackendApiClient` with the deployed
HTTPS API root URL.

## Cloud Deployment

The backend is designed to run on **Azure App Service**, with persistent
data in **Azure SQL Database**.

A typical deployment process is:

1.  Configure the Azure SQL database and firewall access.
2.  Apply EF Core migrations to the Azure database.
3.  Configure the App Service connection string through Azure
    application settings rather than committing credentials.
4.  Publish the ASP.NET Core API to App Service.
5.  Verify the deployed API through its Swagger page.
6.  Update Unity's API base URL to the deployed HTTPS endpoint.



