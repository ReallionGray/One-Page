// Disciplinary Management UI Component
const e = React.createElement;

function DisciplinaryManagement({ api, toast }) {
  const [actions, setActions] = React.useState([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = React.useState('');
  const [showCreateModal, setShowCreateModal] = React.useState(false);

  const loadData = React.useCallback(async () => {
    if (!selectedEmployeeId) return;
    try {
      const res = await api.get(`/hr/employees/${selectedEmployeeId}/disciplinary-actions`);
      if (res.ok) setActions(res.data || []);
    } catch (err) {
      toast('Failed to load disciplinary data', 'err');
    }
  }, [api, toast, selectedEmployeeId]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreateAction = async (formData) => {
    try {
      const res = await api.post('/hr/disciplinary-actions', formData);
      if (res.ok) {
        toast('Disciplinary action created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create action', 'err');
      }
    } catch (err) {
      toast('Failed to create action', 'err');
    }
  };

  const handleResolveAction = async (actionId, resolutionNotes) => {
    try {
      const res = await api.post(`/hr/disciplinary-actions/${actionId}/resolve`, {
        resolutionNotes
      });
      if (res.ok) {
        toast('Disciplinary action resolved', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to resolve action', 'err');
      }
    } catch (err) {
      toast('Failed to resolve action', 'err');
    }
  };

  const handleExpungeAction = async (actionId) => {
    if (!confirm('Are you sure you want to expunge this record? This action cannot be undone.')) return;
    try {
      const res = await api.post(`/hr/disciplinary-actions/${actionId}/expunge`, {});
      if (res.ok) {
        toast('Disciplinary action expunged', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to expunge action', 'err');
      }
    } catch (err) {
      toast('Failed to expunge action', 'err');
    }
  };

  const handleCancelAction = async (actionId) => {
    if (!confirm('Are you sure you want to cancel this action?')) return;
    try {
      const res = await api.post(`/hr/disciplinary-actions/${actionId}/cancel`, {});
      if (res.ok) {
        toast('Disciplinary action cancelled', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to cancel action', 'err');
      }
    } catch (err) {
      toast('Failed to cancel action', 'err');
    }
  };

  const ActionForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'DA-' + Date.now(),
      employeeId: selectedEmployeeId,
      actionType: 'Warning',
      severity: 'Low',
      reason: '',
      description: '',
      effectiveDate: new Date().toISOString().split('T')[0],
      expiryDate: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit({
        ...formData,
        effectiveDate: formData.effectiveDate,
        expiryDate: formData.expiryDate || null
      });
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Create Disciplinary Action'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Action Type'),
            e('select', {
              value: formData.actionType,
              onChange: (e) => setFormData({ ...formData, actionType: e.target.value }),
              required: true
            },
              ['Warning', 'Query', 'Suspension', 'Termination'].map(t => e('option', { key: t, value: t }, t))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Severity'),
            e('select', {
              value: formData.severity,
              onChange: (e) => setFormData({ ...formData, severity: e.target.value }),
              required: true
            },
              ['Low', 'Medium', 'High', 'Critical'].map(s => e('option', { key: s, value: s }, s))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Reason'),
            e('input', {
              type: 'text',
              value: formData.reason,
              onChange: (e) => setFormData({ ...formData, reason: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Description'),
            e('textarea', {
              value: formData.description,
              onChange: (e) => setFormData({ ...formData, description: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Effective Date'),
            e('input', {
              type: 'date',
              value: formData.effectiveDate,
              onChange: (e) => setFormData({ ...formData, effectiveDate: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Expiry Date (Optional)'),
            e('input', {
              type: 'date',
              value: formData.expiryDate,
              onChange: (e) => setFormData({ ...formData, expiryDate: e.target.value })
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Create Action')
          )
        )
      )
    );
  };

  const ActionCard = ({ action }) => {
    const statusColors = {
      Active: '#fbbf24',
      Resolved: '#6ee7b7',
      Expunged: '#9ca3af',
      Cancelled: '#f87171'
    };

    const severityColors = {
      Low: '#6ee7b7',
      Medium: '#fbbf24',
      High: '#f97316',
      Critical: '#ef4444'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, `${action.actionType}: ${action.reason}`),
        e('div', null,
          e('span', {
            className: 'status-badge',
            style: { backgroundColor: statusColors[action.status] || '#9ca3af' }
          }, action.status),
          e('span', {
            className: 'status-badge',
            style: { backgroundColor: severityColors[action.severity] || '#9ca3af', marginLeft: '5px' }
          }, action.severity)
        )
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Effective: ${action.effectiveDate}`),
        action.expiryDate && e('p', null, `Expires: ${action.expiryDate}`),
        e('p', null, `Description: ${action.description}`),
        action.resolvedAt && e('p', null, `Resolved: ${new Date(action.resolvedAt).toLocaleDateString()}`),
        action.resolutionNotes && e('p', null, `Resolution: ${action.resolutionNotes}`)
      ),
      action.status === 'Active' && e('div', { className: 'card-actions' },
        e('button', {
          onClick: () => {
            const notes = prompt('Enter resolution notes:');
            if (notes) handleResolveAction(action.id, notes);
          }
        }, 'Resolve'),
        e('button', { onClick: () => handleCancelAction(action.id) }, 'Cancel')
      ),
      action.status === 'Resolved' && e('div', { className: 'card-actions' },
        e('button', { onClick: () => handleExpungeAction(action.id) }, 'Expunge')
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Disciplinary Management'),
      e('div', null,
        e('input', {
          type: 'text',
          placeholder: 'Employee ID',
          value: selectedEmployeeId,
          onChange: (e) => setSelectedEmployeeId(e.target.value),
          style: { marginRight: '10px' }
        }),
        e('button', { onClick: () => setShowCreateModal(true) }, '+ New Action')
      )
    ),
    e('div', { className: 'items-grid' },
      !selectedEmployeeId ? e('p', null, 'Enter an Employee ID to view disciplinary actions') :
      actions.length === 0 ? e('p', null, 'No disciplinary actions') :
      actions.map(a => e(ActionCard, { key: a.id, action: a }))
    ),
    showCreateModal && e(ActionForm, {
      onSubmit: handleCreateAction,
      onCancel: () => setShowCreateModal(false)
    })
  );
}
