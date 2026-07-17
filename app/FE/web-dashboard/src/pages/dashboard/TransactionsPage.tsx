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
import { getErrorMessage } from '@/utils/apiError';
import { downloadCsv } from '@/utils/csvExport';

const { Option } = Select;
const PAGE_SIZE = 10;
// BE giới hạn pageSize tối đa 100/request (xem PaymentsController.GetHistory) — export phải phân trang gộp lại.
const EXPORT_PAGE_SIZE = 100;

const mapPaymentToTransaction = (p: any): Transaction => ({
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
});

const TransactionsPage: React.FC = () => {
  const [searchText, setSearchText] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);

  useEffect(() => {
    const fetchPayments = async () => {
      setLoading(true);
      try {
        // Forward filter/pagination thật lên BE (PaymentsController.GetHistory) thay vì
        // luôn fetch 100 bản ghi rồi lọc client-side toàn bộ.
        const response = await apiClient.get<any>('/payments', {
          params: {
            page,
            pageSize: PAGE_SIZE,
            status: statusFilter === 'ALL' ? undefined : statusFilter,
          }
        });
        const items = response.data.data || [];
        const pagination = response.data.pagination;

        const mapped: Transaction[] = items.map(mapPaymentToTransaction);

        setTransactions(mapped);
        setTotal(pagination?.total ?? mapped.length);
      } catch (err) {
        message.error(getErrorMessage(err, 'Không tải được lịch sử giao dịch.'));
        setTransactions([]);
        setTotal(0);
      } finally {
        setLoading(false);
      }
    };

    fetchPayments();
  }, [page, statusFilter]);

  // Đổi filter status thì quay về trang 1 (tránh xin trang không tồn tại).
  const handleStatusFilterChange = (val: string) => {
    setStatusFilter(val);
    setPage(1);
  };

  // Tìm kiếm theo text chỉ áp dụng trong phạm vi trang hiện tại — BE không có full-text search.
  const filteredTransactions = useMemo(() => {
    return transactions.filter((t) =>
      t.id.toLowerCase().includes(searchText.toLowerCase()) ||
      t.tableNo.toLowerCase().includes(searchText.toLowerCase()) ||
      t.paymentMethod.toLowerCase().includes(searchText.toLowerCase())
    );
  }, [searchText, transactions]);

  const [exporting, setExporting] = useState(false);

  // Xuất toàn bộ giao dịch khớp status filter đang chọn (không chỉ trang hiện tại) —
  // gộp nhiều request vì BE giới hạn tối đa 100 bản ghi/lần (PaymentsController.GetHistory).
  const handleExportData = async () => {
    setExporting(true);
    try {
      const allItems: any[] = [];
      let currentPage = 1;
      let totalCount = Infinity;

      while (allItems.length < totalCount) {
        const response = await apiClient.get<any>('/payments', {
          params: {
            page: currentPage,
            pageSize: EXPORT_PAGE_SIZE,
            status: statusFilter === 'ALL' ? undefined : statusFilter,
          }
        });
        const items = response.data.data || [];
        totalCount = response.data.pagination?.total ?? items.length;
        allItems.push(...items);
        if (items.length === 0) break;
        currentPage++;
      }

      if (allItems.length === 0) {
        message.warning('Không có giao dịch nào để xuất.');
        return;
      }

      const rows: (string | number)[][] = [
        ['Order ID', 'Date & Time', 'Table No', 'Total Amount (VND)', 'Payment Method', 'Status'],
        ...allItems.map(mapPaymentToTransaction).map((t) => [
          t.id, t.dateTime, t.tableNo, t.totalAmount, t.paymentMethod, t.status,
        ]),
      ];

      const dateStr = new Date().toISOString().slice(0, 10);
      downloadCsv(`transactions_${dateStr}.csv`, rows);
      message.success(`Đã xuất ${allItems.length} giao dịch ra file CSV.`);
    } catch (err) {
      message.error(getErrorMessage(err, 'Không thể xuất dữ liệu giao dịch.'));
    } finally {
      setExporting(false);
    }
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
                onChange={handleStatusFilterChange}
                style={{ width: 140, height: 38 }}
              >
                <Option value="ALL">All Statuses</Option>
                <Option value="SUCCESS">Completed</Option>
                <Option value="PENDING">Pending</Option>
                <Option value="FAILED">Failed</Option>
                <Option value="REFUNDED">Refunded</Option>
                <Option value="EXPIRED">Expired</Option>
              </Select>
            </div>
          </div>

          <Button
            icon={<DownloadOutlined />}
            onClick={handleExportData}
            loading={exporting}
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
            current: page,
            pageSize: PAGE_SIZE,
            total,
            onChange: (nextPage) => setPage(nextPage),
            showTotal: (t, range) => `Showing ${range[0]} to ${range[1]} of ${t} entries`
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
