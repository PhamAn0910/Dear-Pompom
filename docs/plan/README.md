# Plan thực hiện Dear Pompom 2.0 (sau Giai đoạn 0)

Plan này thay cho lộ trình ở mục 9.1 của PRD kể từ sau Giai đoạn 0. Ưu tiên theo yêu cầu của An ngày
2026-11-16: **animation asset và thao tác mượt trước**, các phần còn lại của PRD sau. Phần còn dở của Giai đoạn
0 (AudioMixer, dọn log, quyền nhạc) không làm tiếp ở đây; mục nào còn cần thì đã được đưa vào M5.

Plan viết cho một AI model làm việc một mình, không có Unity Editor, và có thể không mạnh. Vì vậy mỗi bước nhỏ,
có đường dẫn file cụ thể, có cách kiểm và có điều kiện dừng.

## Cách dùng plan

1. Đọc [CLAUDE.md](../../CLAUDE.md) ở gốc project.
2. Đọc ba guide, theo thứ tự: [no-editor-unity.md](../guides/no-editor-unity.md),
   [game-feel.md](../guides/game-feel.md), [animation-assets.md](../guides/animation-assets.md).
3. Tìm bước đầu tiên có trạng thái `Chưa` trong bảng dưới. Làm đúng bước đó, không làm trước bước sau.
4. Làm theo "Trình tự làm một bước" (no-editor-unity.md mục 10).
5. Cập nhật cột trạng thái trong bảng dưới và ghi [dev-log.md](../dev-log.md).

Quy tắc khi plan và thực tế lệch nhau:

- Tên object trong plan khác `docs/scenes/`: tin `docs/scenes/` (chạy lại `python3 Tools/scene_inventory.py`
  nếu nghi nó cũ), sửa plan.
- Mã mẫu trong guide không biên dịch trên CI: sửa mã, rồi **sửa luôn mã mẫu trong guide** để lần sau đúng.
- Bước không làm được như mô tả: dừng, ghi lý do vào [deviations.md](../deviations.md), hỏi An. Không tự nghĩ
  ra cách khác rồi làm tiếp trong im lặng.
- Không gộp nhiều bước vào một commit. Không sửa thứ nằm ngoài bước đang làm, kể cả khi thấy nó xấu: ghi vào
  mục "Việc thấy trên đường" ở cuối trang này.

## Trạng thái

Giá trị: `Chưa`, `Xong` (bước không có code để kiểm), `Kiểu` (qua typecheck), `CI` (Unity build xanh), `Web` (An đã chơi thử và đạt), `Bỏ`.

| Bước | Việc | PRD | Trạng thái |
| --- | --- | --- | --- |
| **M0** | **[Chuẩn bị](m0-chuan-bi.md)** | | |
| M0.1 | Commit phần đã có (lõi chuyển động, công cụ, tài liệu, sửa W1) | | Xong (2026-11-16) |
| M0.2 | Cài đặt build: chạy nền, nén gzip | 8.2 | Chưa |
| M0.3 | Khung director, `SceneFind`, lớp gỡ lỗi F3 | 8.2 | Chưa |
| **M1** | **[Lớp chuyển động](m1-lop-chuyen-dong.md)** | | |
| M1.1 | `Tweener`, `Tween`, `MotionRest`, `Motion.Reduced` | A1, A11 | Chưa |
| M1.2 | `ButtonFeel` cho mọi nút | A1, G2 | Chưa |
| M1.3 | `SceneTransitionService`, gom mọi đường chuyển scene | A4, 5.2 | Chưa |
| M1.4 | Panel mở và đóng có chuyển động | A1 | Chưa |
| **M2** | **[Thao tác mượt](m2-thao-tac.md)** | | |
| M2.1 | Bản đồ: kéo có quán tính, zoom, phân biệt kéo với bấm | A5 | Chưa |
| M2.2 | Đồng hồ theo giờ thật, chữ chỉ đổi khi cần | F3 | Chưa |
| M2.3 | Bàn phím và chống bấm đúp | | Chưa |
| M2.4 | Đo và dọn hiệu năng từng scene | 5.2 | Chưa |
| **M3** | **[Chuyển asset animation](m3-asset-animation.md)** | | |
| M3.1 | Luật import, sprite hạt, thư mục `Resources` | 5.2 | Chưa |
| M3.2 | `PatchClipPlayer` và clip đi bộ | 5.2, A3 | Chưa |
| M3.3 | Clip thiền | 5.2, A3 | Chưa |
| M3.4 | Chín clip ở Nhà, tải lười | 5.2, A3 | Chưa |
| M3.5 | Hoà chéo giữa các trạng thái của Pompom | A3 | Chưa |
| M3.6 | Dọn asset cũ, đo lại bộ nhớ và dung lượng | 5.2 | Chưa |
| **M4** | **[Game feel từng màn](m4-game-feel.md)** | | |
| M4.1 | Canvas phủ và `FxBurst` | A2 | Chưa |
| M4.2 | Màn mừng hoàn thành | A2 | Chưa |
| M4.3 | Lật thẻ | A9 | Chưa |
| M4.4 | Game toán | A9 | Chưa |
| M4.5 | Vòng thở khi thiền | A8 | Chưa |
| M4.6 | Bản đồ sống, zoom vào toà nhà | A4, A5 | Chưa |
| M4.7 | Nhà hát theo nhịp | A7 | Chưa |
| M4.8 | Đổi buổi trong ngày bằng hoà màu | A6 | Chưa |
| M4.9 | Màn bắt đầu, rà soát giảm chuyển động | A11 | Chưa |
| M4.10 | Biểu đồ có chuyển động | A10 | Chưa |
| **M5** | **[Phần còn lại của PRD](m5-con-lai.md)** | | |
| M5.1 | Bộ dựng UI bằng code (`UiKit`) | F1 | Chưa |
| M5.2 | Màn Cài đặt và menu tạm dừng | F1, F5, M2 | Chưa |
| M5.3 | Âm thanh: nhạc theo khu, bộ SFX, giấy phép | M4, M5, B3 | Chưa |
| M5.4 | Nhà hát: stem đồng bộ, `Musician` | T1–T3 | Chưa |
| M5.5 | Hòm thư và thư của Pompom | C1, C2, R3 | Chưa |
| M5.6 | Chương và trạng thái làng | C5 | Chưa |
| M5.7 | Onboarding Chương 1, gợi ý viết nhật ký | C3, C4 | Chưa |
| M5.8 | Pomodoro đầy đủ, đồng hồ sống qua scene | F2, F3 | Chưa |
| M5.9 | Một màn Progress | P1–P3, P6, B4 | Chưa |

