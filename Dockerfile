FROM mcr.microsoft.com/dotnet/sdk AS build
#USER 1101

COPY . .

RUN dotnet build WebOne.csproj -c Release -o app --os linux --arch arm64

FROM mcr.microsoft.com/dotnet/runtime AS runtime
ARG SERVICE_PORT=8080

RUN DEBIAN_FRONTEND=noninteractive \
    apt-get update && \
    apt-get install -y convert

USER 1101

WORKDIR /app

COPY --from=build /app /app

EXPOSE $SERVICE_PORT

CMD ["webone", "$SERVICE_PORT"]
