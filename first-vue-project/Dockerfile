FROM node:20-alpine

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

EXPOSE 5173

# Geliştirme sunucusunu dış bağlantılara açacak şekilde (host belirterek) başlatıyoruz
CMD ["npm", "run", "dev", "--", "--host"]