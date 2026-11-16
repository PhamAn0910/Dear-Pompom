# M1. Lớp chuyển động

Mục đích: sau mốc này, **mọi nút, mọi lần chuyển scene, mọi panel** đều có chuyển động chuẩn, mà không cần một
hình vẽ mới nào. Đây là phần tạo ra nhiều cảm giác "mượt" nhất trên mỗi dòng code.

Đọc trước: [game-feel.md](../guides/game-feel.md) mục 1 đến 6.

## M1.1 `Tweener`, `Tween`, `MotionRest`, `Motion.Reduced`

**PRD:** A1, A11. **Cần trước:** M0.3.

**Tạo:** `Assets/Scripts/Motion/Tweener.cs` chứa đúng mã mẫu ở game-feel.md mục 4 (các class `TweenChannel`,
`Motion`, `Tweener`, `MotionRest`, `Tween`). Có thể tách mỗi class một file trong cùng thư mục.

**Sửa:** `Assets/Scripts/Services/Services.cs`:

1. Thêm property `public static Tweener Tweens { get; private set; }`, gán `null` trong `ResetStatics`.
2. Trong `EnsureBooted`, ngay sau khi tạo `Audio`: `Tweens = root.AddComponent<Tweener>();`
3. Sau khi `Progress` đã tải save:
   ```csharp
   Motion.Reduced = Progress.Save.settings.reduceMotion;
   Events.Subscribe<SettingsChanged>(e => Motion.Reduced = e.Settings.reduceMotion);
   ```
   Tên property của bản save trên `UserProgressManager` có thể khác `Save`: mở
   `Assets/Scripts/Data/Managers/UserProgressManager.cs` và dùng đúng tên đang có. Không thêm property mới nếu
   đã có đường lấy `SettingsData`.
4. Trong `DebugOverlay`: `SetLine("tweens", Tweener.ActiveCount.ToString())` và `SetLine("reduced", ...)` lúc
   dựng chuỗi (mỗi 0,5 giây).

**Stub cần thêm:** `Vector2`, `Vector3` (phép nhân với `float`, phép cộng, `LerpUnclamped`), `Quaternion` chưa
cần; `Transform.localScale`, `localPosition`; `RectTransform : Transform` với `anchoredPosition`;
`CanvasGroup` (`alpha`, `blocksRaycasts`, `interactable`); `SpriteRenderer.color`; `UnityEngine.UI.Graphic` với
`color` (cho `Image : Graphic`); `Color.LerpUnclamped`; `Mathf.Sin`, `Mathf.PI`, `Mathf.Min`;
`Time.unscaledDeltaTime`. Thêm `<Compile Include="../../Assets/Scripts/Motion/**/*.cs" />`.

**Kiểm:**

- Test: logic thời gian đã có test trong `MotionTests.cs` (không cần viết thêm).
- Kiểu: 0 lỗi. CI xanh.
- Web: chưa có gì nhìn thấy được ở bước này; F3 hiện dòng `tweens 0`.

**Xong khi:** CI xanh và F3 có dòng `tweens`.

**Bẫy:**

- `UnityEngine.Object` và `System.Object` trùng tên: trong file có `using System;` phải viết đầy đủ
  `UnityEngine.Object` (mã mẫu đã làm vậy).
- So sánh `item.Owner == null` phải là phép `==` của Unity (kiểu khai báo là `UnityEngine.Object`), không phải
  `ReferenceEquals` hay `is null`: object đã bị `Destroy` chỉ "bằng null" với phép `==` của Unity.
- Không đổi `Tweener` thành thứ tự tạo lười (`if (_instance == null) new GameObject...`). Nó được tạo ở
  `EnsureBooted`, hết.

## M1.2 `ButtonFeel` cho mọi nút

**PRD:** A1, G2. **Cần trước:** M1.1.

**Tạo:** `Assets/Scripts/Motion/ButtonFeel.cs` (mã mẫu game-feel.md mục 5).

**Sửa:** `Assets/Scripts/Directors/CommonDirector.cs`:

```csharp
public static void Attach(Scene scene)
{
    foreach (Button button in SceneFind.All<Button>(scene))
        SceneFind.GetOrAdd<ButtonFeel>(button.gameObject);
}
```

Object `DontDestroyOnLoad` (panel gợi ý `PersistentUICanvas` từ StartMenu) nằm trong scene StartMenu lúc nó tải
nên cũng được gắn một lần.

**Stub cần thêm:** namespace `UnityEngine.EventSystems`: `PointerEventData`, các interface `IPointerEnterHandler`,
`IPointerExitHandler`, `IPointerDownHandler`, `IPointerUpHandler` (mỗi cái một hàm `void OnPointerX(PointerEventData eventData)`).

