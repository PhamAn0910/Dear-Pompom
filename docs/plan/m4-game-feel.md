# M4. Game feel từng màn

Mục đích: mỗi hoạt động có phản hồi riêng, đúng tinh thần "chậm, mềm, không giật". Mọi bước ở đây dùng công cụ
của M1 và M3; không bước nào cần vẽ tay. Các bước độc lập với nhau (trừ khi ghi "Cần trước"), làm theo thứ tự
bảng, mỗi bước một lượt CI nếu muốn.

Đọc trước: [game-feel.md](../guides/game-feel.md) mục 3, 10, 11; [animation-assets.md](../guides/animation-assets.md)
mục 5, 6.

## M4.1 Canvas phủ và `FxBurst`

**PRD:** A2. **Cần trước:** M3.1 (sprite hạt), M1.1.

**Tạo:**

| File | Nội dung |
| --- | --- |
| `Assets/Scripts/Services/UiRoot.cs` | Tạo canvas `[DearPompom UI]` (mã ở no-editor-unity.md mục 6), `DontDestroyOnLoad`. Có ba lớp con theo thứ tự: `Screens` (màn hình), `Fx` (hạt), `Toast`. Property tĩnh `UiRoot.Screens`, `UiRoot.Fx`, `UiRoot.Rect` |
| `Assets/Scripts/Motion/FxBurst.cs` | Theo hợp đồng ở animation-assets.md mục 6.2 |

Thêm `Services.Ui` và tạo nó trong `EnsureBooted`. Kiểm scene có `EventSystem` (mọi scene hiện tại đều có);
`UiRoot` không tự tạo `EventSystem`.

Phần toán của hạt đưa xuống Core để test: **tạo** `Assets/Scripts/Core/Motion/Particle.cs`:

```csharp
public struct Particle
{
    public float X, Y, VelocityX, VelocityY, Gravity, Age, Life, Spin, Rotation, StartScale;
    public bool Alive { get { return Age < Life; } }
    public void Step(float dt);            // tuổi += dt; vận tốc y -= gravity × dt; vị trí += vận tốc × dt; xoay += spin × dt
    public float Scale(FxStyle style);     // theo bảng ở animation-assets.md mục 6.2
    public float Alpha(FxStyle style);
}
public enum FxStyle { Pop, Rise, Fall }
```

Test `ParticleTests`: hạt chết đúng lúc `Age >= Life`; `Alpha` bằng 0 ở cuối đời với cả ba kiểu; `Scale` của
`Pop` vượt 1 rồi về 1; hai bước `dt/2` cho vị trí gần bằng một bước `dt` (sai số dưới 1%).

Số ngẫu nhiên: dùng `UnityEngine.Random.Range` ở lớp Unity để chọn vận tốc đầu, truyền vào struct.

**Kiểm:** Test xanh; Kiểu; CI. Trên web thêm phím thử tạm: trong `DebugOverlay`, khi bảng F3 đang bật, bấm `B`
thì `Fx.Burst` tại vị trí chuột, lần lượt ba kiểu.

```
| 61 | F3 rồi bấm B ở vài chỗ | Hạt toả ra tại con trỏ, mềm, tự biến mất; hạt nằm TRÊN mọi nút và hình | FxBurst, UiRoot |
| 62 | Bấm B liên tục 30 lần | Không giật; F3 không tụt dưới 55 fps | pool 40 hạt |
```

**Bẫy:** `Image` hạt phải `raycastTarget = false`, nếu không hạt sẽ nuốt cú bấm. `UiRoot` có `GraphicRaycaster`
(màn hình sau này cần), nên lớp `Fx` phải không chặn gì.

## M4.2 Màn mừng hoàn thành

**PRD:** A2. **Cần trước:** M4.1, M3.2 (clip `forest_walk` làm hình Pompom).

**Tạo:** `Assets/Scripts/Motion/Celebrate.cs` theo trình tự 5 bước ở animation-assets.md mục 6.3.

Nối: trong `Services.EnsureBooted`,
`Events.Subscribe<ActivityRecorded>(e => { if (Celebrate.ShouldPlay(e.Record)) Celebrate.Play(e.Record); });`

`ShouldPlay` (hàm thuần, đặt trong Core, có test):

| Loại hoạt động | Mừng khi |
| --- | --- |
| Pomodoro, Meditation | `completed == true` |
| MemoryCard, MathGame | `completed == true` (thắng hoặc chơi hết) |
| Journal | luôn (viết xong một bài) |
| ForestWalk, Theatre | không (đó là ghé thăm, không có điểm kết thúc) |

