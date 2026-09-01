// Leave Management UI Component
const e = React.createElement;

function LeaveManagement({ api, toast }) {
  const [policies, setPolicies] = React.useState([]);
  const [balances, setBalances] = React.useState([]);
  const [requests, setRequests] = React.useState([]);
  const [activeTab, setActiveTab] = React.useState('requests');
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [selectedRequest, setSelectedRequest] = React.useState(null);

  const loadData = React.useCallback(async () => {
    try {
      const [policiesRes, balancesRes, requestsRes] = await Promise.all([
        api.get('/hr/leave/policies'),
        api.get('/hr/leave/balances'),
        api.get('/hr/leave/requests')
      ]);
      
      if (policiesRes.ok) setPolicies(policiesRes.data || []);
      if (balancesRes.ok) setBalances(balancesRes.data || []);
      if (requestsRes.ok) setRequests(requestsRes.data || []);
    } catch (err) {
      toast('Failed to load leave data', 'err');
    }
  }, [api, toast]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreateRequest = async (formData) => {
    try {
      const res = await api.post('/hr/leave/requests', formData);
      if (res.ok) {
        toast('Leave request created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create request', 'err');
      }
    } catch (err) {
      toast('Failed to create request', 'err');
    }
  };

  const handleApproveReject = async (requestId, approve, comment) => {
    try {
      const res = await api.post(`/hr/leave/requests/${requestId}/decide`, {
        approve,
        comment
      });
      if (res.ok) {
        toast(approve ? 'Request approved' : 'Request rejected', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to process request', 'err');
      }
    } catch (err) {
      toast('Failed to process request', 'err');
    }
  };

  const LeaveRequestForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'LR-' + Date.now(),
      employeeId: '',
      policyId: '',
      startDate: '',
      endDate: '',
      days: 1,
      reason: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit(formData);
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Create Leave Request'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Employee ID'),
            e('input', {
              type: 'text',
              value: formData.employeeId,
              onChange: (e) => setFormData({ ...formData, employeeId: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Leave Policy'),
            e('select', {
              value: formData.policyId,
              onChange: (e) => setFormData({ ...formData, policyId: e.target.value }),
              required: true
            },
              policies.map(p => e('option', { key: p.id, value: p.id }, p.name))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Start Date'),
            e('input', {
              type: 'date',
              value: formData.startDate,
              onChange: (e) => setFormData({ ...formData, startDate: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'End Date'),
            e('input', {
              type: 'date',
              value: formData.endDate,
              onChange: (e) => setFormData({ ...formData, endDate: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Number of Days'),
            e('input', {
              type: 'number',
              value: formData.days,
              onChange: (e) => setFormData({ ...formData, days: parseFloat(e.target.value) }),
              required: true,
              min: 0.5,
              step: 0.5
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Reason'),
            e('textarea', {
              value: formData.reason,
              onChange: (e) => setFormData({ ...formData, reason: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Submit Request')
          )
        )
      )
    );
  };

  const RequestCard = ({ request }) => {
    const statusColors = {
      Pending: '#fbbf24',
      Approved: '#6ee7b7',
      Rejected: '#f87171',
      Cancelled: '#9ca3af'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, `Request: ${request.id}`),
        e('span', {
          className: 'status-badge',
          style: { backgroundColor: statusColors[request.status] || '#9ca3af' }
        }, request.status)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Employee: ${request.employeeId}`),
        e('p', null, `Policy: ${request.policyId}`),
        e('p', null, `Dates: ${request.startDate} to ${request.endDate}`),
        e('p', null, `Days: ${request.days}`),
        e('p', null, `Reason: ${request.reason}`)
      ),
      request.status === 'Pending' && e('div', { className: 'card-actions' },
        e('button', {
          onClick: () => handleApproveReject(request.id, true, 'Approved')
        }, 'Approve'),
        e('button', {
          onClick: () => handleApproveReject(request.id, false, 'Rejected')
        }, 'Reject')
      )
    );
  };

  const BalanceCard = ({ balance }) => {
    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, balance.policyId),
        e('span', { className: 'card-subtitle' }, `Year: ${balance.year}`)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Entitled: ${balance.entitledDays} days`),
        e('p', null, `Used: ${balance.usedDays} days`),
        e('p', null, `Remaining: ${balance.entitledDays - balance.usedDays} days`)
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Leave Management'),
      e('button', { onClick: () => setShowCreateModal(true) }, '+ New Request')
    ),
    e('div', { className: 'tabs' },
      e('button', {
        className: activeTab === 'requests' ? 'active' : '',
        onClick: () => setActiveTab('requests')
      }, 'Requests'),
      e('button', {
        className: activeTab === 'balances' ? 'active' : '',
        onClick: () => setActiveTab('balances')
      }, 'Balances'),
      e('button', {
        className: activeTab === 'policies' ? 'active' : '',
        onClick: () => setActiveTab('policies')
      }, 'Policies')
    ),
    e('div', { className: 'tab-content' },
      activeTab === 'requests' && e('div', { className: 'items-grid' },
        requests.length === 0 ? e('p', null, 'No leave requests') :
        requests.map(r => e(RequestCard, { key: r.id, request: r }))
      ),
      activeTab === 'balances' && e('div', { className: 'items-grid' },
        balances.length === 0 ? e('p', null, 'No leave balances') :
        balances.map(b => e(BalanceCard, { key: `${b.employeeId}-${b.policyId}`, balance: b }))
      ),
      activeTab === 'policies' && e('div', { className: 'items-grid' },
        policies.length === 0 ? e('p', null, 'No leave policies') :
        policies.map(p => e('div', { className: 'item-card', key: p.id },
          e('div', { className: 'card-header' },
            e('span', { className: 'card-title' }, p.name),
            e('span', { className: 'card-subtitle' }, p.code)
          ),
          e('div', { className: 'card-body' },
            e('p', null, `Annual Entitlement: ${p.annualEntitlement} days`),
            e('p', null, `Allow Carryover: ${p.allowCarryover ? 'Yes' : 'No'}`)
          )
        ))
      )
    ),
    showCreateModal && e(LeaveRequestForm, {
      onSubmit: handleCreateRequest,
      onCancel: () => setShowCreateModal(false)
    })
  );
}
