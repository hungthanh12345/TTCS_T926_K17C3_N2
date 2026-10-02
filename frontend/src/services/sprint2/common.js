import api from '../api';

export { api };
export const unwrap = (response) => response.data?.data ?? response.data;
export const messageOf = (error, fallback) => {
  const status = error.response?.status;
  if (!error.response || status >= 500) return fallback;
  return error.response.data?.message || error.message || fallback;
};
