FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/ChatApp.Bot/ChatApp.Bot.csproj
RUN dotnet publish src/ChatApp.Bot/ChatApp.Bot.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ChatApp.Bot.dll"]
