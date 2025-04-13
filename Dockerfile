FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY lotus2mqtt/ ./lotus2mqtt/
ARG TARGETARCH
RUN if [ "$TARGETARCH" = "amd64" ]; then \
    RID=linux-musl-x64 ; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
    RID=linux-musl-arm64 ; \
    elif [ "$TARGETARCH" = "arm" ]; then \
    RID=linux-musl-arm ; \
    fi \
    && dotnet publish -c Release -o out -r $RID --sc lotus2mqtt/lotus2mqtt.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["/app/lotus2mqtt", "-c", "/config/lotus2mqtt.yml"]