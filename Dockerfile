FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#USER 1101

ARG TARGETARCH

WORKDIR /src
COPY . .

RUN dotnet build WebOne.csproj -c Release -o app --os linux --arch $TARGETARCH

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime

# ARG SERVICE_PORT=8080

# RUN DEBIAN_FRONTEND=noninteractive \
#         apt-get update && \
#         apt-get install -y convert

USER 1101

WORKDIR /app

COPY --from=build /src/app /app

EXPOSE 8080

CMD ["/app/webone", "8080"]
