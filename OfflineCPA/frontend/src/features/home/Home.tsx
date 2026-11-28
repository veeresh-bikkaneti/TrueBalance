import type { Route } from '../../shared/route';

export function Home({ routes }: { routes: readonly Route[] }) {
  return (
    <>
      <h2>TrueBalance</h2>
      <p>Your screens:</p>
      <ul aria-label="Your screens">
        {routes.map((route) => (
          <li key={route.path}>
            <a href={`#${route.path}`}>{route.label}</a>
          </li>
        ))}
      </ul>
    </>
  );
}
