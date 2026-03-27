FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV DB_PATH=/app/data/machinepro.db
RUN mkdir -p /app/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "MachinePro.dll"]
