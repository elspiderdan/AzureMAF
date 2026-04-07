import { useEffect, useRef } from 'react';
import { User, Sparkles, Server } from 'lucide-react';

function ChatArea({ messages, isLoading }) {
  const bottomRef = useRef(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  return (
    <div style={{ flex: 1, padding: '24px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {messages.map((msg, index) => {
        const isUser = msg.role === 'User';
        const isSystem = msg.role === 'System';
        
        return (
          <div 
            key={index} 
            className="animate-slide-up"
            style={{ 
              display: 'flex', 
              flexDirection: isUser ? 'row-reverse' : 'row',
              gap: '16px',
              maxWidth: '85%',
              alignSelf: isUser ? 'flex-end' : 'flex-start'
            }}
          >
            <div style={{ 
              width: '36px', 
              height: '36px', 
              borderRadius: '50%', 
              background: isUser ? 'var(--msg-user-bg)' : isSystem ? 'rgba(255,255,255,0.1)' : 'var(--bg-panel)',
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              boxShadow: isUser ? '0 4px 15px rgba(0, 210, 255, 0.2)' : 'var(--shadow-glass)',
              border: '1px solid var(--border-light)',
              flexShrink: 0
            }}>
              {isUser ? <User size={18} color="#fff" /> : isSystem ? <Server size={18} color="var(--text-muted)" /> : <Sparkles size={18} color="var(--accent-amethyst)" />}
            </div>
            
            <div className="glass-surface" style={{ 
              padding: '16px 20px', 
              background: isUser ? 'var(--msg-user-bg)' : 'var(--msg-agent-bg)',
              color: isUser ? 'var(--msg-user-text)' : 'var(--msg-agent-text)',
              borderBottomRightRadius: isUser ? '4px' : '16px',
              borderBottomLeftRadius: !isUser ? '4px' : '16px',
              lineHeight: '1.6',
              fontSize: '0.95rem'
            }}>
              {msg.content}
            </div>
          </div>
        );
      })}
      
      {isLoading && (
        <div style={{ display: 'flex', gap: '16px', maxWidth: '85%', alignSelf: 'flex-start' }}>
           <div style={{ 
              width: '36px', 
              height: '36px', 
              borderRadius: '50%', 
              background: 'var(--bg-panel)',
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              border: '1px solid var(--border-light)',
            }}>
               <Sparkles size={18} color="var(--accent-amethyst)" className="animate-pulse" />
            </div>
            <div className="glass-surface animate-pulse" style={{ padding: '16px 20px', background: 'var(--msg-agent-bg)' }}>
              <i>Procesando...</i>
            </div>
        </div>
      )}
      <div ref={bottomRef} />
    </div>
  );
}

export default ChatArea;
