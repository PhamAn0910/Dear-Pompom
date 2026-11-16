# M2. Thao tác mượt

Mục đích: những chỗ người chơi **điều khiển trực tiếp** (kéo bản đồ, đồng hồ, bàn phím) phản ứng đúng và êm.
Đọc trước: [game-feel.md](../guides/game-feel.md) mục 7, 8, 9.

## M2.1 Bản đồ: kéo có quán tính, zoom, phân biệt kéo với bấm

**PRD:** A5 (nền cho bản đồ sống). **Cần trước:** M1.3.

**Sửa:** `Assets/Scripts/World/PanZoom.cs`: viết lại thân class theo thuật toán ở game-feel.md mục 7. Giữ tên
class `PanZoom`, giữ bốn field public `zoomOutMin`, `zoomOutMax`, `background`, `panSpeed` (scene World đã gán
giá trị cho chúng; `panSpeed` không còn dùng nhưng phải còn đó).

**Tạo:** `Assets/Scripts/World/MapDrag.cs`:

```csharp
namespace DearPompom
{
    /// <summary>Lets buttons under the map tell a drag from a click. Written by PanZoom, read by LoadSceneDelay.</summary>
    public static class MapDrag
    {
        private static int _lastDragFrame = -10;
        public static void MarkDragEnded() { _lastDragFrame = UnityEngine.Time.frameCount; }
        public static bool JustDragged { get { return UnityEngine.Time.frameCount - _lastDragFrame <= 1; } }
    }
}
```

**Sửa:** `Assets/Scripts/LoadSceneDelay.cs`: dòng đầu của `OnButtonClick`: `if (DearPompom.MapDrag.JustDragged) return;`

Phần tính toán thuần đưa xuống Core để test được. **Tạo** `Assets/Scripts/Core/Motion/PanMath.cs`:

| Hàm | Việc |
| --- | --- |
| `float ClampAxis(float camCentre, float viewSize, float min, float max)` | Kẹp tâm camera để khung nhìn nằm trong `[min, max]`. Nếu khung nhìn rộng hơn ảnh thì trả về tâm ảnh |
| `float MaxOrthoSize(float bgWidth, float bgHeight, float aspect, float wanted)` | `min(wanted, bgHeight / 2, bgWidth / (2 × aspect))` |
| `float ZoomStep(float size, float scrollSign, float min, float max)` | `Clamp(size × (1 − 0.1 × scrollSign), min, max)` |

Thêm test `PanMathTests` trong `Assets/Tests/EditMode/`: khung nhìn nhỏ hơn ảnh (kẹp hai phía), khung nhìn lớn
hơn ảnh (về tâm), zoom tới trần và sàn, `scrollSign = 0` không đổi gì.

Số dùng trong `PanZoom` (hằng số trong class, không phải field public):

| Hằng | Giá trị |
| --- | --- |
| Ngưỡng coi là kéo | 8 pixel màn hình |
| Nửa đời làm mượt vận tốc khi kéo | 0,05 s |
| Nửa đời quán tính sau khi thả | 0,12 s |
| Nửa đời zoom | 0,08 s |
| Vận tốc coi là dừng | 0,01 đơn vị/s |
| Mỗi nấc cuộn | 10% |

`zoomOutMin`, `zoomOutMax` đọc từ field của scene; nếu `zoomOutMin < 2` thì dùng 2 (giá trị scene là 1, quá gần).

**Stub cần thêm:** `Camera` (`orthographicSize`, `aspect`, `ScreenToWorldPoint`), `Camera.main`,
`Bounds` (`min`, `max`, `size`), `SpriteRenderer.bounds`, `Mouse.current` với `leftButton`
(`wasPressedThisFrame`, `isPressed`, `wasReleasedThisFrame`), `position.ReadValue()`, `scroll.ReadValue()`.
Thêm `World/PanZoom.cs`, `World/MapDrag.cs`, `LoadSceneDelay.cs` vào danh sách kiểm tra kiểu.

**Kiểm trên web:**

```
| 29 | World: kéo bản đồ rồi thả tay khi đang kéo nhanh | Bản đồ trượt thêm một đoạn rồi dừng êm, không khựng | PanZoom quán tính |
| 30 | Kéo bản đồ, thả chuột khi con trỏ đang nằm trên một toà nhà | KHÔNG vào toà nhà | MapDrag |
| 31 | Bấm (không kéo) vào một toà nhà | Vào toà nhà như thường | ngưỡng 8 px |
| 32 | Cuộn chuột khi con trỏ ở trên cối xay | Zoom mượt; cối xay vẫn nằm dưới con trỏ | zoom quanh con trỏ |
| 33 | Zoom ra hết cỡ, kéo ra mọi mép | Không bao giờ thấy màu nền ngoài bản đồ | kẹp biên |
| 34 | Ném bản đồ thật mạnh vào mép | Dừng ở mép, không nảy, không rung | velocity = 0 khi bị kẹp |
```

