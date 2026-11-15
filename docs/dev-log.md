# Nhật ký phát triển

Mỗi bước: đã làm gì, commit nào, kiểm chứng tới đâu. Mới nhất ở dưới.

Mức kiểm chứng dùng trong bảng:

- **Test:** có test tự động chạy xanh trên máy này
- **Kiểu:** qua `dotnet build Tools/UnityTypecheck` (chỉ bắt lỗi cú pháp và gọi sai tên)
- **Chưa:** chưa có kiểm chứng nào; cần Unity build hoặc chơi thử

## Giai đoạn 0: nền móng (2026-11-15)

### Bước 0. Chuẩn bị repo

| Việc | Chi tiết |
| --- | --- |
| Phát hiện | Nhánh `main` trên máy và `origin/main` trên GitHub đã tách nhau. Bản trên máy có 33.133 file `Library/` trong git (1,37 GB); bản GitHub sạch và đã có workflow build WebGL. Nội dung `Assets/` của hai bên giống nhau |
| Đã làm | Lấy `origin/main` làm nền, tạo nhánh `v2/phase-0-foundation`. Không file nào trên đĩa bị đụng. Nhánh `main` cũ trên máy giữ nguyên, có thêm bản sao `backup/local-main-2026-11-15` |
| Hệ quả | Mục "thư mục Library nằm trong git" của B9 đã được giải quyết bởi bản trên GitHub |

### Bước 1. Vá tạm B1, B2 trên code 1.0

- Commit `82d9563c`. Đây là thay đổi đang dở trong thư mục làm việc từ trước, commit riêng để lịch sử rõ.
- Kiểm chứng: **Chưa** (được thay bằng engine mới ở bước 3).

### Bước 2. Tên sản phẩm, gitignore, file rỗng (B9)

- Commit `2f7ac4d0`.
- `productName`, `companyName` thành "Dear Pompom". Xoá `Assets/Scripts/SceneVisitLogger.cs` (0 byte, trùng tên với bản trong `Data/`; GUID của nó không scene nào dùng).
- Lưu ý: đổi tên sản phẩm làm đổi thư mục save trên desktop. `SaveStore` ở bước 4 tìm cả thư mục cũ.

### Bước 3. Lõi `DearPompom.Core`

- Commit `e065277b`. Tài liệu: [modules/core.md](modules/core.md), [modules/recommendations.md](modules/recommendations.md).
- Yêu cầu PRD: D1, D2, D3, D4, D6, R2, R4, R5, R6; nền cho C5 (điều kiện chương).
- Kiểm chứng: **Test**, 82 test. Trong đó `RealV1Save_MigratesWithoutLoss` migrate file save 1.0 thật trên máy này: 97/97 dòng, giữ nguyên tổng số phút.
- Đã thử làm hỏng một luật (`BreakAfterPomodorosRule` ngưỡng +1): test bắt được, rồi khôi phục.

### Bước 4. Nối lõi vào Unity

- Commit `c831962e`. Tài liệu: [modules/services.md](modules/services.md).
- Yêu cầu PRD: 8.2 (Bootstrap, sự kiện), D4, D5.
- Kiểm chứng: **Kiểu**. Riêng `MathGameManager.cs` và `GameSceneController2.cs` (mỗi file sửa một lời gọi) không nằm trong danh sách kiểm tra kiểu: **Chưa**.
- Commit này tham chiếu `AudioManager`, file đó nằm ở commit kế. Hai commit phải đi cùng nhau mới biên dịch.

### Bước 5. AudioManager

- Commit `86d2871c`. Tài liệu: [modules/audio.md](modules/audio.md).
- Yêu cầu PRD: M3, M2 (dữ liệu và API), một phần B5.
- Kiểm chứng: **Kiểu**.

### Bước 6. Công cụ và CI

- Commit `56bc4f66` (`Tools/UnityTypecheck`), `fb6f7686` (workflow, tài liệu).
- Workflow: thêm job "Core tests (no Unity)" chạy trước; nhánh `v2/**` build nhưng không deploy; chỉ `main` deploy lên Pages.

### Bước 7. Build trên GitHub (nhánh `v2/phase-0-foundation`)

| Lượt | Kết quả | Lỗi | Sửa |
| --- | --- | --- | --- |
| 1 | Hỏng sau 22 giây ở "Core tests" | Thiếu file csproj trên GitHub (bug-log C1) | `70239d4c` |
| 2 | "Core tests" xanh; "Build WebGL" hỏng sau 5 phút | Một lỗi biên dịch duy nhất, `GraphRenderer` gọi `LoadProgress` đã bị xoá (bug-log C2) | `ed9d64bc` |
| 3 | Xanh cả hai job, build WebGL mất 16 phút | Không có `error CS` hay `warning CS` trong log | |

Lượt 2 xác nhận: assembly `DearPompom.Core` và assembly test biên dịch được trong Unity; toàn bộ script Unity mới
chỉ có đúng một lỗi, nằm ở file chưa sửa.

### Trạng thái cổng Giai đoạn 0 (PRD 9.1)

| Điều kiện qua cổng | Trạng thái |
| --- | --- |
| Test bộ gợi ý xanh | Đạt (dotnet) |
| Test migrate save xanh | Đạt (dotnet) |
| Save 1.0 mở được | Đạt ở mức logic (file thật migrate được). Đường đọc file trong Unity: **Chưa** |
| Unity biên dịch và build WebGL được | Đạt (lượt 3, run 43685572481) |
| Chạy đúng khi chơi | **Chưa**: nhánh `v2/**` không deploy. Cần gộp vào `main` rồi đi hết [web-test.md](web-test.md) |

### Còn lại của Giai đoạn 0

| Mục | Ghi chú |
| --- | --- |
| M1 AudioMixer | Cần editor, xem [deviations.md](deviations.md) V2 |
| Dọn `Debug.Log` ở các script chưa đụng tới | `GraphRenderer`, `GameSceneController2`, `Card`... |
| Xoá script chết | Xem deviations V7 |
| B3 quyền nhạc | Chưa đụng. PRD yêu cầu chốt nguồn nhạc ngay Giai đoạn 0 |
