# TaskManagement
# TPM Task Management

## Ảnh nhiệm vụ

Ảnh đính kèm nhiệm vụ và ảnh nhân viên gửi khi chuyển nhiệm vụ sang chờ duyệt được lưu trực tiếp vào SQL Server trong bảng `TaskImages` (`varbinary(max)`). Mỗi ảnh tối đa 10 MB, tối đa 5 ảnh trong một lần tải. Xóa nhiệm vụ sẽ xóa ảnh liên quan nhờ khóa ngoại cascade.

## Cài đặt Web Push

Ứng dụng dùng thư viện NuGet `WebPush` (đã khai báo trong project). Web Push yêu cầu website chạy HTTPS với chứng chỉ được điện thoại tin cậy; chỉ `localhost` được phép chạy thử không HTTPS. Tạo cặp khóa VAPID một lần bằng `VapidHelper.GenerateVapidKeys()` của thư viện WebPush, rồi cấu hình trên máy chủ chạy ứng dụng:

```json
{
  "WebPush": {
    "PublicKey": "YOUR_PUBLIC_KEY",
    "PrivateKey": "YOUR_PRIVATE_KEY",
    "Subject": "mailto:admin@yourdomain.com"
  }
}
```

Không đưa khóa riêng VAPID lên source control. Khi người dùng đăng nhập trên từng điện thoại và bấm **Bật thông báo điện thoại**, đăng ký Push được gắn với tài khoản đó. Khi nhiệm vụ được giao hoặc chuyển sang nhân viên khác, máy chủ chỉ gửi thông báo tới các thiết bị đã đăng ký của nhân viên nhận việc. Các endpoint hết hạn sẽ tự được xóa.

Trên iPhone/iPad, người dùng cần thêm website vào Màn hình chính trước; Web Push khả dụng với web app trên Home Screen từ iOS/iPadOS 16.4. Không cần cài từ CH Play. Trên Android, mở bằng trình duyệt hỗ trợ Push và cấp quyền thông báo.

## Database

Khi ứng dụng khởi động, EF Core tự chạy các migration còn thiếu, gồm bảng `TaskImages` và `PushSubscriptions`.
