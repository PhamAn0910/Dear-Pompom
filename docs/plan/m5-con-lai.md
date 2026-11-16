# M5. Phần còn lại của PRD

Các mục P0 của PRD nằm ngoài animation và thao tác. Mỗi bước ở đây được viết ở mức **quyết định thiết kế và ranh
giới**, chưa tới mức từng dòng như M0–M4. Quy tắc: **trước khi code một bước M5, viết phần chi tiết của nó vào
chính file này** theo đúng khuôn của M1–M4 (Tạo, Sửa, Stub, Kiểm trên web, Xong khi, Bẫy), đưa An xem, rồi mới làm.

Thứ tự đề nghị: M5.1 → M5.2 → M5.3 → M5.5 → M5.6 → M5.7 → M5.8 → M5.4 → M5.9.

## M5.1 Bộ dựng UI bằng code (`UiKit`)

**Vì sao trước tiên:** Cài đặt, hòm thư, onboarding đều là màn hình mới; không có editor nên phải dựng bằng code.
Không có bộ dựng chung thì mỗi màn sẽ tự chế, lệch nhau.

**Thiết kế:**

- `Assets/Scripts/Ui/UiKit.cs`, các hàm tĩnh tạo object dưới một `Transform` cha trong `UiRoot.Screens`:
  `Panel(parent, size)`, `Label(parent, text, size)`, `Button(parent, text, onClick)`, `Slider(parent, value, onChange)`,
  `Toggle(parent, value, onChange)`, `Row(parent)`, `Column(parent)` (layout group), `Scroll(parent)`.
- Mọi `Button` tạo ra tự có `ButtonFeel`. Mọi màn mở và đóng qua `PanelMotion`.
- Chữ: `TextMeshProUGUI` thêm bằng `AddComponent` tự lấy font mặc định của TMP (`TMP_Settings.defaultFontAsset`).
  Font này chỉ có ký tự Latin cơ bản: tiếng Việt có dấu sẽ thành ô vuông. Song ngữ (C10) cần font khác, thuộc
  Giai đoạn 4 của PRD; ghi vào deviations.md, UI viết tiếng Anh.
- Nền panel: không có sprite 9 mảnh dựng sẵn. Dùng `Assets/Art/UserProgress/message panel.png` (đã có trong game)
  chép sang `Assets/Resources/Ui/panel.png`, tạo sprite có viền co giãn bằng
  `Sprite.Create(tex, rect, pivot, 100, 0, SpriteMeshType.FullRect, border)` với `border` đo bằng mắt từ ảnh, và
  `Image.type = Image.Type.Sliced`.
- Bảng màu và cỡ chữ là hằng số trong `UiKit.Theme`: màu chữ nâu đen `#3A2A20`, nền giấy `#FFF6E3`, nhấn
  `#E8A84C`; cỡ chữ 28 (thân), 40 (tiêu đề), 22 (phụ).
- Điều hướng: `UiScreens.Open(screen)` / `Close()`, một màn tại một thời điểm, có lớp tối phía sau chặn bấm; Escape đóng.

**Xong khi:** có một màn thử (mở bằng phím trong `DebugOverlay`) chứa đủ mỗi loại phần tử một cái, hiển thị đúng
ở 1920×1080 và 1280×720.

## M5.2 Màn Cài đặt và menu tạm dừng

**PRD:** F1, F5, M2, A11. **Cần trước:** M5.1.

- Nút bánh răng cố định ở góc trên phải của `UiRoot` (mọi scene). Sprite: sinh bằng `make_fx_sprites.py` (thêm
  hình `gear`), hoặc chữ "≡" tạm thời.
- Nội dung: ba thanh trượt (`AudioChannel.Music`, `Sfx`, `Ambience`) gọi `Services.Audio.SetVolume`; nút tắt
  tiếng gọi `SetMuted`; công tắc "Reduce motion" ghi `settings.reduceMotion`, lưu, phát `SettingsChanged`; nút
  toàn màn hình (`Screen.fullScreen = !Screen.fullScreen`); nút "Back to map" (`Services.Transitions.Go("World")`).
- Kéo thanh trượt phải nghe thấy ngay: `SetVolume` mỗi lần giá trị đổi, nhưng **chỉ ghi save khi thả tay**
  (hoặc khi đóng màn), không ghi mỗi khung.
- Xuất và xoá dữ liệu (D7) thuộc Giai đoạn 4; để chỗ trống.
- Tạm dừng: mở màn Cài đặt thì các `Countdown` đang chạy **không** dừng (Pomodoro là giờ thật). Riêng đồng hồ
  câu hỏi của game toán gọi `Pause()`/`Resume()` (đã ghi chú ở M2.2).

