import { ShieldAlert, Check, X } from 'lucide-react';

function ApprovalBanner({ status, onApprove, onReject, isLoading }) {
  if (status !== 'WaitingForHuman') return null;

  return (
    <div className="glass-surface animate-slide-up" style={{
      margin: '20px 24px 0',
      padding: '16px 24px',
      background: 'rgba(157, 78, 221, 0.1)',
      border: '1px solid rgba(157, 78, 221, 0.3)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      borderRadius: '12px',
      boxShadow: '0 0 20px rgba(157, 78, 221, 0.15)'
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
        <div style={{ background: 'rgba(157, 78, 221, 0.2)', padding: '8px', borderRadius: '50%', color: 'var(--accent-amethyst)' }}>
          <ShieldAlert size={20} />
        </div>
        <div>
          <h3 style={{ fontSize: '0.95rem', marginBottom: '2px', color: 'var(--text-primary)' }}>Acción Requerida</h3>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', margin: 0 }}>
            El agente ha pausado el flujo y requiere aprobación humana para continuar.
          </p>
        </div>
      </div>
      
      <div style={{ display: 'flex', gap: '10px' }}>
        <button 
          className="btn" 
          onClick={onReject}
          disabled={isLoading}
          style={{ 
            background: 'rgba(255, 82, 82, 0.2)', 
            color: '#fff', 
            border: '1px solid rgba(255, 82, 82, 0.5)'
          }}
        >
          <X size={16} /> Rechazar
        </button>
        <button 
          className="btn" 
          onClick={onApprove} 
          disabled={isLoading}
          style={{ 
            background: 'var(--accent-amethyst)', 
            color: '#fff', 
            border: 'none',
            boxShadow: '0 4px 15px rgba(157, 78, 221, 0.3)'
          }}
        >
          <Check size={16} /> Aprobar Flujo
        </button>
      </div>
    </div>
  );
}

export default ApprovalBanner;
