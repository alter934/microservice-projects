# 1. Resmi Node.js hafif imajını taban alıyoruz
FROM node:20-alpine

# 2. Konteyner içinde projenin yaşayacağı klasörü oluşturuyoruz
WORKDIR /app

# 3. Bağımlılık listelerini kopyalıyoruz
COPY package*.json ./

# Hem kütüphaneleri kuruyoruz hem de canlı izleme için nodemon ekliyoruz
RUN npm install && npm install -g nodemon

# 5. Kalan tüm kaynak kodları (server.js vb.) kopyalıyoruz
COPY . .

# 6. Servisin çalışacağı portu dışarıya duyuruyoruz
EXPOSE 5001

# 7. Konteyner ayağa kalktığında çalışacak komut
CMD ["nodemon", "server.js"]