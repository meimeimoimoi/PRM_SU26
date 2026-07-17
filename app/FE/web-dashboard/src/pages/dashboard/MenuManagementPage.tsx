import React, { useEffect, useState, useMemo } from 'react';
import { useSelector } from 'react-redux';
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
  Switch,
  Upload
} from 'antd';
import type { FormInstance } from 'antd';
import {
  SearchOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  UploadOutlined
} from '@ant-design/icons';
import { MenuItemResponse, MenuCategoryResponse } from '@/types/menu';
import { menuService } from '@/services/menuService';
import { categoryService } from '@/services/categoryService';
import { selectCurrentUser } from '@/store/slices/authSlice';
import { getErrorMessage } from '@/utils/apiError';

const { Option } = Select;

// Dùng chung cho modal Add/Edit: dán link ảnh trực tiếp HOẶC tải file lên (kết quả upload
// tự động điền vào cùng field imageUrl — luồng save phía sau không đổi gì).
const MenuImageField: React.FC<{ form: FormInstance }> = ({ form }) => {
  const [uploading, setUploading] = useState(false);
  const imageUrl = Form.useWatch('imageUrl', form);

  const handleBeforeUpload = async (file: File) => {
    setUploading(true);
    try {
      const url = await menuService.uploadImage(file);
      form.setFieldsValue({ imageUrl: url });
      message.success('Tải ảnh lên thành công!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Tải ảnh lên thất bại.'));
    } finally {
      setUploading(false);
    }
    return false; // chặn Upload tự gửi request riêng — đã tự gọi menuService.uploadImage ở trên
  };

  return (
    <>
      <Form.Item name="imageUrl" label="Ảnh món ăn (dán link hoặc tải ảnh lên)">
        <Input placeholder="Dán liên kết ảnh trực tuyến hoặc tải ảnh lên bên dưới" />
      </Form.Item>
      <Form.Item label=" " colon={false} style={{ marginTop: -12 }}>
        <Space align="center">
          <Upload showUploadList={false} beforeUpload={handleBeforeUpload} accept="image/*">
            <Button icon={<UploadOutlined />} loading={uploading}>Tải ảnh lên</Button>
          </Upload>
          {imageUrl && (
            <img
              src={imageUrl}
              alt="preview"
              style={{ width: 48, height: 48, borderRadius: 8, objectFit: 'cover', border: '1px solid #e5e7eb' }}
            />
          )}
        </Space>
      </Form.Item>
    </>
  );
};

