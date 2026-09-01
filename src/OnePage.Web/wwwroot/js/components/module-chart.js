import { e } from '../utils.js';

// ===== Module Chart =====
function ModuleChart({ items, field, label }) {
  const canvasRef = React.useRef(null);
  React.useEffect(() => {
    const ctx = canvasRef.current;
    if (!ctx) return;
    if (ctx.chartInstance) ctx.chartInstance.destroy();
    const counts = {};
    items.forEach(it => { const v = it[field] || 'unknown'; counts[v] = (counts[v] || 0) + 1; });
    const labels = Object.keys(counts);
    const dataValues = labels.map(l => counts[l]);
    ctx.chartInstance = new Chart(ctx, {
      type: 'bar',
      data: { labels, datasets: [{ label: label || 'Count', data: dataValues, backgroundColor: 'rgba(110, 231, 183, .35)', borderColor: '#6ee7b7', borderWidth: 1 }] },
      options: { plugins: { legend: { display: false } }, responsive: true, maintainHeight: false, scales: { y: { ticks: { color: '#94a3b8' }, grid: { color: 'rgba(255,255,255,.06)' } }, x: { ticks: { color: '#94a3b8' }, grid: { display: false } } } }
    });
    return () => { if (ctx.chartInstance) ctx.chartInstance.destroy(); ctx.chartInstance = null; };
  }, [items, field, label]);
  return e('canvas', { ref: canvasRef, height: 130 });
}

export { ModuleChart };
