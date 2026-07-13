import React, { useState, useMemo, useEffect } from 'react';
import { 
  Table, 
  Tag, 
  Button, 
  Card, 
  Input, 
  Select, 
  message, 
  Tooltip,
  Modal
} from 'antd';
import { 
  SearchOutlined, 
  DownloadOutlined, 
  EyeOutlined 
} from '@ant-design/icons';

import { Transaction } from '@/types/transaction';
import { apiClient } from '../../services/api/client';

const { Option } = Select;

const TransactionsPage: React.FC = () => {
  const [searchText, setSearchText] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPayments = async () => {
      setLoading(true);
      try {
        const response = await apiClient.get<any>('/payments', {
          params: { pageSize: 100 }
        });
        const data = response.data.data || response.data;
        const items = Array.isArray(data) ? data : (data.items || []);

        const mapped: Transaction[] = items.map((p: any) => ({
          id: p.invoiceId ? `#INV-${p.invoiceId}` : `#PAY-${p.id}`,
          dateTime: p.paidAt 
            ? new Date(p.paidAt).toLocaleString('vi-VN')
            : p.createdAt 
            ? new Date(p.createdAt).toLocaleString('vi-VN')
            : 'N/A',
          tableNo: p.tableNumber ? `T-${p.tableNumber}` : 'N/A',
          totalAmount: p.amount !== undefined ? p.amount : (p.totalAmount || 0),
          paymentMethod: p.paymentMethod || p.method || 'N/A',
          status: p.paymentStatus || p.status || 'PENDING',
        }));

        setTransactions(mapped);
      } catch (err) {
        console.warn('API /payments unavailable, showing empty list.', err);
        setTransactions([]);
      } finally {
        setLoading(false);
      }
    };

    fetchPayments();
  }, []);

  // Search & Filter
  const filteredTransactions = useMemo(() => {
    return transactions.filter((t) => {
      const matchesSearch = 
        t.id.toLowerCase().includes(searchText.toLowerCase()) ||
        t.tableNo.toLowerCase().includes(searchText.toLowerCase()) ||
        t.paymentMethod.toLowerCase().includes(searchText.toLowerCase());

      const matchesStatus = 
        statusFilter === 'ALL' || 
        t.status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [searchText, statusFilter, transactions]);

  const handleExportData = () => {
    message.success('Đang kết xuất dữ liệu lịch sử giao dịch dưới dạng CSV...');
  };

  const handleViewDetails = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
  };

  const columns = [
    {
      title: 'ORDER ID',
      dataIndex: 'id',
      key: 'id',
      render: (id: string, record: Transaction) => (
        <a 
          style={{ fontWeight: 600, color: '#1890ff' }} 
          onClick={() => handleViewDetails(record)}
        >
          {id}
        </a>
      ),
    },
    {
      title: 'DATE & TIME',
      dataIndex: 'dateTime',
      key: 'dateTime',
      render: (dateTime: string) => <span style={{ color: '#4a5568' }}>{dateTime}</span>,
    },
    {
      title: 'TABLE NO',
      dataIndex: 'tableNo',
      key: 'tableNo',
      render: (tableNo: string) => <span style={{ fontWeight: 500, color: '#2d3748' }}>{tableNo}</span>,
    },
    {
      title: 'TOTAL AMOUNT',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (amount: number) => (
        <span style={{ fontWeight: 600, color: '#1a202c' }}>
          {amount.toLocaleString('vi-VN')}đ
        </span>
      ),
    },
    {
      title: 'PAYMENT METHOD',
      dataIndex: 'paymentMethod',
      key: 'paymentMethod',
      render: (method: string) => <span style={{ color: '#4a5568' }}>{method}</span>,
    },
    {
      title: 'STATUS',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        let bgColor = '#f6ffed';
        let textColor = '#52c41a';

        if (status === 'CANCELLED' || status === 'FAILED') {
          bgColor = '#fff1f0';
          textColor = '#f5222d';
        } else if (status === 'PENDING' || status === 'EXPIRED') {
          bgColor = '#fffbe6';
          textColor = '#faad14';
        } else if (status === 'REFUNDED') {
          bgColor = '#fafafa';
          textColor = '#8c8c8c';
        }

        return (
          <Tag 
            style={{ 
              borderRadius: 6, 
              padding: '2px 12px', 
              fontWeight: 500,
              backgroundColor: bgColor,
              color: textColor,
              border: 'none'
            }}
          >
            {status}
          </Tag>
        );
      },
    },
    {
      title: 'ACTION',
      key: 'action',
      render: (_: any, record: Transaction) => (
        <Tooltip title="Xem chi tiết hóa đơn">
          <Button 
            type="text" 
            icon={<EyeOutlined style={{ color: '#4a5568' }} />} 
            onClick={() => handleViewDetails(record)}
          />
        </Tooltip>
      ),
    },
  ];

  return (
    <div className="transactions-container">
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ margin: 0, fontSize: '28px', fontWeight: 700, color: '#1a202c' }}>Order & Transaction History</h2>
        <p style={{ margin: 0, color: '#718096', fontSize: '14px' }}>View past orders and manage receipts.</p>
      </div>

      <Card 
        bordered={false}
        style={{
          borderRadius: 8,
          marginBottom: 20,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)'
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 16, alignItems: 'center', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', flex: 1, alignItems: 'center' }}>
            <div style={{ minWidth: 200 }}>
              <span style={{ display: 'block', fontSize: '12px', color: '#718096', marginBottom: 4, fontWeight: 500 }}>Search Order ID</span>
              <Input
                placeholder="e.g. #ORD-1234"
                prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                style={{ borderRadius: 6, height: 38 }}
              />
            </div>

            <div>
              <span style={{ display: 'block', fontSize: '12px', color: '#718096', marginBottom: 4, fontWeight: 500 }}>Status</span>
              <Select
                defaultValue="ALL"
                value={statusFilter}
                onChange={(val) => setStatusFilter(val)}
                style={{ width: 140, height: 38 }}
              >
                <Option value="ALL">All Statuses</Option>
                <Option value="SUCCESS">Completed</Option>
                <Option value="PENDING">Pending</Option>
                <Option value="FAILED">Failed</Option>
                <Option value="CANCELLED">Cancelled</Option>
              </Select>
            </div>
          </div>

          <Button 
            icon={<DownloadOutlined />}
            onClick={handleExportData}
            style={{ 
              borderRadius: 6, 
              height: 38,
              fontWeight: 500,
              alignSelf: 'flex-end',
              marginBottom: 0
            }}
          >
            Export Data
          </Button>
        </div>
      </Card>

      <Card
        bordered={false}
        style={{
          borderRadius: 8,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)'
        }}
      >
        <Table 
          columns={columns} 
          dataSource={filteredTransactions} 
          loading={loading}
          rowKey="id"
          locale={{ emptyText: 'Không có giao dịch nào.' }}
          pagination={{
            pageSize: 10,
            showTotal: (total, range) => `Showing ${range[0]} to ${range[1]} of ${total} entries`
          }}
        />
      </Card>

      <Modal
        title={`Chi tiết giao dịch ${selectedTransaction?.id}`}
        open={selectedTransaction !== null}
        onCancel={() => setSelectedTransaction(null)}
        footer={[
          <Button key="close" onClick={() => setSelectedTransaction(null)}>Đóng</Button>,
        ]}
      >
        {selectedTransaction && (
          <div style={{ marginTop: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <span style={{ color: '#718096' }}>Thời gian:</span>
              <span style={{ fontWeight: 500 }}>{selectedTransaction.dateTime}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <span style={{ color: '#718096' }}>Bàn phục vụ:</span>
              <span style={{ fontWeight: 500 }}>{selectedTransaction.tableNo}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <span style={{ color: '#718096' }}>Phương thức thanh toán:</span>
              <span style={{ fontWeight: 500 }}>{selectedTransaction.paymentMethod}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <span style={{ color: '#718096' }}>Trạng thái:</span>
              <span style={{ fontWeight: 500 }}>{selectedTransaction.status}</span>
            </div>
            <hr style={{ border: 'none', borderTop: '1px solid #f0f0f0', margin: '12px 0' }} />
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '16px', fontWeight: 600 }}>
              <span>TỔNG THANH TOÁN:</span>
              <span style={{ color: '#1890ff' }}>{selectedTransaction.totalAmount.toLocaleString('vi-VN')}đ</span>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default TransactionsPage;
