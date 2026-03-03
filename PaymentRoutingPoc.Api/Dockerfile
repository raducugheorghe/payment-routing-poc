FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5156

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PaymentRoutingPoc.Api/PaymentRoutingPoc.Api.csproj", "PaymentRoutingPoc.Api/"]
COPY ["PaymentRoutingPoc.Domain/PaymentRoutingPoc.Domain.csproj", "PaymentRoutingPoc.Domain/"]
COPY ["PaymentRoutingPoc.Application/PaymentRoutingPoc.Application.csproj", "PaymentRoutingPoc.Application/"]
COPY ["PaymentRoutingPoc.Infrastructure/PaymentRoutingPoc.Infrastructure.csproj", "PaymentRoutingPoc.Infrastructure/"]
RUN dotnet restore "PaymentRoutingPoc.Api/PaymentRoutingPoc.Api.csproj"
COPY . .
WORKDIR "/src/PaymentRoutingPoc.Api"
RUN dotnet build "./PaymentRoutingPoc.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PaymentRoutingPoc.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:5156
ENTRYPOINT ["dotnet", "PaymentRoutingPoc.Api.dll"]
