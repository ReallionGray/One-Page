// Employee Documents UI Component
const e = React.createElement;

function EmployeeDocuments({ api, toast }) {
  const [documents, setDocuments] = React.useState([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = React.useState('');
  const [showCreateModal, setShowCreateModal] = React.useState(false);

  const loadData = React.useCallback(async () => {
    if (!selectedEmployeeId) return;
    try {
      const res = await api.get(`/hr/employees/${selectedEmployeeId}/documents`);
      if (res.ok) setDocuments(res.data || []);
    } catch (err) {
      toast('Failed to load documents', 'err');
    }
  }, [api, toast, selectedEmployeeId]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreateDocument = async (formData) => {
    try {
      const res = await api.post('/hr/employee-documents', formData);
      if (res.ok) {
        toast('Document uploaded successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to upload document', 'err');
      }
    } catch (err) {
      toast('Failed to upload document', 'err');
    }
  };

  const DocumentForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'DOC-' + Date.now(),
      employeeId: selectedEmployeeId,
      documentType: 'Contract',
      fileReference: '',
      expiresOn: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit({
        ...formData,
        expiresOn: formData.expiresOn ? formData.expiresOn : null
      });
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Upload Employee Document'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Document Type'),
            e('select', {
              value: formData.documentType,
              onChange: (e) => setFormData({ ...formData, documentType: e.target.value }),
              required: true
            },
              ['Contract', 'ID', 'Passport', 'Certificate', 'TaxForm', 'Other'].map(t => e('option', { key: t, value: t }, t))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'File Reference/URL'),
            e('input', {
              type: 'url',
              value: formData.fileReference,
              onChange: (e) => setFormData({ ...formData, fileReference: e.target.value }),
              required: true,
              placeholder: 'https://...'
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Expiry Date (Optional)'),
            e('input', {
              type: 'date',
              value: formData.expiresOn,
              onChange: (e) => setFormData({ ...formData, expiresOn: e.target.value })
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Upload Document')
          )
        )
      )
    );
  };

  const DocumentCard = ({ document }) => {
    const isExpired = document.expiresOn && new Date(document.expiresOn) < new Date();
    const statusColor = isExpired ? '#ef4444' : '#6ee7b7';

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, document.documentType),
        isExpired && e('span', {
          className: 'status-badge',
          style: { backgroundColor: statusColor }
        }, 'Expired')
      ),
      e('div', { className: 'card-body' },
        e('p', null, `File: ${document.fileReference}`),
        document.expiresOn && e('p', null, `Expires: ${document.expiresOn}`),
        e('p', null, `Uploaded: ${new Date(document.createdAt).toLocaleDateString()}`)
      ),
      e('div', { className: 'card-actions' },
        e('a', {
          href: document.fileReference,
          target: '_blank',
          rel: 'noopener noreferrer',
          className: 'button-link'
        }, 'View Document')
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Employee Documents'),
      e('div', null,
        e('input', {
          type: 'text',
          placeholder: 'Employee ID',
          value: selectedEmployeeId,
          onChange: (e) => setSelectedEmployeeId(e.target.value),
          style: { marginRight: '10px' }
        }),
        e('button', { onClick: () => setShowCreateModal(true) }, '+ Upload Document')
      )
    ),
    e('div', { className: 'items-grid' },
      !selectedEmployeeId ? e('p', null, 'Enter an Employee ID to view documents') :
      documents.length === 0 ? e('p', null, 'No documents') :
      documents.map(d => e(DocumentCard, { key: d.id, document: d }))
    ),
    showCreateModal && e(DocumentForm, {
      onSubmit: handleCreateDocument,
      onCancel: () => setShowCreateModal(false)
    })
  );
}
