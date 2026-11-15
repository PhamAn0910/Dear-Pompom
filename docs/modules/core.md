# Module: DearPompom.Core (lõi logic, không phụ thuộc Unity)

- **Vị trí:** `Assets/Scripts/Core/` (assembly `DearPompom.Core`, `noEngineReferences: true`)
- **Test:** `Assets/Tests/EditMode/` (chạy trong Unity Test Runner) và `dotnet test Tools/CoreTests` (chạy không cần Unity)
- **PRD:** D1, D2, D3, D4, D6, R2, R5, R6, lỗi B1, B2, B7, B8; kiến trúc 8.2 (IClock, sự kiện)

Lõi không được `using UnityEngine`. Nhờ vậy toàn bộ luật chơi biên dịch và test được bằng `dotnet`
trên máy không cài Unity. Nếu cần gọi API Unity, viết ở lớp `Assets/Scripts/Services/` rồi gọi vào lõi.

## Bản đồ file

| File | Vai trò |
| --- | --- |
| `Time/IClock.cs` | `IClock`, `SystemClock`, `Timestamp` (đọc/ghi ISO 8601). Nguồn "bây giờ" duy nhất |
| `Activities/ActivityType.cs` | enum `ActivityType`, `ActivityKind`, bảng tên scene của từng hoạt động |
| `Activities/Mood.cs` | enum `Mood` (là **loại**, không phải thang điểm) và `Moods.IsLow` |
| `Activities/ActivityRecord.cs` | Một bản ghi hoạt động (thay `ActivityLogEntry` của 1.0) |
| `Activities/ActivityLog.cs` | **Đường ghi duy nhất** (D4): `Add(...)` gắn id, giờ, chương rồi phát `Recorded` |
| `Save/SaveData.cs` | Gốc file save, có `schemaVersion`; `SettingsData` |
| `Save/LegacySaveV1.cs` | Hình dạng file save 1.0 và các tên gõ tay cũ (chỉ nằm ở đây) |
| `Save/SaveMigrator.cs` | Đổi save 1.0 sang 2.0, có `MigrationReport` |
| `Stats/ProgressStats.cs` | Lớp truy vấn (D6): phút theo ngày/loại, ngày có hoạt động, tỷ lệ hoàn thành, tâm trạng |
| `Story/ChapterRules.cs` | Điều kiện mở 4 chương (PRD 4.3) |
| `Events/EventBus.cs` | Publish/subscribe có kiểu; các sự kiện `ActivityRecorded`, `RecommendationReady`, `ChapterUnlocked`, `SettingsChanged` |
| `Recommendations/` | Xem [recommendations.md](recommendations.md) |

## Quy tắc khi sửa

1. **Giá trị enum được ghi vào file save.** Chỉ thêm giá trị mới ở cuối, không đổi số, không dùng lại số cũ.
2. **Không tự `new ActivityRecord` trong code gameplay.** Gọi `UserProgressManager.Instance.RecordActivity(...)`
   (lớp Unity), hàm đó gọi `ActivityLog.Add`.
3. **Không dùng `DateTime.Now` trong lõi.** Nhận `IClock` qua constructor để test điều khiển được thời gian.
4. **Không `DateTime.Parse` từng dòng log.** Dùng `ActivityRecord.Time` / `LocalDate` (đã cache) hoặc `ProgressStats`.
5. Thêm trường mới có giá trị mặc định hợp lý thì **không** cần tăng `schemaVersion`. Đổi nghĩa hoặc xoá trường thì
   tăng `SaveData.CurrentSchemaVersion` và thêm một bước trong `SaveMigrator`.

## Quyết định thiết kế

| Quyết định | Lý do |
| --- | --- |
| Ngày của bản ghi là ngày trên đồng hồ người chơi (`LocalDate`), không phải UTC | 00:30 sáng thứ Hai giờ VN vẫn là Chủ nhật theo UTC; biểu đồ theo ngày sẽ lệch |
| `completed: bool` thay cho loại `PomodoroInterrupted`, `MeditationInterrupted` | Đếm tỷ lệ hoàn thành bằng một trường, không nhân đôi số loại (D2) |
| Tâm trạng là enum loại, không có "tâm trạng trung bình" | Trả lời câu hỏi mở số 6 của PRD: tâm trạng theo loại. `ProgressStats` trả về số lần mỗi loại và loại nhiều nhất |
| Nội dung nhật ký **không** được chép vào log hoạt động | 1.0 chép toàn văn nhật ký vào `notes`. Nhật ký là dữ liệu nhạy cảm, chỉ nên nằm ở một file |
| "Tuần" là cửa sổ trượt 7 ngày kết thúc hôm nay | Không phụ thuộc tuần bắt đầu thứ Hai hay Chủ nhật; ngày đầu tuần không bị "trống số liệu" |
| `ActivityKind` tách phiên mở nhật ký / viết mới / sửa | Điều kiện Chương 3 đếm "thư đã viết": chỉ đếm bài mới, không đếm lần sửa |

## Migrate save 1.0 (D3)

`SaveMigrator.FromV1(legacy, journal, clock, out report)`:

| Tên trong save 1.0 | Thành 2.0 |
| --- | --- |
| `Pomodoro` / `PomodoroInterrupted` | `Pomodoro`, `completed = true / false` |
| `Meditation` / `MeditationInterrupted` | `Meditation`, `completed = true / false` |
| `ForestWalk` | `ForestWalk` |
| `TheatreVisit` | `Theatre` |
| `MemoryCard` | `MemoryCard` |
| `Math Game` | `MathGame` |
| `JournalSession` / `JournalEntry` / `JournalUpdate` | `Journal` + `ActivityKind` tương ứng |

- Tên lạ (ví dụ `VirtualWalk`) và timestamp hỏng bị bỏ qua, có đếm trong `MigrationReport`.
- Tâm trạng của bài nhật ký cũ được lấy lại từ `journalData.json` bằng cách so nội dung bài.
- Id của bản ghi migrate là `v1-00000`, `v1-00001`... nên migrate hai lần cho cùng kết quả.

**Đã kiểm chứng trên file save 1.0 thật của máy này** (test `RealV1Save_MigratesWithoutLoss`, chỉ chạy bằng dotnet):
97/97 dòng log migrate được, 0 dòng bị bỏ, tổng số phút giữ nguyên, lấy lại được tâm trạng cho 9 bài.

## Lỗi hay gặp khi mở rộng

| Triệu chứng | Nguyên nhân thường gặp |
| --- | --- |
| Unity báo `The type or namespace 'UnityEngine' could not be found` trong Core | Có file trong `Core/` dùng API Unity. Chuyển nó ra `Assets/Scripts/Services/` |
| Số liệu sai sau khi thêm loại hoạt động | Quên tăng `ProgressStats.TypeSlots` hoặc quên thêm vào `ActivityTypes.All` |
| Save cũ đọc ra `completed = false` hàng loạt | Trường mới kiểu `bool` mặc định `false` với JsonUtility khi thiếu trong file; cần giá trị khởi tạo trong class |
