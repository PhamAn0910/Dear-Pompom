# Game feel: chuyển động và thao tác mượt

Trang này quy định **mọi thứ chuyển động như thế nào** và **mọi thao tác được trả lời ra sao**. Mục tiêu của PRD
(mục 5): mỗi thao tác được trả lời trong 100 ms bằng hình và tiếng; mọi chuyển động chậm, mềm, không giật;
không animation nào chặn thao tác quá 300 ms.

Đọc [no-editor-unity.md](no-editor-unity.md) trước. Mã trong trang này là **mã mẫu tham chiếu**: đã được thiết kế
kỹ nhưng chưa qua Unity thật, nên khi đưa vào project vẫn phải qua Kiểu, CI và chơi thử.

## 1. Ba luật không được phá

1. **Phản hồi trước, xử lý sau.** Khi người chơi bấm, khung hình ngay sau đó phải có thay đổi nhìn thấy được
   (nút lún xuống) và tiếng. Việc nặng (tải scene, ghi save) chạy sau hoặc song song, không bao giờ trước.
2. **Một thuộc tính, một chủ.** Tại một thời điểm chỉ một tween được điều khiển scale của một object (tương tự
   cho vị trí, alpha, màu). Bắt đầu tween mới trên cùng thuộc tính thì tween cũ bị huỷ. Giá trị "nghỉ" (scale
   gốc, màu gốc) được ghi **một lần lúc khởi tạo**, không đọc lại lúc bắt đầu animation. Lỗi W1 trong
   [bug-log.md](../bug-log.md) (nút toán đứng màu xanh) là do phá luật này.
3. **Không phụ thuộc tốc độ khung hình.** Mọi chuyển động tính từ thời gian trôi qua, dùng các hàm trong Core.
   Cấm viết `x = Mathf.Lerp(x, target, 0.1f)` hay `Lerp(x, target, speed * Time.deltaTime)`: chúng chạy khác
   nhau ở 30 và 144 fps. Dùng `Smooth.Damp` hoặc `Spring`.

## 2. Bộ công cụ có sẵn trong Core (đã có test)

Nằm ở `Assets/Scripts/Core/Motion/` và `Core/Time/`, test ở `Assets/Tests/EditMode/MotionTests.cs`.

| Class | Dùng khi | Ghi chú |
| --- | --- | --- |
| `Ease.Evaluate(EaseType, t)` | Cần đường cong cho một chuyển động có điểm đầu, điểm cuối, thời lượng | 13 đường cong. Không tự viết đường cong mới trong script |
| `Ease.Lerp(from, to, eased)` | Nội suy không kẹp (vì `OutBack` vượt quá 1) | Với vector dùng `Vector3.LerpUnclamped` |
| `TweenState` | Giữ thời gian của một tween: trễ, thời lượng, lặp, đi-về | Lớp Unity `Tweener` bọc nó. Chịu được khung hình dài |
| `Spring` | Đích có thể đổi giữa chừng: hover, nhấn nút, camera, vật bị kéo | Lời giải chính xác, không nổ khi `dt` lớn |
| `Smooth.Damp(cur, target, halfLife, dt)` | Bám theo đích không cần nảy: quán tính, zoom | Thay cho `Lerp(a, b, k * dt)` |
| `Flipbook.FrameAt(...)` | Khung hình thứ mấy của clip từng khung | Dùng bởi `PatchClipPlayer` |
| `Countdown` | Mọi đồng hồ đếm ngược (Pomodoro, thiền, 15 giây của game toán) | Đo theo giờ thật qua `IClock`, đúng cả khi tab ở nền |

Chọn công cụ:

```
Chuyển động có đích cố định và thời lượng biết trước?     → Tween (Ease)
Đích đổi liên tục theo thao tác của người chơi?            → Spring (muốn nảy) hoặc Smooth.Damp (không nảy)
Lặp vô hạn, trang trí (lá lay, mây trôi, thở)?             → hàm sin theo thời gian, xem animation-assets.md mục 5
Từng khung vẽ tay?                                          → PatchClipPlayer
```

## 3. Bảng chuyển động chuẩn

Dùng đúng số trong bảng. Không tự chọn số khác cho từng màn; sự nhất quán tạo ra cảm giác "mượt".