Dòng chữ theo loại (tiếng Anh, ngôn ngữ chính của game), để trong một bảng hằng số, có chỗ điền số phút:
`"{0} minutes of focus. Nice work."`, `"{0} quiet minutes. Well done."`, `"A page for today. Thank you."`,
`"All pairs found!"`, `"Score: {0}. Good thinking."`.

Hình Pompom: một `Image` trong `UiRoot.Screens`, sprite tạo từ sheet của `forest_walk` (khung thứ 2), kích thước
hiển thị cao 420 px trên canvas 1080.

**Kiểm trên web:**

```
| 63 | Pomodoro 1 phút tới hết | Màn mừng hiện: nền tối nhẹ, Pompom nảy ra, hạt bay lên, một dòng chữ; tự tắt sau khoảng 2 giây | Celebrate |
| 64 | Khi màn mừng đang hiện, bấm bất kỳ đâu | Tắt ngay, mềm | bỏ qua được |
| 65 | Pomodoro bị đặt lại giữa chừng | KHÔNG có màn mừng | ShouldPlay |
| 66 | Thắng game lật thẻ; lưu một bài nhật ký | Có màn mừng với câu chữ đúng loại | bảng câu chữ |
```

**Bẫy:**

- Hộp thư gợi ý của Pompom cũng hiện sau hoạt động (ở scene kế tiếp): hai thứ không được chồng nhau. Màn mừng
  hiện **ngay tại scene của hoạt động**; thư hiện sau khi đổi scene. Nếu người chơi đổi scene khi màn mừng còn
  hiện, `Celebrate` nghe `SceneChanging` và tắt ngay.
- Hết Pomodoro còn kích hoạt chuyển clip "bắt đầu nghỉ": màn mừng phủ lên trên, không sao.
- Không phát màn mừng hai lần cho cùng một bản ghi.

## M4.3 Lật thẻ

**PRD:** A9. **Cần trước:** M1.1.

**Sửa:** `School/Card.cs`, `School/GameSceneController2.cs`. Công thức ở game-feel.md mục 11.

1. `Card.Reveal()` và `Card.Unreveal()` chạy animation lật 250 ms (co ngang, đổi mặt, nở ra). Thêm
   `public bool IsFlipping`.
2. `GameSceneController2.CanReveal` trả `false` khi có thẻ đang lật.
3. `CheckMatch`: chờ thẻ thứ hai lật xong (250 ms) rồi mới so.
   - Khớp: `Tween.Punch` hai thẻ, `Fx.BurstAt` kiểu `Pop` tại mỗi thẻ, chờ 240 ms, `Tween.Fade` 200 ms, rồi mới
     `SetActive(false)` và `CheckForWinCondition()`.
   - Không khớp: `Tween.Shake` hai thẻ, chờ 280 ms, rồi `Unreveal()` (lật úp có animation).
4. Lúc chia bài: thẻ hiện lần lượt, trễ 30 ms mỗi thẻ, tối đa 12 thẻ có trễ, bằng `Tween.Scale` 0 → 1
   `OutBack` 250 ms.
5. Xoá các `Debug.Log` trong hai file.

**Điểm kỹ thuật phải đúng:**

- `GameSceneController2` đặt `card.transform.localScale` theo độ khó sau khi tạo thẻ. `MotionRest.Of(card)` phải
  được gọi **sau** dòng đó. Nếu thẻ được dùng lại ở ván sau với scale khác, gán lại `MotionRest.Of(t).Scale`.
- Lật là đổi `localScale.x` của thẻ từ `rest.x` về 0 rồi về `rest.x`; dùng `Tween.Value` với kênh riêng (100),
  không dùng `Tween.Scale` (nó scale đều cả hai trục).
- Thẻ đang lật không nhận bấm: kiểm `IsFlipping` ở đầu `OnPointerClick`.
- `BoxCollider2D` của thẻ co theo scale: trong lúc lật, bấm trúng hay trượt không quan trọng vì đã chặn.

**Kiểm trên web:**

