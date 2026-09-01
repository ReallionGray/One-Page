// Recruitment & Applicant Tracking UI Component
const e = React.createElement;

function Recruitment({ api, toast }) {
  const [postings, setPostings] = React.useState([]);
  const [applications, setApplications] = React.useState([]);
  const [interviews, setInterviews] = React.useState([]);
  const [offers, setOffers] = React.useState([]);
  const [activeTab, setActiveTab] = React.useState('postings');
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [modalType, setModalType] = React.useState('posting');

  const loadData = React.useCallback(async () => {
    try {
      const [postingsRes, applicationsRes] = await Promise.all([
        api.get('/hr/recruitment/jobs'),
        api.get('/hr/recruitment/applications')
      ]);
      
      if (postingsRes.ok) setPostings(postingsRes.data || []);
      if (applicationsRes.ok) setApplications(applicationsRes.data || []);
    } catch (err) {
      toast('Failed to load recruitment data', 'err');
    }
  }, [api, toast]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreatePosting = async (formData) => {
    try {
      const res = await api.post('/hr/recruitment/jobs', formData);
      if (res.ok) {
        toast('Job posting created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create posting', 'err');
      }
    } catch (err) {
      toast('Failed to create posting', 'err');
    }
  };

  const handleUpdatePosting = async (postingId, action) => {
    try {
      const endpoint = action === 'publish' ? `/hr/recruitment/jobs/${postingId}/publish` : `/hr/recruitment/jobs/${postingId}/close`;
      const res = await api.post(endpoint, {});
      if (res.ok) {
        toast(`Job ${action}ed successfully`, 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to update posting', 'err');
      }
    } catch (err) {
      toast('Failed to update posting', 'err');
    }
  };

  const handleCreateApplication = async (formData) => {
    try {
      const res = await api.post('/hr/recruitment/applications', formData);
      if (res.ok) {
        toast('Application submitted successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to submit application', 'err');
      }
    } catch (err) {
      toast('Failed to submit application', 'err');
    }
  };

  const PostingForm = ({ onSubmit, onCancel }) => {
    const [formData, setFormData] = React.useState({
      id: 'JP-' + Date.now(),
      title: '',
      description: '',
      departmentId: '',
      locationId: '',
      requirements: '',
      responsibilities: '',
      minSalary: '',
      maxSalary: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit({
        ...formData,
        minSalary: formData.minSalary ? parseFloat(formData.minSalary) : null,
        maxSalary: formData.maxSalary ? parseFloat(formData.maxSalary) : null
      });
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Create Job Posting'),
        e('form', { onSubmit: handleSubmit },
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
            e('label', null, 'Description'),
            e('textarea', {
              value: formData.description,
              onChange: (e) => setFormData({ ...formData, description: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Department ID'),
            e('input', {
              type: 'text',
              value: formData.departmentId,
              onChange: (e) => setFormData({ ...formData, departmentId: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Location ID'),
            e('input', {
              type: 'text',
              value: formData.locationId,
              onChange: (e) => setFormData({ ...formData, locationId: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Requirements'),
            e('textarea', {
              value: formData.requirements,
              onChange: (e) => setFormData({ ...formData, requirements: e.target.value })
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Responsibilities'),
            e('textarea', {
              value: formData.responsibilities,
              onChange: (e) => setFormData({ ...formData, responsibilities: e.target.value })
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Min Salary'),
            e('input', {
              type: 'number',
              value: formData.minSalary,
              onChange: (e) => setFormData({ ...formData, minSalary: e.target.value }),
              min: 0
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Max Salary'),
            e('input', {
              type: 'number',
              value: formData.maxSalary,
              onChange: (e) => setFormData({ ...formData, maxSalary: e.target.value }),
              min: 0
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Create Posting')
          )
        )
      )
    );
  };

  const ApplicationForm = ({ onSubmit, onCancel, postings }) => {
    const [formData, setFormData] = React.useState({
      id: 'APP-' + Date.now(),
      jobPostingId: '',
      candidateName: '',
      candidateEmail: '',
      candidatePhone: '',
      resumeUrl: '',
      coverLetter: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit(formData);
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Submit Job Application'),
        e('form', { onSubmit: handleSubmit },
          e('div', { className: 'form-group' },
            e('label', null, 'Job Posting'),
            e('select', {
              value: formData.jobPostingId,
              onChange: (e) => setFormData({ ...formData, jobPostingId: e.target.value }),
              required: true
            },
              postings.map(p => e('option', { key: p.id, value: p.id }, p.title))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Candidate Name'),
            e('input', {
              type: 'text',
              value: formData.candidateName,
              onChange: (e) => setFormData({ ...formData, candidateName: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Candidate Email'),
            e('input', {
              type: 'email',
              value: formData.candidateEmail,
              onChange: (e) => setFormData({ ...formData, candidateEmail: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Candidate Phone'),
            e('input', {
              type: 'tel',
              value: formData.candidatePhone,
              onChange: (e) => setFormData({ ...formData, candidatePhone: e.target.value })
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Resume URL'),
            e('input', {
              type: 'url',
              value: formData.resumeUrl,
              onChange: (e) => setFormData({ ...formData, resumeUrl: e.target.value })
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Cover Letter'),
            e('textarea', {
              value: formData.coverLetter,
              onChange: (e) => setFormData({ ...formData, coverLetter: e.target.value })
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Submit Application')
          )
        )
      )
    );
  };

  const PostingCard = ({ posting }) => {
    const statusColors = {
      Draft: '#9ca3af',
      Published: '#6ee7b7',
      Closed: '#f87171'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, posting.title),
        e('span', {
          className: 'status-badge',
          style: { backgroundColor: statusColors[posting.status] || '#9ca3af' }
        }, posting.status)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Department: ${posting.departmentId}`),
        e('p', null, `Location: ${posting.locationId}`),
        posting.minSalary && e('p', null, `Salary: $${posting.minSalary} - $${posting.maxSalary}`),
        e('p', null, `Posted: ${new Date(posting.createdAt).toLocaleDateString()}`)
      ),
      e('div', { className: 'card-actions' },
        posting.status === 'Draft' && e('button', {
          onClick: () => handleUpdatePosting(posting.id, 'publish')
        }, 'Publish'),
        posting.status === 'Published' && e('button', {
          onClick: () => handleUpdatePosting(posting.id, 'close')
        }, 'Close')
      )
    );
  };

  const ApplicationCard = ({ application }) => {
    const statusColors = {
      Applied: '#60a5fa',
      Screening: '#fbbf24',
      Interviewing: '#a78bfa',
      Offered: '#6ee7b7',
      Hired: '#34d399',
      Rejected: '#f87171'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, application.candidateName),
        e('span', {
          className: 'status-badge',
          style: { backgroundColor: statusColors[application.status] || '#9ca3af' }
        }, application.status)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Email: ${application.candidateEmail}`),
        e('p', null, `Phone: ${application.candidatePhone || 'N/A'}`),
        e('p', null, `Applied: ${new Date(application.createdAt).toLocaleDateString()}`)
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Recruitment'),
      e('div', null,
        e('button', { onClick: () => { setModalType('posting'); setShowCreateModal(true); } }, '+ New Posting'),
        e('button', { onClick: () => { setModalType('application'); setShowCreateModal(true); } }, '+ New Application')
      )
    ),
    e('div', { className: 'tabs' },
      e('button', {
        className: activeTab === 'postings' ? 'active' : '',
        onClick: () => setActiveTab('postings')
      }, 'Job Postings'),
      e('button', {
        className: activeTab === 'applications' ? 'active' : '',
        onClick: () => setActiveTab('applications')
      }, 'Applications')
    ),
    e('div', { className: 'tab-content' },
      activeTab === 'postings' && e('div', { className: 'items-grid' },
        postings.length === 0 ? e('p', null, 'No job postings') :
        postings.map(p => e(PostingCard, { key: p.id, posting: p }))
      ),
      activeTab === 'applications' && e('div', { className: 'items-grid' },
        applications.length === 0 ? e('p', null, 'No applications') :
        applications.map(a => e(ApplicationCard, { key: a.id, application: a }))
      )
    ),
    showCreateModal && modalType === 'posting' && e(PostingForm, {
      onSubmit: handleCreatePosting,
      onCancel: () => setShowCreateModal(false)
    }),
    showCreateModal && modalType === 'application' && e(ApplicationForm, {
      onSubmit: handleCreateApplication,
      onCancel: () => setShowCreateModal(false),
      postings
    })
  );
}