| Tình huống | Thuộc tính | Giá trị | Thời lượng | Đường cong |
| --- | --- | --- | --- | --- |
| Rê chuột lên nút | scale | ×1,04 | spring 6 Hz, damping 0,8 | |
| Nhấn nút xuống | scale | ×0,95 | spring 10 Hz, damping 1 | |
| Thả nút | scale | về ×1 (hoặc ×1,04 nếu còn rê) | spring 6 Hz, damping 0,55 (nảy nhẹ) | |
| Mở panel | scale 0,92 → 1 và alpha 0 → 1 | | 220 ms | `OutCubic` |
| Đóng panel | alpha 1 → 0 và scale 1 → 0,96 | | 160 ms | `InCubic` |
| Chuyển scene, mờ đi | alpha lớp đen 0 → 1 | | 180 ms | `InQuad` |
| Chuyển scene, hiện ra | alpha lớp đen 1 → 0 | | 260 ms | `OutQuad` |
| Vật xuất hiện (thư, huy hiệu) | scale 0 → 1 | | 300 ms | `OutBack` |
| Vật biến mất | scale 1 → 0 | | 180 ms | `InBack` |
| Nhấn mạnh đúng (ghép thẻ, đáp án đúng) | scale ×1,12 rồi về | | 240 ms (120 đi, 120 về) | `OutQuad`, đi-về |
| Báo sai | lắc ngang 10 px, 3 chu kỳ, tắt dần | | 280 ms | sin × (1 − t) |
| Lật thẻ | scale X 1 → 0, đổi mặt, 0 → 1 | | 125 ms + 125 ms | `InQuad` rồi `OutQuad` |
| Số chạy (điểm, phút) | giá trị | | 400 ms | `OutCubic` |
| Danh sách hiện lần lượt | trễ mỗi phần tử | 40 ms, tối đa 6 phần tử | | |
| Màu nháy rồi về | màu → màu nhấn → màu nghỉ | | 100 ms + 300 ms | `OutQuad` |
| Đổi buổi trong ngày | alpha hai lớp nền chéo nhau | | 4 giây | `InOutSine` |
| Mừng hoàn thành | hạt + Pompom | tối đa 2 giây, bấm để bỏ qua | | |

Quy tắc chọn đường cong: vật **đi vào** dùng `Out...` (nhanh rồi chậm lại, cảm giác nhạy); vật **đi ra** dùng
`In...`; vật **di chuyển giữa hai chỗ trên màn hình** dùng `InOut...`. Không dùng `Linear` trừ thanh tiến độ và
vòng quay đều.

## 4. Lớp Unity: `Tweener` và `Tween`

Thư mục `Assets/Scripts/Motion/`. Một `Tweener` duy nhất, gắn vào object dịch vụ trong `Services.EnsureBooted`.

Hợp đồng (các bước sau dựa vào đúng những điều này):

- `Tween.X(...)` trả về `int` id; `Tween.Kill(id)` dừng tại chỗ; `Tween.KillAll(owner)` dừng mọi tween của một object.
- Khoá của một tween là cặp **(owner, channel)**. Chạy tween mới cùng khoá thì tween cũ bị huỷ, **không** gọi
  `onDone` của tween cũ.
- Owner bị `Destroy` (đổi scene) thì tween tự biến mất, không lỗi, không gọi `onDone`.
- Dùng `Time.unscaledDeltaTime`, kẹp tối đa 0,1 giây mỗi khung.
- Khi `Motion.Reduced` bật (PRD A11): tween kênh `Alpha` và `Color` chạy bình thường; mọi kênh khác nhảy thẳng
  tới giá trị cuối trong khung đầu, tween đi-về thì bỏ qua. `onDone` vẫn được gọi.
- `onDone` được phép bắt đầu tween khác.

