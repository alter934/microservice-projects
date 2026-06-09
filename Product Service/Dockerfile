# .NET 10 çalışma zamanı (runtime) imajını kullanıyoruz
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

# Yerelde derlediğimiz net_out klasörünü konteynere kopyala
COPY ./net_out .

# Ürün servisinin konteyner içinde dinleyeceği port (Eski Node.js portuyla aynı tutuyoruz: 5001)
ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "ProductApi.dll"]