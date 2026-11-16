# M0. Chuẩn bị

Mục đích: có nền để mọi bước sau chỉ việc thêm, và có công cụ để nhìn thấy game đang làm gì trên web.
Không bước nào ở đây đổi thứ người chơi thấy, trừ thời gian tải.

## M0.1 Commit phần đã có

**Cần trước:** không. **Cần An đồng ý trước khi commit.**

Thư mục làm việc đang có thay đổi chưa commit từ hai phiên:

| Nhóm | File |
| --- | --- |
| Sửa lỗi W1 (phiên trước) | `Assets/Scripts/School/MathGameManager.cs`, `Tools/UnityTypecheck/UnityStubs.cs`, `Tools/UnityTypecheck/UnityTypecheck.csproj`, một dòng trong `docs/bug-log.md` và `docs/dev-log.md` |
| Lõi chuyển động | `Assets/Scripts/Core/Motion/*`, `Assets/Scripts/Core/Time/Countdown.cs`, `Assets/Tests/EditMode/MotionTests.cs` và các `.meta` |
| Công cụ | `Tools/art/extract_patches.py`, `Tools/art/make_fx_sprites.py`, `Tools/scene_inventory.py` |
| Tài liệu | `CLAUDE.md`, `AGENTS.md`, `docs/guides/*`, `docs/plan/*`, `docs/scenes/*`, `docs/README.md` |

**Làm:**

1. `git status --short` và `git diff --stat` để thấy đúng các nhóm trên, không có gì lạ.
2. Chạy cả ba kiểm tra, phải xanh hết:
   ```bash
   python3 Tools/gen_meta.py --check && dotnet test Tools/CoreTests --nologo && dotnet build Tools/UnityTypecheck --nologo
   ```
3. Bốn commit riêng, theo đúng bốn nhóm trên, theo thứ tự bảng. Trong commit W1, thay chữ `(commit này)` ở
   dòng W1 của `docs/bug-log.md` bằng mã commit thật (commit xong, lấy mã, sửa, commit bổ sung
   `Docs: record the W1 commit id`).
4. Tác giả commit phải là địa chỉ noreply của GitHub (đã đặt sẵn trong `git config user.email` của repo). Kiểm:
   `git log -1 --format=%ae` phải kết thúc bằng `@users.noreply.github.com`.

**Xong khi:** `git status --short` rỗng; `dotnet test` báo 104 test đạt.

**Bẫy:** không `git add -A` mù. Thư mục `Library/`, `obj/`, `Tools/**/bin` đã bị bỏ qua bởi `.gitignore`; nếu
`git status` hiện chúng thì dừng lại.

## M0.2 Cài đặt build: chạy nền và nén

**PRD:** 8.2 (khởi động dưới 5 giây). **Cần trước:** M0.1.

**Sửa:** `ProjectSettings/ProjectSettings.asset`, đúng ba dòng, không đụng dòng nào khác:

| Dòng hiện tại | Đổi thành | Tác dụng |
| --- | --- | --- |
| `  runInBackground: 0` | `  runInBackground: 1` | Game không tự dừng khi cửa sổ mất tiêu điểm |
| `  webGLCompressionFormat: 2` | `  webGLCompressionFormat: 1` | Nén gzip các file build (2 là không nén) |
| `  webGLDecompressionFallback: 0` | `  webGLDecompressionFallback: 1` | Kèm bộ giải nén bằng JavaScript, vì GitHub Pages không gửi header `Content-Encoding` cho file của Unity |

Giữ nguyên hai dấu cách đầu dòng. Dùng công cụ sửa chuỗi chính xác, không viết lại file.

**Kiểm:**

- `git diff ProjectSettings/ProjectSettings.asset` chỉ có đúng 3 dòng đổi.
- CI: bước "Prepare site" in danh sách file; phải thấy đuôi `.unityweb` (ví dụ `DearPompom.data.unityweb`).
- Web: trang mở được, vào tới StartMenu. DevTools > Network: ghi tổng dung lượng tải vào dev-log (trước và sau).

Dòng thêm vào `docs/web-test.md`:

```
| 15 | Mở trang với DevTools > Network, tắt cache | Trang vào tới StartMenu; các file lớn có đuôi .unityweb; ghi tổng MB | M0.2, ProjectSettings |
```

**Xong khi:** trang chạy và dung lượng tải nhỏ hơn trước.

**Nếu hỏng:** trang đứng ở thanh tải hoặc Console báo lỗi giải nén thì đổi lại `webGLCompressionFormat: 2` và
`webGLDecompressionFallback: 0`, giữ `runInBackground: 1`, ghi vào deviations.md.

**Bẫy:** `runInBackground` không làm trình duyệt gọi khung hình khi tab bị ẩn. Đồng hồ vẫn phải chuyển sang
`Countdown` ở M2.2.

## M0.3 Khung director, `SceneFind`, lớp gỡ lỗi

**PRD:** 8.2. **Cần trước:** M0.1. **Đọc:** no-editor-unity.md mục 5, 7, 9.