```csharp
using System;
using System.Collections.Generic;
using DearPompom.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DearPompom
{
    public enum TweenChannel { Scale, Position, Rotation, Alpha, Color, Custom }

    /// <summary>Global motion switches. Reduced comes from SettingsData.reduceMotion (PRD A11).</summary>
    public static class Motion
    {
        public static bool Reduced;
    }

    /// <summary>Runs every tween in the game. One instance, created by Services.EnsureBooted.</summary>
    public sealed class Tweener : MonoBehaviour
    {
        private sealed class Item
        {
            public int Id;
            public UnityEngine.Object Owner;
            public int Channel;
            public TweenState State;
            public float From, To;
            public Action<float> Apply;
            public Action Done;
            public bool Dead;
        }

        public const float MaxDelta = 0.1f;
        private static Tweener _instance;
        private readonly List<Item> _items = new List<Item>();
        private int _nextId = 1;

        public static int ActiveCount { get { return _instance == null ? 0 : _instance._items.Count; } }

        private void Awake() { _instance = this; }

        public static int Run(UnityEngine.Object owner, int channel, float from, float to, float duration,
            EaseType ease, Action<float> apply, Action done = null, float delay = 0f, int repeats = 0, bool pingPong = false)
        {
            if (_instance == null || owner == null || apply == null) return 0;
            _instance.KillChannel(owner, channel);

            bool instant = Motion.Reduced && channel != (int)TweenChannel.Alpha && channel != (int)TweenChannel.Color;
            if (instant)
            {
                apply(pingPong && repeats % 2 == 1 ? from : to); // a there-and-back tween rests where it started
                if (done != null) done();
                return 0;
            }

            var item = new Item
            {
                Id = _instance._nextId++, Owner = owner, Channel = channel, From = from, To = to,
                Apply = apply, Done = done, State = new TweenState(duration, ease, delay, repeats, pingPong),
            };
            _instance._items.Add(item);
            if (delay <= 0f) apply(from);
            return item.Id;
        }

        public static void Kill(int id)
        {
            if (_instance == null || id == 0) return;
            foreach (Item item in _instance._items) if (item.Id == id) item.Dead = true;
        }

        public static void KillAll(UnityEngine.Object owner)
        {
            if (_instance == null) return;
            foreach (Item item in _instance._items) if (item.Owner == owner) item.Dead = true;
        }

        private void KillChannel(UnityEngine.Object owner, int channel)
        {
            foreach (Item item in _items)
                if (item.Channel == channel && item.Owner == owner) item.Dead = true;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxDelta);
            // Index loop: callbacks may add tweens while we iterate. New ones start next frame.
            int count = _items.Count;
            for (int i = 0; i < count; i++)
            {
                Item item = _items[i];
                if (item.Dead) continue;
                if (item.Owner == null) { item.Dead = true; continue; } // owner destroyed with its scene
                float progress = item.State.Advance(dt);
                item.Apply(Ease.Lerp(item.From, item.To, progress));
                if (item.State.IsComplete)
                {
                    item.Dead = true;
                    if (item.Done != null) item.Done();
                }
            }
            _items.RemoveAll(x => x.Dead);
        }
    }

    /// <summary>Remembers an object's resting scale and position the first time anything animates it.</summary>
    public sealed class MotionRest : MonoBehaviour
    {
        public Vector3 Scale;
        public Vector3 LocalPosition;
        public Vector2 AnchoredPosition;

        public static MotionRest Of(Transform target)
        {
            MotionRest rest = target.GetComponent<MotionRest>();
            if (rest != null) return rest;
            rest = target.gameObject.AddComponent<MotionRest>();
            rest.Scale = target.localScale;
            rest.LocalPosition = target.localPosition;
            var rect = target as RectTransform;
            if (rect != null) rest.AnchoredPosition = rect.anchoredPosition;
            return rest;
        }
    }

    /// <summary>The API game code calls. Every method returns a tween id (0 when nothing was started).</summary>
    public static class Tween
    {
        public static void Kill(int id) { Tweener.Kill(id); }
        public static void KillAll(UnityEngine.Object owner) { Tweener.KillAll(owner); }

        /// <summary>Scale relative to the resting scale: 1 = rest, 0.95 = pressed.</summary>
        public static int Scale(Transform t, float from, float to, float duration, EaseType ease, Action done = null, float delay = 0f)
        {
            Vector3 rest = MotionRest.Of(t).Scale;
            return Tweener.Run(t, (int)TweenChannel.Scale, from, to, duration, ease, v => t.localScale = rest * v, done, delay);
        }

        /// <summary>Grow by <paramref name="amount"/> and come back. For "correct!" moments.</summary>
        public static int Punch(Transform t, float amount = 0.12f, float duration = 0.24f)
        {
            Vector3 rest = MotionRest.Of(t).Scale;
            return Tweener.Run(t, (int)TweenChannel.Scale, 1f, 1f + amount, duration * 0.5f, EaseType.OutQuad,
                v => t.localScale = rest * v, null, 0f, 1, true);
        }

        /// <summary>Horizontal shake that dies out. For "wrong" moments. Works for UI and world objects.</summary>
        public static int Shake(Transform t, float distance = 10f, float duration = 0.28f, int cycles = 3)
        {
            MotionRest rest = MotionRest.Of(t);
            var rect = t as RectTransform;
            return Tweener.Run(t, (int)TweenChannel.Position, 0f, 1f, duration, EaseType.Linear, p =>
            {
                float offset = Mathf.Sin(p * cycles * 2f * Mathf.PI) * distance * (1f - p);
                if (rect != null) rect.anchoredPosition = rest.AnchoredPosition + new Vector2(offset, 0f);
                else t.localPosition = rest.LocalPosition + new Vector3(offset * 0.01f, 0f, 0f); // px -> units at 100 ppu
            });
        }

        public static int Fade(CanvasGroup group, float to, float duration, EaseType ease = EaseType.OutQuad, Action done = null)
        {
            return Tweener.Run(group, (int)TweenChannel.Alpha, group.alpha, to, duration, ease, v => group.alpha = v, done);
        }

        public static int Fade(SpriteRenderer sprite, float to, float duration, EaseType ease = EaseType.OutQuad, Action done = null)
        {
            return Tweener.Run(sprite, (int)TweenChannel.Alpha, sprite.color.a, to, duration, ease, v =>
            {
                Color c = sprite.color; c.a = v; sprite.color = c;
            }, done);
        }

        /// <summary>Flash a UI graphic to a colour and return to its resting colour (pass the colour stored at start-up).</summary>
        public static int Flash(Graphic graphic, Color rest, Color flash, float hold = 0.1f, float back = 0.3f)
        {
            graphic.color = flash;
            return Tweener.Run(graphic, (int)TweenChannel.Color, 0f, 1f, back, EaseType.OutQuad,
                v => graphic.color = Color.LerpUnclamped(flash, rest, v), null, hold);
        }

        /// <summary>Anything else: a number over time. Use a channel of 100 or more for your own keys.</summary>
        public static int Value(UnityEngine.Object owner, int channel, float from, float to, float duration, EaseType ease,
            Action<float> apply, Action done = null, float delay = 0f)
        {
            return Tweener.Run(owner, channel, from, to, duration, ease, apply, done, delay);
        }
    }
}
```

