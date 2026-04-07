import { useState } from 'react';
import { Plus, Hash, LogIn } from 'lucide-react';

function Sidebar({ onSelectWorkflow }) {
  const [inputId, setInputId] = useState('');

  const generateUuid = () => {
    return crypto.randomUUID();
  };

  const handleNew = () => {
    onSelectWorkflow(generateUuid());
  };

  const handleConnect = (e) => {
    e.preventDefault();
    if (inputId.trim()) {
      onSelectWorkflow(inputId.trim());
      setInputId('');
    }
  };

  return (
    <aside className="glass-panel" style={{ width: '280px', display: 'flex', flexDirection: 'column', padding: '20px' }}>
      <div style={{ marginBottom: '30px' }}>
        <h1 className="font-gradient" style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '1.5rem', marginBottom: '24px' }}>
          AzureMAF <br/><span style={{fontSize: '1rem', color: 'var(--text-muted)'}}>Frontend</span>
        </h1>
        
        <button className="btn btn-primary" onClick={handleNew} style={{ width: '100%', marginBottom: '20px' }}>
          <Plus size={18} /> New Workflow
        </button>

        <div style={{ borderTop: '1px solid var(--border-light)', margin: '20px 0' }}></div>
        
        <form onSubmit={handleConnect}>
          <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '8px' }}>
            Connect to Existing
          </label>
          <div style={{ position: 'relative', marginBottom: '12px' }}>
            <Hash size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
            <input 
              className="input-base" 
              placeholder="Workflow UUID" 
              value={inputId}
              onChange={(e) => setInputId(e.target.value)}
              style={{ paddingLeft: '36px' }}
            />
          </div>
          <button type="submit" className="btn btn-outline" style={{ width: '100%' }}>
            <LogIn size={18} /> Connect
          </button>
        </form>
      </div>

      <div style={{ marginTop: 'auto', fontSize: '0.75rem', color: 'var(--text-muted)', textAlign: 'center' }}>
        Minimalist UI Engine
      </div>
    </aside>
  );
}

export default Sidebar;
