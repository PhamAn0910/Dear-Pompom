# Nhật ký lỗi

Ghi mỗi lỗi tìm thấy khi build hoặc khi chơi bản web. Giữ cả lỗi đã sửa: lần sau gặp triệu chứng giống thì tra lại.

| # | Ngày | Tìm thấy ở | Triệu chứng | Nguyên nhân | Sửa ở commit | Test giữ lại |
| --- | --- | --- | --- | --- | --- | --- |
| B1 | 2026-11-15 | Đọc code (PRD 2.2) | Gợi ý luôn là "You haven't tried VirtualWalk yet!" | Luật tìm tên hoạt động mà game không bao giờ ghi | `90704c88` (vá tạm), `00c0664d` (tận gốc) | `RecommendationRuleTests.B1_*` |
| B2 | 2026-11-15 | Đọc code (PRD 2.2) | Mọi bài nhật ký bị coi là tâm trạng thấp | Luật đọc `score`, nhật ký luôn ghi 0 | `90704c88`, `00c0664d` | `RecommendationRuleTests.B2_*` |
| B8 | 2026-11-15 | Đọc code (PRD 2.2) | Save không có phiên bản, ghi đè trực tiếp | `File.WriteAllText` thẳng vào file chính | `00c0664d`, `5e6cf7e0` | `SaveMigratorTests`, `RealV1Save_MigratesWithoutLoss` |
| L1 | 2026-11-15 | Đọc code | Nội dung nhật ký bị lưu hai nơi | 1.0 chép toàn văn vào `notes` của log | `5e6cf7e0` | `SaveMigratorTests.JournalText_IsNotCopiedIntoTheLog` |
| L2 | 2026-11-15 | Đọc code | Phiên thiền hoàn thành có thể bị ghi thêm một bản "interrupted" | `OnMeditationComplete` không đặt lại `sessionStartTimeUnscaled` | `5e6cf7e0` | chưa có (cần chạy trong Unity) |
| L3 | 2026-11-15 | Đọc code | Nút mute nhà hát tắt luôn tiếng click và nhạc nền | `AudioListener.volume = 0` | `d866bb52` | chưa có |
| L4 | 2026-11-15 | Đọc scene | Scene StartMenu đặt ngưỡng "nghỉ sau N Pomodoro" là 1, không phải 3 | Giá trị demo còn sót trong Inspector | `5e6cf7e0` (ngưỡng giờ nằm trong `BreakAfterPomodorosRule`, mặc định 3) | `BreakAfterPomodoros_FiresOnTheThirdFinishedSession` |

## Mẫu cho lỗi mới

```
| W1 | <ngày> | Bản web, bước <n> của web-test.md | <thấy gì, kèm dòng Console> | <nguyên nhân> | <commit> | <test> |
```
