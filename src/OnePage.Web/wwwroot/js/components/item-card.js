import { e, prettifyKey, formatValue } from '../utils.js';

// ===== Item Card =====
function ItemCard({ item }) {
  const entries = Object.entries(item || {}).filter(([k]) => !['id', 'tenantId', 'createdAt', 'updatedAt'].includes(k));
  const title = item.id || item.tag || item.name || item.reference || item.sku || '#item';
  return e('div', { className: 'module-card card-ghost h-100' },
    e('div', { className: 'module-card-head' }, e('h6', { className: 'module-card-title' }, String(title).slice(0, 36))),
    e('div', { className: 'module-card-body' }, entries.map(([k, v]) =>
      e('div', { className: 'module-card-field' },
        e('span', { className: 'module-field-label' }, prettifyKey(k) + ':'),
        e('span', { className: 'module-field-value' }, formatValue(v))))));
}

export { ItemCard };
