const e = React.createElement;
const root = document.getElementById('app');

function Header(){
  return e('header',{className:'app-header'},
    e('div',{className:'brand'}, e('div',{className:'logo'}), e('div',null, e('h2',null,'OnePage Demo'), e('div',{className:'small-muted'},'Assets • Approvals • Inventory • POS'))),
    e('div',null, e('button',{className:'btn btn-sm btn-light', onClick: ()=>window.location.reload()},'Refresh'))
  );
}

function ApiClient(){
  const base = window.__ONEPAGE_API_BASE__ || 'http://localhost:5001/api/v1';
  async function json(path, method='GET', body){
    const opts = { method, headers: {'Content-Type':'application/json', 'X-Tenant-Id': 'demo-tenant' } };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(base+path, opts);
    try { return await res.json(); } catch(e){ return null; }
  }
  return { json };
}

function AssetForm({onCreated}){
  const [tag,setTag] = React.useState('ASSET-'+Math.floor(Math.random()*9000+1000));
  const [name,setName] = React.useState('Demo Laptop');
  const [location,setLocation] = React.useState('HQ');
  const api = React.useMemo(()=>ApiClient(),[]);
  async function submit(e){
    e.preventDefault();
    const id = 'asset-'+Date.now();
    const payload = { Id: id, Tag: tag, Name: name, Description: 'Demo asset', LocationId: location, CustodianEmployeeId: null, LegalEntityId: null, BranchId: null, DepartmentId: null };
    const res = await api.json('/assets','POST',payload);
    onCreated(res);
  }
  return e('form',{onSubmit:submit,className:'card-ghost mb-3'},
    e('h5',null,'Create asset'),
    e('div',{className:'row'},
      e('div',{className:'col-md-4 field'}, e('label',null,'Tag'), e('input',{className:'form-control', value:tag, onChange:ev=>setTag(ev.target.value)})),
      e('div',{className:'col-md-8 field'}, e('label',null,'Name'), e('input',{className:'form-control', value:name, onChange:ev=>setName(ev.target.value)}))
    ),
    e('div',{className:'row'},
      e('div',{className:'col-md-6 field'}, e('label',null,'Location'), e('input',{className:'form-control', value:location, onChange:ev=>setLocation(ev.target.value)})),
      e('div',{className:'col-md-6 field d-flex align-items-end'}, e('button',{className:'btn btn-accent w-100'},'Create Asset'))
    )
  );
}

function AssetList(){
  const [items,setItems] = React.useState([]);
  const api = React.useMemo(()=>ApiClient(),[]);
  React.useEffect(()=>{ api.json('/assets').then(r=>{ if(Array.isArray(r)) setItems(r); else if(r) setItems([]); }); },[]);
  return e('div',null,
    e('h5',null,'Assets'),
    e('div',{className:'table-glass p-2'},
      e('table',{className:'table table-dark table-striped mb-0'},
        e('thead',null, e('tr',null, e('th',null,'Tag'), e('th',null,'Name'), e('th',null,'Status'))),
        e('tbody',null, items.map(it=> e('tr',{key:it.id}, e('td',null,it.tag), e('td',null,it.name), e('td',null,it.status))))
      )
    )
  );
}

function ApprovalList(){
  const [items,setItems] = React.useState([]);
  const api = React.useMemo(()=>ApiClient(),[]);
  React.useEffect(()=>{ api.json('/approvals').then(r=>{ if(Array.isArray(r)) setItems(r); else if(r) setItems([]); }); },[]);
  return e('div',null,
    e('h5',null,'Approvals (demo)'),
    e('div',{className:'card-ghost p-2'}, items.length===0 ? e('div',null,'No pending approvals') : items.map(a=> e('div',{key:a.id, className:'mb-2'}, e('div',null, e('strong',null,a.resourceType), ' ', a.resourceId), e('div',null, e('small',null,'Requested by: '+a.requestedBy))))
  );
}

function Main(){
  const [lastCreated,setLastCreated] = React.useState(null);
  return e('div',null,
    e(Header),
    e('div',{className:'row mb-4'}, e('div',{className:'col-md-8'}, e(AssetForm,{onCreated:setLastCreated}), e('div',{className:'card-ghost p-3 mt-3'}, lastCreated ? e('pre',null,JSON.stringify(lastCreated,null,2)) : e('div',null,'Create assets to emit audit events.'))), e('div',{className:'col-md-4'}, e(AssetList), e('div',{className:'mt-3'}, e(ApprovalList)))),
    e('div',{className:'footer-note'},'OnePage demo • Prototype UI — not for production')
  );
}

ReactDOM.createRoot(document.getElementById('app')).render(e(Main));