## Thứ tự và các lượt đẩy lên CI

Mỗi lượt CI mất 16 đến 30 phút, mỗi lượt chơi thử cần An. Gom bước như sau:

| Lượt | Gồm | An kiểm trên web |
| --- | --- | --- |
| 1 | M0.1 → M0.3 | Game vẫn chạy như cũ; F3 hiện bảng gỡ lỗi; trang tải nhanh hơn |
| 2 | M1.1 → M1.4 | Nút nảy, chuyển cảnh mờ, panel trượt |
| 3 | M2.1 → M2.4 | Kéo bản đồ mượt, đồng hồ đúng khi tab ở nền |
| 4 | M3.1 → M3.2 | Pompom đi bộ trông y như cũ |
| 5 | M3.3 → M3.5 | Thiền và Nhà trông y như cũ, chuyển trạng thái mềm hơn |
| 6 | M3.6 | Vẫn y như cũ, tải nhanh hơn nhiều |
| 7 trở đi | Từng bước M4, M5 | theo bước |

Lượt 4 tách riêng vì đây là lần đầu `PatchClipPlayer` và luật import chạy trên Unity thật; nếu sai thì sửa trên
clip nhỏ nhất.

## Điều đã có sẵn khi bắt đầu

| Thứ | Ở đâu | Mức kiểm |
| --- | --- | --- |
| `Ease`, `Spring`, `Smooth`, `TweenState`, `Flipbook`, `PatchClipData` | `Assets/Scripts/Core/Motion/` | Test (22 test trong `MotionTests.cs`) |
| `Countdown` | `Assets/Scripts/Core/Time/Countdown.cs` | Test |
| `extract_patches.py` | `Tools/art/` | Đã chạy trên cả 9 chuỗi ảnh, sai khác 0 |
| `make_fx_sprites.py` | `Tools/art/` | Đã chạy, đã xem ảnh |
| `scene_inventory.py` và `docs/scenes/` | `Tools/`, `docs/scenes/` | Đã chạy trên 11 scene, 23 prefab |
| Mã mẫu `Tweener`, `ButtonFeel`, `SceneTransitionService`, `PatchClipPlayer`, `SceneFind`, `TextureImportRules` | trong ba guide | **Chưa** biên dịch bằng Unity thật |

## Việc thấy trên đường

Ghi ở đây những thứ thấy sai nhưng nằm ngoài bước đang làm. Mỗi dòng: ngày, file, mô tả.

- 2026-11-16, `Assets/Scripts/RenderMap.cs`: gọi `SceneManager.LoadScene("Map")`, scene tên `Map` không tồn tại.
  Không scene nào dùng script này. Xử lý ở M1.3.
- 2026-11-16, `Assets/Scripts/scene_loader.cs`: class `SceneController` tải scene kế tiếp khi bấm chuột bất kỳ
  đâu. Chỉ nằm trong prefab `Scene Transition`, prefab này không scene nào dùng. Xử lý ở M1.3.
- 2026-11-16, scene SchoolCard: lỗi đỏ `DifficultySettings component not found on GameManager` (đã ghi ở
  dev-log bước 8). Có hai object `GameManager` và `Controller` cùng mang script `GameManager`.
- 2026-11-16, tên file có dấu tiếng Việt và dấu cách trong `Assets/Art` (`thiền0000.jpg`, `Khung số.png`,
  `map_0002_nhà-hát.png`). Đang chạy được; không đổi tên vì scene tham chiếu bằng GUID. Asset mới không đặt tên
  kiểu này.