Cách dùng thường gặp:

```csharp
// Mở panel (bảng chuẩn: scale 0,92 -> 1 và alpha 0 -> 1, 220 ms, OutCubic)
panel.SetActive(true);
group.alpha = 0f;
Tween.Scale(panel.transform, 0.92f, 1f, 0.22f, EaseType.OutCubic);
Tween.Fade(group, 1f, 0.22f, EaseType.OutCubic);

// Đóng panel: tắt object SAU khi animation xong, không trước
group.blocksRaycasts = false;                       // ngừng nhận bấm ngay lập tức
Tween.Scale(panel.transform, 1f, 0.96f, 0.16f, EaseType.InCubic);
Tween.Fade(group, 0f, 0.16f, EaseType.InCubic, () => panel.SetActive(false));

// Số điểm chạy lên
Tween.Value(scoreText, 100, oldScore, newScore, 0.4f, EaseType.OutCubic,
    v => scoreText.text = Mathf.RoundToInt(v).ToString());
```

Lỗi hay gặp với tween:

| Lỗi | Triệu chứng | Sửa |
| --- | --- | --- |
| `SetActive(false)` ngay sau khi gọi tween đóng | Panel biến mất tức thì, không thấy animation | Tắt trong `done` |
| Mở panel khi animation đóng chưa xong | `done` của lần đóng tắt panel vừa mở | Không xảy ra với `Tweener` này (tween cũ bị huỷ, `done` không chạy), miễn là cả mở và đóng cùng dùng `Tween.Fade` trên **cùng một** `CanvasGroup` |
| Đọc `transform.localScale` làm "scale gốc" lúc bắt đầu | Bấm nhanh nhiều lần thì object to dần hoặc nhỏ dần | Luôn qua `MotionRest.Of(t)` |
| Tween scale một object nằm trong `LayoutGroup` rồi đổi `sizeDelta` | Layout nhảy | Chỉ tween scale, không tween kích thước |
| Tween vị trí của object có `ContentSizeFitter` hoặc do layout điều khiển | Bị layout ghi đè mỗi khung | Bọc nội dung trong một object con, tween object con |
| Gọi `Tween.*` trong `Update` | Mỗi khung tạo một tween mới, tween không bao giờ tiến | Chỉ gọi tại sự kiện (bấm, bắt đầu, kết thúc) |

## 5. Nút bấm: `ButtonFeel`

Mọi `Button` trong mọi scene nhận component này, do `CommonDirector` gắn (`SceneFind.All<Button>(scene)`).
Nút do code tạo sau đó tự gắn khi tạo.

```csharp
using DearPompom.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DearPompom
{
    /// <summary>Hover, press and release feedback for one button. Numbers come from docs/guides/game-feel.md.</summary>
    public sealed class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float Hover = 1.04f, Pressed = 0.95f;
        private Button _button;
        private Vector3 _restScale;
        private Spring _spring = new Spring(1f);
        private bool _over, _down;
        private bool _moving = true; // false = the spring is at rest, Update returns at once

        private void Awake()
        {
            _button = GetComponent<Button>();
            _restScale = MotionRest.Of(transform).Scale;
        }

        // The panel was closed while hovered: never come back enlarged.
        // Do NOT put the component to sleep with "enabled = false": that would call this method.
        private void OnDisable()
        {
            _over = _down = false;
            _spring = new Spring(1f);
            transform.localScale = _restScale;
            _moving = false;
        }

        public void OnPointerEnter(PointerEventData e) { _over = true; _moving = true; }
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; _moving = true; }
        public void OnPointerDown(PointerEventData e) { _down = true; _moving = true; }
        public void OnPointerUp(PointerEventData e) { _down = false; _moving = true; }

        private void Update()
        {
            if (!_moving) return;
            bool usable = _button == null || _button.interactable;
            float target = !usable ? 1f : (_down ? Pressed : (_over ? Hover : 1f));
            if (Motion.Reduced)
            {
                transform.localScale = _restScale * (_down && usable ? 0.97f : 1f);
                _spring = new Spring(1f);
                _moving = false;
                return;
            }
            float frequency = _down ? 10f : 6f;
            float damping = _down ? 1f : (_over ? 0.8f : 0.55f);
            _spring.Step(target, frequency, damping, Mathf.Min(Time.unscaledDeltaTime, Tweener.MaxDelta));
            transform.localScale = _restScale * _spring.Value;
            if (_spring.IsSettled(target)) _moving = false;
        }
    }
}
```

