// Offline mock behavior must be explicitly enabled and is never included in production builds.
export const isMockModeEnabled =
  import.meta.env.DEV && import.meta.env.VITE_ENABLE_MOCK_DATA === 'true';