**Xong khi:** sáu dòng đạt, `PanMathTests` xanh.

**Bẫy:**

- Canvas của World là World Space và các toà nhà là con của nó: camera di chuyển thì chúng đứng yên trong thế
  giới, đúng ý. Không di chuyển canvas.
- Camera World có `localScale` 0,8026: không ảnh hưởng tới `orthographicSize`, đừng "sửa".
- Trên web, trang có thể cuộn theo bánh xe chuột nếu canvas không chiếm hết trang: nếu An báo trang bị cuộn, ghi
  vào bug-log (sửa ở template HTML, ngoài phạm vi bước này).
- Tính vận tốc bằng `move / dt` phải tránh `dt = 0`: bỏ qua khung có `dt < 0.0001`.

## M2.2 Đồng hồ theo giờ thật

**PRD:** F3 (phần "tiếp tục đếm khi thu nhỏ cửa sổ"). **Cần trước:** M0.1.

`Countdown` đã có trong Core, có test. Ba script đổi cách đếm:

| File | Hiện tại | Đổi thành |
| --- | --- | --- |
| `Home/PomodoroDayNight.cs` | `timeRemaining -= Time.unscaledDeltaTime;` và `UpdateTimerDisplay()` mỗi khung | `Countdown _timer`; `Update` theo mẫu game-feel.md mục 8. Số phút ghi log lấy từ `_timer.Elapsed.TotalMinutes` thay cho `Time.unscaledTime - sessionStartTimeUnscaled` |
| `Forest/MeditationTimer.cs` | cùng kiểu | cùng cách |
| `School/MathGameManager.cs` | `timeLeft -= Time.deltaTime;` và gán `timerText.text` mỗi khung | `Countdown` 15 giây mỗi câu; chữ `"Time: " + giây` chỉ gán khi số giây đổi |

Quy tắc chung cho cả ba:

1. Tạo `_timer = new Countdown(Services.Clock);` trong `Start` (hoặc `Awake`).
2. Bỏ biến `timerIsRunning`: dùng `_timer.IsRunning`. Bỏ `timeRemaining`: dùng `_timer.Remaining`.
3. Chỗ nào trước đây đặt `timerIsRunning = false` để dừng: gọi `_timer.Stop()`, **sau khi** đã lấy `Elapsed` để
   ghi log.
4. Giữ nguyên mọi lời gọi `RecordActivity` và mọi điều kiện `completed`.
5. `UpdateTimeOfDay()` trong `PomodoroDayNight.Update` gọi `DateTime.Now` mỗi khung: đổi thành kiểm mỗi 30 giây
   (so `Time.unscaledTime` với mốc lần kiểm trước).

**Kiểm:**

- Kiểu: cả ba file đã nằm trong danh sách kiểm tra kiểu.
- Test: không cần thêm (logic nằm trong `Countdown`).
- Web:

```
| 35 | Pomodoro 2 phút; chuyển sang tab khác 1 phút; quay lại | Đồng hồ chỉ khoảng 01:00, không phải 02:00 | Countdown |
| 36 | Pomodoro 1 phút, để tab ở nền quá 1 phút, quay lại | Phiên đã hoàn thành ngay khi quay lại; bản ghi "completed": true, minutes khoảng 1 | Countdown.IsDue |
| 37 | Thiền 1 phút tới hết | Như bước 4 của danh sách cũ, không ghi thêm bản "interrupted" | MeditationTimer |
| 38 | Game toán: để hết 15 giây không trả lời | Hành vi hết giờ như trước | MathGameManager |
| 39 | Pomodoro đang chạy, bấm đặt lại giữa chừng | Bản ghi "completed": false, minutes là số phút đã chạy thật | Elapsed |
```

**Xong khi:** năm dòng đạt.

**Bẫy:**

- Lỗi L2 (bug-log): phiên thiền hoàn thành bị ghi thêm bản "interrupted". Sau khi hoàn thành phải `_timer.Stop()`
  để `OnDisable` không coi là đang chạy.
- Game toán dùng `Time.deltaTime` (dừng theo `timeScale`). Sau khi đổi, đồng hồ câu hỏi chạy theo giờ thật: nếu
  sau này có menu tạm dừng (M5.2), phải gọi `_timer.Pause()` và `Resume()`. Ghi chú điều này ngay trong code.
- Đừng định dạng chuỗi rồi mới so sánh số: so `(int)Math.Ceiling(Remaining.TotalSeconds)` với số lần trước, khác
  thì mới tạo chuỗi. `Countdown.Format()` tạo chuỗi mới mỗi lần gọi.

## M2.3 Bàn phím và chống bấm đúp

**Cần trước:** M1.4, M2.2.

