# Làm Unity khi không có Unity Editor

Máy phát triển không cài Unity. Mọi thứ thường làm bằng chuột trong editor (kéo object vào scene, gán
sprite trong Inspector, tạo Animator) phải làm bằng **code chạy lúc runtime** hoặc **file sinh bằng script**.
Trang này là luật chung. Đọc hết một lần trước khi làm bất kỳ bước nào trong [plan](../plan/README.md).

## 1. Bốn mức kiểm chứng

Mỗi thay đổi phải đi qua các mức này theo thứ tự. Không nhảy cóc: mức sau chậm hơn mức trước rất nhiều.

| Mức | Lệnh | Bắt được | Không bắt được | Thời gian |
| --- | --- | --- | --- | --- |
| Test | `dotnet test Tools/CoreTests --nologo` | Sai logic trong `Assets/Scripts/Core` | Mọi thứ dùng `UnityEngine` | 5 giây |
| Kiểu | `dotnet build Tools/UnityTypecheck --nologo` | Lỗi cú pháp, gõ sai tên, gọi sai vào Core | API Unity thật (stub do ta tự viết, có thể sai) | 5 giây |
| CI | push nhánh `v2/**`, xem GitHub Actions | Unity thật biên dịch và build WebGL | Hành vi khi chạy | 16 đến 30 phút |
| Web | An chơi bản trên GitHub Pages (sau khi gộp vào `main`) | Hành vi thật, cảm giác, hiệu năng | | cần người |

Hệ quả cho cách làm việc:

- **Đẩy logic xuống Core.** Cái gì tính được bằng số (thời gian, easing, khung hình thứ mấy, toạ độ) thì viết
  trong `Assets/Scripts/Core` và viết test. Lớp Unity chỉ còn việc "lấy số, gán vào transform".
- **Gom nhiều bước rồi mới push.** Một lượt CI mất ít nhất 16 phút. Làm xong 2 đến 4 bước nhỏ liên quan, qua
  Test và Kiểu, rồi mới push một lần.
- **Mỗi bước phải tự ghi cách kiểm trên web.** Thêm dòng vào [web-test.md](../web-test.md) để An biết bấm gì,
  phải thấy gì. Không có dòng đó thì coi như bước chưa xong.
- **Log có tiền tố.** Mọi `Debug.Log` mới bắt đầu bằng `[DearPompom]` để lọc trong Console của trình duyệt.

Đọc lỗi CI:

```bash
gh run list -R PhamAn0910/Dear-Pompom -L 3
```

```bash
gh run view <id> -R PhamAn0910/Dear-Pompom --log-failed | grep -E "error CS|Error building|Exception" | head -40
```

## 2. Việc được làm và không được làm với file Unity

| Loại file | Được | Không được |
| --- | --- | --- |
| `*.cs` | Tạo, sửa tự do | Đổi tên class hoặc tên hàm public mà scene đang gọi (xem mục 4) |
| `*.meta` | Để `python3 Tools/gen_meta.py` sinh | Sửa tay GUID; xoá meta của file còn dùng |
| `*.unity`, `*.prefab` | Đọc (qua `docs/scenes/`). Sửa **một giá trị số hoặc bool** của component có sẵn, khi plan ghi rõ | Thêm, xoá, di chuyển object hoặc component. Thêm tham chiếu `{fileID, guid}`. Unity không báo lỗi khi YAML sai, nó lặng lẽ bỏ object đó |
| `*.anim`, `*.controller` | Đọc | Tạo mới bằng tay. Dùng `PatchClipPlayer` và tween bằng code thay thế |
| `ProjectSettings/*.asset` | Sửa một dòng `tên: giá trị` khi plan ghi rõ | Thêm khối mới, đổi thứ tự |
| `Packages/manifest.json` | Không đụng nếu plan không yêu cầu | Thêm package bên thứ ba (không stub được, không test được) |

Sau khi thêm file trong `Assets/` luôn chạy:

```bash
python3 Tools/gen_meta.py
```

