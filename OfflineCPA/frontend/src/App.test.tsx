import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App', () => {
  it('shows Home at #/ with the app name and the list of screens', () => {
    window.location.hash = '#/';
    render(<App />);

    expect(screen.getByRole('heading', { level: 2, name: 'TrueBalance' })).toBeTruthy();
    expect(screen.getByRole('list', { name: 'Your screens' }).textContent).toContain('Home');
  });

  it('shows the not-found page at #/nowhere', () => {
    window.location.hash = '#/nowhere';
    render(<App />);

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Go to Home' }).getAttribute('href')).toBe('#/');
  });
});
