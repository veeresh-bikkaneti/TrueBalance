import type { Route } from '../../shared/route';
import { VaultScreen } from './VaultScreen';

export const route: Route = { path: '/vault', label: 'Vault', order: 60, component: VaultScreen };