const MenuManagementPage: React.FC = () => {
  const currentUser = useSelector(selectCurrentUser);
  const isManager = currentUser?.role === 'MANAGER';

  const [menuItems, setMenuItems] = useState<MenuItemResponse[]>([]);
  const [categories, setCategories] = useState<MenuCategoryResponse[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [categoryFilter, setCategoryFilter] = useState<string>('ALL');

  // Modals
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState<boolean>(false);
  const [selectedItem, setSelectedItem] = useState<MenuItemResponse | null>(null);
  
  const [addForm] = Form.useForm();
  const [editForm] = Form.useForm();

  // 1. Fetch menu items
  const fetchMenu = async () => {
    setLoading(true);
    try {
      const data = await menuService.getAllMenuItems();
      setMenuItems(data || []);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được thực đơn.'));
      setMenuItems([]);
    } finally {
      setLoading(false);
    }
  };

  // Danh mục thật từ BE (GET /api/v1/menu-categories) — thay cho hardcode Food/Drink.
  const fetchCategories = async () => {
    try {
      const data = await categoryService.getAll();
      setCategories(data || []);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được danh mục món ăn.'));
    }
  };

  useEffect(() => {
    fetchMenu();
    fetchCategories();
  }, []);

  const categoryName = (categoryId: number) =>
    categories.find((c) => c.id === categoryId)?.name || 'Chưa phân loại';

  // 2. Add Item Handler
  const handleAddItem = async (values: any) => {
    try {
      const requestData = {
        name: values.name,
        description: values.description,
        price: values.price,
        imageUrl: values.imageUrl || 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=150',
        categoryId: values.categoryId
      };

      // BE Create chỉ trả về { id, name, createdAt } (MenuItemCreatedResponse), không phải
      // MenuItemResponse đầy đủ — tự dựng lại item hiển thị từ chính requestData đã gửi lên,
      // không lấy price/imageUrl/isAvailable từ response (sẽ luôn undefined → NaN/ảnh sai).
      const newItem = await menuService.createMenuItem(requestData);
      setMenuItems((prev) => [...prev, {
        id: newItem.id,
        name: requestData.name,
        description: requestData.description,
        price: requestData.price,
        imageUrl: requestData.imageUrl,
        categoryId: requestData.categoryId,
        categoryName: categoryName(requestData.categoryId),
        isAvailable: true,
        averageRating: 0
      }]);
      setIsAddModalOpen(false);
      addForm.resetFields();
      message.success('Thêm món ăn mới thành công!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Thêm món ăn thất bại.'));
    }
  };

  // 3. Edit Item Handler
  const handleEditItem = async (values: any) => {
    if (!selectedItem) return;
    try {
      const requestData = {
        name: values.name,
        description: values.description,
        price: values.price,
        imageUrl: values.imageUrl || selectedItem.imageUrl,
        categoryId: values.categoryId,
        isAvailable: values.isAvailable
      };

      // PATCH chỉ trả về { id, name, isAvailable, updatedAt } — merge vào item cũ
      // thay vì thay thế toàn bộ, để không mất price/description/imageUrl/categoryId.
      await menuService.updateMenuItem(selectedItem.id, requestData);
      setMenuItems((prev) => prev.map((item) => item.id === selectedItem.id ? {
        ...item,
        name: values.name,
        description: values.description,
        price: values.price,
        imageUrl: requestData.imageUrl,
        categoryId: requestData.categoryId,
        categoryName: categoryName(requestData.categoryId),
        isAvailable: values.isAvailable
      } : item));
      setIsEditModalOpen(false);
      setSelectedItem(null);
      message.success('Cập nhật món ăn thành công!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Cập nhật món ăn thất bại.'));
    }
  };

  // 4. Toggle Availability Handler
  const handleToggleAvailability = async (id: number, nextAvailable: boolean) => {
    try {
      await menuService.toggleAvailability(id, nextAvailable);
      setMenuItems((prev) => prev.map((item) => item.id === id ? { ...item, isAvailable: nextAvailable } : item));
      message.success('Đã cập nhật trạng thái khả dụng!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Cập nhật trạng thái thất bại.'));
    }
  };

  // 5. Delete Item Handler
  const handleDeleteItem = async (id: number) => {
    try {
      await menuService.deleteMenuItem(id);
      setMenuItems((prev) => prev.filter((item) => item.id !== id));
      message.success('Đã xóa món ăn khỏi thực đơn!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Xóa món ăn thất bại.'));
    }
  };

  // Open Edit Modal
  const openEditModal = (item: MenuItemResponse) => {
    setSelectedItem(item);
    editForm.setFieldsValue({
      name: item.name,
      description: item.description,
      price: item.price,
      categoryId: item.categoryId,
      isAvailable: item.isAvailable,
      imageUrl: item.imageUrl
    });
    setIsEditModalOpen(true);
  };

  // Search & Filter Filtered Items
  const filteredItems = useMemo(() => {
    return menuItems.filter((item) => {
      const matchesSearch = 
        item.name.toLowerCase().includes(searchText.toLowerCase()) ||
        (item.description && item.description.toLowerCase().includes(searchText.toLowerCase()));
      
      const matchesCategory =
        categoryFilter === 'ALL' ||
        item.categoryId?.toString() === categoryFilter;

      return matchesSearch && matchesCategory;
    });
  }, [menuItems, searchText, categoryFilter]);

  // Format Currency
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN').format(price) + 'đ';
  };

  // Table Columns config
  const columns = [
    {
      title: 'ITEM',
      dataIndex: 'name',
      key: 'item',
      render: (_name: string, record: MenuItemResponse) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <img 
            src={record.imageUrl || 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=150'} 
            alt={record.name}
            style={{ width: 44, height: 44, borderRadius: 8, objectFit: 'cover' }}
          />
          <div>
            <div style={{ fontWeight: 600, color: '#1a202c', fontSize: '14px' }}>{record.name}</div>
            <div style={{ fontSize: '12px', color: '#718096' }}>{record.description || 'No description'}</div>
          </div>
        </div>
      ),
    },
    {
      title: 'CATEGORY',
      dataIndex: 'categoryName',
      key: 'category',
      render: (catName: string) => {
        const isFood = catName === 'Food';
        return (
          <Tag 
            color={isFood ? 'warning' : 'processing'}
            style={{
              borderRadius: 12,
              padding: '2px 12px',
              fontWeight: 500,
              border: 'none',
              backgroundColor: isFood ? '#ffe7ba' : '#e6f7ff',
              color: isFood ? '#d46b08' : '#1890ff'
            }}
          >
            {catName || 'Food'}
          </Tag>
        );
      },
    },
    {
      title: 'PRICE (VND)',
      dataIndex: 'price',
      key: 'price',
      render: (price: number) => (
        <span style={{ fontWeight: 600, color: '#2d3748' }}>{formatPrice(price)}</span>
      ),
    },
    {
      title: 'STATUS',
      dataIndex: 'isAvailable',
      key: 'status',
      render: (isAvailable: boolean) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span 
            style={{ 
              width: 8, 
              height: 8, 
              borderRadius: '50%', 
              backgroundColor: isAvailable ? '#52c41a' : '#f5222d',
              display: 'inline-block'
            }} 
          />
          <span style={{ color: isAvailable ? '#52c41a' : '#f5222d', fontSize: '13px', fontWeight: 500 }}>
            {isAvailable ? 'Available' : 'Out of Stock'}
          </span>
        </div>
      ),
    },
    // CRUD/toggle chỉ dành cho MANAGER — theo phân quyền: STAFF chỉ xử lý đơn, không quản lý menu.
    ...(isManager
      ? [
          {
            title: 'ACTIONS',
            key: 'actions',
            render: (_: any, record: MenuItemResponse) => (
              <Space size="middle">
                <Tooltip title="Chỉnh sửa món">
                  <Button
                    type="text"
                    icon={<EditOutlined style={{ color: '#1890ff' }} />}
                    onClick={() => openEditModal(record)}
                  />
                </Tooltip>
                <Tooltip title="Đổi trạng thái khả dụng">
                  <Switch
                    size="small"
                    checked={record.isAvailable}
                    onChange={(checked) => handleToggleAvailability(record.id, checked)}
                  />
                </Tooltip>
                <Tooltip title="Xóa món">
                  <Button
                    type="text"
                    danger
                    icon={<DeleteOutlined />}
                    onClick={() => handleDeleteItem(record.id)}
                  />
                </Tooltip>
              </Space>
            ),
          },
        ]
      : []),
  ];

  return (
    <div className="menu-management-container">
      {/* Title Header */}
      <div style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2 style={{ margin: 0, fontSize: '28px', fontWeight: 700, color: '#1a202c' }}>Menu Management</h2>
          <p style={{ margin: 0, color: '#718096', fontSize: '14px' }}>Manage your restaurant's dishes, drinks, and availability.</p>
        </div>
        {isManager && (
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
            Add New Item
          </Button>
        )}
      </div>

      {/* Filter toolbar */}
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
            placeholder="Search menu items..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ borderRadius: 6, height: 38, maxWidth: 400 }}
          />

          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#718096', fontWeight: 500, fontSize: '13px' }}>Category:</span>
            <Select
              defaultValue="ALL"
              value={categoryFilter}
              onChange={(val) => setCategoryFilter(val)}
              style={{ width: 180, height: 38 }}
            >
              <Option value="ALL">All Categories</Option>
              {categories.map((c) => (
                <Option key={c.id} value={c.id.toString()}>{c.name}</Option>
              ))}
            </Select>
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card
        bordered={false}
        style={{
          borderRadius: 8,
          boxShadow: '0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px 0 rgba(0,0,0,0.06)'
        }}
      >
        <Table 
          columns={columns} 
          dataSource={filteredItems} 
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 5,
            showTotal: (total, range) => `Showing ${range[0]} to ${range[1]} of ${total} items`
          }}
          locale={{ emptyText: 'Không tìm thấy món ăn nào' }}
        />
      </Card>

      {/* Modal: Add Item */}
      <Modal
        title="Thêm Món Ăn Mới"
        open={isAddModalOpen}
        onCancel={() => setIsAddModalOpen(false)}
        footer={null}
        destroyOnClose
        style={{ borderRadius: 12 }}
      >
        <Form
          form={addForm}
          layout="vertical"
          onFinish={handleAddItem}
          initialValues={{ price: 10000, categoryId: categories[0]?.id }}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="name"
            label="Tên món ăn"
            rules={[{ required: true, message: 'Vui lòng nhập tên món!' }]}
          >
            <Input placeholder="Ví dụ: Phở Bò Đặc Biệt" />
          </Form.Item>

          <Form.Item
            name="description"
            label="Mô tả món ăn (Tiếng Anh / Phụ đề)"
          >
            <Input placeholder="Ví dụ: Special Beef Noodle Soup" />
          </Form.Item>

          <Form.Item
            name="price"
            label="Đơn giá (VND)"
            rules={[{ required: true, message: 'Vui lòng nhập đơn giá!' }]}
          >
            <InputNumber style={{ width: '100%' }} min={1000} step={5000} formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
          </Form.Item>

          <Form.Item
            name="categoryId"
            label="Danh mục"
            rules={[{ required: true, message: 'Vui lòng chọn danh mục!' }]}
          >
            <Select placeholder="Chọn danh mục">
              {categories.map((c) => (
                <Option key={c.id} value={c.id}>{c.name}</Option>
              ))}
            </Select>
          </Form.Item>

          <MenuImageField form={addForm} />

          <Form.Item style={{ marginBottom: 0, marginTop: 24, textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setIsAddModalOpen(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">Lưu lại</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal: Edit Item */}
      <Modal
        title="Chỉnh Sửa Món Ăn"
        open={isEditModalOpen}
        onCancel={() => setIsEditModalOpen(false)}
        footer={null}
        destroyOnClose
        style={{ borderRadius: 12 }}
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={handleEditItem}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="name"
            label="Tên món ăn"
            rules={[{ required: true, message: 'Vui lòng nhập tên món!' }]}
          >
            <Input placeholder="Ví dụ: Phở Bò Đặc Biệt" />
          </Form.Item>

          <Form.Item
            name="description"
            label="Mô tả món ăn"
          >
            <Input placeholder="Ví dụ: Special Beef Noodle Soup" />
          </Form.Item>

          <Form.Item
            name="price"
            label="Đơn giá (VND)"
            rules={[{ required: true, message: 'Vui lòng nhập đơn giá!' }]}
          >
            <InputNumber style={{ width: '100%' }} min={1000} step={5000} formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
          </Form.Item>

          <Form.Item
            name="categoryId"
            label="Danh mục"
            rules={[{ required: true, message: 'Vui lòng chọn danh mục!' }]}
          >
            <Select placeholder="Chọn danh mục">
              {categories.map((c) => (
                <Option key={c.id} value={c.id}>{c.name}</Option>
              ))}
            </Select>
          </Form.Item>

          <MenuImageField form={editForm} />

          <Form.Item
            name="isAvailable"
            label="Còn hàng"
            valuePropName="checked"
          >
            <Switch />
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

export default MenuManagementPage;