Điểm cần hiểu:

- Khi lò xo đứng yên, `_moving = false` và `Update` thoát ngay ở dòng đầu, nên 30 nút gần như không tốn gì.
  Không dùng `enabled = false` để cho ngủ: Unity sẽ gọi `OnDisable`, và hàm đó đặt lại trạng thái hover.
- `ButtonFeel` điều khiển scale bằng lò xo riêng. **Không gọi `Tween.Scale` hay `Tween.Punch` lên chính object
  của nút** (hai chủ cho một thuộc tính). Muốn nhấn mạnh một nút: punch object con (icon) hoặc object cha.
- Ảnh của nút phải có `raycastTarget` bật thì mới nhận sự kiện con trỏ. Nút có sẵn trong scene đều bật.
- Âm thanh bấm: các nút trong scene đã gọi `SoundEffects.PlayClick` qua `onClick`. Giữ nguyên, không thêm tiếng
  trong `ButtonFeel`, nếu không sẽ kêu hai lần.

### Chống bấm đúp và bấm trong lúc chuyển cảnh

- Trong lúc `SceneTransitionService` đang chạy, lớp phủ bật `blocksRaycasts`, mọi nút bên dưới không nhận bấm.
- Hành động chỉ được xảy ra một lần (lưu nhật ký, bắt đầu game): đặt `button.interactable = false` ngay dòng
  đầu của handler, bật lại khi xong.
- Trong game lật thẻ, `controller.CanReveal` đã chặn lật thẻ thứ ba. Khi thêm animation lật, `CanReveal` phải
  là `false` suốt thời gian thẻ đang lật, không chỉ lúc chờ so khớp.

## 6. Chuyển scene: `SceneTransitionService`

Thay cho `scene_loader.cs`, `LoadSceneDelay`, `RenderPlace`, `RenderPlaceWithoutCard`, `RenderMap` (PRD 5.2).
Vì scene đang gọi các class cũ theo tên, **giữ class cũ, đổi thân hàm thành một dòng gọi dịch vụ** (xem
no-editor-unity.md mục 4).

Hợp đồng:

- `Services.Transitions.Go(string sceneName)`; gọi khi đang chuyển thì bị bỏ qua.
- Trình tự: bật chặn bấm → mờ ra 180 ms → `LoadSceneAsync` → chờ xong → chờ thêm 1 khung (để `Start` của scene
  mới chạy) → hiện ra 260 ms → tắt chặn bấm. Tổng dưới 0,8 giây với scene tải nhanh (PRD A4).
- Lớp phủ là canvas riêng `sortingOrder = 900`, `DontDestroyOnLoad`, một `Image` đen phủ kín, một `CanvasGroup`.
- Phát sự kiện `SceneChanging(from, to)` trước khi mờ, để âm thanh crossfade cùng lúc.
- `Motion.Reduced` không tắt fade (fade là thứ duy nhất được giữ theo A11).

```csharp
using System.Collections;
using DearPompom.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DearPompom
{
    public sealed class SceneTransitionService : MonoBehaviour
    {
        public const float FadeOut = 0.18f, FadeIn = 0.26f;
        private CanvasGroup _group;
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            var go = new GameObject("[DearPompom Transition]");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            go.AddComponent<GraphicRaycaster>();
            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var black = new GameObject("Black");
            black.transform.SetParent(go.transform, false);
            var image = black.AddComponent<Image>();
            image.color = Color.black;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void Go(string sceneName)
        {
            if (IsBusy || string.IsNullOrEmpty(sceneName)) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[DearPompom] scene '" + sceneName + "' is not in the build list");
                return;
            }
            StartCoroutine(Run(sceneName));
        }

        private IEnumerator Run(string sceneName)
        {
            IsBusy = true;
            _group.blocksRaycasts = true;
            yield return Fade(0f, 1f, FadeOut, EaseType.InQuad);
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone) yield return null;
            yield return null; // let the new scene run Start before we reveal it
            yield return Fade(1f, 0f, FadeIn, EaseType.OutQuad);
            _group.blocksRaycasts = false;
            IsBusy = false;
        }

        // A plain loop, not the Tweener: the transition must work even if a tween bug exists.
        private IEnumerator Fade(float from, float to, float duration, EaseType ease)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, Tweener.MaxDelta);
                _group.alpha = Ease.Lerp(from, to, Ease.Evaluate(ease, t / duration));
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
```

