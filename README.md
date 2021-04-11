# Availabot
A Discord bot written using Disqord to track user availability

## Usage

 - Use the setup command to initialise the bot in its own channel
 - React to the message or use a command (!help) to set your status
 - Look at the message to find people who are available and @Mention the configured Available role to ping available people

## Setup

 - This project uses EF Core with Postgresql, so set that up and apply migrations and stuff.
 - Use an appsettings.json file with this template:
 ```json
 {
  "token": "your-bot-token",
  "userId": 000000000000000000,
  "prefix": "your-desired-prefix",

  "database": {
    "server": "your-server-ip",
    "port": 5432,
    "database": "your-database-name",
    "userId": "your-username",
    "password": "your-password"
  }
}
 ```
 
 ## Contributing
 
Issues and PRs are welcome.
