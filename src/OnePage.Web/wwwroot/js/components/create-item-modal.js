import { e } from '../utils.js';

// ===== Create Item Modal =====
function CreateItemModal({ module, onClose, onCreate }) {
  const [formData, setFormData] = React.useState({});
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState(null);
  const fields = module.createFields || [];

  const handleChange = (name, value) => setFormData({ ...formData, [name]: value });

  const handleSubmit = async () => {
    for (const f of fields) {
      if (f.required && (formData[f.name] === undefined || formData[f.name] === null || formData[f.name] === '')) {
        setError(f.label + ' is required');
        return;
      }
    }
    setError(null);
    setSaving(true);
    try {
      const payload = {};
      fields.forEach(f => {
        if (formData[f.name] !== undefined && formData[f.name] !== '') {
          if (f.type === 'number') payload[f.name] = Number(formData[f.name]);
          else payload[f.name] = formData[f.name];
        }
      });
      await onCreate(payload);
    } catch (err) { setError(err.message || 'Create failed'); }
    finally { setSaving(false); }
  };

  return e('div', { className: 'detail-overlay' },
    e('div', { className: 'detail-panel card-ghost' },
      e('div', { className: 'd-flex justify-content-between align-items-center mb-3' },
        e('h5', { className: 'mb-0' }, 'New ' + module.label),
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: saving }, '×')),
      error ? e('div', { className: 'alert alert-danger' }, error) : null,
      e('div', { className: 'detail-grid' },
        fields.map(f => e('div', { className: 'detail-row', key: f.name },
          e('span', { className: 'detail-label' }, f.label + (f.required ? ' *' : '')),
          e('span', { className: 'detail-value' },
            f.type === 'number'
              ? e('input', { type: 'number', className: 'form-control form-control-sm', placeholder: f.placeholder || '', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving })
              : f.type === 'date'
                ? e('input', { type: 'date', className: 'form-control form-control-sm', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving })
                : e('input', { type: 'text', className: 'form-control form-control-sm', placeholder: f.placeholder || '', value: formData[f.name] ?? '', onChange: ev => handleChange(f.name, ev.target.value), disabled: saving }))))),
      e('div', { className: 'd-flex justify-content-end gap-2 mt-3' },
        e('button', { className: 'btn btn-sm btn-outline-light', onClick: onClose, disabled: saving }, 'Cancel'),
        e('button', { className: 'btn btn-sm btn-accent', onClick: handleSubmit, disabled: saving },
          saving ? e('span', null, e('span', { className: 'spinner-border spinner-border-sm' }), ' Creating...') : 'Create'))));
}

export { CreateItemModal };
