# Dear Pompom: hướng dẫn cho AI làm việc trong repo này

Game Unity 6000.1.3f1 (2D, URP), bản chơi là WebGL trên GitHub Pages. **Máy này không có Unity Editor.** Bạn
không mở được scene, không thấy được game chạy. Người chơi thử là An, trên bản web.

## Đọc trước khi làm bất cứ việc gì

1. [docs/plan/README.md](docs/plan/README.md): việc tiếp theo là gì (bước đầu tiên có trạng thái `Chưa`).
2. [docs/guides/no-editor-unity.md](docs/guides/no-editor-unity.md): luật làm Unity không có editor.
3. [docs/guides/game-feel.md](docs/guides/game-feel.md): chuyển động và thao tác.
4. [docs/guides/animation-assets.md](docs/guides/animation-assets.md): hình và animation.
5. `docs/scenes/<Scene>.md`: tên object thật trong scene. Không đoán tên.

## Ba lệnh kiểm, chạy trước mỗi commit

```bash
python3 Tools/gen_meta.py --check && dotnet test Tools/CoreTests --nologo && dotnet build Tools/UnityTypecheck --nologo
```

Thiếu `.meta` thì chạy `python3 Tools/gen_meta.py` (không có `--check`).

## Luật cứng

- **Làm đúng một bước của plan mỗi lần.** Không làm trước, không "tiện tay" sửa thứ khác. Thấy gì sai ngoài
  phạm vi: ghi vào mục "Việc thấy trên đường" của `docs/plan/README.md`.
- **Không sửa `*.unity`, `*.prefab`, `*.anim`, `*.controller` bằng tay**, trừ khi bước trong plan ghi rõ một giá
  trị cụ thể. Hành vi mới gắn vào scene bằng code (director).
- **Không đổi tên, không xoá** class, file script, hàm public hay field public mà scene đang dùng. Đổi thân hàm.
- **Logic tính toán nằm trong `Assets/Scripts/Core`** (không `using UnityEngine`) và có test. Lớp Unity chỉ áp số.
- **Chuyển động dùng `Tween`, `Spring`, `Smooth.Damp`**, số lấy từ bảng ở game-feel.md mục 3. Không viết
  `Lerp(a, b, k * Time.deltaTime)`. Không tự chế đường cong.
- **Một thuộc tính, một chủ**: không để hai thứ cùng điều khiển scale (hay màu, vị trí) của một object.
- **Đồng hồ dùng `Countdown`**, không cộng dồn `deltaTime`.
- **Asset mới** nằm trong `Assets/Resources/`, tên `chữ_thường_gạch_dưới`, tải bằng `Resources.Load`, có kiểm `null`.
- **Không thêm package** vào `Packages/manifest.json`.
- **Không tự vẽ hình nhân vật.** Hình mới theo quy trình ở animation-assets.md mục 8, An duyệt.
- **Không commit, không push, không gộp `main` khi An chưa bảo.** Tác giả commit dùng email noreply của GitHub
  (đã đặt trong cấu hình repo). Không push nhánh `main` cũ trên máy (xem dev-log bước 0).
- **Mỗi bước phải cập nhật tài liệu**: trạng thái trong plan, một mục trong `docs/dev-log.md`, các dòng kiểm trong
  `docs/web-test.md`, lỗi tìm thấy vào `docs/bug-log.md`, chỗ làm khác PRD vào `docs/deviations.md`.
- Tài liệu viết tiếng Việt. Code, chú thích trong code, thông điệp commit viết tiếng Anh. Chữ trong game tiếng Anh.

## Khi không chắc

Dừng và hỏi An. Một câu hỏi rẻ hơn một lượt CI 20 phút và một lượt chơi thử hỏng. Đặc biệt phải hỏi khi: bước
trong plan không khớp code thật; cần xoá file; cần đổi hành vi ghi dữ liệu người chơi; cần hình hoặc nhạc mới.

## Bản đồ repo

```
Assets/Scripts/Core/        Logic thuần C#, có test (dotnet test Tools/CoreTests)
Assets/Scripts/Core/Motion/ Ease, Spring, TweenState, Flipbook, PatchClipData
Assets/Scripts/Services/    Dịch vụ sống suốt game, tạo trong Services.EnsureBooted
Assets/Scripts/Motion/      (tạo ở M1) Tweener, ButtonFeel, PatchClipPlayer, FxBurst
Assets/Scripts/Directors/   (tạo ở M0.3) code gắn hành vi vào từng scene
Assets/Scripts/<Khu vực>/   Script cũ gắn trong scene: Home, Forest, School, Theatre, World, Data
Assets/Resources/           Asset tải bằng tên: Clips/, Fx/, Ui/, Sfx/
Assets/Editor/              (tạo ở M3.1) script chỉ chạy trong Unity trên CI
Assets/Tests/EditMode/      Test NUnit, chạy được bằng dotnet
ArtSource/                  (tạo ở M3.6) ảnh gốc từng khung, ngoài Unity
Tools/art/                  extract_patches.py, make_fx_sprites.py
Tools/scene_inventory.py    Sinh docs/scenes/
Tools/gen_meta.py           Sinh .meta
Tools/UnityTypecheck/       Kiểm tra kiểu bằng stub
docs/plan/                  Plan từng bước
docs/guides/                Ba guide
docs/scenes/                Cây object của từng scene (sinh tự động)
```

PRD: Claude Doc "PRD — Dear Pompom 2.0", https://claude.ai/artifact/QWFafPtHwtBrXR54NaQjSm (đọc bằng công cụ docs,
không tải bằng trình duyệt).
