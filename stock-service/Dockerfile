FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

# Dockerignore engeline takılmayan tertemiz net_out klasörünü kopyalıyoruz
COPY ./net_out .

ENV ASPNETCORE_URLS=http://+:5002

# ls -R çıktımızda gördüğümüz tam dosya adı (Büyük/küçük harfe dikkat)
ENTRYPOINT ["dotnet", "StockApi.dll"]