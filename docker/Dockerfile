FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Fun88.Web/Fun88.Web.csproj Fun88.Web/
RUN dotnet restore Fun88.Web/Fun88.Web.csproj
COPY src/Fun88.Web/ Fun88.Web/
RUN dotnet publish Fun88.Web/Fun88.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fun88.Web.dll"]