```
| 67 | Lật một thẻ | Thẻ xoay lật trong 1/4 giây, không đổi hình tức thì | Card.Reveal |
| 68 | Lật hai thẻ khớp | Hai thẻ nảy lên, có hạt, rồi mờ đi; điểm tăng | ghép đúng |
| 69 | Lật hai thẻ không khớp | Hai thẻ lắc ngang rồi lật úp lại | ghép sai |
| 70 | Bấm thật nhanh vào 4 thẻ liên tiếp | Chỉ 2 thẻ được lật; không thẻ nào kẹt nửa chừng hay kẹt ngửa | CanReveal, IsFlipping |
| 71 | Chơi hết ván Easy, Medium, Hard | Thắng được; bản ghi "type": 5 có score như trước | không đổi logic |
```

**Bẫy:** ván kết thúc khi cặp cuối khớp: `CheckForWinCondition` phải chạy **sau** animation, và việc ghi hoạt
động (rồi màn mừng M4.2) chỉ xảy ra một lần.

## M4.4 Game toán

**PRD:** A9. **Cần trước:** M1.1, M2.2.

**Sửa:** `School/MathGameManager.cs`.

1. Thay coroutine `FlashOption` và mảng `optionFlashes` bằng `Tween.Flash(image, restColor, flashColor)`. Mảng
   màu nghỉ đã có từ lần sửa W1: giữ nó.
2. Đúng: nền nút nháy xanh lá `#7BC67B`; chữ của nút (object con `TextMeshProUGUI`) `Tween.Punch` 0,12; hạt `Pop`
   6 hạt màu `#FFE278` tại nút.
3. Sai: nền nút nháy đỏ `#E57373`; cha chung của bốn nút `Tween.Shake` 10 px. Nút đúng nháy xanh nhẹ để người
   chơi thấy đáp án.
4. Câu hỏi mới: chữ câu hỏi `Tween.Scale` 0,9 → 1 `OutCubic` 180 ms cùng fade 180 ms.
5. Điểm: số chạy lên 400 ms (`Tween.Value`). Streak mốc 3, 5, 10: punch mạnh dần 0,12 / 0,18 / 0,25.
6. Đồng hồ còn 5 giây trở xuống: chữ đồng hồ đổi sang `#E57373` và mỗi giây punch nhẹ 0,08.

**Kiểm trên web:**

```
| 72 | Trả lời đúng | Nút nháy xanh rồi về màu cũ, chữ nảy, có hạt; câu mới hiện mềm | MathGameManager |
| 73 | Trả lời sai | Nút nháy đỏ, cả hàng nút lắc; nút đúng nháy xanh | báo sai |
| 74 | Trả lời thật nhanh 10 câu liên tiếp | Không nút nào kẹt màu (lỗi W1 không tái diễn) | Tween.Flash một chủ |
| 75 | Để đồng hồ xuống dưới 5 giây | Số đỏ và nảy theo từng giây | đồng hồ |
```

