# Dear Pompom 2.0: tài liệu kỹ thuật

Tài liệu này đi cùng code. Mỗi bước làm có một mục trong [dev-log.md](dev-log.md), mỗi module có một trang
trong `modules/`. Khi gặp lỗi trên bản web, bắt đầu từ [web-test.md](web-test.md).

| Trang | Dùng khi |
| --- | --- |
| [dev-log.md](dev-log.md) | Muốn biết bước nào đã làm gì, ở commit nào, đã kiểm chứng tới đâu |
| [web-test.md](web-test.md) | Test bản web: cần kiểm gì, lỗi nào dễ xảy ra, đọc log ở đâu |
| [bug-log.md](bug-log.md) | Ghi lỗi tìm thấy và cách sửa |
| [deviations.md](deviations.md) | Chỗ nào làm khác PRD và vì sao |
| [modules/core.md](modules/core.md) | Lõi dữ liệu: bản ghi, save, migrate, thống kê, chương |
| [modules/recommendations.md](modules/recommendations.md) | Bộ gợi ý: engine, bảng luật, cách thêm luật |
| [modules/services.md](modules/services.md) | Lớp Unity: Bootstrap, lưu file, đường ghi hoạt động |
| [modules/audio.md](modules/audio.md) | AudioManager và các script âm thanh cũ |

## Làm việc khi không có Unity Editor

Máy phát triển hiện không cài Unity. Ba lớp kiểm tra thay thế, từ nhanh tới chậm:

| Lệnh | Kiểm được gì | Không kiểm được gì | Thời gian |
| --- | --- | --- | --- |
| `dotnet test Tools/CoreTests` | Toàn bộ logic trong `Assets/Scripts/Core` (82 test) | Mọi thứ dùng API Unity | vài giây |
| `dotnet build Tools/UnityTypecheck` | Lỗi cú pháp, gõ sai tên, gọi sai vào lõi trong các script Unity đã liệt kê | API Unity thật (stub tự viết có thể sai), scene, prefab | vài giây |
| Push nhánh `v2/**` lên GitHub | Unity thật biên dịch và build WebGL | Hành vi khi chạy | khoảng 30 phút |
| Push `main` | Như trên, rồi đưa lên GitHub Pages để chơi thử | | khoảng 30 phút |

Sau khi tạo file mới trong `Assets/`, chạy `python3 Tools/gen_meta.py` để sinh `.meta` có GUID ổn định.

## Cấu trúc thư mục

```
Assets/Scripts/Core/        Logic thuần C#, không using UnityEngine (assembly DearPompom.Core)
Assets/Scripts/Services/    Dịch vụ sống suốt game: Services, SaveStore, AudioManager, RecommendationService
Assets/Scripts/<Khu vực>/   Script gắn trong scene (Home, Forest, School, Theatre, World, Data, Graphs)
Assets/Tests/EditMode/      Test NUnit cho lõi (Unity Test Runner và dotnet dùng chung file)
Assets/Plugins/WebGL/       JavaScript cho bản web (đồng bộ file save)
Tools/Core, Tools/CoreTests Project dotnet để build và test lõi ngoài Unity
Tools/UnityTypecheck        Kiểm tra kiểu script Unity bằng stub
Tools/gen_meta.py           Sinh file .meta
docs/                       Tài liệu này
```

## Luồng dữ liệu khi người chơi xong một hoạt động

```
Script scene (vd. PomodoroDayNight)
   -> UserProgressManager.RecordActivity(ActivityType.Pomodoro, phút, completed, targetMinutes)
        -> ActivityLog.Add            gắn id, giờ, chương; thêm vào SaveData.records
        -> SaveStore.SaveProgress     ghi save/progress.json (file tạm, đổi tên, giữ .bak)
        -> Events.Publish(ActivityRecorded)
              -> RecommendationService   hỏi engine -> Publish(RecommendationReady)
                    -> RecommendationSystem   giữ thư, hiện sau khi sang scene kế -> MarkShown
```
