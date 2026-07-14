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
  message,
  Tooltip,
  Avatar
} from 'antd';
import {
  SearchOutlined,
  PlusOutlined,
  EditOutlined,
  StopOutlined
} from '@ant-design/icons';

import { StaffMember, StaffRole } from '@/types/staff';
import { staffService } from '@/services/staffService';
import { getErrorMessage } from '@/utils/apiError';

const { Option } = Select;

const ROLE_LABEL: Record<StaffRole, string> = {
  MANAGER: 'Quản lý',
  STAFF: 'Nhân viên',
  CHEF: 'Đầu bếp'
};

const ROLE_COLOR: Record<StaffRole, { bg: string; text: string }> = {
  MANAGER: { bg: '#f0f5ff', text: '#2f54eb' },
  STAFF: { bg: '#e6f7ff', text: '#1890ff' },
  CHEF: { bg: '#fff7e6', text: '#d46b08' }
};

const StaffManagementPage: React.FC = () => {
  const [staffList, setStaffList] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<string>('ALL');

  // Modals
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState<boolean>(false);
  const [selectedStaff, setSelectedStaff] = useState<StaffMember | null>(null);

  const [addForm] = Form.useForm();
  const [editForm] = Form.useForm();

  const fetchStaff = async () => {
    setLoading(true);
    try {
      const data = await staffService.getAll();
      setStaffList(data);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được danh sách nhân viên.'));
      setStaffList([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStaff();
  }, []);

  // Search & Filter
  const filteredStaff = useMemo(() => {
    return staffList.filter((s) => {
      const matchesSearch =
        s.fullName.toLowerCase().includes(searchText.toLowerCase()) ||
        s.email.toLowerCase().includes(searchText.toLowerCase());

      const matchesRole = roleFilter === 'ALL' || s.role === roleFilter;

      return matchesSearch && matchesRole;
    });
  }, [staffList, searchText, roleFilter]);

  // Add staff member
  const handleAddStaff = async (values: any) => {
    try {
      const newStaff = await staffService.create({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        role: values.role
      });
      setStaffList((prev) => [...prev, newStaff]);
      setIsAddModalOpen(false);
      addForm.resetFields();
      message.success(`Đã thêm nhân viên ${values.fullName} thành công!`);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Thêm nhân viên thất bại.'));
    }
  };

  // Edit staff member
  const handleEditStaff = async (values: any) => {
    if (!selectedStaff) return;
    try {
      const updated = await staffService.update(selectedStaff.id, {
        fullName: values.fullName,
        email: values.email,
        role: values.role,
        isActive: values.isActive
      });
      setStaffList((prev) => prev.map((s) => (s.id === selectedStaff.id ? updated : s)));
      setIsEditModalOpen(false);
      setSelectedStaff(null);
      message.success('Cập nhật thông tin nhân viên thành công!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Cập nhật nhân viên thất bại.'));
    }
  };

  // Deactivate staff member (BE chỉ soft-deactivate, không xóa vĩnh viễn — và Manager
  // không thể tự vô hiệu hóa chính mình, BE sẽ trả lỗi STAFF_CANNOT_DEACTIVATE_SELF).
  const handleDeactivateStaff = (id: number, name: string) => {
    Modal.confirm({
      title: 'Vô hiệu hóa nhân viên',
      content: `Bạn chắc chắn muốn vô hiệu hóa tài khoản của ${name}? Nhân viên sẽ không thể đăng nhập nhưng dữ liệu vẫn được giữ lại.`,
      okText: 'Vô hiệu hóa',
      okType: 'danger',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await staffService.deactivate(id);
          setStaffList((prev) => prev.map((s) => (s.id === id ? { ...s, isActive: false } : s)));
          message.success('Đã vô hiệu hóa nhân viên thành công!');
        } catch (error: any) {
          message.error(getErrorMessage(error, 'Vô hiệu hóa nhân viên thất bại.'));
        }
      }
    });
  };

  // Get Initials for Avatar
  const getInitials = (name: string) => {
    return name.split(' ').map((n) => n[0]).join('').substring(0, 2).toUpperCase();
  };

  // Columns Configuration
  const columns = [
    {
      title: 'STAFF ID',
      dataIndex: 'id',
      key: 'id',
      render: (id: number) => <span style={{ fontWeight: 600, color: '#4a5568' }}>#{id}</span>,
    },
    {
      title: 'FULL NAME',
      dataIndex: 'fullName',
      key: 'fullName',
      render: (fullName: string) => {
        const initials = getInitials(fullName);
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Avatar
              style={{
                backgroundColor: '#e6f7ff',
                color: '#1890ff',
                fontWeight: 600,
                border: '1px solid #1890ff'
              }}
            >
              {initials}
            </Avatar>
            <span style={{ fontWeight: 500, color: '#1a202c' }}>{fullName}</span>
          </div>
        );
      },
    },
    {
      title: 'EMAIL',
      dataIndex: 'email',
      key: 'email',
      render: (email: string) => <span style={{ color: '#4a5568', fontSize: '13px' }}>{email}</span>,
    },
    {
      title: 'ROLE',
      dataIndex: 'role',
      key: 'role',
      render: (role: StaffRole) => {
        const { bg, text } = ROLE_COLOR[role] || ROLE_COLOR.STAFF;
        return (
          <Tag
            style={{
              borderRadius: 12,
              padding: '2px 12px',
              fontWeight: 500,
              backgroundColor: bg,
              color: text,
              border: 'none'
            }}
          >
            {ROLE_LABEL[role] || role}
          </Tag>
        );
      },
    },
    {
      title: 'STATUS',
      dataIndex: 'isActive',
      key: 'status',
      render: (isActive: boolean) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span
            style={{
              width: 8,
              height: 8,
              borderRadius: '50%',
              backgroundColor: isActive ? '#1890ff' : '#bfbfbf',
              display: 'inline-block'
            }}
          />
          <span style={{ color: isActive ? '#1890ff' : '#718096', fontSize: '13px', fontWeight: 500 }}>
            {isActive ? 'Active' : 'Inactive'}
          </span>
        </div>
      ),
    },
    {
      title: 'ACTIONS',
      key: 'actions',
      render: (_: any, record: StaffMember) => (
        <Space size="middle">
          <Tooltip title="Chỉnh sửa nhân viên">
            <Button
              type="text"
              icon={<EditOutlined style={{ color: '#4a5568' }} />}
              onClick={() => {
                setSelectedStaff(record);
                editForm.setFieldsValue(record);
                setIsEditModalOpen(true);
              }}
            />
          </Tooltip>
          <Tooltip title="Vô hiệu hóa nhân viên">
            <Button
              type="text"
              danger
              disabled={!record.isActive}
              icon={<StopOutlined />}
              onClick={() => handleDeactivateStaff(record.id, record.fullName)}
            />
          </Tooltip>
        </Space>
      ),
    },
  ];

  return (
    <div className="staff-management-container">
      {/* Page Header */}
      <div style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2 style={{ margin: 0, fontSize: '28px', fontWeight: 700, color: '#1a202c' }}>Staff Management</h2>
          <p style={{ margin: 0, color: '#718096', fontSize: '14px' }}>Manage employee access, roles, and details.</p>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setIsAddModalOpen(true)}
          style={{
            backgroundColor: '#1890ff',
            borderRadius: 6,
            height: 38,
            fontWeight: 500
          }}
        >
          Add Staff Member
        </Button>
      </div>

      {/* Filter Toolbar */}
      <Card
        bordered={false}
        style={{
          borderRadius: 8,
          marginBottom: 20,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)'
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 16, alignItems: 'center', flexWrap: 'wrap' }}>
          <Input
            placeholder="Search staff by name, email..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ borderRadius: 6, height: 38, maxWidth: 400 }}
          />

          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#718096', fontWeight: 500, fontSize: '13px' }}>Role:</span>
            <Select
              defaultValue="ALL"
              value={roleFilter}
              onChange={(val) => setRoleFilter(val)}
              style={{ width: 150, height: 38 }}
            >
              <Option value="ALL">All Roles</Option>
              <Option value="MANAGER">Quản lý</Option>
              <Option value="STAFF">Nhân viên</Option>
              <Option value="CHEF">Đầu bếp</Option>
            </Select>
          </div>
        </div>
      </Card>

      {/* Staff Table */}
      <Card
        bordered={false}
        style={{
          borderRadius: 8,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)'
        }}
      >
        <Table
          columns={columns}
          dataSource={filteredStaff}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 6,
            showTotal: (total, range) => `Showing ${range[0]} to ${range[1]} of ${total} results`
          }}
          locale={{ emptyText: 'Không có nhân viên nào' }}
        />
      </Card>

      {/* Modal: Add Staff Member */}
      <Modal
        title="Thêm Nhân Viên Mới"
        open={isAddModalOpen}
        onCancel={() => setIsAddModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form
          form={addForm}
          layout="vertical"
          onFinish={handleAddStaff}
          initialValues={{ role: 'STAFF' }}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="fullName"
            label="Họ và Tên"
            rules={[{ required: true, message: 'Vui lòng nhập họ tên nhân viên!' }]}
          >
            <Input placeholder="Ví dụ: Nguyễn Văn A" />
          </Form.Item>

          <Form.Item
            name="email"
            label="Email đăng nhập"
            rules={[
              { required: true, message: 'Vui lòng nhập email!' },
              { type: 'email', message: 'Vui lòng nhập đúng định dạng email!' }
            ]}
          >
            <Input placeholder="Ví dụ: nhanvien@smartdine.com" />
          </Form.Item>

          <Form.Item
            name="password"
            label="Mật khẩu"
            rules={[
              { required: true, message: 'Vui lòng nhập mật khẩu!' },
              { min: 6, message: 'Mật khẩu phải có ít nhất 6 ký tự!' }
            ]}
          >
            <Input.Password placeholder="Đặt mật khẩu ban đầu cho nhân viên" />
          </Form.Item>

          <Form.Item
            name="role"
            label="Vai trò / Vị trí"
            rules={[{ required: true }]}
          >
            <Select>
              <Option value="MANAGER">Quản lý</Option>
              <Option value="STAFF">Nhân viên</Option>
              <Option value="CHEF">Đầu bếp</Option>
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

      {/* Modal: Edit Staff Member */}
      <Modal
        title="Chỉnh Sửa Thông Tin Nhân Viên"
        open={isEditModalOpen}
        onCancel={() => setIsEditModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={handleEditStaff}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="fullName"
            label="Họ và Tên"
            rules={[{ required: true, message: 'Vui lòng nhập họ tên nhân viên!' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="email"
            label="Email đăng nhập"
            rules={[
              { required: true, message: 'Vui lòng nhập email!' },
              { type: 'email', message: 'Vui lòng nhập đúng định dạng email!' }
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="role"
            label="Vai trò"
            rules={[{ required: true }]}
          >
            <Select>
              <Option value="MANAGER">Quản lý</Option>
              <Option value="STAFF">Nhân viên</Option>
              <Option value="CHEF">Đầu bếp</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="isActive"
            label="Trạng thái hoạt động"
          >
            <Select>
              <Option value={true}>Active (Đang làm việc)</Option>
              <Option value={false}>Inactive (Nghỉ việc/Khóa)</Option>
            </Select>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setIsEditModalOpen(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Cập nhật</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default StaffManagementPage;
