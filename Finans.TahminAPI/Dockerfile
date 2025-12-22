FROM node:18-slim

# Python ve gerekli kütüphaneleri yükle
RUN apt-get update || : && apt-get install -y python3 python3-pip
RUN pip3 install numpy pandas scikit-learn joblib --break-system-packages

WORKDIR /app

# Önce paket dosyasını kopyala
COPY package.json .
RUN npm install

# Sonra tüm kodları kopyala
COPY . .

EXPOSE 3000
CMD ["node", "server.js"]