Sau khi sửa bất kỳ scene hay prefab nào (hiếm), hoặc muốn biết tên object:

```bash
python3 Tools/scene_inventory.py
```

## 3. Biết scene có gì: `docs/scenes/`

`Tools/scene_inventory.py` đọc YAML của 11 scene và 23 prefab, ghi ra `docs/scenes/<Tên>.md`: cây object, vị
trí, component, sprite, hàm mà mỗi nút gọi. **Đây là nguồn duy nhất để biết tên object.** Không đoán tên.

Ví dụ, trích `docs/scenes/Home.md`:

```
- `Canvas` ... Canvas renderMode=0; CanvasScaler scaleMode=1 reference=1920x1080 match=0
  - `SetupUI`
    - `StartButton` — ui pos (613, 268): Image `Art/House/UI Homepage/Start.png`; Button onClick -> PlayClick
- `Background Sprites`
  - `morningstudy0000_0`: SpriteRenderer `...morningstudy0000.jpg`; Animator `...morningstudy0000_0.controller`
```

Đường dẫn của nút Start là `Canvas/SetupUI/StartButton`. Dòng `Script<306cc8c2>` là script của package chưa có
trong bảng tên của công cụ; bỏ qua được.

Ba sự thật về scene hiện tại mà code mới phải biết:

- **Canvas không đồng nhất.** Home, StartMenu, Progress dùng chuẩn 1920×1080; các scene còn lại dùng 800×600;
  World dùng canvas World Space. Vì vậy **không gắn UI mới vào canvas có sẵn**. UI mới nằm trong canvas riêng
  do code tạo (mục 6).
- **Camera** của mọi scene là orthographic, size 5: màn hình cao 10 đơn vị, rộng 17,78 đơn vị ở tỉ lệ 16:9.
  Ảnh nền 1920×1080 ở 100 pixel mỗi đơn vị được đặt scale khoảng 0,926 để vừa khít.
- **Object tắt sẵn** (ghi `(inactive)`) không tìm được bằng `GameObject.Find`. Phải đi từ cha đang bật:
  `parent.Find("tên con")` của `Transform` tìm được cả con đang tắt.

## 4. Giữ nguyên tên mà scene đang gọi

Scene lưu tham chiếu tới script bằng GUID trong file `.meta`, và lưu tên hàm của nút bằng chuỗi. Ví dụ mọi nút
chuyển scene gọi `LoadSceneDelay.OnButtonClick`. Vì vậy:

- **Muốn đổi hành vi của mọi nút chuyển scene: sửa thân hàm `LoadSceneDelay.OnButtonClick`**, giữ nguyên tên
  class, tên file, tên hàm, tên các field public (`sceneName`, `soundEffects`). Không cần đụng scene nào.
- Không xoá, không đổi tên file script có trong `docs/scenes/`. Muốn bỏ một script: để thân class rỗng và ghi
  chú, tới khi có editor mới gỡ khỏi scene.
- Trước khi đổi tên hoặc xoá một thành viên public: `grep -rn "TênHàm" Assets/Scripts Assets/Scenes Assets/Prefab`.
  Lỗi C2 trong [bug-log.md](../bug-log.md) là do bỏ bước này.

## 5. Gắn hành vi vào scene bằng code: "director"

Không thêm được component vào scene bằng editor, nên mỗi scene có một **director**: một class tĩnh chạy một lần
sau khi scene tải xong, tìm object theo đường dẫn, rồi `AddComponent` và nối sự kiện.

```
Services.EnsureBooted()                      (đã có, chạy trước scene đầu tiên)
  └─ SceneDirectors.Install()                (mới) đăng ký SceneManager.sceneLoaded
        └─ khi scene "Home" tải xong  → HomeDirector.Attach(scene)
        └─ mọi scene                  → CommonDirector.Attach(scene)   (nút bấm, chuyển cảnh)
```

Quy tắc viết director:

