# Module: âm thanh

- **Vị trí:** `Assets/Scripts/Services/AudioManager.cs` và các script cũ đã chuyển hướng
- **PRD:** M2 (phần dữ liệu), M3, lỗi B5 (phần `AudioListener`), B6 (phần API)
- **Kiểm chứng:** mới qua kiểm tra kiểu bằng stub. **Chưa nghe thử.**

## API

| Hàm | Tác dụng |
| --- | --- |
| `PlayMusic(clip, fade = 1.5)` | Crossfade sang bản mới (hai AudioSource A/B). Gọi lại với bản đang phát thì không làm gì |
| `StopMusic(fade)` | Tắt dần nhạc nền |
| `SetAmbience(clip, fade)` | Lớp âm thanh môi trường, lặp |
| `PlaySfx(clip, volumeScale)` / `PlaySfx(id)` | Hiệu ứng một lần. Id lạ thì bỏ qua |
| `RegisterSfx(id, clip)` | Đăng ký clip theo id (`SoundEffects` đăng ký `ui.click`) |
| `SetVolume(channel, 0..1)`, `GetVolume(channel)` | Âm lượng kênh, lưu ngay vào `SaveData.settings` |
| `SetMuted(bool)`, `Muted` | Tắt tiếng toàn bộ |
| `Register(source, channel, background)` | Đưa một AudioSource trong scene vào kênh; âm lượng hiện tại của nó thành mức gốc |
| `DuckBackground(bool, fade)` | Hạ hoặc trả nhạc nền khu vực, chỉ tác động nguồn đăng ký `background = true` |

Âm lượng thực của một nguồn = mức gốc x âm lượng kênh x hệ số fade x (hệ số duck nếu là nhạc nền), bằng 0 khi tắt tiếng.

Mỗi lần đổi cài đặt phát sự kiện `SettingsChanged` trên `Services.Events`.

## Script 1.0 đã chuyển hướng

| Script | Trước | Nay |
| --- | --- | --- |
| `PersistentAudio` (Music.prefab) | Chỉ `DontDestroyOnLoad` | Đăng ký AudioSource của nó vào kênh Music, là nhạc nền |
| `SoundEffects.PlayClick` | `src.PlayOneShot` | `Services.Audio.PlaySfx(clickSound)`; trường `src` giữ lại cho scene nhưng không dùng |
| `EntryButtonSound` | Gọi `SoundEffects.Instance` (lỗi null nếu scene thiếu) | `PlaySfx("ui.click")` |
| `TheatreSceneAudioController` | Mute mọi AudioSource `DontDestroyOnLoad` | `DuckBackground(true)` khi vào, `false` khi rời |
| `InstrumentStatic.ToggleMute` | Đặt `AudioListener.volume`, mute mọi AudioSource | Chỉ mute nguồn thuộc scene Theatre |

**Quy tắc:** không script nào được đụng `AudioListener.volume`.

## Chưa làm (Giai đoạn 1)

- Thanh trượt âm lượng và màn Cài đặt (M2 phần UI, F1)
- AudioMixer thật (M1), xem [../deviations.md](../deviations.md) V2
- Nhạc theo khu vực (M4), bộ 20 SFX (M5)
- Stem nhà hát bằng `PlayScheduled` (T1), `SongData` (T2), gộp script nhạc công (T3)
- **Quyền nhạc (B3) vẫn chưa xử lý.** `Assets/Audio/Howl/*` là bản cover "Merry-Go-Round of Life";
  `Starlight Ice Cream.mp3` không rõ nguồn. Cả hai đang nằm trong repo công khai và bản web công khai

## Lỗi có thể gặp

| Triệu chứng | Nguyên nhân có thể |
| --- | --- |
| Nhạc nền to gấp đôi hoặc không đổi theo thanh trượt | `Music.prefab` có AudioSource không nằm dưới `PersistentAudio` nên không được đăng ký |
| Vào nhà hát nhạc nền không nhỏ đi | Scene Theatre thiếu `TheatreSceneAudioController`, hoặc nhạc nền chưa đăng ký `background` |
| Rời nhà hát nhạc nền không trở lại | `restoreAudioOnExit` bị tắt trong scene |
| Không có tiếng click | `clickSound` chưa gán trên `SoundEffects` của scene đó; hoặc trình duyệt chặn âm thanh trước cú bấm đầu tiên |
| Stem nhà hát không đổi theo thanh Music | Chưa đăng ký stem vào `AudioManager` (việc của T1 đến T3) |
