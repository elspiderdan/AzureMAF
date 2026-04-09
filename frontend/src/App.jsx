import { useState, useEffect, useCallback } from 'react';
import Sidebar from './components/Sidebar';
import ChatArea from './components/ChatArea';
import ChatInput from './components/ChatInput';
import ApprovalBanner from './components/ApprovalBanner';
import PromptManager from './components/PromptManager';
import { Bot } from 'lucide-react';

const API_BASES = {
  default: 'http://localhost:5041/api/workflows',
  custom: 'http://localhost:5041/api/custom-workflows'
};
const PROMPT_API_BASE = 'http://localhost:5041/api/prompts';

function App() {
  const [agentType, setAgentType] = useState('default');
  const [workflowId, setWorkflowId] = useState('');
  const [messages, setMessages] = useState([]);
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [prompts, setPrompts] = useState([]);
  const [activePromptId, setActivePromptId] = useState(null);
  const currentApiBase = API_BASES[agentType];
  const isWaitingForHuman = status === 'WaitingForHuman' || status?.startsWith('WaitingForHumanStep');

  // Load history when workflowId changes
  useEffect(() => {
    if (!workflowId) {
      setMessages([]);
      setStatus('');
      return;
    }

    const fetchHistory = async () => {
      try {
        setError('');
        const res = await fetch(`${currentApiBase}/${workflowId}/history`);
        if (res.ok) {
          const data = await res.json();
          setMessages(data.messages || []);
          setStatus(data.status || '');
        } else {
          // New workflow
          setMessages([{ role: 'System', content: 'Nuevo flujo de trabajo iniciado. Escribe un mensaje para comenzar.' }]);
          setStatus('Created');
        }
      } catch {
        setError('No se pudo cargar el historial del workflow.');
      }
    };

    fetchHistory();
  }, [workflowId, currentApiBase]);

  const fetchPrompts = useCallback(async () => {
    try {
      const res = await fetch(`${PROMPT_API_BASE}?agent=${agentType}`);
      if (!res.ok) throw new Error('failed prompt list');
      const data = await res.json();
      setPrompts(data.items || []);
      setActivePromptId(data.activePromptId || null);
    } catch {
      setError('No se pudo cargar la configuracion de prompts.');
    }
  }, [agentType]);

  useEffect(() => {
    fetchPrompts();
  }, [fetchPrompts]);

  const handleSendMessage = async (msgText) => {
    if (!workflowId || !msgText.trim()) return;

    const userMessage = { role: 'User', content: msgText };
    setMessages(prev => [...prev, userMessage]);
    setIsLoading(true);
    setError('');

    try {
      const res = await fetch(`${currentApiBase}/${workflowId}/chat?message=${encodeURIComponent(msgText)}`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data.messages || []);
        setStatus(data.status || '');
      } else {
        const err = await res.json();
        setError(err.error || 'No se pudo enviar el mensaje.');
      }
    } catch {
      setError('Error de conectividad enviando mensaje.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async () => {
    if (!workflowId) return;
    setIsLoading(true);
    setError('');
    try {
      const res = await fetch(`${currentApiBase}/${workflowId}/approve`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data.messages || []);
        setStatus(data.status || '');
      } else {
        const err = await res.json();
        setError(err.error || 'No se pudo aprobar.');
      }
    } catch {
      setError('Error aprobando workflow.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleReject = async () => {
    if (!workflowId) return;
    setIsLoading(true);
    setError('');
    try {
      const res = await fetch(`${currentApiBase}/${workflowId}/reject`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data.messages || []);
        setStatus(data.status || '');
      } else {
        const err = await res.json();
        setError(err.error || 'No se pudo rechazar.');
      }
    } catch {
      setError('Error rechazando workflow.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreatePrompt = async ({ version, content }) => {
    try {
      setError('');
      const res = await fetch(PROMPT_API_BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ agentName: agentType, version, content })
      });

      if (!res.ok) {
        const err = await res.json();
        setError(err.error || 'No se pudo crear el prompt.');
        return false;
      }

      await fetchPrompts();
      return true;
    } catch {
      setError('Error creando version del prompt.');
      return false;
    }
  };

  const handleActivatePrompt = async (promptId) => {
    try {
      setError('');
      const res = await fetch(`${PROMPT_API_BASE}/${promptId}/activate?agent=${agentType}`, {
        method: 'POST'
      });

      if (!res.ok) {
        setError('No se pudo activar el prompt seleccionado.');
        return;
      }

      await fetchPrompts();
    } catch {
      setError('Error activando prompt.');
    }
  };

  return (
    <div className="app-container">
      <Sidebar
        onSelectWorkflow={setWorkflowId}
        selectedAgent={agentType}
        onSelectAgent={(type) => {
          setAgentType(type);
          setWorkflowId('');
          setMessages([]);
          setStatus('');
          setError('');
        }}
      />
      
      <main className="main-content">
        <header className="glass-panel" style={{ padding: '20px', borderRight: 'none', borderBottom: '1px solid var(--border-light)', display: 'flex', alignItems: 'center', gap: '12px' }}>
          <Bot className="font-gradient" size={28} color="var(--accent-cyan)" />
          <div>
            <h2 style={{ fontSize: '1.2rem', marginBottom: '2px' }}>
              AzureMAF <span className="font-gradient">{agentType === 'custom' ? 'Custom Agent' : 'Agent'}</span>
            </h2>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              {workflowId ? `Workflow: ${workflowId}` : 'No workflow selected'}
            </div>
          </div>
        </header>

        {workflowId ? (
          <>
            <PromptManager
              prompts={prompts}
              activePromptId={activePromptId}
              isLoading={isLoading}
              onRefresh={fetchPrompts}
              onCreate={handleCreatePrompt}
              onActivate={handleActivatePrompt}
            />
            <ApprovalBanner status={status} onApprove={handleApprove} onReject={handleReject} isLoading={isLoading} />
            {error && (
              <div style={{ margin: '12px 24px 0', color: '#ff7b7b', fontSize: '0.9rem' }}>
                {error}
              </div>
            )}
            <ChatArea messages={messages} isLoading={isLoading} />
            <ChatInput onSend={handleSendMessage} disabled={isLoading || isWaitingForHuman} />
          </>
        ) : (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-muted)' }}>
            <div style={{ textAlign: 'center' }}>
              <Bot size={64} style={{ opacity: 0.2, marginBottom: '16px' }} />
              <p>Create or select a workflow to begin</p>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
