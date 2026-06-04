import express from 'express';
import cors from 'cors';
import { createServer } from 'http';
import { Server } from 'socket.io';
import { config } from './config.js';
import apiRouter from './routes/api.js';
import { authenticate } from './services/authService.js';
import {
  startPriceFeed,
  setSocketIo,
  getLatestPrices,
  getConnectionState,
} from './services/priceFeedService.js';

const app = express();
const httpServer = createServer(app);

const io = new Server(httpServer, {
  cors: { origin: ['http://localhost:5173', 'http://localhost:3000'], methods: ['GET', 'POST'] },
});

app.use(cors({ origin: true }));
app.use(express.json());
app.use('/api', apiRouter);

setSocketIo(io);

io.on('connection', (socket) => {
  socket.emit('status', { ...getConnectionState(), prices: getLatestPrices() });
});

async function bootstrap() {
  console.log('Starting Real-Time Trading API...');
  await tryConnectFeed();

  httpServer.listen(config.port, () => {
    console.log(`API listening on http://localhost:${config.port}`);
    console.log(`Health: http://localhost:${config.port}/api/health`);
  });

  setInterval(() => tryConnectFeed(), 30000);
}

async function tryConnectFeed() {
  try {
    await authenticate();
    await startPriceFeed();
  } catch (err) {
    console.warn('[Bootstrap] Feed unavailable:', err.message);
  }
}

bootstrap();
