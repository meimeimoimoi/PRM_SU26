FROM node:20-alpine

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install --production

COPY server.js .

RUN mkdir -p data/maps

EXPOSE 3001

CMD ["node", "server.js"]