**Tạo:**

| File | Nội dung |
| --- | --- |
| `Assets/Scripts/Directors/SceneFind.cs` | Chép mã mẫu ở no-editor-unity.md mục 5 |
| `Assets/Scripts/Directors/SceneDirectors.cs` | Đăng ký `SceneManager.sceneLoaded`, gọi director theo tên scene |
| `Assets/Scripts/Directors/CommonDirector.cs` | `public static void Attach(Scene scene)`; lúc này chỉ log `[DearPompom] scene <tên>: <n> buttons` |
| `Assets/Scripts/Services/DebugOverlay.cs` | Bảng F3 |

**Sửa:** `Assets/Scripts/Services/Services.cs`: trong `EnsureBooted`, sau dòng `root.AddComponent<SceneVisitLogger>();`
thêm `root.AddComponent<DebugOverlay>();` và `SceneDirectors.Install();`. Thêm việc gỡ đăng ký vào `ResetStatics`.

`SceneDirectors`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DearPompom
{
    /// <summary>Runs the per-scene wiring code after each scene loads. See docs/guides/no-editor-unity.md.</summary>
    public static class SceneDirectors
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Uninstall() // called from Services.ResetStatics
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _installed = false;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // One failing director must not stop the others, or the scene itself.
            Run(scene, "Common", CommonDirector.Attach);
            switch (scene.name)
            {
                // Later steps add lines here, for example:
                // case "Home": Run(scene, "Home", HomeDirector.Attach); break;
            }
        }

        private static void Run(Scene scene, string name, System.Action<Scene> attach)
        {
            try { attach(scene); }
            catch (System.Exception e)
            {
                Debug.LogError("[DearPompom] " + name + "Director failed in scene '" + scene.name + "'");
                Debug.LogException(e);
            }
        }
    }
}
```

Vì `Install` chạy trong `BeforeSceneLoad`, sự kiện `sceneLoaded` của scene đầu tiên vẫn tới.

`DebugOverlay`: dùng `OnGUI` (IMGUI), vì nó không cần dựng canvas và không thể bị UI khác che.

- Bật tắt: `Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame`.
- Mỗi khung cộng `Time.unscaledDeltaTime` vào cửa sổ 2 giây: tính FPS trung bình và FPS thấp nhất
  (`1 / dt lớn nhất`) của cửa sổ, cập nhật chuỗi hiển thị **mỗi 0,5 giây**, không dựng chuỗi mỗi khung.
- `public static void SetLine(string key, string value)`: hệ thống khác đăng ký dòng của mình (ví dụ
  `SetLine("tweens", "3")`). Lưu trong `Dictionary<string, string>`.
- Dòng mặc định: tên scene, FPS trung bình và thấp nhất, kích thước màn hình, `Motion.Reduced` (thêm ở M1.1).
- `OnGUI` thoát ngay ở dòng đầu khi đang tắt. Khi bật: `GUI.Box` nền tối + `GUI.Label` ở góc trên trái, rộng 360.

**Stub cần thêm** (`Tools/UnityTypecheck/UnityStubs.cs`): `Scene.GetRootGameObjects()`, `Transform.Find`,
`Component.GetComponentsInChildren<T>(bool)`, `GameObject.GetComponent<T>()`, `GameObject.AddComponent<T>()` (nếu
chưa có), `SceneManager.sceneLoaded` (`event UnityAction<Scene, LoadSceneMode>`), `Debug.LogWarning`,
`Debug.LogError`, `Debug.LogException`, `GUI.Box`, `GUI.Label`, `Rect`, `Screen.width/height`,
namespace `UnityEngine.InputSystem` với `Keyboard.current.f3Key.wasPressedThisFrame`. Thêm
`<Compile Include="../../Assets/Scripts/Directors/**/*.cs" />` vào `UnityTypecheck.csproj`.

**Kiểm:**

- Kiểu: 0 lỗi.
- CI xanh.
- Web: Console có `[DearPompom] scene StartMenu: 2 buttons` (số nút đúng theo `docs/scenes/StartMenu.md`, tính
  cả nút đang ẩn); đổi scene thì có dòng mới. Bấm F3: hiện bảng có FPS; bấm lần nữa: mất.

Dòng thêm vào `docs/web-test.md`:

```
| 16 | Bấm F3 ở StartMenu, rồi vào World | Bảng hiện tên scene và FPS; tên scene đổi theo; F3 lần nữa thì ẩn | DebugOverlay, SceneDirectors |
```

**Xong khi:** bảng F3 chạy trên web và Console có dòng đếm nút ở mỗi scene.

**Bẫy:**

- `Keyboard.current` có thể `null` (thiết bị không có bàn phím): luôn kiểm.
- Trình duyệt giữ F3 cho "tìm kiếm" trên một số máy. Nếu An báo F3 không ăn, đổi sang phím `` ` `` (`backquoteKey`).
- Không để `OnGUI` chạy logic khi đang tắt: Unity gọi nó nhiều lần mỗi khung.