1. Tìm object bằng `SceneFind` (mã mẫu bên dưới), không bằng `GameObject.Find` trần. Thiếu object thì
   `SceneFind` ghi `[DearPompom] missing object 'Canvas/X' in scene 'Home'` và trả `null`; director phải
   kiểm `null` rồi bỏ qua phần đó. **Một object thiếu không được làm hỏng cả scene.**
2. Chỉ tìm trong `Attach`, lưu kết quả vào field. Không bao giờ tìm trong `Update`.
3. Director không chứa logic chơi. Nó chỉ nối: "nút này dùng ButtonFeel", "object này thay bằng PatchClipPlayer".
4. Mỗi director một file: `Assets/Scripts/Directors/<Scene>Director.cs`.
5. `Attach` phải chạy lại được nhiều lần (scene được tải lại mỗi lần người chơi quay về): kiểm
   `GetComponent<T>() == null` trước khi `AddComponent<T>()`.

Mã mẫu `Assets/Scripts/Directors/SceneFind.cs` (chưa qua Unity thật; phải qua Kiểu và CI):

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DearPompom
{
    /// <summary>Finds scene objects by the paths listed in docs/scenes/. Never throws.</summary>
    public static class SceneFind
    {
        /// <summary>
        /// "Canvas/SetupUI/StartButton": the first segment is a root object of the scene, the rest
        /// are children. Finds inactive objects too. Returns null and logs when a segment is missing.
        /// </summary>
        public static Transform ByPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0]) { current = root.transform; break; }
            }
            for (int i = 1; current != null && i < parts.Length; i++)
            {
                current = current.Find(parts[i]); // Transform.Find also returns inactive children
            }
            if (current == null)
                Debug.LogWarning("[DearPompom] missing object '" + path + "' in scene '" + scene.name + "'");
            return current;
        }

        public static T ByPath<T>(Scene scene, string path) where T : Component
        {
            Transform found = ByPath(scene, path);
            if (found == null) return null;
            T component = found.GetComponent<T>();
            if (component == null)
                Debug.LogWarning("[DearPompom] '" + path + "' has no " + typeof(T).Name + " in scene '" + scene.name + "'");
            return component;
        }

        /// <summary>Every component of a type in the scene, including on inactive objects.</summary>
        public static System.Collections.Generic.List<T> All<T>(Scene scene) where T : Component
        {
            var result = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        public static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }
    }
}
```

Lưu ý `Transform.Find("a/b")` hiểu dấu `/` là phân cấp, nên object có tên chứa `/` (ví dụ `Pause/Resume` trong
Theatre) không tìm được bằng đường dẫn. Với chúng, dùng `SceneFind.All<Button>(scene)` rồi so `name`.

## 6. Asset không gán được trong Inspector thì lấy bằng `Resources`

Không kéo thả được, nên mọi asset mà code mới cần phải nằm trong `Assets/Resources/` và được tải bằng tên:

```csharp
Texture2D sheet = Resources.Load<Texture2D>("Clips/home_night_study/home_night_study_sheet0");
TextAsset json = Resources.Load<TextAsset>("Clips/home_night_study/home_night_study");
AudioClip pop = Resources.Load<AudioClip>("Sfx/ui_pop");
```

Quy tắc:

- Đường dẫn tính từ sau `Resources/`, **không có đuôi file**, phân biệt hoa thường trên bản web.
- Tên file chỉ dùng `a-z`, `0-9`, `_`. Không dấu cách, không tiếng Việt có dấu (tên như `thiền0000.jpg` gây
  lỗi so tên giữa macOS và Linux của CI).
- Mọi thứ trong `Resources/` đều vào bản build, kể cả không ai dùng. Xoá file thừa.
- Tải một lần, giữ lại. Không gọi `Resources.Load` trong `Update`.
- Luôn kiểm `null` và log tên đường dẫn khi thiếu.

Cài đặt import của ảnh (kiểu Sprite, tắt mipmap, không co giãn) được ép bằng một `AssetPostprocessor` theo thư
mục, xem [animation-assets.md](animation-assets.md) mục 4. Unity trên CI chạy nó khi import, nên không cần viết
tay phần cài đặt trong `.meta`.

UI mới (màn Cài đặt, hòm thư, lớp phủ chuyển cảnh) dựng hoàn toàn bằng code trong một canvas riêng:

```csharp
var go = new GameObject("[DearPompom UI]");
Object.DontDestroyOnLoad(go);
var canvas = go.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 500;                       // trên mọi canvas của scene (cao nhất hiện có là 100)
var scaler = go.AddComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
scaler.matchWidthOrHeight = 0.5f;
go.AddComponent<GraphicRaycaster>();
```

Thứ tự lớp đã định: scene 0 đến 100, UI của game 500, lớp chuyển cảnh 900, lớp gỡ lỗi 1000.

## 7. Thêm API Unity vào bộ stub

`Tools/UnityTypecheck/UnityStubs.cs` là bản khai báo rỗng của những API Unity mà script đang dùng. Khi script
mới gọi một API chưa có, `dotnet build Tools/UnityTypecheck` báo `CS0246` hoặc `CS1061`. Khi đó:

1. Thêm **đúng** khai báo còn thiếu vào `UnityStubs.cs`, cùng namespace với Unity. Chữ ký lấy từ tài liệu Unity
   6 (docs.unity3d.com/6000.1/Documentation/ScriptReference), không tự nghĩ.
2. Thân hàm để rỗng hoặc trả giá trị mặc định. Stub không cần chạy đúng, chỉ cần đúng kiểu.
3. Thêm file script mới vào danh sách `<Compile Include=...>` trong `Tools/UnityTypecheck/UnityTypecheck.csproj`.
   Thư mục mới thì thêm cả thư mục: `<Compile Include="../../Assets/Scripts/Motion/**/*.cs" />`.
4. Stub sai thì Kiểu vẫn xanh nhưng CI đỏ. Khi CI báo lỗi API, sửa cả script lẫn stub cho khớp Unity thật.

Những API có bẫy về chữ ký, hay bị viết sai:

| API | Đúng | Sai thường gặp |
| --- | --- | --- |
| Tìm object | `Object.FindFirstObjectByType<T>()`, `FindObjectsByType<T>(FindObjectsSortMode.None)` | `FindObjectOfType` (đã lỗi thời, cảnh báo trong Unity 6) |
| Chuột | `UnityEngine.InputSystem.Mouse.current.position.ReadValue()`, luôn kiểm `Mouse.current != null` | `Input.mousePosition` (kiểu cũ; project đang bật cả hai nhưng code thống nhất dùng Input System mới) |
| Phím | `Keyboard.current.f3Key.wasPressedThisFrame` | `Input.GetKeyDown` |
| Sự kiện con trỏ | implement `IPointerDownHandler` (namespace `UnityEngine.EventSystems`) | `OnMouseDown` trên object UI |
| Sprite từ texture | `Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect)` | Bỏ `FullRect`: Unity dò viền trong suốt, rất chậm và cần texture đọc được |
| Chữ | `TMPro.TextMeshProUGUI`, gán `.text` | `UnityEngine.UI.Text` |
| Tải scene | `SceneManager.LoadSceneAsync(name)` rồi `allowSceneActivation` | `Application.LoadLevel` |
| Thời gian thật | `Time.unscaledDeltaTime`, `Time.realtimeSinceStartupAsDouble` | Dùng `Time.deltaTime` cho UI (đứng khi `timeScale = 0`) |

Project đặt `activeInputHandler: 2` (bật cả hai hệ input), nên `Input.*` kiểu cũ vẫn chạy. Dù vậy code mới
chỉ dùng Input System mới, để sau này tắt hệ cũ không phải sửa lại. Kiểm trước khi push:

```bash
grep -rnE "Input\.(Get|mouse|touch|anyKey)" Assets/Scripts && echo "CÓ INPUT CŨ, NÊN SỬA"
```

## 8. Bẫy riêng của bản WebGL

| Bẫy | Hậu quả | Cách tránh |
| --- | --- | --- |
| Tab ở nền thì trình duyệt ngừng gọi khung hình | Đồng hồ cộng dồn `deltaTime` bị đứng hoặc nhảy | Đồng hồ dùng `DearPompom.Core.Countdown` (đo theo giờ thật) |
| Khung hình đầu sau khi quay lại tab có `deltaTime` rất lớn | Tween nhảy, vật thể văng | `Tweener` kẹp `dt` tối đa 0,1 giây; `Spring` và `TweenState` đã chịu được |
| Âm thanh bị khoá tới lần bấm đầu tiên | Nhạc không phát ở màn đầu | Không phát nhạc trước cú bấm đầu; `AudioManager` đã xử lý |
| Không có luồng phụ | `Thread`, `Task.Run` không chạy | Dùng coroutine |
| File hệ thống là IndexedDB, ghi bất đồng bộ | Mất save nếu đóng tab ngay | Chỉ ghi qua `SaveStore` (đã gọi `FS.syncfs`) |
| Bộ nhớ có trần, cấp phát lớn làm đứng hình | Giật khi vào scene nặng | Theo ngân sách texture trong [animation-assets.md](animation-assets.md) |
| `Shader.Find("...")` trả `null` nếu shader không có trong build | Vật thể màu hồng hoặc vô hình | Không tạo `Material` bằng `Shader.Find`. Dùng `SpriteRenderer` và `Image` mặc định, hoặc lấy `sharedMaterial` từ một renderer có sẵn trong scene |
| Tên file khác hoa thường | Chạy trên macOS, hỏng trên web | Tên asset mới toàn chữ thường |
| `System.IO.File` với đường dẫn ngoài `persistentDataPath` | Lỗi | Dữ liệu đọc-chỉ dùng `Resources` + `TextAsset` |

## 9. Lớp gỡ lỗi trên màn hình

Không có editor nên không có Profiler, Hierarchy hay Stats. Bước M0.3 của plan tạo `DebugOverlay`: bấm **F3**
để hiện tên scene, FPS trung bình và thấp nhất trong 2 giây, số tween đang chạy, bộ nhớ texture ước lượng của
các clip đang tải. Mọi bước liên quan tới hiệu năng trong plan đều kiểm bằng lớp này. Khi thêm hệ thống mới có
thứ cần đếm (hạt, clip), cho nó đăng ký một dòng vào `DebugOverlay`.

## 10. Trình tự làm một bước, từ đầu tới cuối

1. Đọc bước trong plan và các mục guide mà bước dẫn tới.
2. Đọc `docs/scenes/<Scene>.md` của scene liên quan. Chép đúng tên object.
3. Nếu có phần tính toán: viết vào Core, viết test, chạy `dotnet test Tools/CoreTests --nologo`.
4. Viết script Unity. Thêm vào `UnityTypecheck.csproj`. Bổ sung stub nếu thiếu. Chạy
   `dotnet build Tools/UnityTypecheck --nologo` tới khi 0 lỗi.
5. `python3 Tools/gen_meta.py`. Chạy lệnh grep `Input.` ở mục 7.
6. Cập nhật tài liệu: một mục trong [dev-log.md](../dev-log.md), các dòng kiểm mới trong
   [web-test.md](../web-test.md), ô trạng thái của bước trong [plan](../plan/README.md).
7. Commit nhỏ, một bước một commit, thông điệp tiếng Anh, nói rõ mức kiểm chứng đã đạt.
8. Khi An yêu cầu push: push nhánh `v2/...`, chờ CI, sửa tới xanh. Chỉ gộp `main` khi An đồng ý.
9. Sau khi An chơi thử: ghi kết quả vào dev-log, lỗi vào [bug-log.md](../bug-log.md).

Không tự ý push hay gộp `main`: đó là hành động ra ngoài máy, chỉ làm khi An nói.
