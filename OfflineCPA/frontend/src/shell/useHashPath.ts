import { useSyncExternalStore } from 'react';

const subscribe = (onChange: () => void) => {
  window.addEventListener('hashchange', onChange);
  return () => window.removeEventListener('hashchange', onChange);
};

/** The path after `#`. Hash routing never reaches the server, so it needs no fallback there. */
export const useHashPath = () => useSyncExternalStore(subscribe, () => window.location.hash.slice(1) || '/');
