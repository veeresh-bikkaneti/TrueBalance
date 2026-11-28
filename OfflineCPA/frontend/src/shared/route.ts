import type { ComponentType } from 'react';

/** One screen. Each feature exports one of these from `features/<name>/route.tsx`. */
export interface Route {
  /** Hash path, such as `/` or `/accounts`. */
  path: string;
  /** Text in the nav. */
  label: string;
  /** Lower numbers come first. Leave gaps so new screens fit between. */
  order: number;
  /** Gets every route, already sorted, in case the screen wants to list them. */
  component: ComponentType<{ routes: readonly Route[] }>;
}
