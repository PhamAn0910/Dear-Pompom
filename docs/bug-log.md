# Nhật ký lỗi

Ghi mỗi lỗi tìm thấy khi build hoặc khi chơi bản web. Giữ cả lỗi đã sửa: lần sau gặp triệu chứng giống thì tra lại.

| # | Ngày | Tìm thấy ở | Triệu chứng | Nguyên nhân | Sửa ở commit | Test giữ lại |
| --- | --- | --- | --- | --- | --- | --- |
| B1 | 2026-11-15 | Đọc code (PRD 2.2) | Gợi ý luôn là "You haven't tried VirtualWalk yet!" | Luật tìm tên hoạt động mà game không bao giờ ghi | `82d9563c` (vá tạm), `e065277b` (tận gốc) | `RecommendationRuleTests.B1_*` |
| B2 | 2026-11-15 | Đọc code (PRD 2.2) | Mọi bài nhật ký bị coi là tâm trạng thấp | Luật đọc `score`, nhật ký luôn ghi 0 | `82d9563c`, `e065277b` | `RecommendationRuleTests.B2_*` |
| B8 | 2026-11-15 | Đọc code (PRD 2.2) | Save không có phiên bản, ghi đè trực tiếp | `File.WriteAllText` thẳng vào file chính | `e065277b`, `c831962e` | `SaveMigratorTests`, `RealV1Save_MigratesWithoutLoss` |
| L1 | 2026-11-15 | Đọc code | Nội dung nhật ký bị lưu hai nơi | 1.0 chép toàn văn vào `notes` của log | `c831962e` | `SaveMigratorTests.JournalText_IsNotCopiedIntoTheLog` |
| L2 | 2026-11-15 | Đọc code | Phiên thiền hoàn thành có thể bị ghi thêm một bản "interrupted" | `OnMeditationComplete` không đặt lại `sessionStartTimeUnscaled` | `c831962e` | chưa có (cần chạy trong Unity) |
| L3 | 2026-11-15 | Đọc code | Nút mute nhà hát tắt luôn tiếng click và nhạc nền | `AudioListener.volume = 0` | `86d2871c` | chưa có |
| L4 | 2026-11-15 | Đọc scene | Scene StartMenu đặt ngưỡng "nghỉ sau N Pomodoro" là 1, không phải 3 | Giá trị demo còn sót trong Inspector | `c831962e` (ngưỡng giờ nằm trong `BreakAfterPomodorosRule`, mặc định 3) | `BreakAfterPomodoros_FiresOnTheThirdFinishedSession` |
| C1 | 2026-11-15 | CI lượt 1, job "Core tests" | `MSB1003: Specify a project or solution file` | `.gitignore` chặn `*.csproj` (file Unity tự sinh) nên 3 project trong `Tools/` không được commit | `70239d4c` | Job CI chạy trên checkout sạch |
| C2 | 2026-11-15 | CI lượt 2, job "Build WebGL" | `GraphRenderer.cs(41,29): error CS1061: 'UserProgressManager' does not contain a definition for 'LoadProgress'` | Xoá hàm public `LoadProgress` khi viết lại `UserProgressManager` mà không tìm nơi gọi ở script ngoài danh sách kiểm tra kiểu | `ed9d64bc` | Trước khi xoá hoặc đổi tên thành viên public: `grep -rn "TênHàm" Assets/Scripts` |
| W1 | 2026-11-16 | Bản web, bước 11 của web-test.md (game toán) | Một nút đáp án đứng màu xanh lá mãi, không trở lại màu nền | Lỗi có từ 1.0: `FlashOption` đọc "màu gốc" lúc bắt đầu nháy. Khi nháy lần hai bắt đầu trong 0,5 giây của lần một trên cùng nút, màu gốc bị ghi là xanh | `3c91bd96` | chưa có test tự động; `MathGameManager.cs` đã vào danh sách kiểm tra kiểu. Cần chơi lại trên web để xác nhận |

## Mẫu cho lỗi mới

```
| W1 | <ngày> | Bản web, bước <n> của web-test.md | <thấy gì, kèm dòng Console> | <nguyên nhân> | <commit> | <test> |
```
