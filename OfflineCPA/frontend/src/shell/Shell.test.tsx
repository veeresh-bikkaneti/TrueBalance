import { act, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { Route } from '../shared/route';
import { Shell } from './Shell';

const route = (path: string, label: string, order: number): Route => ({
  path,
  label,
  order,
  component: () => <h2>{label} screen</h2>,
});

// Handed over out of order on purpose.
const routes = [route('/second', 'Second', 20), route('/', 'First', 0), route('/third', 'Third', 30)];

const goTo = (hash: string) =>
  act(() => {
    window.location.hash = hash;
    window.dispatchEvent(new HashChangeEvent('hashchange'));
  });

describe('Shell', () => {
  it('shows a nav item for each route, in order', () => {
    render(<Shell routes={routes} />);

    const links = within(screen.getByRole('navigation')).getAllByRole('link');
    expect(links.map((link) => link.textContent)).toEqual(['First', 'Second', 'Third']);
    expect(links.map((link) => link.getAttribute('href'))).toEqual(['#/', '#/second', '#/third']);
  });

  it('follows the hash and marks the current screen', () => {
    render(<Shell routes={routes} />);
    goTo('#/second');

    expect(screen.getByRole('heading', { name: 'Second screen' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Second' }).getAttribute('aria-current')).toBe('page');
  });
});
