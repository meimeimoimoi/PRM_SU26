import React, { useEffect, useRef, useState } from 'react';
import { FloatButton, Input, Button, Spin, Typography, Tooltip } from 'antd';
import { MessageOutlined, SendOutlined, RobotOutlined, UserOutlined, CloseOutlined, PlusOutlined } from '@ant-design/icons';
import { aiService } from '@/services/aiService';

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
}

const SUGGESTED_QUESTIONS = [
  'Doanh thu tháng này là bao nhiêu?',
  'Có bao nhiêu bàn đang trống?',
];

/**
 * Khung chat AI bật/tắt được cho admin dashboard — gọi AiController.Query (AI.API), service này
 * tự phân loại ý định (bàn/doanh thu/món bán chạy) rồi trả lời bằng dữ liệu thật của hệ thống.
 * Chỉ hiển thị cho MANAGER vì endpoint /ai/query yêu cầu role Manager (DashboardLayout đã lọc).
 * Hiển thị dạng khung nhỏ neo góc dưới-phải (không dùng Drawer full-height) để không che hết trang.
 */
const ChatWidget: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  const sendMessage = async (text: string) => {
    const prompt = text.trim();
    if (!prompt || loading) return;

    const userMsg: ChatMessage = { id: `u-${Date.now()}`, role: 'user', text: prompt };
    setMessages((prev) => [...prev, userMsg]);
    setInput('');
    setLoading(true);

    try {
      const answer = await aiService.ask(prompt);
      setMessages((prev) => [
        ...prev,
        { id: `a-${Date.now()}`, role: 'assistant', text: answer || 'Xin lỗi, tôi chưa có câu trả lời cho câu hỏi này.' },
      ]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        { id: `e-${Date.now()}`, role: 'assistant', text: 'Không thể kết nối tới trợ lý AI. Vui lòng thử lại sau.' },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const startNewSession = () => {
    setMessages([]);
    setInput('');
  };

  if (!open) {
    return (
      <FloatButton
        icon={<MessageOutlined />}
        type="primary"
        style={{ insetInlineEnd: 24, insetBlockEnd: 24 }}
        onClick={() => setOpen(true)}
        tooltip="Trợ lý AI"
      />
    );
  }

  return (
    <div
      style={{
        position: 'fixed',
        insetInlineEnd: 24,
        insetBlockEnd: 24,
        width: 360,
        height: 520,
        background: '#fff',
        borderRadius: 12,
        boxShadow: '0 6px 24px rgba(0,0,0,0.18)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        zIndex: 1000,
      }}
    >
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '10px 14px',
          borderBottom: '1px solid #f0f0f0',
          background: '#fafafa',
        }}
      >
        <span style={{ fontWeight: 600, fontSize: 14 }}>
          <RobotOutlined style={{ marginRight: 8, color: '#1890ff' }} />
          Trợ lý AI SmartDine
        </span>
        <div style={{ display: 'flex', gap: 4 }}>
          <Tooltip title="Đoạn chat mới">
            <Button
              type="text"
              size="small"
              icon={<PlusOutlined />}
              onClick={startNewSession}
              disabled={messages.length === 0}
            />
          </Tooltip>
          <Tooltip title="Đóng">
            <Button type="text" size="small" icon={<CloseOutlined />} onClick={() => setOpen(false)} />
          </Tooltip>
        </div>
      </div>

      {/* Messages */}
      <div style={{ flex: 1, overflowY: 'auto', padding: 14 }}>
        {messages.length === 0 && (
          <div style={{ marginBottom: 16 }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 12 }}>
              {SUGGESTED_QUESTIONS.map((q) => (
                <Button key={q} size="small" onClick={() => sendMessage(q)} style={{ textAlign: 'left' }}>
                  {q}
                </Button>
              ))}
            </div>
          </div>
        )}

        {messages.map((m) => (
          <div
            key={m.id}
            style={{
              display: 'flex',
              justifyContent: m.role === 'user' ? 'flex-end' : 'flex-start',
              marginBottom: 10,
            }}
          >
            <div
              style={{
                maxWidth: '80%',
                padding: '8px 12px',
                borderRadius: 12,
                background: m.role === 'user' ? '#1890ff' : '#f0f2f5',
                color: m.role === 'user' ? '#fff' : '#1a202c',
                fontSize: 13,
                whiteSpace: 'pre-wrap',
              }}
            >
              {m.role === 'assistant' && <RobotOutlined style={{ marginRight: 6 }} />}
              {m.role === 'user' && <UserOutlined style={{ marginRight: 6 }} />}
              {m.text}
            </div>
          </div>
        ))}

        {loading && (
          <div style={{ display: 'flex', justifyContent: 'flex-start', marginBottom: 10 }}>
            <Spin size="small" />
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <div style={{ borderTop: '1px solid #f0f0f0', padding: 10, display: 'flex', gap: 8 }}>
        <Input
          placeholder="Nhập câu hỏi..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onPressEnter={() => sendMessage(input)}
          disabled={loading}
        />
        <Button
          type="primary"
          icon={<SendOutlined />}
          onClick={() => sendMessage(input)}
          loading={loading}
          disabled={!input.trim()}
        />
      </div>
    </div>
  );
};

export default ChatWidget;
