import { useState, useEffect, useRef } from 'react';
import Sidebar from './components/Sidebar';
import ChatArea from './components/ChatArea';
import ChatInput from './components/ChatInput';
import ApprovalBanner from './components/ApprovalBanner';
import { Bot } from 'lucide-react';

const API_BASE = 'http://localhost:5041/api/workflows';

function App() {
  const [workflowId, setWorkflowId] = useState('');
  const [messages, setMessages] = useState([]);
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  // Load history when workflowId changes
  useEffect(() => {
    if (!workflowId) {
      setMessages([]);
      setStatus('');
      return;
    }

    const fetchHistory = async () => {
      try {
        const res = await fetch(`${API_BASE}/${workflowId}/history`);
        if (res.ok) {
          const data = await res.json();
          setMessages(data.messages || []);
          setStatus(data.status || '');
        } else {
          // New workflow
          setMessages([{ role: 'System', content: 'Nuevo flujo de trabajo iniciado. Escribe un mensaje para comenzar.' }]);
          setStatus('Created');
        }
      } catch (error) {
        console.error('Error fetching history:', error);
      }
    };

    fetchHistory();
  }, [workflowId]);

  const handleSendMessage = async (msgText) => {
    if (!workflowId || !msgText.trim()) return;

    const userMessage = { role: 'User', content: msgText };
    setMessages(prev => [...prev, userMessage]);
    setIsLoading(true);

    try {
      const res = await fetch(`${API_BASE}/${workflowId}/chat?message=${encodeURIComponent(msgText)}`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data.messages || []);
        setStatus(data.status || '');
      } else {
        console.error('Failed to send message');
      }
    } catch (error) {
      console.error('Error during chat:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async () => {
    if (!workflowId) return;
    setIsLoading(true);
    try {
      const res = await fetch(`${API_BASE}/${workflowId}/approve`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data.messages || []);
        setStatus(data.status || '');
      }
    } catch (error) {
      console.error('Error approving workflow:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="app-container">
      <Sidebar 
        currentId={workflowId} 
        onSelectWorkflow={setWorkflowId} 
      />
      
      <main className="main-content">
        <header className="glass-panel" style={{ padding: '20px', borderRight: 'none', borderBottom: '1px solid var(--border-light)', display: 'flex', alignItems: 'center', gap: '12px' }}>
          <Bot className="font-gradient" size={28} color="var(--accent-cyan)" />
          <div>
            <h2 style={{ fontSize: '1.2rem', marginBottom: '2px' }}>AzureMAF <span className="font-gradient">Agent</span></h2>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              {workflowId ? `Workflow: ${workflowId}` : 'No workflow selected'}
            </div>
          </div>
        </header>

        {workflowId ? (
          <>
            <ApprovalBanner status={status} onApprove={handleApprove} isLoading={isLoading} />
            <ChatArea messages={messages} isLoading={isLoading} />
            <ChatInput onSend={handleSendMessage} disabled={isLoading || status === 'PendingApproval'} />
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
