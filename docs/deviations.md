# Chỗ làm khác PRD

Mỗi dòng là một quyết định có chủ ý. Cột "Khi nào xem lại" nói điều kiện để quay về đúng PRD.

| # | PRD viết | Đã làm | Lý do | Khi nào xem lại |
| --- | --- | --- | --- | --- |
| V1 | 8.2: một scene Bootstrap tạo dịch vụ | `Services.EnsureBooted()` chạy bằng `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` | Không có Unity Editor để tạo và sửa scene. Bảo đảm như nhau: dịch vụ có trước `Awake` của mọi scene | Khi có editor và cần gán asset cho dịch vụ qua Inspector |
| V2 | M1: AudioMixer 4 nhóm | 3 kênh Music, SFX, Ambience áp bằng code trong `AudioManager`; "Master" là nút tắt tiếng | Asset AudioMixer chỉ tạo được trong editor | Khi có editor: tạo mixer, sửa `AudioManager.Apply` |
| V3 | R1: mỗi luật gợi ý là một ScriptableObject | Điều kiện là class C# trong lõi; trọng số, thời gian nghỉ, lời thư nằm trong `DefaultRules.cs` | Luật cần test được không cần Unity; chưa tạo được asset | Khi có editor và muốn chỉnh số trong Inspector |
| V4 | D2: `mood` thang 1 đến 5; D6: "tâm trạng trung bình" | `Mood` là enum loại (Productive, Tired, Happy, Sad, Surprised); thống kê trả số lần từng loại và loại nhiều nhất | Câu trả lời cho câu hỏi mở số 6: tâm trạng theo loại, không theo mức | Nếu sau này thêm thang mức độ riêng |
| V5 | D2: không nói gì về `notes` | Nội dung nhật ký không còn được chép vào log hoạt động | 1.0 lưu toàn văn nhật ký ở hai file. Dữ liệu nhạy cảm nên ở một nơi | Không |
| V6 | D5: gộp nhật ký và tiến độ vào một thư mục | Cùng thư mục `save/`, nhưng vẫn là hai file `progress.json` và `journal.json` | Xuất số liệu (D7) sẽ không vô tình kèm chữ nhật ký | Không |
| V7 | 8.2: xoá `DataVisualizationController`, `WebCharts`, `CardDebug`, `OffsetExample`, bản cũ `GameSceneController` | Mới xoá file rỗng `Assets/Scripts/SceneVisitLogger.cs` | `CardDebug` và `GameSceneController` vẫn được scene `SchoolCard` và prefab `Cardd` tham chiếu; xoá khi không mở được scene để gỡ là rủi ro. Phần còn lại để Giai đoạn 1 cùng lúc gộp màn Progress | Giai đoạn 1 |
| V8 | 9.1 Giai đoạn 0 có M1 đến M3 | M2 mới có phần dữ liệu và API (`SetVolume`, `SetMuted`, lưu vào save); chưa có thanh trượt | Màn Cài đặt (F1) thuộc Giai đoạn 1 và cần dựng UI | Giai đoạn 1 |
| V9 | 4.3 Chương 2: "đã thử 4 trong 6 hoạt động" | Đếm 4 trong 7 loại (`ActivityType` có 7 loại theo D1) | PRD mục 1 liệt kê 7 hoạt động, D1 cũng 7 | Nếu muốn gộp hai trò ở Trường thành một |
