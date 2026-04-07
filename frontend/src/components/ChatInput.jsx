import { useState, useRef, useEffect } from 'react';
import { Send } from 'lucide-react';

function ChatInput({ onSend, disabled }) {
  const [text, setText] = useState('');
  const inputRef = useRef(null);

  useEffect(() => {
    if (!disabled && inputRef.current) {
      inputRef.current.focus();
    }
  }, [disabled]);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (text.trim() && !disabled) {
      onSend(text);
      setText('');
    }
  };

  return (
    <div style={{ padding: '24px', background: 'var(--bg-base)', borderTop: '1px solid var(--border-light)' }}>
      <form onSubmit={handleSubmit} style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
        <input 
          ref={inputRef}
          className="input-base glass-surface"
          style={{ 
            padding: '16px 60px 16px 20px', 
            borderRadius: '24px',
            background: 'rgba(20, 22, 27, 0.4)',
            fontSize: '1rem',
            opacity: disabled ? 0.6 : 1
          }}
          placeholder={disabled ? "Por favor, espera..." : "Escribe un mensaje al agente..."}
          value={text}
          onChange={(e) => setText(e.target.value)}
          disabled={disabled}
        />
        <button 
          type="submit" 
          disabled={!text.trim() || disabled}
          style={{
            position: 'absolute',
            right: '8px',
            width: '40px',
            height: '40px',
            borderRadius: '50%',
            border: 'none',
            background: (!text.trim() || disabled) ? 'rgba(255,255,255,0.1)' : 'var(--accent-gradient)',
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: (!text.trim() || disabled) ? 'not-allowed' : 'pointer',
            transition: 'all 0.2s',
            boxShadow: (!text.trim() || disabled) ? 'none' : '0 0 15px rgba(0, 210, 255, 0.3)'
          }}
        >
          <Send size={18} style={{ transform: 'translateX(2px)' }} />
        </button>
      </form>
    </div>
  );
}

export default ChatInput;