**Kiểm trên web** (thêm vào web-test.md):

```
| 17 | Rê chuột lên nút Play ở StartMenu | Nút to lên nhẹ, mượt, không giật | ButtonFeel |
| 18 | Nhấn giữ nút Play rồi kéo chuột ra ngoài mới thả | Nút lún khi nhấn, trở về cỡ thường, KHÔNG chuyển scene | ButtonFeel |
| 19 | Bấm thật nhanh 10 lần vào một nút tâm trạng trong nhật ký | Nút vẫn đúng cỡ ban đầu sau khi dừng | ButtonFeel, MotionRest |
| 20 | Mở nhật ký, rê lên nút x, bấm đóng, mở lại | Nút x ở cỡ thường, không bị to | ButtonFeel.OnDisable |
```

**Xong khi:** bốn dòng trên đạt ở Home, World, SchoolMath.

**Bẫy:**

- Các toà nhà trên bản đồ là `Button` có `Image` to chồng lên nhau một phần. Hover ×1,04 trên hình lớn là nhiều
  pixel: nếu An thấy quá tay, thêm field `HoverScale` và đặt 1,02 cho các nút có `rect.width` lớn hơn 4 đơn vị
  trong canvas World. Không đổi số chuẩn cho nút thường.
- Thẻ bài (`Card`) không phải `Button`, không nhận `ButtonFeel`. Làm ở M4.3.
- Nút trong `HorizontalLayoutGroup` (nút tâm trạng): scale không làm xô layout, an toàn.

## M1.3 `SceneTransitionService`

**PRD:** A4, 5.2 "một nơi điều khiển". **Cần trước:** M1.1.

**Tạo:** `Assets/Scripts/Services/SceneTransitionService.cs` (mã mẫu game-feel.md mục 6).
Thêm class sự kiện vào `Assets/Scripts/Core/Events/EventBus.cs`:

```csharp
/// <summary>A scene change has started (before the fade). Audio and effects react to this.</summary>
public sealed class SceneChanging
{
    public readonly string From;
    public readonly string To;
    public SceneChanging(string from, string to) { From = from; To = to; }
}
```

Phát nó ở đầu `Run`: `Services.Events.Publish(new SceneChanging(SceneManager.GetActiveScene().name, sceneName));`

**Sửa:**

| File | Sửa gì |
| --- | --- |
| `Services/Services.cs` | Property `Transitions`, tạo bằng `root.AddComponent<SceneTransitionService>()`, đặt `null` trong `ResetStatics` |
| `Assets/Scripts/LoadSceneDelay.cs` | Thân `OnButtonClick`: phát tiếng click như cũ (kiểm `soundEffects != null`), rồi `Services.Transitions.Go(sceneName);`. Xoá coroutine `LoadSceneAfterDelay`. **Giữ** tên class, tên hàm, hai field public |
| `Assets/Scripts/RenderPlace.cs` | Thân `LoadScene`: `Services.Transitions.Go(sceneName);` |
| `Assets/Scripts/RenderPlaceWithoutCard.cs` | Trong `LoadScene`: giữ phần dọn thẻ, thay `SceneManager.LoadScene(sceneName)` bằng `Services.Transitions.Go(sceneName)` |
| `Assets/Scripts/RenderMap.cs` | Đổi `"Map"` thành `"World"` và gọi qua `Services.Transitions.Go` |
| `Assets/Scripts/scene_loader.cs` | Xoá hàm `Update` (bấm đâu cũng nhảy scene). Giữ class `SceneController` và các field |

Sau khi sửa, kiểm không còn đường tải scene nào khác:

```bash
grep -rn "SceneManager.LoadScene" Assets/Scripts
```

Kết quả chỉ được còn trong `SceneTransitionService.cs` (dạng `LoadSceneAsync`) và trong `scene_loader.cs` (code
chết). Chỗ khác thì chuyển sang `Services.Transitions.Go`.

**Stub cần thêm:** `AsyncOperation` (`isDone`), `SceneManager.LoadSceneAsync(string)`,
`Application.CanStreamedLevelBeLoaded(string)`, `Canvas` (`renderMode`, `sortingOrder`), `RenderMode`,
`GraphicRaycaster`, `Image.rectTransform`, `RectTransform.anchorMin/anchorMax/offsetMin/offsetMax`,
`Vector2.zero/one`, `Color.black`, `MonoBehaviour.StartCoroutine(IEnumerator)`.

**Kiểm trên web:**

