# Test bản web

Code Giai đoạn 0 **chưa từng chạy trong Unity**. Trang này liệt kê cần kiểm gì và lỗi nào dễ xảy ra nhất.

## Đọc log ở đâu

- **Build hỏng:** GitHub > Actions > "Build WebGL and deploy to GitHub Pages" > job "Build WebGL". Tìm `error CS`.
  Bằng dòng lệnh: `gh run list -R PhamAn0910/Dear-Pompom -L 3`, rồi `gh run view <id> --log-failed`.
- **Lỗi khi chơi:** mở DevTools của trình duyệt (F12) > Console. Mọi log của code mới bắt đầu bằng `[DearPompom]`.
- **File save trên web:** DevTools > Application > IndexedDB > `/idbfs` > `FILE_DATA`. Tìm khoá kết thúc bằng `save/progress.json`.

## Danh sách kiểm (theo thứ tự)

| # | Làm | Kết quả đúng | Nếu sai, xem |
| --- | --- | --- | --- |
| 1 | Mở trang, xem Console | `[DearPompom] save: ...` rồi `[DearPompom] services ready.`; không có lỗi đỏ | [services.md](modules/services.md) "Khởi động" |
| 2 | Start > vào World | Sau 1 giây hiện "Welcome to the village of Pompom!" | `RecommendationSystem.OnSceneLoaded` |
| 3 | Bấm nút bất kỳ | Có tiếng click | [audio.md](modules/audio.md) |
| 4 | Vào Nhà, chạy Pomodoro 1 phút tới hết | Không lỗi; IndexedDB có `progress.json` chứa bản ghi `"type": 1`, `"completed": true`, `"targetMinutes": 1` | `PomodoroDayNight.LogSession` |
| 5 | Chạy Pomodoro rồi bấm đặt lại giữa chừng | Bản ghi mới có `"completed": false` | như trên |
| 6 | Viết một bài nhật ký, chọn tâm trạng Sad | `journal.json` có bài; `progress.json` có bản ghi `"type": 4, "kind": 2, "mood": 3, "notes": ""` | `JournalDataManager` |
| 7 | Rời Nhà sang scene khác | Sau 1 giây hiện một thư của Pompom (không phải câu "VirtualWalk") | [recommendations.md](modules/recommendations.md) |
| 8 | Đóng thư, làm thêm 3 hoạt động | Không có hai thư giống nhau liên tiếp; tối đa 3 thư trong ngày | `RecommendationEngine` |
| 9 | Vào Nhà hát | Nhạc nền làng nhỏ dần rồi tắt; bấm nhạc cụ có tiếng; tiếng click vẫn còn | `TheatreSceneAudioController`, `InstrumentStatic` |
| 10 | Rời Nhà hát | Nhạc nền trở lại; `progress.json` có bản ghi `"type": 7` | `SceneVisitLogger` |
| 11 | Chơi lật thẻ tới hết; chơi toán tới Game Over | Bản ghi `"type": 5` và `"type": 6`, có `score` | `GameSceneController2`, `MathGameManager` |
| 12 | Mở màn Progress và UserProgress | Biểu đồ vẫn vẽ như 1.0 | bản chiếu `currentUserProgress` |
| 13 | Tải lại trang (F5) | Dữ liệu còn nguyên; Console ghi `loaded progress.json (N records)` | `SaveStore`, jslib |
| 14 | Đóng tab ngay sau khi xong một hoạt động, mở lại | Hoạt động đó vẫn còn | `FS.syncfs` |

Số của `type`: 1 Pomodoro, 2 Meditation, 3 ForestWalk, 4 Journal, 5 MemoryCard, 6 MathGame, 7 Theatre.

## Chỗ mình ngờ nhất (chưa kiểm chứng được)

| Rủi ro | Vì sao ngờ | Dấu hiệu |
| --- | --- | --- |
| Lỗi biên dịch ở script Unity | Chỉ kiểm bằng stub tự viết | Job "Build WebGL" đỏ, có `error CS` |
| `FS.syncfs` trong jslib trên Unity 6 | Tên `FS` có thể không lộ ra cho jslib ở bản Emscripten mới | Console: `file sync is unavailable`; mất dữ liệu ở bước 14 |
| Save 1.0 trên web không được nhận | Không chắc `persistentDataPath` trên WebGL có đổi khi đổi tên sản phẩm | Console: `no save found, started a new one` dù trước đó đã chơi bản 1.0 trên cùng trình duyệt |
| Bản sao dịch vụ trong StartMenu | `_Managers` trong scene tự gỡ component; nếu có script khác trông vào nó thì hỏng | `MissingReferenceException` nhắc `UserProgressManager` |
| Test assembly làm hỏng build | `Assets/Tests/EditMode` biên dịch trong editor khi build | `error CS` trong file `*Tests.cs` |
