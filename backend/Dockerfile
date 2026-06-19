FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskBoard.slnx ./
COPY src/TaskBoard.Domain/TaskBoard.Domain.csproj src/TaskBoard.Domain/
COPY src/TaskBoard.Application/TaskBoard.Application.csproj src/TaskBoard.Application/
COPY src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj src/TaskBoard.Infrastructure/
COPY src/TaskBoard.Api/TaskBoard.Api.csproj src/TaskBoard.Api/

RUN dotnet restore src/TaskBoard.Api/TaskBoard.Api.csproj

COPY . .
RUN dotnet publish src/TaskBoard.Api/TaskBoard.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskBoard.Api.dll"]
