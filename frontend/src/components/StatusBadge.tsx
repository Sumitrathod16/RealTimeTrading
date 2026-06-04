import type { ConnectionState } from '../types';

const labels: Record<ConnectionState, string> = {
  Connected: 'Connected',
  Connecting: 'Connecting…',
  Disconnected: 'Disconnected',
  Error: 'Error',
};

export function StatusBadge({ state, authOk }: { state: ConnectionState; authOk: boolean }) {
  const cls = state.toLowerCase();
  return (
    <div className="status-group">
      <span className={`status-badge status-${cls}`}>{labels[state]}</span>
      <span className={`status-badge ${authOk ? 'auth-ok' : 'auth-warn'}`}>
        {authOk ? 'Auth OK' : 'Auth Pending'}
      </span>
    </div>
  );
}
