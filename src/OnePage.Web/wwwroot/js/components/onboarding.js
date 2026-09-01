// Onboarding & Offboarding UI Component
const e = React.createElement;

function OnboardingOffboarding({ api, toast }) {
  const [checklistItems, setChecklistItems] = React.useState([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = React.useState('');
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [modalType, setModalType] = React.useState('checklist');

  const loadData = React.useCallback(async () => {
    if (!selectedEmployeeId) return;
    try {
      const res = await api.get(`/hr/employees/${selectedEmployeeId}/checklist-items`);
      if (res.ok) setChecklistItems(res.data || []);
    } catch (err) {
      toast('Failed to load checklist data', 'err');
    }
  }, [api, toast, selectedEmployeeId]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreateChecklistItem = async (formData) => {
    try {
      const res = await api.post('/hr/checklist-items', formData);
      if (res.ok) {
        toast('Checklist item created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create checklist item', 'err');
      }
    } catch (err) {
      toast('Failed to create checklist item', 'err');
    }
  };

  const handleCompleteChecklistItem = async (itemId, evidence) => {
    try {
      const res = await api.post(`/hr/checklist-items/${itemId}/complete`, { evidence });
      if (res.ok) {
        toast('Checklist item completed', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to complete item', 'err');
      }
    } catch (err) {
      toast('Failed to complete item', 'err');
    }
  };

  const handleOffboardEmployee = async (employeeId, effectiveDate) => {
    if (!confirm('Are you sure you want to offboard this employee?')) return;
    try {
      const res = await api.post(`/hr/employees/${employeeId}/offboard`, { effectiveDate });
      if (res.ok) {
        toast('Employee offboarded successfully', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to offboard employee', 'err');
      }
    } catch (err) {
      toast('Failed to offboard employee', 'err');
    }
  };

  const ChecklistForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'CI-' + Date.now(),
      employeeId: selectedEmployeeId,
      kind: 'Onboarding',
      title: '',
      ownerUserId: '',
      dueDate: new Date().toISOString().split('T')[0]
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit({
        ...formData,
        dueDate: formData.dueDate ? formData.dueDate : null
      });
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Create Checklist Item'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Type'),
            e('select', {
              value: formData.kind,
              onChange: (e) => setFormData({ ...formData, kind: e.target.value }),
              required: true
            },
              ['Onboarding', 'Offboarding'].map(k => e('option', { key: k, value: k }, k))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Title'),
            e('input', {
              type: 'text',
              value: formData.title,
              onChange: (e) => setFormData({ ...formData, title: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Owner User ID'),
            e('input', {
              type: 'text',
              value: formData.ownerUserId,
              onChange: (e) => setFormData({ ...formData, ownerUserId: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Due Date'),
            e('input', {
              type: 'date',
              value: formData.dueDate,
              onChange: (e) => setFormData({ ...formData, dueDate: e.target.value })
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Create Item')
          )
        )
      )
    );
  };

  const ChecklistCard = ({ item }) => {
    const statusColors = {
      Pending: '#fbbf24',
      InProgress: '#60a5fa',
      Completed: '#6ee7b7',
      Skipped: '#9ca3af'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, item.title),
        e('div', null,
          e('span', {
            className: 'status-badge',
            style: { backgroundColor: statusColors[item.status] || '#9ca3af' }
          }, item.status),
          e('span', { className: 'card-subtitle', style: { marginLeft: '10px' } }, item.kind)
        )
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Owner: ${item.ownerUserId}`),
        item.dueDate && e('p', null, `Due: ${item.dueDate}`),
        item.completedAt && e('p', null, `Completed: ${new Date(item.completedAt).toLocaleDateString()}`),
        item.evidence && e('p', null, `Evidence: ${item.evidence}`)
      ),
      item.status === 'Pending' && e('div', { className: 'card-actions' },
        e('button', {
          onClick: () => {
            const evidence = prompt('Enter evidence URL or notes:');
            if (evidence) handleCompleteChecklistItem(item.id, evidence);
          }
        }, 'Complete')
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Onboarding & Offboarding'),
      e('div', null,
        e('input', {
          type: 'text',
          placeholder: 'Employee ID',
          value: selectedEmployeeId,
          onChange: (e) => setSelectedEmployeeId(e.target.value),
          style: { marginRight: '10px' }
        }),
        e('button', { onClick: () => { setModalType('checklist'); setShowCreateModal(true); } }, '+ Checklist Item'),
        e('button', { 
          onClick: () => {
            const date = prompt('Enter effective date (YYYY-MM-DD):');
            if (date) handleOffboardEmployee(selectedEmployeeId, date);
          }
        }, 'Offboard Employee')
      )
    ),
    e('div', { className: 'items-grid' },
      !selectedEmployeeId ? e('p', null, 'Enter an Employee ID to view checklist items') :
      checklistItems.length === 0 ? e('p', null, 'No checklist items') :
      checklistItems.map(c => e(ChecklistCard, { key: c.id, item: c }))
    ),
    showCreateModal && modalType === 'checklist' && e(ChecklistForm, {
      onSubmit: handleCreateChecklistItem,
      onCancel: () => setShowCreateModal(false)
    })
  );
}
