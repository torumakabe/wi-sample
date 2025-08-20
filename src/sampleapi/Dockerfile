FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /app
ADD . .
RUN dotnet build Api.csproj && dotnet publish -c release

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=builder /app/bin/release/net9.0/publish/ .
# Kubernetes runAsNonRoot requires USER to be numeric
USER 65532:65532
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SampleApi.dll"]