| Việc | Ở đâu | Cách |
| --- | --- | --- |
| Enter trong ô phút bắt đầu Pomodoro | `PomodoroDayNight.Start` | `minutesInput.onSubmit.AddListener(_ => StartPomodoro());` |
| Enter trong ô phút bắt đầu thiền | `MeditationTimer.Start` | tương tự |
| Số phút không hợp lệ (rỗng, 0, âm, trên 180) | `StartPomodoro`, hàm bắt đầu thiền | Không bắt đầu; `Tween.Shake` ô nhập; không log lỗi |
| Escape đóng sổ nhật ký và hộp thư gợi ý | `HomeDirector` hoặc ngay trong `JournalUIManager.Update` | `Keyboard.current.escapeKey.wasPressedThisFrame` và panel đang mở thì `ToggleNotebook(false)` |
| Nút Save của nhật ký bấm đúp | `JournalUIManager.SaveCurrentEntry` | `saveButton.interactable = false` ở dòng đầu, bật lại ở cuối hàm |
| Phím số 1–4 chọn đáp án toán | `MathGameManager.Update` | `Keyboard.current.digit1Key.wasPressedThisFrame` gọi `OnOptionSelected(0)`, v.v. |

**Stub cần thêm:** `TMP_InputField.onSubmit` (kiểu có `AddListener(UnityAction<string>)`),
`Keyboard.current.escapeKey`, `digit1Key`…`digit4Key`.

**Kiểm trên web:**

```
| 40 | Home: gõ 1 vào ô phút, bấm Enter | Pomodoro bắt đầu | onSubmit |
| 41 | Để ô phút trống, bấm Start | Ô nhập lắc nhẹ, không bắt đầu, không lỗi đỏ | kiểm đầu vào |
| 42 | Mở sổ, bấm Escape | Sổ đóng có animation | Escape |
| 43 | Game toán: bấm phím 1–4 | Chọn đáp án tương ứng | phím số |
```

**Bẫy:** trên web, Escape cũng thoát toàn màn hình của trình duyệt; đó là hành vi của trình duyệt, không chặn.
Khi đang gõ trong ô nhật ký, phím số không được chọn đáp án (hai thứ ở hai scene khác nhau nên không đụng, nhưng
giữ quy tắc: phím tắt chỉ chạy khi không có `TMP_InputField` nào đang được chọn,
`EventSystem.current.currentSelectedGameObject`).

## M2.4 Đo và dọn hiệu năng

**PRD:** 5.2 (60 fps), 8.2 (bỏ `Debug.Log` khỏi đường chạy chính). **Cần trước:** M0.3.

**Làm, theo thứ tự:**

1. Liệt kê log trong đường chạy nóng:
   ```bash
   grep -rnE "Debug\.Log\(" Assets/Scripts --include=*.cs | grep -v "\[DearPompom\]" | grep -v "/Core/"
   ```
   Với mỗi dòng: nằm trong `Update`, vòng lặp, hoặc handler bấm (ví dụ `Card`, `GameSceneController2`,
   `RenderPlaceWithoutCard`, `GraphRenderer`) thì xoá. Log lỗi thật (`LogError` khi thiếu tham chiếu) thì giữ và
   thêm tiền tố `[DearPompom]`.
2. Tìm việc tìm kiếm mỗi khung:
   ```bash
   grep -rnE "Camera\.main|FindFirstObjectByType|FindObjectsByType|GameObject\.Find|GetComponent" Assets/Scripts --include=*.cs
   ```
   Chỗ nào nằm trong `Update`/`LateUpdate`/`FixedUpdate`: chuyển ra `Awake`/`Start`, giữ vào field.
3. Chữ đổi mỗi khung: sau M2.2 chỉ còn `timerText` đổi mỗi giây. Kiểm lại không còn `.text =` nào trong `Update`
   mà không có điều kiện "đã đổi":
   ```bash
   grep -rnE "\.text = " Assets/Scripts --include=*.cs
   ```
4. `Forest/EndlessScrollingObject.cs`, `TreeSpawner.cs`, `SunSpawner.cs`: nếu có `Instantiate`/`Destroy` lặp lại
   lúc chơi thì ghi vào "Việc thấy trên đường" (đổi sang pool là việc của M4, không làm ở đây).
5. An đo trên web bằng F3: FPS thấp nhất ở mỗi scene trong 10 giây chơi bình thường. Ghi bảng vào dev-log.

**Kiểm trên web:**

```
| 44 | Bật F3, vào lần lượt 11 scene, ở mỗi scene 10 giây | Ghi FPS trung bình và thấp nhất của từng scene vào dev-log | DebugOverlay |
```

**Xong khi:** có bảng FPS của 11 scene trong dev-log; scene nào dưới 55 fps được ghi rõ là việc cần xử lý, kèm
nghi phạm (với Home và ForestSitting, nghi phạm là 144 và 30 ảnh toàn màn hình, sẽ hết ở M3).

**Bẫy:** không tối ưu thứ chưa đo. Bước này chỉ dọn những mẫu sai rõ ràng liệt kê ở trên và lấy số liệu gốc để
so sánh sau M3.
