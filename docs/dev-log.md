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

### Bước 8. Gộp vào `main` và chơi thử bản web (2026-11-15, 2026-11-16)

- `main` được fast-forward tới `4b0c5e7b` (run 43688167221), bản Pages là code Giai đoạn 0.
- Người chơi thử: An, Chrome, trình duyệt để tiếng Việt. Kết quả theo [web-test.md](web-test.md):

| Bước | Kết quả | Bằng chứng |
| --- | --- | --- |
| 1 Khởi động | Đạt | Console: `[DearPompom] save: ...`, `services ready` |
| 3 Tiếng click | Đạt | An nghe được |
| 9 Nhạc cụ nhà hát | Đạt một phần | Console: `Started 2 instrument(s)`; chưa xác nhận nhạc nền nhỏ dần và trở lại |
| 11 Lật thẻ | Đạt | Console: `Logged Memory Card Game: Difficulty Easy, Score 4` |
| 11 Game toán | Chơi được, có lỗi hiển thị W1 | Ảnh chụp: một nút đứng màu xanh. Chưa xem bản ghi `"type": 6` |
| 13 Tải lại trang | Đạt | Lần mở đầu: `no save found, started a new one`; sau F5: `loaded progress.json (8 records)` |
| 2, 4–8, 10, 12, 14 | Chưa kiểm | Thư của Pompom, Pomodoro, nhật ký, biểu đồ, đóng tab ngay sau hoạt động |

- Rủi ro "`FS.syncfs` không lộ ra cho jslib trên Unity 6" đã loại: Console có `warning: 2 FS.syncfs operations in flight at once`, tức lệnh có chạy. Cảnh báo vô hại, do hai lần lưu sát nhau.
- Rủi ro "save 1.0 trên web không được nhận": chưa kết luận. `no save found` ở lần mở đầu chỉ là lỗi nếu trình duyệt đó từng có save 1.0.
- Lỗi đỏ có sẵn, chưa xử lý: `DifficultySettings component not found on GameManager` (`GameSceneController2.cs:340`). Lật thẻ Easy vẫn chạy.
- Sửa W1: `MathGameManager` giữ màu nền của từng nút từ `Start` và mỗi nút chỉ có một lần nháy đang chạy. Kiểm chứng: **Kiểu** (file đã thêm vào `Tools/UnityTypecheck`, stub thêm `Color`, `Random`, `Mathf.Ceil`). Chưa chơi lại trên web.

### Trạng thái cổng Giai đoạn 0 (PRD 9.1)

| Điều kiện qua cổng | Trạng thái |
| --- | --- |
| Test bộ gợi ý xanh | Đạt (dotnet) |
| Test migrate save xanh | Đạt (dotnet) |
| Save 1.0 mở được | Đạt ở mức logic (file thật migrate được). Save 2.0 đọc lại được trên web (bước 8). Save 1.0 thật trên web: **Chưa** |
| Unity biên dịch và build WebGL được | Đạt (lượt 3, run 43685572481) |
| Chạy đúng khi chơi | Một phần (bước 8): khởi động, lưu qua F5, lật thẻ, tiếng click đạt. Còn các bước 2, 4–8, 10, 12, 14 và chơi lại game toán sau khi sửa W1 |

### Còn lại của Giai đoạn 0

| Mục | Ghi chú |
| --- | --- |
| M1 AudioMixer | Cần editor, xem [deviations.md](deviations.md) V2 |
| Dọn `Debug.Log` ở các script chưa đụng tới | `GraphRenderer`, `GameSceneController2`, `Card`... |
| Xoá script chết | Xem deviations V7 |
| B3 quyền nhạc | Chưa đụng. PRD yêu cầu chốt nguồn nhạc ngay Giai đoạn 0 |

## Chuyển hướng: plan mới, ưu tiên animation và thao tác (2026-11-16)

An yêu cầu bỏ phần còn dở của Giai đoạn 0 và lập plan chi tiết cho các model AI sau này làm theo, trọng tâm là
asset animation và thao tác mượt. Thứ tự mới ở [plan/README.md](plan/README.md); lý do lệch PRD ở deviations V10–V13.

### Bước 9. Plan, guide, công cụ, lõi chuyển động

Commit: `3c91bd96` (sửa W1), `3e26ab07` (lõi chuyển động), `2f140659` (công cụ), và commit tài liệu ngay sau đó. Đây là bước M0.1 của plan.

| Thứ | Nội dung | Kiểm chứng |
| --- | --- | --- |
| `docs/plan/` | 6 file: tổng quan và trạng thái, M0 đến M5, 36 bước | Đã đối chiếu tên object với `docs/scenes/`, tên hàm với code |
| `docs/guides/` | `no-editor-unity.md`, `game-feel.md`, `animation-assets.md`, kèm mã mẫu `SceneFind`, `Tweener`, `ButtonFeel`, `SceneTransitionService`, `PatchClipPlayer`, `TextureImportRules` | Mã mẫu: **Chưa** (chưa biên dịch bằng Unity thật, cũng chưa qua kiểm tra kiểu) |
| `CLAUDE.md`, `AGENTS.md` | Điểm vào cho AI model | |
| `Assets/Scripts/Core/Motion/` | `Ease`, `Spring`, `Smooth`, `TweenState`, `Flipbook`, `PatchClipData` | **Test**: 22 test mới, tổng 104 |
| `Assets/Scripts/Core/Time/Countdown.cs` | Đồng hồ đếm ngược theo giờ thật | **Test** |
| `Tools/art/extract_patches.py` | Chuỗi ảnh toàn màn hình thành nền + ô 64 px + JSON | Chạy trên cả 9 chuỗi ảnh với `--verify`: sai khác 0, giảm 80–92% bộ nhớ chưa nén. Kết quả chưa đưa vào `Assets/` |
| `Tools/art/make_fx_sprites.py` | 6 sprite hạt trắng có hạt bút sáp | Đã chạy và xem ảnh. Kết quả chưa đưa vào `Assets/` |
| `Tools/scene_inventory.py`, `docs/scenes/` | Cây object của 11 scene và 23 prefab | Đã chạy; `--check` xanh |

Phát hiện khi khảo sát, đã đưa vào plan:

- Scene Home tham chiếu 9 Animator cùng lúc, tức 144 ảnh 1920×1080 (khoảng 150 MB texture nén). Trong tranh buổi
  sáng chỉ có tia nắng, lá, khói và cánh tay chuyển động.
- Bản build WebGL đang không nén (`webGLCompressionFormat: 2`) và `runInBackground: 0` (plan M0.2).
- Ba đồng hồ (Pomodoro, thiền, toán) cộng dồn `deltaTime`, sẽ đứng khi tab ở nền (plan M2.2).
- Kéo bản đồ rồi thả chuột trên một toà nhà thì vào toà nhà đó (plan M2.1).
- Canvas của các scene dùng hai chuẩn khác nhau (800×600 và 1920×1080); UI mới dựng trong canvas riêng.
- `RenderMap` trỏ tới scene `Map` không tồn tại; `SceneController` tải scene kế khi bấm bất kỳ đâu. Cả hai không
  được scene nào dùng (plan M1.3).

