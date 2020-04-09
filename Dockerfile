# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
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

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine
WORKDIR /runtime
ARG MT_APP=/app
ENV MT_APP="${MT_APP}"
#ARG MT_TRANSPORT=
#ENV MT_TRANSPORT="${MT_TRANSPORT}"
#ARG MT_RMQ_HOST=
#ENV MT_RMQ_HOST="${MT_RMQ_HOST}"
#ARG MT_RMQ_PORT=
#ENV MT_RMQ_PORT="${MT_RMQ_PORT}"
#ARG MT_RMQ_SSL=
#ENV MT_RMQ_SSL="${MT_RMQ_SSL}"
#ARG MT_RMQ_VHOST=
#ENV MT_RMQ_VHOST="${MT_RMQ_VHOST}"
#ARG MT_RMQ_USERNAME=
#ENV MT_RMQ_USERNAME="${MT_RMQ_USERNAME}"
#ARG MT_RMQ_PASSWORD=
#ENV MT_RMQ_PASSWORD="${MT_RMQ_PASSWORD}"
COPY --from=build /runtime ./
EXPOSE 80 443
RUN mkdir /app

ENTRYPOINT ["dotnet", "/runtime/MassTransit.Platform.Runtime.dll"]