**Bẫy:** `ButtonFeel` đang giữ scale của nút: không `Punch` chính nút, chỉ punch chữ con (luật "một thuộc tính
một chủ"). `Tween.Flash` huỷ lần nháy trước trên cùng `Image`, và luôn về **màu nghỉ đã lưu**.

## M4.5 Vòng thở khi thiền

**PRD:** A8 (bản tối thiểu: bài 4-4-4; chọn bài thở là C9, làm sau). **Cần trước:** M2.2, M3.1.

**Tạo:**

- `Assets/Scripts/Core/Motion/Breathing.cs` (thuần, có test):
  `BreathPhase At(double elapsedSeconds, float inhale, float hold, float exhale)` trả pha (`Inhale`, `Hold`,
  `Exhale`), tiến độ 0..1 trong pha, và `Size` 0..1 đã qua `InOutSine` (hít: 0 → 1, giữ: 1, thở: 1 → 0).
  Test: đầu chu kỳ `Size = 0`; cuối pha hít `Size = 1`; suốt pha giữ `Size = 1`; chu kỳ lặp đúng sau 12 giây;
  `hold = 0` không chia cho 0.
- `Assets/Scripts/Motion/BreathRing.cs`: hai `Image` trong `UiRoot.Screens`: vòng ngoài cố định (`ring`, alpha
  0,35) và đĩa trong (`dot`, màu `#BEE6FF`, alpha 0,5) scale từ 0,6 tới 1 theo `Size`. Một dòng chữ nhỏ bên dưới:
  `"Breathe in"`, `"Hold"`, `"Breathe out"`, đổi bằng fade 200 ms, chỉ gán khi pha đổi.

**Sửa:** `Forest/MeditationTimer.cs`: khi phiên bắt đầu, `BreathRing.Show(_timer)`; khi kết thúc hoặc bị ngắt,
`BreathRing.Hide()`. Vòng đọc `_timer.Elapsed` mỗi khung nên không bao giờ lệch đồng hồ.

Vị trí: góc phải, tâm tại (1500, 540) trên canvas 1920×1080, đường kính 300. Không che Pompom (ở giữa trái).

**Kiểm trên web:**

```
| 76 | Thiền 1 phút | Vòng tròn nở 4 giây, giữ 4 giây, co 4 giây, lặp lại; chữ đổi theo pha | BreathRing |
| 77 | Chuyển tab 10 giây rồi quay lại | Vòng ở đúng pha theo đồng hồ, không chạy bù | đọc từ Countdown |
| 78 | Hết giờ hoặc bấm đặt lại | Vòng mờ đi | Hide |
```

**Bẫy:** `Motion.Reduced`: vòng không co giãn, chỉ còn chữ đổi theo pha.

## M4.6 Bản đồ sống và zoom vào toà nhà

**PRD:** A4, A5. **Cần trước:** M2.1, M1.3.

**Tạo:** `Assets/Scripts/Motion/Ambient.cs` (animation-assets.md mục 5), `Assets/Scripts/Directors/WorldDirector.cs`.

Phần A, chuyển động trang trí. Tên object theo `docs/scenes/World.md` (đều là con của `Canvas`):

| Object | Hiệu ứng | Số |
| --- | --- | --- |
| `House`, `School`, `Theatre` | `Breathe` | amount 0,012, period 5 s, pha theo vị trí |
| `Forest` | `Sway` quanh đáy | amount 0,6°, period 6 s |
| `Progress` (cối xay) | `Sway` quanh đáy | amount 1°, period 4 s |
| 3 đám mây (tạo mới bằng code, sprite `dot` kéo dãn hoặc sprite mây sinh thêm trong `make_fx_sprites.py`) | `Drift` ngang qua bản đồ | 0,15–0,3 đơn vị/s, alpha 0,7, nằm trên bản đồ, dưới UI |

Phần B, rê chuột: `Image.color` của toà nhà `(0.92, 0.92, 0.92)` khi thường, trắng khi rê (150 ms). Gắn bằng một
component nhỏ `HoverTint` nghe `IPointerEnterHandler`/`IPointerExitHandler`.

Phần C, zoom vào toà nhà khi bấm (A4): thêm vào `SceneTransitionService` hàm
`GoWithZoom(string sceneName, Vector3 worldFocus)`. Trong lúc mờ ra (180 ms), camera World trượt về `worldFocus`
và `orthographicSize` giảm 25% bằng `InQuad`. `LoadSceneDelay.OnButtonClick`: nếu scene hiện tại là `World` thì
gọi `GoWithZoom(sceneName, transform.position)`, ngược lại `Go`. `PanZoom` phải ngừng điều khiển camera khi
`Services.Transitions.IsBusy`. `Motion.Reduced`: dùng `Go` thường.

**Kiểm trên web:**

```
| 79 | Nhìn bản đồ 10 giây không chạm | Các toà nhà "thở" rất nhẹ, không đồng loạt; mây trôi | Ambient |
| 80 | Rê chuột qua từng toà nhà | Toà nhà sáng lên và to nhẹ | HoverTint, ButtonFeel |
| 81 | Bấm một toà nhà | Camera lao nhẹ về phía toà nhà trong lúc màn hình mờ đi | GoWithZoom |
| 82 | Kéo bản đồ khi các toà nhà đang chuyển động | Không rung, không lệch | Ambient + PanZoom |
```

**Bẫy:**

- `Breathe` đổi scale, `ButtonFeel` cũng đổi scale của cùng object: **hai chủ**. Cách xử lý: `Ambient` không
  chạm vào object nút. Trong `WorldDirector`, tạo một object con mới mang bản sao `Image` (cùng sprite, cùng
  kích thước, `raycastTarget = false`), tắt hiển thị `Image` của nút bằng `color.a = 0` nhưng vẫn giữ
  `raycastTarget` để nhận bấm, và cho `Ambient` chạy trên object con. `HoverTint` đổi màu `Image` của object con.
- Biên độ `Breathe` 0,012 là rất nhỏ một cách có chủ ý. Không tăng nếu An không yêu cầu.
- Cánh cối xay quay (PRD A5) cần hình cánh tách riêng: chưa có (animation-assets.md mục 7). Ghi yêu cầu vào
  `docs/art-requests.md`, không tự cắt.

## M4.7 Nhà hát theo nhịp

**PRD:** A7. **Cần trước:** M5.4 (stem đồng bộ bằng `dspTime`). Nếu M5.4 chưa làm, làm bản rút gọn dưới đây và
ghi rõ trong dev-log.

Bản rút gọn (không cần BPM chính xác): nhạc công đang chơi nhún theo `AudioSource.time` của stem của nó với một
BPM ước lượng đặt làm hằng số; nhạc công bị tắt thì đứng yên, xoay −6° và mờ còn 70% bằng tween 300 ms (thay cho
việc chỉ đổi alpha tức thì trong `InstrumentButton.UpdateButtonColor`).

Bản đầy đủ: công thức ở game-feel.md mục 11 ("Nhạc công theo nhịp"), BPM lấy từ `SongData`.

Object: `Canvas/Violin`, `Canvas/Piano` (theo `docs/scenes/Theatre.md`). Cả hai là `Button` có `ButtonFeel`: áp
cùng cách "object con mang hình" như M4.6 để tránh hai chủ.

**Kiểm trên web:**

```
| 83 | Theatre: bấm Start | Hai nhạc công nhún đều theo nhạc | nhịp |
| 84 | Bấm vào chó violin để tắt | Chó hạ người, mờ nhẹ, ngừng nhún; tiếng violin tắt; mèo vẫn nhún | tắt stem |
```

## M4.8 Đổi buổi trong ngày bằng hoà màu

**PRD:** A6. **Cần trước:** M3.5.

`PomodoroDayNight.UpdateTimeOfDay` phát hiện đổi buổi (6 giờ, 12 giờ, 18 giờ). Khi `currentTimeOfDay` đổi, gọi
`UpdateBackground` với thời lượng hoà **4 giây** và đường cong `InOutSine` thay cho 250 ms. Thêm tham số thời
lượng cho hàm hoà đã viết ở M3.5.

Không dùng ánh sáng URP 2D (PRD nhắc tới nhưng cần editor để dựng `Light2D` và material Lit): ba bộ tranh sáng,
trưa, tối đã vẽ sẵn ánh sáng, hoà chéo là đủ. Ghi vào deviations.md.

Để thử mà không chờ tới giờ: trong `DebugOverlay`, phím `T` (khi F3 bật) gọi một hàm thử của
`PomodoroDayNight` ép sang buổi kế tiếp.

```
| 85 | Home, F3, bấm T | Tranh chuyển từ buổi này sang buổi kế trong khoảng 4 giây, mềm, Pompom vẫn chuyển động | hoà 4 giây |
```

## M4.9 Màn bắt đầu và rà soát giảm chuyển động

**PRD:** A11, C4 (ấn tượng đầu). **Cần trước:** M1.2.

`StartMenuDirector`: khi scene tải xong, các phần tử của `Canvas/Background` hiện lần lượt: `Dear` (trễ 100 ms),
`Pompom` (200 ms), `PlayButton` (400 ms), mỗi cái `Tween.Scale` 0 → 1 `OutBack` 300 ms. `PlayButton` thêm
`Breathe` amount 0,02 period 2,4 s trên object con mang hình (cùng cách M4.6) để mời bấm.

Rà soát `Motion.Reduced`: đặt tạm `Motion.Reduced = true` bằng phím `R` trong `DebugOverlay`, đi hết 11 scene,
đối chiếu bảng ở game-feel.md mục 10. Ghi mọi thứ còn chuyển động sai vào bug-log và sửa.

```
| 86 | Tải lại trang | Chữ Dear, Pompom và nút Play lần lượt nảy ra | StartMenuDirector |
| 87 | F3, bấm R, đi hết các scene | Chỉ còn fade; không gì lắc, nảy, bay; clip Pompom vẫn chạy | Motion.Reduced |
```

## M4.10 Biểu đồ có chuyển động

**PRD:** A10 (ưu tiên P2). **Cần trước:** M5.9 (một màn Progress). Không làm trước M5.9: hai hệ vẽ biểu đồ cũ sẽ
bị gộp, làm animation cho chúng bây giờ là bỏ đi.

Khi làm: cột mọc từ 0 lên chiều cao thật (400 ms, `OutCubic`, trễ 30 ms mỗi cột), số tóm tắt chạy lên bằng
`Tween.Value`.
