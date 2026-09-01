import { getStoredUser } from './utils.js';

function ApiClient() {
  const base = window.__ONEPAGE_API_BASE__ || 'http://localhost:5001/api/v1';
  const getUser = () => { try { return JSON.parse(localStorage.getItem('onepage_user') || '{}'); } catch { return {}; } };
  async function request(path, method = 'GET', body) {
    const user = getUser();
    const headers = { 'Content-Type': 'application/json', 'X-Tenant-Id': user.tenantId || 'demo-tenant' };
    if (user.accessToken) headers['Authorization'] = 'Bearer ' + user.accessToken;
    const opts = { method, headers };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(base + path, opts);
    const text = await res.text();
    let data; try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    return { ok: res.ok, status: res.status, data };
  }
  return {
    get: (path) => request(path, 'GET'),
    post: (path, body) => request(path, 'POST', body),
    put: (path, body) => request(path, 'PUT', body),
    del: (path) => request(path, 'DELETE')
  };
}

export { ApiClient };
