// Performance Management UI Component
const e = React.createElement;

function PerformanceManagement({ api, toast }) {
  const [reviews, setReviews] = React.useState([]);
  const [goals, setGoals] = React.useState([]);
  const [feedback, setFeedback] = React.useState([]);
  const [cycles, setCycles] = React.useState([]);
  const [activeTab, setActiveTab] = React.useState('reviews');
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  const [modalType, setModalType] = React.useState('review');

  const loadData = React.useCallback(async () => {
    try {
      const [reviewsRes, cyclesRes] = await Promise.all([
        api.get('/hr/performance/reviews'),
        api.get('/hr/performance/review-cycles/active')
      ]);
      
      if (reviewsRes.ok) setReviews(reviewsRes.data || []);
      if (cyclesRes.ok) setCycles(cyclesRes.data || []);
    } catch (err) {
      toast('Failed to load performance data', 'err');
    }
  }, [api, toast]);

  React.useEffect(() => { loadData(); }, [loadData]);

  const handleCreateReview = async (formData) => {
    try {
      const res = await api.post('/hr/performance/reviews', formData);
      if (res.ok) {
        toast('Performance review created successfully', 'ok');
        setShowCreateModal(false);
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to create review', 'err');
      }
    } catch (err) {
      toast('Failed to create review', 'err');
    }
  };

  const handleUpdateReview = async (reviewId, action, data) => {
    try {
      let endpoint = '';
      if (action === 'submit') endpoint = `/hr/performance/reviews/${reviewId}/submit`;
      else if (action === 'start') endpoint = `/hr/performance/reviews/${reviewId}/start`;
      else if (action === 'complete') endpoint = `/hr/performance/reviews/${reviewId}/complete`;
      
      const res = await api.post(endpoint, data);
      if (res.ok) {
        toast(`Review ${action}ed successfully`, 'ok');
        loadData();
      } else {
        toast(res.data?.detail || 'Failed to update review', 'err');
      }
    } catch (err) {
      toast('Failed to update review', 'err');
    }
  };

  const ReviewForm = ({ onSubmit, onCancel, cycles }) => {
    const [formData, setFormData] = React.useState({
      id: 'PR-' + Date.now(),
      employeeId: '',
      reviewCycleId: '',
      framework: 'Custom',
      reviewPeriodStart: '',
      reviewPeriodEnd: ''
    });

    const handleSubmit = (e) => {
      e.preventDefault();
      onSubmit(formData);
    };

    return e('div', { className: 'modal-overlay' },
      e('div', { className: 'modal' },
        e('h3', null, 'Create Performance Review'),
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
            e('label', null, 'Review Cycle'),
            e('select', {
              value: formData.reviewCycleId,
              onChange: (e) => setFormData({ ...formData, reviewCycleId: e.target.value }),
              required: true
            },
              cycles.map(c => e('option', { key: c.id, value: c.id }, c.name))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Framework'),
            e('select', {
              value: formData.framework,
              onChange: (e) => setFormData({ ...formData, framework: e.target.value }),
              required: true
            },
              ['Custom', 'OKR', 'BalancedScorecard'].map(f => e('option', { key: f, value: f }, f))
            )
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Review Period Start'),
            e('input', {
              type: 'date',
              value: formData.reviewPeriodStart,
              onChange: (e) => setFormData({ ...formData, reviewPeriodStart: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-group' },
            e('label', null, 'Review Period End'),
            e('input', {
              type: 'date',
              value: formData.reviewPeriodEnd,
              onChange: (e) => setFormData({ ...formData, reviewPeriodEnd: e.target.value }),
              required: true
            })
          ),
          e('div', { className: 'form-actions' },
            e('button', { type: 'button', onClick: onCancel }, 'Cancel'),
            e('button', { type: 'submit' }, 'Create Review')
          )
        )
      )
    );
  };

  const ReviewCard = ({ review }) => {
    const statusColors = {
      Draft: '#9ca3af',
      InProgress: '#fbbf24',
      Submitted: '#60a5fa',
      Completed: '#6ee7b7'
    };

    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, `Review: ${review.id}`),
        e('span', {
          className: 'status-badge',
          style: { backgroundColor: statusColors[review.status] || '#9ca3af' }
        }, review.status)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Employee: ${review.employeeId}`),
        e('p', null, `Framework: ${review.framework}`),
        e('p', null, `Period: ${review.reviewPeriodStart} to ${review.reviewPeriodEnd}`),
        review.overallScore && e('p', null, `Overall Score: ${review.overallScore}`)
      ),
      e('div', { className: 'card-actions' },
        review.status === 'Draft' && e('button', {
          onClick: () => handleUpdateReview(review.id, 'start', {})
        }, 'Start'),
        review.status === 'InProgress' && e('button', {
          onClick: () => handleUpdateReview(review.id, 'submit', {})
        }, 'Submit'),
        review.status === 'Submitted' && e('button', {
          onClick: () => {
            const score = prompt('Enter overall score (0-100):');
            const comments = prompt('Enter manager comments:');
            if (score) handleUpdateReview(review.id, 'complete', { overallScore: parseFloat(score), managerComments: comments });
          }
        }, 'Complete')
      )
    );
  };

  const CycleCard = ({ cycle }) => {
    return e('div', { className: 'item-card' },
      e('div', { className: 'card-header' },
        e('span', { className: 'card-title' }, cycle.name),
        e('span', { className: 'card-subtitle' }, cycle.framework)
      ),
      e('div', { className: 'card-body' },
        e('p', null, `Description: ${cycle.description}`),
        e('p', null, `Period: ${cycle.startDate} to ${cycle.endDate}`)
      )
    );
  };

  return e('div', { className: 'module-view' },
    e('div', { className: 'module-header' },
      e('h2', null, 'Performance Management'),
      e('button', { onClick: () => { setModalType('review'); setShowCreateModal(true); } }, '+ New Review')
    ),
    e('div', { className: 'tabs' },
      e('button', {
        className: activeTab === 'reviews' ? 'active' : '',
        onClick: () => setActiveTab('reviews')
      }, 'Reviews'),
      e('button', {
        className: activeTab === 'cycles' ? 'active' : '',
        onClick: () => setActiveTab('cycles')
      }, 'Review Cycles')
    ),
    e('div', { className: 'tab-content' },
      activeTab === 'reviews' && e('div', { className: 'items-grid' },
        reviews.length === 0 ? e('p', null, 'No performance reviews') :
        reviews.map(r => e(ReviewCard, { key: r.id, review: r }))
      ),
      activeTab === 'cycles' && e('div', { className: 'items-grid' },
        cycles.length === 0 ? e('p', null, 'No active review cycles') :
        cycles.map(c => e(CycleCard, { key: c.id, cycle: c }))
      )
    ),
    showCreateModal && modalType === 'review' && e(ReviewForm, {
      onSubmit: handleCreateReview,
      onCancel: () => setShowCreateModal(false),
      cycles
    })
  );
}
