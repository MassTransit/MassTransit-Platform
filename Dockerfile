# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY ./src/*.sln .
COPY ./src/Directory.Build.props .
COPY ./src/MassTransit.Platform/*.csproj ./MassTransit.Platform/
COPY ./src/MassTransit.Platform.Abstractions/*.csproj ./MassTransit.Platform.Abstractions/
COPY ./src/MassTransit.Platform.Runtime/*.csproj ./MassTransit.Platform.Runtime/
RUN dotnet restore -r linux-musl-x64

COPY ./src/MassTransit.Platform ./MassTransit.Platform
COPY ./src/MassTransit.Platform.Abstractions ./MassTransit.Platform.Abstractions
COPY ./src/MassTransit.Platform.Runtime ./MassTransit.Platform.Runtime
RUN dotnet publish ./MassTransit.Platform.Runtime/MassTransit.Platform.Runtime.csproj -c Release -o /runtime -r linux-musl-x64 --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
RUN apk add --no-cache icu-libs && \
    mkdir /app

WORKDIR /runtime
COPY --from=build /runtime ./
ENV MT_APP=/app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 80 443

ENTRYPOINT ["dotnet", "/runtime/MassTransit.Platform.Runtime.dll"]
