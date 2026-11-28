import type { Route } from '../shared/route';
import { NotFound } from './NotFound';
import { useHashPath } from './useHashPath';

/** Layout, nav and routing. It knows nothing about any particular screen. */
export function Shell({ routes }: { routes: readonly Route[] }) {
  const path = useHashPath();
  const sorted = [...routes].sort((a, b) => a.order - b.order);
  const Screen = sorted.find((route) => route.path === path)?.component;

  return (
    <div className="wrap">
      <header>
        <h1>TrueBalance</h1>
      </header>
      <div className="app">
        <nav aria-label="Screens">
          <ul>
            {sorted.map((route) => (
              <li key={route.path}>
                <a href={`#${route.path}`} aria-current={route.path === path ? 'page' : undefined}>
                  {route.label}
                </a>
              </li>
            ))}
          </ul>
        </nav>
        <main>{Screen ? <Screen routes={sorted} /> : <NotFound />}</main>
      </div>
    </div>
  );
}
