import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// No test globals, so React Testing Library needs to be told to tidy up between tests.
afterEach(() => {
  cleanup();
  window.location.hash = '';
});
