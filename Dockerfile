FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HotelResortBooking.sln ./
COPY Directory.Build.props ./
COPY global.json ./
COPY nuget.config ./
COPY src/HotelResortBooking.Api/HotelResortBooking.Api.csproj src/HotelResortBooking.Api/
COPY src/HotelResortBooking.Application/HotelResortBooking.Application.csproj src/HotelResortBooking.Application/
COPY src/HotelResortBooking.Domain/HotelResortBooking.Domain.csproj src/HotelResortBooking.Domain/
COPY src/HotelResortBooking.Infrastructure/HotelResortBooking.Infrastructure.csproj src/HotelResortBooking.Infrastructure/

RUN dotnet restore src/HotelResortBooking.Api/HotelResortBooking.Api.csproj

COPY src/ src/
RUN dotnet publish src/HotelResortBooking.Api/HotelResortBooking.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/uploads
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HotelResortBooking.Api.dll"]
