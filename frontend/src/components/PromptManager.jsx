import { useMemo, useState } from 'react';
import { Settings2, Plus, CheckCircle2 } from 'lucide-react';

function PromptManager({
  prompts,
  activePromptId,
  isLoading,
  onRefresh,
  onCreate,
  onActivate
}) {
  const [version, setVersion] = useState('');
  const [content, setContent] = useState('');
  const sortedPrompts = useMemo(
    () => [...prompts].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)),
    [prompts]
  );

  const submitCreate = async (e) => {
    e.preventDefault();
    if (!version.trim() || !content.trim()) return;
    const ok = await onCreate({ version: version.trim(), content: content.trim() });
    if (ok) {
      setVersion('');
      setContent('');
    }
  };

  return (
    <section className="glass-surface" style={{ margin: '16px 24px 0', padding: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <h3 style={{ display: 'flex', gap: '8px', alignItems: 'center', fontSize: '0.95rem' }}>
          <Settings2 size={16} /> Prompt del agente
        </h3>
        <button className="btn btn-outline" onClick={onRefresh} disabled={isLoading}>Refrescar</button>
      </div>

      <form onSubmit={submitCreate} style={{ display: 'grid', gap: '8px', marginBottom: '12px' }}>
        <input
          className="input-base"
          placeholder="Version (ej. v2, release-2026-04)"
          value={version}
          onChange={(e) => setVersion(e.target.value)}
          disabled={isLoading}
        />
        <textarea
          className="input-base"
          style={{ minHeight: '90px', resize: 'vertical' }}
          placeholder="Contenido del prompt de sistema"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          disabled={isLoading}
        />
        <button type="submit" className="btn btn-primary" disabled={isLoading || !version.trim() || !content.trim()}>
          <Plus size={14} /> Crear nueva version
        </button>
      </form>

      <div style={{ maxHeight: '160px', overflowY: 'auto', display: 'grid', gap: '8px' }}>
        {sortedPrompts.map((item) => (
          <div key={item.id} className="glass-surface" style={{ padding: '10px 12px', borderRadius: '10px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: '8px', alignItems: 'center' }}>
              <strong>{item.version}</strong>
              {activePromptId === item.id ? (
                <span style={{ fontSize: '0.8rem', color: '#88f7a1', display: 'flex', gap: '4px', alignItems: 'center' }}>
                  <CheckCircle2 size={14} /> Activo
                </span>
              ) : (
                <button className="btn btn-outline" onClick={() => onActivate(item.id)} disabled={isLoading}>
                  Usar
                </button>
              )}
            </div>
            <p style={{ marginTop: '6px', fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
              {item.content}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

export default PromptManager;
