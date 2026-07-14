import React, { useEffect, useState, useMemo } from 'react';
import { 
  Table, 
  Tag, 
  Button, 
  Space, 
  Card, 
  Input, 
  Select, 
  Modal, 
  Form, 
  InputNumber, 
  message, 
  Tooltip,
  Dropdown,
  MenuProps
} from 'antd';
import { 
  SearchOutlined, 
  PlusOutlined, 
  EditOutlined, 
  DeleteOutlined, 
  CheckCircleOutlined, 
  ExclamationCircleOutlined 
} from '@ant-design/icons';
import { Table as TableType, TableStatus } from '@/types/table';
import { tableService } from '@/services/tableService';
import { getErrorMessage } from '@/utils/apiError';

const { Option } = Select;

const TableManagementPage: React.FC = () => {
  const [tables, setTables] = useState<TableType[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');

  // Modal state
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [form] = Form.useForm();

  // 1. Fetch tables from API
  const fetchTables = async () => {
    setLoading(true);
    try {
      const data = await tableService.getAllTables();
      // Sort tables by tableNumber ascending
      const sorted = [...(data || [])].sort((a, b) => a.tableNumber - b.tableNumber);
      setTables(sorted);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được danh sách bàn.'));
      setTables([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTables();
  }, []);

  // 3. Create Table Handler
  const handleAddTable = async (values: { tableNumber: number; capacity: number }) => {
    try {
      // Check if tableNumber already exists
      if (tables.some(t => t.tableNumber === values.tableNumber)) {
        message.error(`Bàn số ${values.tableNumber} đã tồn tại!`);
        return;
      }

      const newTable = await tableService.createTable(values.tableNumber, values.capacity);
      setTables((prev) => [...prev, newTable].sort((a, b) => a.tableNumber - b.tableNumber));
      setIsAddModalOpen(false);
      form.resetFields();
      message.success('Thêm bàn mới thành công');
    } catch (error: any) {
      message.error(error.message || 'Thêm bàn thất bại');
    }
  };

  // 4. Update Status Handler
  const handleStatusChange = async (tableId: number, status: TableStatus) => {
    try {
      const updated = await tableService.updateTableStatus(tableId, status);
      setTables((prev) => prev.map((t) => (t.id === tableId ? updated : t)));
      message.success(`Đã chuyển trạng thái bàn sang ${status === 'AVAILABLE' ? 'Trống (AVAILABLE)' : 'Đang có khách (OCCUPIED)'}`);
    } catch (error: any) {
      message.error(error.message || 'Cập nhật trạng thái thất bại');
    }
  };


  // 5. Filter & Search logic
  const filteredTables = useMemo(() => {
    return tables.filter((table) => {
      const formattedId = `T-${table.tableNumber.toString().padStart(2, '0')}`;
      const zone = table.zone || 'Main Hall';
      
      const matchesSearch = 
        formattedId.toLowerCase().includes(searchText.toLowerCase()) ||
        zone.toLowerCase().includes(searchText.toLowerCase());

      const matchesStatus = 
        statusFilter === 'ALL' || 
        (statusFilter === 'AVAILABLE' && table.status === 'AVAILABLE') ||
        (statusFilter === 'OCCUPIED' && table.status === 'OCCUPIED');

      return matchesSearch && matchesStatus;
    });
  }, [tables, searchText, statusFilter]);

  // 6. Table columns configuration
  const columns = [
    {
      title: 'TABLE ID',
      dataIndex: 'tableNumber',
      key: 'tableNumber',
      render: (num: number) => (
        <span style={{ fontWeight: 600, color: '#2c3e50' }}>
          T-{num.toString().padStart(2, '0')}
        </span>
      ),
    },
    {
      title: 'CAPACITY (PAX)',
      dataIndex: 'capacity',
      key: 'capacity',
      render: (cap: number) => (
        <span style={{ color: '#4a5568' }}>{cap}</span>
      ),
    },
    {
      title: 'LOCATION/ZONE',
      dataIndex: 'zone',
      key: 'zone',
      render: (zone: string) => (
        <span style={{ color: '#4a5568' }}>{zone || 'Main Hall'}</span>
      ),
    },
    {
      title: 'STATUS',
      dataIndex: 'status',
      key: 'status',
      render: (status: TableStatus) => {
        const isAvailable = status === 'AVAILABLE';
        return (
          <Tag 
            color={isAvailable ? 'success' : 'error'}
            style={{
              borderRadius: 12,
              padding: '2px 12px',
              fontWeight: 500,
              fontSize: '12px',
              border: 'none',
              backgroundColor: isAvailable ? '#e6f7ff' : '#fff2f0',
              color: isAvailable ? '#1890ff' : '#ff4d4f'
            }}
          >
            {isAvailable ? 'Empty' : 'Occupied'}
          </Tag>
        );
      },
    },
    {
      title: 'ACTIONS',
      key: 'actions',
      render: (_: any, record: TableType) => {
        const items: MenuProps['items'] = [
          {
            key: 'available',
            label: 'Đánh dấu Trống (Empty)',
            icon: <CheckCircleOutlined style={{ color: '#52c41a' }} />,
            onClick: () => handleStatusChange(record.id, 'AVAILABLE')
          },
          {
            key: 'occupied',
            label: 'Đánh dấu Có Khách (Occupied)',
            icon: <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />,
            onClick: () => handleStatusChange(record.id, 'OCCUPIED')
          }
        ];

        return (
          <Space size="middle">
            <Tooltip title="Đổi trạng thái">
              <Dropdown menu={{ items }} trigger={['click']}>
                <Button 
                  type="text" 
                  icon={<EditOutlined style={{ color: '#718096' }} />} 
                  style={{ padding: 4 }}
                />
              </Dropdown>
            </Tooltip>
            <Tooltip title="Xóa bàn (Chỉ Manager)">
              <Button 
                type="text" 
                danger 
                icon={<DeleteOutlined style={{ color: '#a0aec0' }} />} 
                style={{ padding: 4 }}
                onClick={() => message.warning('Chức năng xóa bàn yêu cầu quyền Quản trị tối cao')}
              />
            </Tooltip>
          </Space>
        );
      },
    },
  ];

  return (
    <div style={{ padding: '0 0px 24px 0px' }}>
      <Card 
        bordered={false}
        style={{
          borderRadius: 8,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)',
          background: '#ffffff'
        }}
      >
        {/* Filter Toolbar */}
        <div style={{ 
          display: 'flex', 
          justifyContent: 'space-between', 
          alignItems: 'center', 
          marginBottom: 24, 
          flexWrap: 'wrap',
          gap: 16
        }}>
          <div style={{ display: 'flex', gap: 16, flex: 1, minWidth: 280, maxWidth: 600 }}>
            <Input
              placeholder="Search tables by ID or zone..."
              prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              style={{ borderRadius: 6, height: 38 }}
            />
            <Select
              defaultValue="ALL"
              value={statusFilter}
              onChange={(val) => setStatusFilter(val)}
              style={{ width: 160, height: 38 }}
              className="table-status-select"
            >
              <Option value="ALL">All Status</Option>
              <Option value="AVAILABLE">Empty (Available)</Option>
              <Option value="OCCUPIED">Occupied</Option>
            </Select>
          </div>
          
          <Button 
            type="primary" 
            icon={<PlusOutlined />}
            onClick={() => setIsAddModalOpen(true)}
            style={{ 
              backgroundColor: '#1890ff', 
              borderRadius: 6, 
              height: 38,
              fontWeight: 500,
              display: 'flex',
              alignItems: 'center'
            }}
          >
            Add New Table
          </Button>
        </div>

        {/* Tables list */}
        <Table 
          columns={columns} 
          dataSource={filteredTables} 
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showTotal: (total, range) => `Showing ${range[0]} to ${range[1]} of ${total} tables`,
            itemRender: (_, type, originalElement) => {
              if (type === 'prev') {
                return <Button size="small">Previous</Button>;
              }
              if (type === 'next') {
                return <Button size="small">Next</Button>;
              }
              return originalElement;
            }
          }}
          locale={{ emptyText: 'Không tìm thấy bàn ăn nào khớp với bộ lọc' }}
        />
      </Card>

      {/* Modal: Add New Table */}
      <Modal
        title="Thêm Bàn Ăn Mới"
        open={isAddModalOpen}
        onCancel={() => setIsAddModalOpen(false)}
        footer={null}
        destroyOnClose
        style={{ borderRadius: 12 }}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleAddTable}
          initialValues={{ capacity: 4 }}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="tableNumber"
            label="Số bàn"
            rules={[
              { required: true, message: 'Vui lòng nhập số bàn!' },
              { type: 'integer', min: 1, message: 'Số bàn phải là số nguyên dương lớn hơn 0!' }
            ]}
          >
            <InputNumber style={{ width: '100%' }} placeholder="Ví dụ: 11" />
          </Form.Item>

          <Form.Item
            name="capacity"
            label="Sức chứa tối đa (PAX)"
            rules={[
              { required: true, message: 'Vui lòng nhập sức chứa!' },
              { type: 'integer', min: 1, max: 20, message: 'Sức chứa hợp lệ từ 1 đến 20 khách!' }
            ]}
          >
            <InputNumber style={{ width: '100%' }} placeholder="Ví dụ: 4" />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setIsAddModalOpen(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Lưu lại</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>


    </div>
  );
};

export default TableManagementPage;
