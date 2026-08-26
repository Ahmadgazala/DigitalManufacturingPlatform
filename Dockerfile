FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DMP.Web/DMP.Web.csproj src/DMP.Web/
RUN dotnet restore src/DMP.Web/DMP.Web.csproj
COPY src/ src/
RUN dotnet publish src/DMP.Web/DMP.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
ENV DOTNET_HOSTBUILDER__RELOADCONFIG=false
EXPOSE 10000
ENTRYPOINT ["dotnet", "DMP.Web.dll"]
