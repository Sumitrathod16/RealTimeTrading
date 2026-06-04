import { Router } from 'express';
import {
  getLatestPrices,
  getConnectionState,
} from '../services/priceFeedService.js';
import { getAuthStatus, authenticate } from '../services/authService.js';
import { placeOrder, listTrades, computePositions } from '../services/tradeService.js';

const router = Router();

router.get('/health', async (_req, res) => {
  const ws = getConnectionState();
  const auth = getAuthStatus();
  res.json({
    status: 'ok',
    timestamp: new Date().toISOString(),
    auth,
    websocket: ws,
  });
});

router.get('/prices', (_req, res) => {
  res.json(getLatestPrices());
});

router.post('/orders', (req, res) => {
  const result = placeOrder(req.body);
  if (!result.success) {
    return res.status(400).json({ success: false, message: result.message });
  }
  res.status(201).json(result);
});

router.get('/trades', (req, res) => {
  const limit = Math.min(parseInt(req.query.limit || '100', 10), 500);
  res.json(listTrades(limit));
});

router.get('/positions', (_req, res) => {
  const trades = listTrades(500);
  const positions = computePositions(trades, getLatestPrices());
  res.json(positions);
});

router.post('/auth/refresh', async (_req, res) => {
  try {
    await authenticate(true);
    res.json({ success: true, auth: getAuthStatus() });
  } catch (err) {
    res.status(502).json({ success: false, message: err.message });
  }
});

export default router;