Bẫy:

- Tên scene trong `sceneName` của các nút phải có trong build list. `RenderMap` đang gọi `"Map"`, scene đó
  **không tồn tại** (scene bản đồ tên là `World`). `Go` log lỗi rõ ràng thay vì đứng màn hình đen.
- Coroutine chạy trên object dịch vụ (`DontDestroyOnLoad`), nên sống qua lúc đổi scene. Đừng chạy coroutine
  chuyển cảnh trên object của scene cũ: nó bị huỷ giữa chừng và màn hình đen mãi.
- `PomodoroDayNight.OnDisable` ghi phiên "bị ngắt" khi rời scene. Hành vi đó giữ nguyên tới bước F3.

## 7. Bản đồ: kéo, quán tính, zoom, và phân biệt kéo với bấm

`World/PanZoom.cs` hiện chỉ kéo được, dừng khựng khi thả, và có lỗi: kéo bản đồ rồi thả chuột trên một toà nhà
thì **vào luôn toà nhà đó** (uGUI vẫn coi là click vì `Button` không xử lý drag).

Thuật toán thay thế, giữ tên class `PanZoom` và các field public (`zoomOutMin`, `zoomOutMax`, `background`,
`panSpeed`) vì scene World đang tham chiếu:

```
Mỗi khung (dt = min(unscaledDeltaTime, 0.1)):

1. NHẤN (leftButton.wasPressedThisFrame):
     grabWorld   = ScreenToWorld(chuột)       // điểm trên bản đồ đang bị "nắm"
     pressScreen = chuột;  dragging = false;  velocity = 0

2. GIỮ (leftButton.isPressed):
     nếu |chuột - pressScreen| > 8 px  → dragging = true
     nếu dragging:
         move      = grabWorld - ScreenToWorld(chuột)      // đưa điểm bị nắm về dưới con trỏ
         cam.pos  += move
         velocity  = Damp(velocity, move / dt, halfLife 0.05, dt)   // vận tốc mượt, đơn vị/giây

3. THẢ (leftButton.wasReleasedThisFrame):
     nếu dragging → MapDrag.lastDragFrame = Time.frameCount       // để nút bấm biết đây là kéo
     dragging = false                                             // velocity giữ nguyên cho quán tính

4. KHÔNG GIỮ: quán tính
     cam.pos  += velocity * dt
     velocity  = Damp(velocity, 0, halfLife 0.12, dt)
     nếu |velocity| < 0.01 → velocity = 0

5. ZOOM (Mouse.current.scroll.ReadValue().y khác 0):
     targetSize = Clamp(targetSize * (1 - 0.1 * Sign(scroll)), zoomMin, zoomMax)
   Mỗi khung:
     before   = ScreenToWorld(chuột)
     cam.orthographicSize = Damp(cam.orthographicSize, targetSize, halfLife 0.08, dt)
     after    = ScreenToWorld(chuột)
     cam.pos += before - after                                    // điểm dưới con trỏ đứng yên khi zoom

6. KẸP BIÊN (luôn chạy cuối, sau mọi thay đổi):
     zoomMax thật = min(zoomOutMax, bg.height / 2, bg.width / (2 * aspect))   // không bao giờ thấy ngoài ảnh
     cam.x = Clamp(cam.x, bg.min.x + viewW/2, bg.max.x - viewW/2);  tương tự cho y
     nếu bị kẹp theo trục nào thì velocity của trục đó = 0
```

Phân biệt kéo với bấm: thêm vào đầu `LoadSceneDelay.OnButtonClick`:

```csharp
if (MapDrag.JustDragged) return; // the pointer was dragging the map, not clicking a building
```

với `MapDrag.JustDragged` trả `true` khi `Time.frameCount - lastDragFrame <= 1`. Sự kiện click của uGUI chạy
trong cùng khung với lúc thả chuột, nên so khung là đủ.

Chi tiết phải đúng:

- `ScreenToWorldPoint` cần `z` là khoảng cách từ camera: `new Vector3(mx, my, -cam.transform.position.z)`.
- Dùng `Mouse.current.scroll.ReadValue().y`; trên web giá trị mỗi nấc khác nhau theo trình duyệt (có thể là
  120, 100 hoặc 3), nên chỉ lấy **dấu**.
