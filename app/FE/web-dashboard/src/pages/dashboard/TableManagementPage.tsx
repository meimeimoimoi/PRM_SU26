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
  MenuProps,
  QRCode,
  Typography
} from 'antd';
import {
  SearchOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  QrcodeOutlined,
  CopyOutlined,
  SwapOutlined
} from '@ant-design/icons';
import { Table as TableType, TableStatus } from '@/types/table';
import { Location } from '@/types/location';
import { tableService } from '@/services/tableService';
import { locationService } from '@/services/locationService';
import { getErrorMessage } from '@/utils/apiError';

const { Option } = Select;
const CREATE_LOCATION_VALUE = '__create_new__';

const TableManagementPage: React.FC = () => {
  const [tables, setTables] = useState<TableType[]>([]);
  const [locations, setLocations] = useState<Location[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');

  // Modal state
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [isAddLocationModalOpen, setIsAddLocationModalOpen] = useState<boolean>(false);
  const [qrModalTable, setQrModalTable] = useState<TableType | null>(null);
  const [editModalTable, setEditModalTable] = useState<TableType | null>(null);
  const [form] = Form.useForm();
  const [locationForm] = Form.useForm();
  const [editForm] = Form.useForm();

  // 1. Fetch tables + locations from API
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

  const fetchLocations = async () => {
    try {
      const data = await locationService.getAll();
      setLocations(data || []);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được danh sách khu vực.'));
    }
  };

  useEffect(() => {
    fetchTables();
    fetchLocations();
  }, []);

  // 2. Create Table Handler
  const handleAddTable = async (values: { tableNumber: number; capacity: number; locationId?: number }) => {
    try {
      // Check if tableNumber already exists
      if (tables.some(t => t.tableNumber === values.tableNumber)) {
        message.error(`Bàn số ${values.tableNumber} đã tồn tại!`);
        return;
      }

      const newTable = await tableService.createTable(values.tableNumber, values.capacity, values.locationId);
      setTables((prev) => [...prev, newTable].sort((a, b) => a.tableNumber - b.tableNumber));
      setIsAddModalOpen(false);
      form.resetFields();
      message.success('Thêm bàn mới thành công');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Thêm bàn thất bại'));
    }
  };

  // Tạo location mới từ popup con trong modal Add Table — chọn luôn location vừa tạo.
  const handleCreateLocation = async (values: { name: string }) => {
    try {
      const newLocation = await locationService.create(values.name);
      setLocations((prev) => [...prev, newLocation]);
      form.setFieldsValue({ locationId: newLocation.id });
      setIsAddLocationModalOpen(false);
      locationForm.resetFields();
      message.success('Tạo khu vực mới thành công');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Tạo khu vực thất bại'));
    }
  };

  // 3. Update Status Handler
  const handleStatusChange = async (tableId: number, status: TableStatus) => {
    try {
      const updated = await tableService.updateTableStatus(tableId, status);
      setTables((prev) => prev.map((t) => (t.id === tableId ? updated : t)));
      message.success(`Đã chuyển trạng thái bàn sang ${status === 'AVAILABLE' ? 'Trống (AVAILABLE)' : 'Đang có khách (OCCUPIED)'}`);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Cập nhật trạng thái thất bại'));
    }
  };

  // 4. Edit basic info (Capacity + Location) — không cho sửa Số bàn vì QR đã in mã hóa theo số bàn.
  const openEditModal = (table: TableType) => {
    setEditModalTable(table);
    editForm.setFieldsValue({ capacity: table.capacity, locationId: table.locationId });
  };

  const handleEditTable = async (values: { capacity: number; locationId?: number }) => {
    if (!editModalTable) return;
    try {
      const updated = await tableService.updateTable(editModalTable.id, values);
      setTables((prev) => prev.map((t) => (t.id === editModalTable.id ? updated : t)));
      setEditModalTable(null);
      message.success('Cập nhật thông tin bàn thành công');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Cập nhật thông tin bàn thất bại'));
    }
  };

  // 5. Delete table — BE từ chối (422) nếu bàn đang OCCUPIED.
  const handleDeleteTable = (table: TableType) => {
    Modal.confirm({
      title: 'Xóa bàn ăn',
      content: `Bạn chắc chắn muốn xóa bàn T-${table.tableNumber.toString().padStart(2, '0')}? Mã QR đã in cho bàn này sẽ không còn dùng được nữa.`,
      okText: 'Xóa',
      okType: 'danger',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await tableService.deleteTable(table.id);
          setTables((prev) => prev.filter((t) => t.id !== table.id));
          message.success('Đã xóa bàn thành công');
        } catch (error: any) {
          message.error(getErrorMessage(error, 'Xóa bàn thất bại'));
        }
      }
    });
  };

  // 6. Filter & Search logic
  const filteredTables = useMemo(() => {
    return tables.filter((table) => {
      const formattedId = `T-${table.tableNumber.toString().padStart(2, '0')}`;
      const locationName = table.locationName || '';

      const matchesSearch =
        formattedId.toLowerCase().includes(searchText.toLowerCase()) ||
        locationName.toLowerCase().includes(searchText.toLowerCase());

      const matchesStatus =
        statusFilter === 'ALL' ||
        (statusFilter === 'AVAILABLE' && table.status === 'AVAILABLE') ||
        (statusFilter === 'OCCUPIED' && table.status === 'OCCUPIED');

      return matchesSearch && matchesStatus;
    });
  }, [tables, searchText, statusFilter]);

  // 7. Table columns configuration
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
      title: 'LOCATION',
      dataIndex: 'locationName',
      key: 'locationName',
      render: (locationName: string) => (
        <span style={{ color: '#4a5568' }}>{locationName || 'Chưa gán khu vực'}</span>
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
            <Tooltip title="Xem mã QR để dán lên bàn">
              <Button
                type="text"
                icon={<QrcodeOutlined style={{ color: '#1890ff' }} />}
                style={{ padding: 4 }}
                onClick={() => setQrModalTable(record)}
              />
            </Tooltip>
            <Tooltip title="Sửa thông tin (Sức chứa, Khu vực)">
              <Button
                type="text"
                icon={<EditOutlined style={{ color: '#718096' }} />}
                style={{ padding: 4 }}
                onClick={() => openEditModal(record)}
              />
            </Tooltip>
            <Tooltip title="Đổi trạng thái">
              <Dropdown menu={{ items }} trigger={['click']}>
                <Button
                  type="text"
                  icon={<SwapOutlined style={{ color: '#718096' }} />}
                  style={{ padding: 4 }}
                />
              </Dropdown>
            </Tooltip>
            <Tooltip title="Xóa bàn">
              <Button
                type="text"
                danger
                icon={<DeleteOutlined style={{ color: '#a0aec0' }} />}
                style={{ padding: 4 }}
                onClick={() => handleDeleteTable(record)}
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
              placeholder="Search tables by ID or location..."
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

          <Form.Item name="locationId" label="Khu vực">
            <Select
              placeholder="Chọn khu vực (tùy chọn)"
              allowClear
              onChange={(val) => {
                if (val === CREATE_LOCATION_VALUE) {
                  form.setFieldsValue({ locationId: undefined });
                  setIsAddLocationModalOpen(true);
                }
              }}
            >
              {locations.map((l) => (
                <Option key={l.id} value={l.id}>{l.name}</Option>
              ))}
              <Option key={CREATE_LOCATION_VALUE} value={CREATE_LOCATION_VALUE}>
                + Tạo khu vực mới...
              </Option>
            </Select>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setIsAddModalOpen(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Lưu lại</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal con: Tạo khu vực mới (mở từ trong modal Add Table khi chưa có lựa chọn phù hợp) */}
      <Modal
        title="Tạo Khu Vực Mới"
        open={isAddLocationModalOpen}
        onCancel={() => setIsAddLocationModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={locationForm} layout="vertical" onFinish={handleCreateLocation} style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Tên khu vực"
            rules={[{ required: true, message: 'Vui lòng nhập tên khu vực!' }]}
          >
            <Input placeholder="Ví dụ: Tầng 2, Sân vườn, Phòng VIP" />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setIsAddLocationModalOpen(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Tạo</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal: Sửa thông tin bàn — chỉ Sức chứa + Khu vực, khóa Số bàn */}
      <Modal
        title={editModalTable ? `Sửa Thông Tin - Bàn T-${editModalTable.tableNumber.toString().padStart(2, '0')}` : 'Sửa Thông Tin Bàn'}
        open={editModalTable !== null}
        onCancel={() => setEditModalTable(null)}
        footer={null}
        destroyOnClose
        style={{ borderRadius: 12 }}
      >
        <Form form={editForm} layout="vertical" onFinish={handleEditTable} style={{ marginTop: 16 }}>
          <Form.Item label="Số bàn">
            <InputNumber style={{ width: '100%' }} value={editModalTable?.tableNumber} disabled />
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Không thể đổi số bàn vì mã QR đã in mã hóa theo số này. Muốn đổi số bàn, hãy xóa và tạo bàn mới.
            </Typography.Text>
          </Form.Item>

          <Form.Item
            name="capacity"
            label="Sức chứa tối đa (PAX)"
            rules={[
              { required: true, message: 'Vui lòng nhập sức chứa!' },
              { type: 'integer', min: 1, max: 20, message: 'Sức chứa hợp lệ từ 1 đến 20 khách!' }
            ]}
          >
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="locationId" label="Khu vực">
            <Select placeholder="Chọn khu vực" allowClear>
              {locations.map((l) => (
                <Option key={l.id} value={l.id}>{l.name}</Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setEditModalTable(null)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Cập nhật</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal: QR bàn — link dán trên bàn để khách quét bằng camera điện thoại (không cần cài app) */}
      <Modal
        title={qrModalTable ? `Mã QR - Bàn T-${qrModalTable.tableNumber.toString().padStart(2, '0')}` : 'Mã QR'}
        open={qrModalTable !== null}
        onCancel={() => setQrModalTable(null)}
        footer={[
          <Button key="close" onClick={() => setQrModalTable(null)}>Đóng</Button>,
        ]}
      >
        {qrModalTable?.qrCode ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16, padding: '8px 0' }}>
            <QRCode value={qrModalTable.qrCode} size={220} />
            <Space.Compact style={{ width: '100%' }}>
              <Input value={qrModalTable.qrCode} readOnly />
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  navigator.clipboard.writeText(qrModalTable.qrCode!);
                  message.success('Đã copy link!');
                }}
              />
            </Space.Compact>
            <Typography.Text type="secondary" style={{ fontSize: 12, textAlign: 'center' }}>
              In mã này và dán lên bàn. Khách quét bằng camera điện thoại thường sẽ mở thẳng trang đặt món, không cần cài app.
            </Typography.Text>
          </div>
        ) : (
          <Typography.Text type="warning">
            Bàn này chưa có mã QR (có thể được tạo trước khi tính năng này ra mắt). Vui lòng liên hệ dev để tạo lại.
          </Typography.Text>
        )}
      </Modal>
    </div>
  );
};

export default TableManagementPage;
