# Sai lệch giữa sequence diagram (PDF) và code thực tế

File này ghi lại các điểm mà 4 file sequence diagram trong thư mục này
(`1a-customer-scan-order.pdf`, `1b-customer-payment.pdf`, `2-order-processing.pdf`,
`3-manager-management.pdf`) mô tả khác với hành vi thực tế của code. Các điểm dưới
đây không ảnh hưởng đến tính đúng đắn runtime — code đang chạy đúng ý đồ thiết kế,
chỉ có phần mô tả trong PDF là thiếu hoặc lỗi thời. Không dựng lại PDF vì không có
file nguồn (mermaid/markdown) đi kèm.

## 1. Flow 1a — PlaceOrder trả về 201, không phải 200 OK

PDF `1a-customer-scan-order.pdf` ghi `POST /api/v1/orders` → `200 OK`.

Thực tế: [OrdersController.cs](../Services/SmartDine.Order.API/Controllers/OrdersController.cs)
dùng `Created(...)` → **201 Created**, đúng chuẩn REST cho hành động tạo mới resource
(Order). Đây là hành vi đúng, PDF mô tả sai status code.

## 2. Flow 1b — Thanh toán CASH bỏ qua PayOS gateway hoàn toàn

PDF `1b-customer-payment.pdf` mô tả `CreatePaymentLinkAsync` (HMAC-SHA256 signed) được
gọi cho mọi phương thức thanh toán.

Thực tế: [PaymentService.cs](../Shared/SmartDine.Application/Services/PaymentService.cs)
— khi `paymentMethod == CASH`, gateway PayOS được **bỏ qua hoàn toàn**
(`qrUrl`/`deeplink` để `null`, `externalRef = orderCode.ToString()`). Chỉ các phương
thức không phải CASH mới gọi gateway. PDF không mô tả nhánh rẽ này.

## 3. Flow 2 — GetActiveOrders dùng blacklist, không phải whitelist PENDING/COOKING/READY

PDF `2-order-processing.pdf` mô tả "danh sách Order đang PENDING/COOKING/READY".

Thực tế: query trong `OrderRepository` là
`Where(o => o.Status != OrderStatus.COMPLETED && o.Status != OrderStatus.CANCELLED)`
— tức "chưa hoàn tất, chưa huỷ", không phải whitelist 3 trạng thái cụ thể. Vì enum
`OrderStatus` còn có trạng thái `CONFIRMED` (giữa PENDING và COOKING trong transition
graph ở [Order.cs](../Shared/SmartDine.Domain/Entities/Order.cs)), đơn ở trạng thái
CONFIRMED cũng được coi là "active" và hiển thị cho bếp/nhân viên — hành vi này đúng
ý đồ (không bỏ sót đơn), PDF chỉ chưa liệt kê đủ trạng thái.

## 4. ~~Flow 3 — GetHistory cho phép cả STAFF~~ — đã sửa, không còn sai lệch

Trước đây `PaymentsController.GetHistory` có `[Authorize(Roles = Roles.StaffAndManager)]`,
khác với doc-comment ngay phía trên nó ("Roles: MANAGER.") và với PDF `3-manager-management.pdf`
(đặt mục này dưới flow "Manager — Quản lý..."). Theo quy tắc phân quyền chính thức
(STAFF chỉ xử lý đơn: đổi trạng thái + thanh toán cho khách; MANAGER mới xem báo cáo/lịch
sử giao dịch), đã sửa lại thành `[Authorize(Roles = Roles.Manager)]` — khớp với cả PDF lẫn
comment gốc trong code. `CompletePayment`/`CompletePaymentByTable` (thao tác thanh toán hộ
khách) vẫn giữ `StaffAndManager` vì đó là một phần của việc xử lý đơn/thanh toán cho khách.
