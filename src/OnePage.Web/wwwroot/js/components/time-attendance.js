// Time & Attendance UI Component
const e = React.createElement;

function TimeAttendance({ api, toast }) {
  const [timeEntries, setTimeEntries] = React.useState([]);
  const [overtimeRequests, setOvertimeRequests] = React.useState([]);
  const [activeTab, setActiveTab] = React.useState('entries');
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [modalType, setModalType] = React.useState('clock-in');
  const [selectedEmployeeId, setSelectedEmployeeId] = React.useState('');

  const loadData = React.useCallback(async () => {
    if (!selectedEmployeeId) return;
    try {
      const [entriesRes, overtimeRes] = await Promise.all([
        api.get(`/hr/time/entries/employee/${selectedEmployeeId}`),
        api.get(`/hr/overtime/requests/employee/${selectedEmployeeId}`)
      ]);
      
      if (entriesRes.ok) setTimeEntries(entriesRes.data || []);
      if (overtimeRes.ok) setOvertimeRequests(overtimeRes.data || []);
    } catch (err) {
      toast('Failed to load time & attendance data', 'err');
    }
  }, [api, toast, selectedEmployeeId]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleClockIn = async (formData) => {
    try {
      const res = await api.post('/hr/time/entries', formData);
      if (res.ok) {
        toast('Clocked in successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to clock in', 'err');
      }
    } catch (err) {
      toast('Failed to clock in', 'err');
    }
  };

  const handleClockOut = async (entryId, clockOutTime, notes) => {
    try {
      const res = await api.post(`/hr/time/entries/${entryId}/clockout`, {
        clockOutTime,
        notes
      });
      if (res.ok) {
        toast('Clocked out successfully', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to clock out', 'err');
      }
    } catch (err) {
      toast('Failed to clock out', 'err');
    }
  };

  const handleCreateOvertime = async (formData) => {
    try {
      const res = await api.post('/hr/overtime/requests', formData);
      if (res.ok) {
        toast('Overtime request created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create overtime request', 'err');
      }
    } catch (err) {
      toast('Failed to create overtime request', 'err');
    }
  };

  const handleApproveOvertime = async (requestId) => {
    try {
      const res = await api.post(`/hr/overtime/requests/${requestId}/approve`, {});
      if (res.ok) {
        toast('Overtime request approved', 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to approve request', 'err');
      }
    } catch (err) {
      toast('Failed to approve request', 'err');
    }
  };

  const ClockInForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'TE-' + Date.now(),
      employeeId: selectedEmployeeId,
      clockIn: new Date().toISOString(),
      location: '',
      notes: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit(formData);
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Clock In'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Location'),
            e('input', {
              type: 'text',
              value: formData.location,
              onChange: (e) => setFormData({ ...formData, location: e.target.value }),
              placeholder: 'e.g. Office, Remote'
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Notes'),
            e('textarea', {
              value: formData.notes,
              onChange: (e) => setFormData({ ...formData, notes: e.target.value })
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Clock In')
          )
        )
      )
    );
  };

  const OvertimeForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'OT-' + Date.now(),
      employeeId: selectedEmployeeId,
      startTime: '',
      endTime: '',
      hours: 0,
      reason: '',
      description: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit(formData);
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Request Overtime'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Start Time'),
            e('input', {
              type: 'datetime-local',
              value: formData.startTime,
              onChange: (e) => setFormData({ ...formData, startTime: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'End Time'),
            e('input', {
              type: 'datetime-local',
              value: formData.endTime,
              onChange: (e) => setFormData({ ...formData, endTime: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Hours'),
            e('input', {
              type: 'number',
              value: formData.hours,
              onChange: (e) => setFormData({ ...formData, hours: parseFloat(e.target.value) }),
              required: true,
              min: 0,
              step: 0.5
            })
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
              onChange: (e) => setFormData({ ...formData, description: e.target.value })
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

  const TimeEntryCard = ({ entry }) => {
    const clockIn = new Date(entry.clockIn);
    const clockOut = entry.clockOut ? new Date(entry.clockOut) : null;
    
    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, `Entry: ${entry.id}`),
        !clockOut && e('span', { className: 'status-badge', style: { backgroundColor: '#fbbf24' } }, 'Active')
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Clock In: ${clockIn.toLocaleString()}`),
        clockOut ? e('p', null, `Clock Out: ${clockOut.toLocaleString()}`) : e('p', null, 'Clock Out: —'),
        entry.location && e('p', null, `Location: ${entry.location}`),
        entry.notes && e('p', null, `Notes: ${entry.notes}`)
      ),
      !clockOut && e('div', { className: 'card-actions' },
        e('button', {
          onClick: () => {
            const notes = prompt('Add notes for clock out:');
            handleClockOut(entry.id, new Date().toISOString(), notes || '');
          }
        }, 'Clock Out')
      )
    );
  };

  const OvertimeCard = ({ request }) => {
    const statusColors = {
      Pending: '#fbbf24',
      Approved: '#6ee7b7',
      Rejected: '#f87171'
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
        e('p', null, `Start: ${new Date(request.startTime).toLocaleString()}`),
        e('p', null, `End: ${new Date(request.endTime).toLocaleString()}`),
        e('p', null, `Hours: ${request.hours}`),
        e('p', null, `Reason: ${request.reason}`)
      ),
      request.status === 'Pending' && e('div', { className: 'card-actions' },
        e('button', { onClick: () => handleApproveOvertime(request.id) }, 'Approve')
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Time & Attendance'),
      e('div', null,
        e('input', {
          type: 'text',
          placeholder: 'Employee ID',
          value: selectedEmployeeId,
          onChange: (e) => setSelectedEmployeeId(e.target.value),
          style: { marginRight: '10px' }
        }),
        e('button', { onClick: () => { setModalType('clock-in'); setShowCreateModal(true); } }, 'Clock In'),
        e('button', { onClick: () => { setModalType('overtime'); setShowCreateModal(true); } }, '+ Overtime Request')
      )
    ),
    e('div', { className: 'tabs' },
      e('button', {
        className: activeTab === 'entries' ? 'active' : '',
        onClick: () => setActiveTab('entries')
      }, 'Time Entries'),
      e('button', {
        className: activeTab === 'overtime' ? 'active' : '',
        onClick: () => setActiveTab('overtime')
      }, 'Overtime Requests')
    ),
    e('div', { className: 'tab-content' },
      activeTab === 'entries' && e('div', { className: 'items-grid' },
        !selectedEmployeeId ? e('p', null, 'Enter an Employee ID to view time entries') :
        timeEntries.length === 0 ? e('p', null, 'No time entries') :
        timeEntries.map(t => e(TimeEntryCard, { key: t.id, entry: t }))
      ),
      activeTab === 'overtime' && e('div', { className: 'items-grid' },
        !selectedEmployeeId ? e('p', null, 'Enter an Employee ID to view overtime requests') :
        overtimeRequests.length === 0 ? e('p', null, 'No overtime requests') :
        overtimeRequests.map(o => e(OvertimeCard, { key: o.id, request: o }))
      )
    ),
    showCreateModal && modalType === 'clock-in' && e(ClockInForm, {
      onSubmit: handleClockIn,
      onCancel: () => setShowCreateModal(false)
    }),
    showCreateModal && modalType === 'overtime' && e(OvertimeForm, {
      onSubmit: handleCreateOvertime,
      onCancel: () => setShowCreateModal(false)
    })
  );
}
