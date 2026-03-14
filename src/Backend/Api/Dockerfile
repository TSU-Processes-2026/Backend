FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef \
    && export PATH="$PATH:/root/.dotnet/tools"

COPY src/Backend/Api/Api.csproj src/Backend/Api/
COPY src/Backend/Application/Application.csproj src/Backend/Application/
COPY src/Backend/Domain/Domain.csproj src/Backend/Domain/
COPY src/Backend/Infrastructure/Infrastructure.csproj src/Backend/Infrastructure/

RUN dotnet restore src/Backend/Api/Api.csproj

COPY src/Backend/ ./src/Backend/

RUN dotnet build src/Backend/Api/Api.csproj -c Release
RUN dotnet publish src/Backend/Api/Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "export PATH=\"$PATH:/root/.dotnet/tools\" && dotnet ef database update --project src/Backend/Infrastructure --startup-project src/Backend/Api && dotnet Api.dll"]