import React, { useState, useEffect } from 'react';
import {
  Card,
  Tabs,
  Form,
  Input,
  InputNumber,
  Button,
  message,
  Row,
  Col,
  Typography,
  Spin,
  Empty
} from 'antd';
import { settingsService } from '@/services/settingsService';
import { getErrorMessage } from '@/utils/apiError';

const { Title } = Typography;

const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

const SettingsPage: React.FC = () => {
  const [form] = Form.useForm();
  const [activeTab, setActiveTab] = useState<string>('general');
  const [loading, setLoading] = useState<boolean>(true);
  const [saving, setSaving] = useState<boolean>(false);

  const fetchSettings = async () => {
    setLoading(true);
    try {
      const settings = await settingsService.getSettings();
      form.setFieldsValue(settings);
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Không tải được cấu hình nhà hàng.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Save changes handler — PATCH /api/v1/settings, chỉ gửi field trong form (partial update).
  const handleSaveChanges = async (values: any) => {
    setSaving(true);
    try {
      const updated = await settingsService.updateSettings(values);
      form.setFieldsValue(updated);
      message.success('Đã lưu cấu hình nhà hàng thành công!');
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Lưu cấu hình thất bại.'));
    } finally {
      setSaving(false);
    }
  };

  const tabItems = [
    {
      key: 'general',
      label: 'General Info',
      children: (
        <Card
          title={<span style={{ fontSize: '18px', fontWeight: 600 }}>Restaurant Details</span>}
          bordered={false}
          style={{ borderRadius: 8 }}
        >
          {loading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 40 }}>
              <Spin />
            </div>
          ) : (
            <Form form={form} layout="vertical" onFinish={handleSaveChanges} requiredMark={false}>
              <Row gutter={24}>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="restaurantName"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Tên nhà hàng</span>}
                    rules={[{ required: true, message: 'Vui lòng nhập tên nhà hàng' }]}
                  >
                    <Input style={{ height: 38, borderRadius: 6 }} />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="phone"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Số điện thoại liên hệ</span>}
                  >
                    <Input style={{ height: 38, borderRadius: 6 }} />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item
                name="address"
                label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Địa chỉ</span>}
              >
                <Input style={{ height: 38, borderRadius: 6 }} />
              </Form.Item>

              <Row gutter={24}>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="openingTime"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Giờ mở cửa</span>}
                    rules={[{ pattern: TIME_PATTERN, message: 'Định dạng HH:mm, ví dụ 08:00' }]}
                  >
                    <Input placeholder="08:00" style={{ height: 38, borderRadius: 6 }} />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="closingTime"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Giờ đóng cửa</span>}
                    rules={[{ pattern: TIME_PATTERN, message: 'Định dạng HH:mm, ví dụ 22:00' }]}
                  >
                    <Input placeholder="22:00" style={{ height: 38, borderRadius: 6 }} />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={24}>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="taxRate"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Thuế VAT (%)</span>}
                    rules={[{ type: 'number', min: 0, max: 100, message: 'Giá trị hợp lệ từ 0 đến 100' }]}
                  >
                    <InputNumber style={{ width: '100%', height: 38 }} min={0} max={100} step={1} addonAfter="%" />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={12}>
                  <Form.Item
                    name="serviceChargeRate"
                    label={<span style={{ fontWeight: 500, color: '#4a5568' }}>Phí dịch vụ (%)</span>}
                    rules={[{ type: 'number', min: 0, max: 100, message: 'Giá trị hợp lệ từ 0 đến 100' }]}
                  >
                    <InputNumber style={{ width: '100%', height: 38 }} min={0} max={100} step={1} addonAfter="%" />
                  </Form.Item>
                </Col>
              </Row>

              <div style={{ marginTop: 24, display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
                <Button
                  onClick={fetchSettings}
                  disabled={saving}
                  style={{ borderRadius: 6, height: 38, padding: '0 20px', fontWeight: 500 }}
                >
                  Hủy thay đổi
                </Button>
                <Button
                  type="primary"
                  htmlType="submit"
                  loading={saving}
                  style={{
                    borderRadius: 6,
                    height: 38,
                    padding: '0 20px',
                    fontWeight: 500,
                    backgroundColor: '#1890ff'
                  }}
                >
                  Lưu thay đổi
                </Button>
              </div>
            </Form>
          )}
        </Card>
      )
    },
    {
      key: 'config',
      label: 'System Config',
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={4}>Cấu hình thiết bị & hệ thống</Title>
          <Empty
            description="Quản lý máy in hóa đơn, tích hợp POS và phân giải mã QR quét bàn — tính năng chưa được phát triển."
            style={{ padding: '24px 0' }}
          />
        </Card>
      )
    },
    {
      key: 'account',
      label: 'My Account',
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={4}>Tài khoản cá nhân</Title>
          <Empty
            description="Đổi mật khẩu và bảo mật 2 lớp — tính năng chưa được phát triển."
            style={{ padding: '24px 0' }}
          />
        </Card>
      )
    }
  ];

  return (
    <div className="system-settings-container">
      {/* Page Title */}
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ margin: 0, fontSize: '28px', fontWeight: 700, color: '#1a202c' }}>System Settings</h2>
      </div>

      {/* Tabs */}
      <Tabs
        activeKey={activeTab}
        onChange={(key) => setActiveTab(key)}
        items={tabItems}
        style={{ marginBottom: 24 }}
      />
    </div>
  );
};

export default SettingsPage;