- Không pan khi con trỏ đang ở trên UI của game (hòm thư, cài đặt): kiểm
  `EventSystem.current.IsPointerOverGameObject()` lúc nhấn. Các toà nhà trên bản đồ cũng là UI (canvas World
  Space), nên chỉ bỏ qua khi object bị trỏ thuộc canvas `[DearPompom UI]`.
- Lưu camera vào field trong `Start` (`Camera.main` mỗi khung là thừa).
- Scale gốc của camera World là 0,8026: không đổi nó.

## 8. Đồng hồ và chữ đổi mỗi giây

Hiện `PomodoroDayNight`, `MeditationTimer`, `MathGameManager` trừ dần `timeRemaining -= Time.unscaledDeltaTime`.
Khi tab ở nền, trình duyệt không gọi khung hình, đồng hồ đứng. Chuyển sang `Countdown`:

```csharp
private Countdown _timer;                     // tạo trong Start: new Countdown(Services.Clock)
private string _shown = "";

void StartPomodoro() { _timer.Start(System.TimeSpan.FromMinutes(minutes)); }

void Update()
{
    if (!_timer.IsRunning) return;
    string text = _timer.Format();
    if (text != _shown) { _shown = text; timerText.text = text; }   // chỉ gán khi chữ thật sự đổi
    if (_timer.IsDue) { _timer.Stop(); OnPomodoroComplete(); }
}
```

Hai lợi ích: đúng giờ khi tab ở nền, và không còn tạo một chuỗi mới cộng một lần dựng lại lưới chữ mỗi khung
(gán `.text` của TextMeshPro mỗi khung là nguồn giật phổ biến nhất của UI). Số phút ghi vào log lấy từ
`_timer.Elapsed.TotalMinutes`.

Quy tắc chung cho chữ: **chỉ gán `.text` khi nội dung đổi.**

## 9. Ngân sách hiệu năng

Mục tiêu PRD: 60 fps trên laptop đồ hoạ tích hợp; mỗi scene dưới 150 MB texture; chuyển scene dưới 1 giây.

| Việc | Luật |
| --- | --- |
| Tìm object | Không `Find`, `FindFirstObjectByType`, `GetComponent`, `Camera.main` trong `Update`. Tìm một lần, giữ lại |
| Cấp phát mỗi khung | Không `new`, không nối chuỗi, không LINQ, không `foreach` trên `IEnumerable` trong `Update`. Lambda của tween tạo lúc bắt đầu tween là chấp nhận được |
| Coroutine | Tạo sẵn `WaitForSeconds` dùng lại, hoặc dùng tween có `delay` |
| Canvas | Thứ đổi mỗi khung (đồng hồ, thanh tiến độ, số điểm) không nằm chung canvas với nền tĩnh. Thêm `Canvas` con (nested canvas) cho object động bằng `AddComponent<Canvas>()` + `AddComponent<GraphicRaycaster>()` nếu nó có nút |
| Layout | Không bật tắt object trong `LayoutGroup` mỗi khung. Dựng danh sách một lần |
| Sprite động | Nhiều `SpriteRenderer` dùng chung một texture (sheet) thì được gộp lệnh vẽ. Không tạo `Material` riêng cho từng sprite; đổi màu bằng `spriteRenderer.color` |
| Hạt | Tối đa 40 sprite hạt sống cùng lúc, lấy từ pool, không `Instantiate`/`Destroy` lúc chơi |
| Tải asset | `Resources.Load` lúc vào scene, không lúc đang chơi. Clip lớn tải trong lúc màn hình đang đen của chuyển cảnh |
| Khung hình | Không đặt `Application.targetFrameRate` trên WebGL (để trình duyệt tự đồng bộ). Không đặt `QualitySettings.vSyncCount` bằng code |
| Log | Không `Debug.Log` trong `Update` hay trong vòng lặp. Trên web mỗi dòng log tốn thời gian thật |

Đo trên bản web: bật `DebugOverlay` (F3), đọc FPS thấp nhất trong 2 giây ở từng scene, khi đang chạy animation
nặng nhất của scene đó. Ghi số vào dev-log. Dưới 55 thì chưa đạt.

## 10. Giảm chuyển động (PRD A11)

`SettingsData.reduceMotion` đã có trong save. `Motion.Reduced` được gán khi khởi động và mỗi khi có sự kiện
`SettingsChanged`.

