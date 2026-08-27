FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY FacultyManagement.sln global.json ./
COPY FacultyManagement.Api/FacultyManagement.Api.csproj FacultyManagement.Api/
COPY FacultyManagement.Business/FacultyManagement.Business.csproj FacultyManagement.Business/
COPY FacultyManagement.Data/FacultyManagement.Data.csproj FacultyManagement.Data/
RUN dotnet restore FacultyManagement.Api/FacultyManagement.Api.csproj
COPY . .
RUN dotnet publish FacultyManagement.Api/FacultyManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FacultyManagement.Api.dll"]