## M5.3 Âm thanh: nhạc theo khu, SFX, giấy phép

**PRD:** M4, M5, B3, 6.3. **Cần trước:** M1.3 (sự kiện `SceneChanging`).

- **Giấy phép trước.** Tạo `Assets/Audio/LICENSES.md`. Hai file hiện có ("Merry-Go-Round of Life" cover,
  "Starlight Ice Cream") không rõ quyền: không dùng trong bản 2.0. Không tự tải nhạc từ mạng về repo khi chưa có
  An duyệt nguồn và giấy phép từng file.
- **SFX tự sinh, không cần quyền.** Viết `Tools/audio/make_sfx.py` tổng hợp WAV bằng numpy (sóng sin, nhiễu lọc,
  bao biên độ): bấm, mở panel, đóng panel, lật thẻ, đúng, sai, chuông hết giờ, thư đến, viết bút, mở chương, và
  các biến thể cao độ, đủ 20 tiếng. Mono, 44,1 kHz, 16 bit, mỗi tiếng dưới 1 giây, đỉnh −6 dB, có fade 5 ms hai
  đầu để không lách tách. Ra `Assets/Resources/Sfx/`. Nghe thử từng file trước khi commit (An nghe).
- Đăng ký id: `Services.Audio.RegisterSfx(id, clip)` lúc khởi động cho mọi file trong bảng `SfxIds`; gọi bằng
  `PlaySfx(id)`. Tên id dạng `ui.click`, `card.flip`, `math.correct`.
- **Tiếng phát lúc nhấn xuống**, không phải lúc thả: thêm `PlaySfx("ui.press")` vào `ButtonFeel.OnPointerDown`
  và gỡ tiếng click cũ khỏi luồng `onClick` bằng cách cho `SoundEffects.PlayClick()` không làm gì (giữ hàm vì
  scene gọi nó).
- **Nhạc theo khu:** bảng `scene → track`; nghe `SceneChanging`, gọi `PlayMusic(clip, 1.5f)`; vào Theatre thì
  `StopMusic`. Nhạc nền mới: tự sinh bằng script (hợp âm đệm chậm, vài nốt giai điệu ngẫu nhiên có hạt giống cố
  định) là phương án không cần quyền; chất lượng do An nghe và quyết.
- Tần số lấy mẫu và nén: OGG Vorbis cho nhạc (Unity tự chuyển), WAV cho SFX.

## M5.4 Nhà hát: stem đồng bộ và `Musician`

**PRD:** T1, T2, T3, B5. **Cần trước:** M5.3 (có nhạc hợp pháp có stem).

- Mọi stem bắt đầu bằng `PlayScheduled(AudioSettings.dspTime + 0.2)`, cùng một mốc. Tắt một nhạc cụ là đưa
  `volume` về 0 bằng tween 150 ms, **không** `Stop`, không `mute` đột ngột.
- `SongData` theo PRD là ScriptableObject: không tạo được asset, nên là class C# thường trong code hoặc JSON trong
  `Resources/Songs/` (tên, BPM, danh sách stem, giấy phép). Ghi vào deviations.md.
- Một component `Musician` thay cho `InstrumentAnimator`, `InstrumentController`, `InstrumentStatic`,
  `InstrumentButton`. Scene Theatre đang gọi `InstrumentButton.ToggleInstrumentMute`: giữ class và hàm đó, cho nó
  chuyển lời gọi sang `Musician`.
- Sau bước này quay lại làm bản đầy đủ của M4.7.

## M5.5 Hòm thư và thư của Pompom

**PRD:** C1, C2, R3. **Cần trước:** M5.1.

- Dữ liệu: `LetterState` trong `SaveData` (danh sách thư đã nhận: id, thời điểm, đã đọc). Thêm field mới có giá
  trị mặc định thì không cần tăng `schemaVersion` (xem ghi chú đầu `SaveData.cs`).
- Nội dung thư: bảng trong Core (`Letters.cs`): id, loại (cốt truyện hoặc ngữ cảnh), chương, mẫu chữ có chỗ điền.
  Viết 12 thư cốt truyện trước (PRD 9.3). Giọng văn theo PRD 4.4: ấm, ngắn, cụ thể, không phán xét, không bao giờ
  trách vắng mặt. Mỗi thư tối đa 60 từ.
- `RecommendationService` đã chọn gợi ý: đổi đích giao từ hộp thoại bật lên sang "thêm thư vào hòm". Hộp thoại cũ
  (`RecommendationSystem`) ngừng hiện. Giữ class vì scene StartMenu tham chiếu.