| Thành phần | Khi `Motion.Reduced` |
| --- | --- |
| Fade (alpha, màu), chuyển scene | Giữ nguyên |
| Scale, vị trí, xoay qua `Tween` | Nhảy thẳng tới đích (đã có trong `Tweener`) |
| `ButtonFeel` | Không hover, nhấn chỉ ×0,97, không lò xo |
| Lắc, punch | Bỏ (tween đi-về nghỉ tại điểm đầu) |
| Hạt, mừng hoàn thành | Không sinh hạt; chỉ hiện chữ và fade |
| Chuyển động trang trí (lá lay, mây) | Dừng tại pha 0 |
| Zoom vào toà nhà khi chuyển cảnh | Thay bằng fade thường |
| Clip từng khung của Pompom | Vẫn chạy (đó là nội dung, không phải hiệu ứng) |

Mỗi component chuyển động mới phải đọc `Motion.Reduced`. Kiểm trên web: bật công tắc, đi hết các scene, không
được còn thứ gì lắc, nảy hay bay.

## 11. Công thức cho từng loại phản hồi

**Lật thẻ** (thẻ là `SpriteRenderer`, mặt sau là object con `CardBack`):

```csharp
// Card.Reveal(): chặn bấm ngay, co ngang về 0, đổi mặt, nở ra
_isRevealed = true;
Transform t = transform;
Tween.Value(this, 100, 1f, 0f, 0.125f, EaseType.InQuad, v => SetScaleX(t, v), () =>
{
    cardBack.SetActive(false);
    Tween.Value(this, 100, 0f, 1f, 0.125f, EaseType.OutQuad, v => SetScaleX(t, v));
});
```

`SetScaleX` nhân với scale gốc lấy từ `MotionRest` (thẻ được đặt scale động theo độ khó, nên phải gọi
`MotionRest.Of` **sau** khi `GameSceneController2` đã đặt scale cho thẻ; nếu thẻ được dùng lại với scale khác,
cập nhật `MotionRest.Scale`). Controller phải chờ đủ 250 ms trước khi so khớp.

**Ghép đúng**: `Tween.Punch` cả hai thẻ, tiếng "đúng", sau 240 ms mới mờ đi (`Tween.Fade` 200 ms) rồi tắt.
**Ghép sai**: `Tween.Shake` cả hai thẻ, chờ 280 ms, rồi lật úp lại bằng animation lật ngược.

**Đáp án toán**: đúng thì `Tween.Flash` nền nút sang xanh lá + `Tween.Punch` chữ của nút (object con, không phải
nút); sai thì `Tween.Flash` sang đỏ + `Tween.Shake` cha chứa các nút. Màu nghỉ lấy từ mảng đã lưu lúc `Start`
(đã có sau khi sửa W1).

**Streak tăng dần**: streak 3, 5, 10 thì punch số streak mạnh dần (0,12; 0,18; 0,25) và cao độ tiếng tăng
(`AudioSource.pitch` 1,0; 1,06; 1,12). Không thêm rung màn hình.

**Vòng thở (thiền)**: một sprite hình tròn mềm; scale theo pha thở, mỗi pha dùng `InOutSine`. Bài 4-4-4: hít 4
giây (0,6 → 1), giữ 4 giây, thở 4 giây (1 → 0,6). Pha tính từ `Countdown.Elapsed` của phiên, không cộng dồn
`deltaTime`, để vòng tròn và đồng hồ không bao giờ lệch nhau.

**Nhạc công theo nhịp**: `phase = (AudioSettings.dspTime - songStartDsp) * bpm / 60`; độ nhún
`= Mathf.Pow(1 - (float)(phase % 1), 3) * 0.05` (bật lên ở đầu phách, rơi nhanh). Áp vào `scale.y` của nhạc công
đang chơi; nhạc công bị tắt thì nhún bằng 0 và xoay −6° (hạ đàn) bằng tween 300 ms.

**Mừng hoàn thành** (PRD A2): xem [animation-assets.md](animation-assets.md) mục 6 (`FxBurst`).

## 12. Danh sách kiểm trước khi coi một màn là "mượt"

- [ ] Mọi nút có hover, nhấn, thả. Bấm nhanh 10 lần liên tiếp không làm nút lệch kích thước.
- [ ] Không có thứ gì xuất hiện hoặc biến mất tức thì (trừ khi `Motion.Reduced`).
- [ ] Mở rồi đóng panel thật nhanh, lặp 5 lần: panel không kẹt nửa chừng, không tự tắt sau khi mở.
- [ ] Rời scene giữa lúc animation đang chạy: không có lỗi đỏ trong Console.
- [ ] Để tab ở nền 1 phút rồi quay lại: không có gì văng, đồng hồ đúng giờ thật.
- [ ] F3: FPS thấp nhất từ 55 trở lên trong lúc animation nặng nhất chạy.
- [ ] Bật giảm chuyển động: chỉ còn fade.
- [ ] Mọi số thời lượng và đường cong khớp bảng ở mục 3.