```
| 21 | StartMenu > Play; World > từng toà nhà; nút Map ở từng scene | Mỗi lần: màn hình mờ sang đen rồi hiện scene mới, không chớp trắng, không khựng | SceneTransitionService |
| 22 | Bấm đúp thật nhanh vào một toà nhà | Chỉ chuyển một lần, không lỗi đỏ | IsBusy, blocksRaycasts |
| 23 | Trong lúc đang mờ, bấm lung tung | Không nút nào ăn | lớp phủ chặn bấm |
| 24 | Chơi lật thẻ vài lượt, bấm Back | Về School, không còn thẻ sót lại trên màn hình | RenderPlaceWithoutCard |
| 25 | Chạy Pomodoro rồi bấm Map giữa chừng | Về World; progress.json có bản ghi "completed": false (như trước) | PomodoroDayNight.OnDisable |
```

**Xong khi:** năm dòng trên đạt; tổng thời gian một lần chuyển dưới 0,8 giây (đếm bằng mắt là đủ).

**Bẫy:**

- `RenderPlaceWithoutCard` huỷ object điều khiển thẻ **trước** khi mờ: người chơi sẽ thấy thẻ biến mất rồi mới
  mờ. Chấp nhận ở bước này; M4.3 chuyển việc dọn sang sau khi màn hình đã đen (nghe `SceneChanging` rồi chờ
  `FadeOut`).
- Âm thanh click phát ngay, không chờ: giữ nguyên.
- `SceneVisitLogger` nghe `sceneLoaded`, không bị ảnh hưởng.

## M1.4 Panel mở và đóng

**PRD:** A1. **Cần trước:** M1.1.

**Tạo:** `Assets/Scripts/Motion/PanelMotion.cs`:

```csharp
public static class PanelMotion
{
    public static void Show(GameObject panel);   // bật object, scale 0,92 -> 1, alpha 0 -> 1, 220 ms OutCubic
    public static void Hide(GameObject panel);   // chặn bấm ngay, alpha -> 0, scale -> 0,96, 160 ms InCubic, rồi SetActive(false)
}
```

Cả hai tự thêm `CanvasGroup` nếu chưa có (`SceneFind.GetOrAdd<CanvasGroup>`). `Show` đặt
`blocksRaycasts = true`. Gọi `Show` trên panel đang mở, hoặc `Hide` trên panel đang đóng, không làm gì.
Mã lõi lấy từ ví dụ "Mở panel" và "Đóng panel" ở game-feel.md mục 4.

**Sửa:**

| File | Dòng hiện tại | Đổi thành |
| --- | --- | --- |
| `Home/JournalUIManager.cs`, `ToggleNotebook` | `notebookPanel.SetActive(show);` | `if (show) PanelMotion.Show(notebookPanel); else PanelMotion.Hide(notebookPanel);` |
| `Data/RecommendationSystem.cs`, `Display` | `recommendationUIPanel.SetActive(true);` | `PanelMotion.Show(recommendationUIPanel);` |
| `Data/RecommendationSystem.cs`, `HideRecommendationUI` | `recommendationUIPanel.SetActive(false);` | `PanelMotion.Hide(recommendationUIPanel);` |

Các dòng `SetActive(false)` chạy lúc khởi tạo (`Start`, `Awake`) **giữ nguyên**: lúc đó không cần animation.

**Kiểm trên web:**

```
| 26 | Home > bấm nút sổ > bấm x | Sổ phóng ra và hiện dần; khi đóng thì mờ đi rồi mới biến mất | PanelMotion, JournalUIManager |
| 27 | Mở và đóng sổ thật nhanh 5 lần | Sổ không kẹt nửa chừng; trạng thái cuối đúng với lần bấm cuối | Tweener: tween cũ bị huỷ |
| 28 | Làm một hoạt động rồi đổi scene để thư Pompom hiện | Hộp thư phóng ra mềm; bấm Got it thì mờ đi | RecommendationSystem |
```

**Xong khi:** ba dòng đạt.

**Bẫy:**

- `notebookPanel` có scale gốc khác 1 (xem `docs/scenes/Home.md`): `Tween.Scale` nhân với scale nghỉ qua
  `MotionRest`, nên **`PanelMotion.Show` phải gọi `MotionRest.Of(panel.transform)` trước lần đầu đổi scale**,
  tức trước cả khi đặt scale 0,92. Dùng `Tween.Scale` là tự đúng.
- Sau `Hide`, `ToggleNotebook(true)` ngay lập tức phải mở được: `Show` gọi `SetActive(true)` và đè tween đóng.
  `done` của tween đóng không chạy vì tween đã bị huỷ.
- `RefreshEntriesList()` chạy trong `ToggleNotebook(true)` dựng lại danh sách: để nguyên thứ tự hiện có (bật
  panel trước, rồi mới dựng).
