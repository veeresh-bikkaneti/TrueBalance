import type { Route } from './shared/route';
import { Shell } from './shell/Shell';

// Vite finds every feature at build time, so adding a screen means adding a folder.
const found = import.meta.glob<{ route: Route }>('./features/*/route.tsx', { eager: true });
const routes = Object.values(found).map((module) => module.route);

function App() {
  return <Shell routes={routes} />;
}

export default App;