- UI: biểu tượng phong bì cạnh nút bánh răng, có chấm "chưa đọc" (`Breathe` nhẹ khi có thư mới). Bấm mở màn hòm
  thư: danh sách bên trái, thư đang đọc bên phải với chân dung Pompom (animation-assets.md mục 7), nút "Go now"
  gọi `Services.Transitions.Go(scene)` và ghi sự kiện đã nhận lời.
- Thư đến: phong bì bay từ giữa màn hình về biểu tượng (500 ms `InOutCubic`), biểu tượng `Punch`, tiếng `mail.arrive`.

## M5.6 Chương và trạng thái làng

**PRD:** C5. `ChapterRules` đã có trong Core (có test).

- `ChapterService` nghe `ActivityRecorded`, hỏi `ChapterRules`, nếu lên chương thì ghi `save.chapter`, phát
  `ChapterUnlocked`, thêm thư cốt truyện của chương.
- Bản đồ đổi theo chương mà không có tranh mới: Chương 1 các toà nhà chưa mở được tô xám 60% và không `Breathe`;
  mở chương thì toà nhà tương ứng sáng lên bằng tween 1 giây kèm hạt `Rise`. Trang trí thật (cờ, đèn lồng, khán
  giả) là yêu cầu hình mới: ghi vào `docs/art-requests.md`.
- Khoá cứng hay chỉ gợi ý? PRD nói "làng sống lại", không nói khoá hoạt động. Mặc định: **không khoá** hoạt động
  nào, chỉ đổi hình. Hỏi An nếu muốn khác.

## M5.7 Onboarding Chương 1 và gợi ý viết nhật ký

**PRD:** C3, C4. **Cần trước:** M5.5.

- Lần chạy đầu (save mới, chưa có bản ghi nào): sau StartMenu, thư chào của Pompom mở sẵn; nút "Go now" đưa tới
  Home; ở Home, ô phút điền sẵn 5 và nút Start có `Breathe` mời bấm; xong Pomodoro thì thư trả lời đến. Dưới 5
  phút, không có màn chữ dài.
- 30 câu gợi mở nhật ký: bảng trong Core, chọn theo `ngày trong năm mod 30`, hiện làm placeholder của ô nhập, có
  nút "another" đổi câu. Bỏ qua được bằng cách cứ gõ.

## M5.8 Pomodoro đầy đủ và đồng hồ sống qua scene

**PRD:** F2, F3. **Cần trước:** M2.2.

- `PomodoroSession` trong Core (thuần, có test): máy trạng thái học → nghỉ ngắn → … → nghỉ dài sau 4 vòng, dựa
  trên `Countdown`. Mặc định 25/5/15.
- Chuyển `Countdown` của Pomodoro và thiền từ script của scene sang một dịch vụ (`Services.Timers`) để rời scene
  không ngắt phiên. Khi đó `PomodoroDayNight.OnDisable` không còn ghi "bị ngắt". Đồng hồ nhỏ ở góc `UiRoot` hiện
  khi có phiên đang chạy và người chơi ở scene khác.
- Đây là thay đổi hành vi ghi dữ liệu: viết test cho mọi đường (hoàn thành, huỷ, rời scene rồi quay lại, đóng
  tab giữa phiên) trước khi sửa script scene.

## M5.9 Một màn Progress

**PRD:** B4, P1–P3, P6. Quyết định giữ `GraphHandler` hay `GraphRenderer` cần thử cả hai (PRD 9.4, câu 8); không
có editor nên phép thử là đọc code và chạy trên web. Mặc định đề nghị: viết mới ba thành phần P1, P2, P3 bằng
`UiKit` + `Image` (thẻ tóm tắt, lịch nhiệt là lưới ô vuông, cột chồng là các `Image` xếp chồng), đọc từ
`ProgressStats` (đã có, có test), rồi cho hai scene `Progress` và `UserProgress` cùng mở màn mới. Hai hệ vẽ cũ
ngừng dùng. Không hiển thị streak, không tô đỏ ngày vắng (PRD 7.2).

## Các mục chưa xếp bước

| PRD | Ghi chú |
| --- | --- |
| C6–C9, T4–T6, M6, M7, P4, P5, P7, R4, F4, F6 | Ưu tiên P1/P2; viết bước khi M5.1–M5.9 xong |
| C10 song ngữ | Cần font có dấu tiếng Việt cho TMP: tạo font asset cần editor. Hỏi An |
| D7 xuất và xoá dữ liệu | Trên web: xuất bằng cách tải file qua jslib |
| M1 AudioMixer | Cần editor (deviations V2); giữ 3 kênh bằng code |
| Build Windows, macOS | Ngoài khả năng của máy này; CI có thể build thêm nền tảng |
