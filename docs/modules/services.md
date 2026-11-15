# Module: Services (lớp Unity nối vào lõi)

- **Vị trí:** `Assets/Scripts/Services/`, `Assets/Scripts/Data/`, `Assets/Plugins/WebGL/DearPompomFiles.jslib`
- **PRD:** 8.2 (Bootstrap, sự kiện), D4, D5, lỗi B8
- **Kiểm chứng:** mới qua kiểm tra kiểu bằng stub. **Chưa chạy trong Unity.** Xem [../web-test.md](../web-test.md)

## Khởi động

`Services.EnsureBooted()` được Unity gọi trước khi scene đầu tiên tải. Thứ tự tạo có ý nghĩa:

| Thứ tự | Dịch vụ | Kiểu | Vai trò |
| --- | --- | --- | --- |
| 1 | `Services.Clock` | `SystemClock` | Nguồn "bây giờ" |
| 2 | `Services.Events` | `EventBus` | Kênh sự kiện; lỗi trong handler được `Debug.LogException` |
| 3 | `Services.Store` | `SaveStore` | Đọc ghi file |
| 4 | `Services.Progress` | `UserProgressManager` (MonoBehaviour) | Tải save trong `Awake`, là đường ghi hoạt động |
| 5 | `Services.Audio` | `AudioManager` (MonoBehaviour) | Đọc âm lượng từ save vừa tải |
| 6 | `Services.Recommendations` | `RecommendationService` | Nghe `ActivityRecorded`, phát `RecommendationReady` |
| 7 | `SceneVisitLogger` | MonoBehaviour | Ghi thời gian ở Theatre và ForestWalking |

Tất cả MonoBehaviour nằm trên một GameObject `[DearPompom Services]`, `DontDestroyOnLoad`.

### Bản sao còn nằm trong scene StartMenu

Scene `StartMenu` của 1.0 có sẵn `_Managers` (gắn `UserProgressManager` và `SceneVisitLogger`) và
`RecommendationSystemController`. Không sửa được scene khi không có editor, nên:

| Script trong scene | Xử lý trong `Awake` |
| --- | --- |
| `UserProgressManager`, `SceneVisitLogger` trên `_Managers` | Thấy đã có bản của Services thì `Destroy(this)` (chỉ gỡ component, không xoá GameObject) |
| `RecommendationSystem` | Vẫn dùng bản trong scene vì nó giữ tham chiếu tới panel và text. Nếu StartMenu tải lại, bản thứ hai tự huỷ |

## Lưu file (`SaveStore`)

```
<persistentDataPath>/save/progress.json      SaveData: bản ghi, cài đặt, trạng thái gợi ý
<persistentDataPath>/save/progress.json.bak  bản tốt trước đó
<persistentDataPath>/save/journal.json       nhật ký
<persistentDataPath>/save/journal.json.bak
```

Ghi: viết ra `*.tmp`, chép file hiện tại sang `*.bak`, xoá file hiện tại, đổi tên `*.tmp`. Trên WebGL gọi thêm
`FS.syncfs` (jslib) để đẩy xuống IndexedDB.

Đọc `progress.json` theo thứ tự, dừng ở bước đầu tiên thành công:

1. `save/progress.json` có `schemaVersion >= 2`
2. `save/progress.json.bak`
3. File 1.0 `userProgress.json`: tìm trong `persistentDataPath`, rồi `../../DefaultCompany/My project/` (tên sản phẩm cũ trên desktop). Migrate bằng `SaveMigrator`
4. Save mới

File 1.0 không bao giờ bị sửa hay xoá. Dòng log `[DearPompom] save: ...` lúc khởi động cho biết đã đi nhánh nào.

**Hệ quả của việc đổi tên sản phẩm (B9):** trên desktop, `persistentDataPath` đổi từ
`DefaultCompany/My project` sang `Dear Pompom/Dear Pompom`. Bước 3 ở trên xử lý việc này.

## Ghi một hoạt động

Chỉ có một cách:

```csharp
UserProgressManager.Instance.RecordActivity(
    ActivityType.Pomodoro, actualMinutes, completed: true, targetMinutes: 25);
```

| Nơi gọi | Loại | Ghi chú |
| --- | --- | --- |
| `PomodoroDayNight.LogSession` | Pomodoro | `completed = false` khi dừng sớm hoặc rời scene |
| `MeditationTimer.LogSession` | Meditation | như trên |
| `JournalDataManager.Record` | Journal | `kind` = JournalSession / JournalEntry / JournalUpdate; có `mood` |
| `GameSceneController2.LogGameCompletion` | MemoryCard | có `score`, `difficulty` |
| `MathGameManager.GameOver` | MathGame | có `score`, `difficulty = "Standard"` |
| `SceneVisitLogger` | Theatre, ForestWalk | rời scene sau hơn 3 giây |

## Tương thích với biểu đồ 1.0

`GraphRenderer` và `DataGrapher` vẫn đọc `UserProgressManager.Instance.currentUserProgress.allActivityLogs`.
Thuộc tính đó giờ là **bản chiếu chỉ đọc**, dựng lại từ `SaveData.records` với tên kiểu cũ (`"Math Game"`,
`"PomodoroInterrupted"`...). Sửa vào đó không được lưu. Giai đoạn 1 thay hai script này bằng màn Progress đọc `ProgressStats`.

## Lỗi có thể gặp

| Triệu chứng | Chỗ xem |
| --- | --- |
| Console: `RecordActivity called before the save was loaded` | `UserProgressManager.Awake` không chạy hoặc ném lỗi; xem dòng log `save:` phía trên |
| Tải lại trang web thì mất dữ liệu | `FS.syncfs` không chạy: tìm `[DearPompom] could not sync` hoặc `file sync is unavailable` trong console trình duyệt |
| Hộp gợi ý không bao giờ hiện | `RecommendationSystem` cần `recommendationUIPanel` và `recommendationText` gán trong scene StartMenu; thư chỉ hiện 1 giây sau lần chuyển scene kế tiếp; tối đa 3 thư mỗi ngày |
| Hai bản ghi cho một phiên | Script scene gọi `LogSession` hai lần; `sessionStartTimeUnscaled` phải về 0 sau lần đầu |
| `NullReferenceException` ở `Services.Audio` trong `Awake` của script scene | Script chạy trước khi boot: gọi `Services.EnsureBooted()` đầu `Awake` |
