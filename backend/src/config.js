import dotenv from 'dotenv';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: path.resolve(__dirname, '../../.env') });

export const config = {
  port: parseInt(process.env.PORT || '5000', 10),
  authUrl: process.env.AUTH_URL || 'http://s138.sysfx.com:10001/api/v2/auth/token',
  wsBaseUrl: process.env.WS_URL || 'ws://s138.sysfx.com:10006/ws',
  userId: process.env.USER_ID || '',
  accountId: process.env.ACCOUNT_ID || '',
  password: process.env.PASSWORD || '',
  dbPath: process.env.DB_PATH || path.resolve(__dirname, '../../data/trades.db'),
  wsReconnectMaxMs: 30000,
  wsReconnectInitialMs: 1000,
};
