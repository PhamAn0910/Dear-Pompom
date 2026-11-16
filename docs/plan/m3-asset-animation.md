# M3. Chuyển asset animation

Mục đích: mọi animation vẽ tay hiện có của Pompom chạy bằng **clip ô vá**, trông giống hệt bản cũ, với khoảng
một phần tám bộ nhớ và dung lượng tải. Không vẽ gì mới.

Đọc trước: [animation-assets.md](../guides/animation-assets.md) mục 1 đến 4 và mục 9.

Nguyên tắc an toàn xuyên suốt mốc này: **clip mới tải hỏng thì Animator cũ vẫn chạy.** Chỉ tắt component cũ sau
khi `PatchClipPlayer.Create` trả về khác `null`. Asset cũ chỉ bị gỡ ở M3.6, sau khi An xác nhận trên web.

## M3.1 Luật import, sprite hạt, thư mục `Resources`

**PRD:** 5.2. **Cần trước:** M0.3.

**Tạo:**

1. `Assets/Editor/TextureImportRules.cs`: mã mẫu ở animation-assets.md mục 4.
2. Sprite hạt:
   ```bash
   python3 Tools/art/make_fx_sprites.py Assets/Resources/Fx
   ```
3. Meta:
   ```bash
   python3 Tools/gen_meta.py
   ```

Không thêm `Assets/Editor/` vào `UnityTypecheck.csproj` (stub không có `UnityEditor`).

Thêm một dòng log để thấy luật import có chạy trên CI: cuối `OnPreprocessTexture`,
`Debug.Log("[DearPompom] import rules applied: " + path);`

**Kiểm:**

- `python3 Tools/gen_meta.py --check` xanh.
- CI: build xanh, và log của job "Build WebGL" có `import rules applied: Assets/Resources/Fx/dot.png`. Tìm:
  ```bash
  gh run view <id> -R PhamAn0910/Dear-Pompom --log | grep "import rules applied" | head
  ```
  Nếu không có dòng nào: CI đang dùng cache `Library` cũ và không import lại file mới là điều không thể (file
  mới luôn được import), vậy nghĩa là script không chạy. Xem lỗi biên dịch của assembly Editor trong log.

**Xong khi:** CI xanh và log có dòng import cho cả 6 sprite hạt.

**Bẫy:**

- Thư mục phải tên đúng `Editor` (hoa chữ E) để Unity không đưa script vào bản build. Sai tên thì build WebGL
  hỏng với lỗi `UnityEditor` không tồn tại.
- `.meta` do `gen_meta.py` sinh chỉ có GUID; phần cài đặt do `TextureImportRules` điền lúc import. Đừng viết tay
  phần `TextureImporter` vào meta.
- Sáu file PNG 128×128 là rất nhỏ; nếu `git status` cho thấy file lớn lạ trong `Assets/Resources`, dừng lại.

## M3.2 `PatchClipPlayer` và clip đi bộ

**PRD:** 5.2, A3. **Cần trước:** M3.1, M1.1. Đây là lần đầu `PatchClipPlayer` chạy trên Unity thật: làm trên clip
nhỏ nhất.

**Làm:**

1. Tách clip:
   ```bash
   python3 Tools/art/extract_patches.py Assets/Art/Forest/mainwalk1j Assets/Resources/Clips/forest_walk --name forest_walk --fps 12 --verify
   ```
   Phải thấy `mode=trim frames=8` và `PSNR 99.0`. Kết quả: `forest_walk.json`, `forest_walk_sheet0.png` (2048×1024).
2. `python3 Tools/gen_meta.py`
3. **Tạo** `Assets/Scripts/Motion/PatchClipPlayer.cs` (mã mẫu animation-assets.md mục 3.4).
4. **Tạo** `Assets/Scripts/Motion/ClipSwap.cs`, hàm dùng chung cho cả mốc:
   ```csharp
   /// <summary>Puts a patch clip on top of an old Animator object and silences the old components.</summary>
   public static PatchClipPlayer Replace(Transform old, string clipName)
   ```
   Thân hàm theo đoạn mã ở animation-assets.md mục 3.5: lấy `SpriteRenderer` cũ (nếu `null` thì log và trả
   `null`), tạo player làm con, thành công thì tắt `Animator` (nếu có) và `SpriteRenderer` cũ. Gọi lại lần hai
   trên cùng object thì trả player đã có (`old.GetComponentInChildren<PatchClipPlayer>(true)`).
5. **Tạo** `Assets/Scripts/Directors/ForestWalkingDirector.cs`:
   ```csharp
   public static void Attach(Scene scene)
   {
       Transform character = SceneFind.ByPath(scene, "Character");
       if (character != null) ClipSwap.Replace(character, "forest_walk");
   }
   ```
   Thêm `case "ForestWalking": Run(scene, "ForestWalking", ForestWalkingDirector.Attach); break;` vào
   `SceneDirectors`.
6. `DebugOverlay.SetLine("clips", <số player đang sống>)`: `PatchClipPlayer` tăng một biến đếm tĩnh trong `Build`,
   giảm trong `OnDestroy`.

**Stub cần thêm:** `Resources.Load<T>(string)`, `TextAsset.text`, `Texture2D` (`width`, `height`), `Sprite`,
`Sprite.Create(Texture2D, Rect, Vector2, float, uint, SpriteMeshType)`, `SpriteMeshType`,
`SpriteRenderer` (`sprite`, `sortingLayerID`, `sortingOrder`, `enabled`, `color`), `Animator : Behaviour`,
`JsonUtility.FromJson<T>` (đã có), `Object.Destroy`.

**Kiểm trên web:**

```
| 45 | Forest > Walk | Pompom đi bộ như bản cũ: cùng vị trí, cùng cỡ, cùng tốc độ bước; không có ô vuông lạ quanh nhân vật | PatchClipPlayer, forest_walk |
| 46 | Ở màn đi bộ bấm F3 | Dòng clips 1 | bộ đếm clip |
| 47 | Xem Console khi vào màn đi bộ | Không có lỗi đỏ; không có "[DearPompom] ... missing" | Resources |
```

Để so "như bản cũ" cần ảnh chụp màn hình bản hiện tại: **trước khi gộp bước này vào `main`**, nhờ An chụp màn
đi bộ, màn thiền (3 trạng thái) và Nhà (học, nghỉ) trên bản web hiện tại. Lưu vào `docs/reference-shots/`.

**Xong khi:** ba dòng đạt và An xác nhận hình giống ảnh chụp cũ.

**Nếu sai, tra theo triệu chứng:**

| Thấy | Nguyên nhân có khả năng nhất | Sửa |
| --- | --- | --- |
| Nhân vật lệch chỗ so với cũ | Sai dấu trục y, hoặc object con có `localPosition` khác 0 | Dùng `PatchClipData.RegionCentre`; kiểm `SetParent(parent, false)` |
| Nhân vật to hoặc nhỏ hơn | Pixel mỗi đơn vị khác 100, hoặc sheet bị co lúc import | Log `sheet.width + "x" + sheet.height`: phải bằng kích thước file PNG. Nếu nhỏ hơn: luật import không chạy |
| Hình bị cắt sai, thấy mảnh của khung khác | Lật trục y khi tạo rect | Dùng `PatchClipData.PatchTextureRect` với `sheet.height` |
| Vẫn là hình cũ, không đổi | Director không chạy hoặc `Create` trả `null` | Xem Console tìm `[DearPompom]` |
| Không thấy nhân vật | Thứ tự vẽ thấp hơn nền | Dùng `sortingLayerID` và `sortingOrder` của `SpriteRenderer` cũ |
| Viền nhân vật có quầng sáng | `alphaIsTransparency` chưa bật | Luật import |

## M3.3 Clip thiền

**PRD:** 5.2, A3. **Cần trước:** M3.2 đạt mức Web.

**Làm:**

1. Chép khung ra thư mục tạm (thư mục tạm nằm trong scratchpad của phiên làm việc, không nằm trong project) rồi
   tách ba clip. Tên file gốc có dấu tiếng Việt; chép bằng Python để tránh lỗi tên trong shell:
   ```python
   import glob, shutil, os
   src = sorted(glob.glob("Assets/Art/Forest/Meditation/*.jpg"))   # 15 khung, thứ tự 0000..0014
   assert len(src) == 15
   for name, frames in (("start", src[0:3]), ("loop", src[3:15])):
       os.makedirs(tmp + "/" + name, exist_ok=True)
       for i, f in enumerate(frames):
           shutil.copy(f, "%s/%s/%04d.jpg" % (tmp, name, i))
   ```
   ```bash
   python3 Tools/art/extract_patches.py <tmp>/start Assets/Resources/Clips/forest_meditate_start --name forest_meditate_start --no-loop --verify
   ```
   ```bash
   python3 Tools/art/extract_patches.py <tmp>/loop Assets/Resources/Clips/forest_meditate --name forest_meditate --verify
   ```
   ```bash
   python3 Tools/art/extract_patches.py Assets/Art/Forest/Meditationrest Assets/Resources/Clips/forest_meditate_rest --name forest_meditate_rest --verify
   ```
2. **Tạo** `Assets/Scripts/Directors/ForestSittingDirector.cs`. Bảng ghép (tên object theo
   `docs/scenes/ForestSitting.md`):

   | Object trong scene | Clip |
   | --- | --- |
   | `Awake` | `forest_meditate_rest` |
   | `StartMeditating` | `forest_meditate_start` |
   | `Meditation` | `forest_meditate` |

3. `MeditationTimer` bật tắt ba object này bằng `SetActive`: không sửa.

**Kiểm trên web:**

```
| 48 | ForestSitting: trước khi bấm Start | Pompom ngồi nghỉ, chuyển động như cũ | forest_meditate_rest |
| 49 | Bấm Start với 1 phút | Chuyển sang tư thế thiền như cũ | forest_meditate_start, forest_meditate |
| 50 | Nhìn kỹ toàn màn hình khi đang thiền | Không có đường kẻ lưới, không có ô vuông nhấp nháy | khe giữa các ô |
```

**Xong khi:** ba dòng đạt.

**Bẫy:**

- Clip `start` chỉ có 3 khung, không lặp, giữ khung cuối. Bản cũ lặp 3 khung đó liên tục tới khi script đổi trạng
  thái. Nếu An thấy khác bản cũ theo hướng xấu, bỏ `--no-loop` và tách lại.
- Dòng 50 là phép thử khe hở. Nếu thấy đường kẻ: áp cách sửa "+0,5 pixel" ở animation-assets.md mục 3.4 cho
  **mọi** clip chế độ `patch` (sửa trong `SpriteFor`, kiểm `Data.mode == "patch"`).
- Ảnh nền của ba clip gần như cùng một bức tranh: đúng, vì mỗi clip tự mang nền của nó. Chưa gộp nền ở bước này.

## M3.4 Chín clip ở Nhà, tải lười

**PRD:** 5.2, A3. **Cần trước:** M3.3 đạt mức Web.

**Làm:**

1. Tách 9 clip. Với mỗi buổi `B` trong `Morning`, `Noon`, `Night` (viết thường trong tên clip):

   | Clip | Nguồn | Khung | Cờ |
   | --- | --- | --- | --- |
   | `home_<b>_study` | `Assets/Art/House/<B>Study/` | cả 24 | |
   | `home_<b>_startrest` | `Assets/Art/House/<B>Rest/` | 0000–0010 (11 khung) | `--no-loop` |
   | `home_<b>_rest` | `Assets/Art/House/<B>Rest/` | 0011–0023 (13 khung) | |

   Hai clip nghỉ dùng chung thư mục: chép khung ra thư mục tạm như ở M3.3. Kiểm `len(src) == 24` trước khi cắt.
   Mỗi lệnh phải in `PSNR 99.0`.

2. Chín player không được tải cùng lúc. **Tạo** `Assets/Scripts/Motion/ClipSlot.cs`: component gắn lên object
   cũ, giữ tên clip, và chỉ gọi `ClipSwap.Replace` ở lần `OnEnable` đầu tiên:

   ```csharp
   public sealed class ClipSlot : MonoBehaviour
   {
       public string ClipName;
       public PatchClipPlayer Player { get; private set; }
       private bool _tried;

       private void OnEnable()
       {
           if (_tried) return;
           _tried = true;
           Player = ClipSwap.Replace(transform, ClipName);
       }
   }
   ```

   `HomeDirector` phải đặt `ClipName` **trước** khi `OnEnable` chạy. Với object đang bật, `AddComponent` gọi
   `OnEnable` ngay lập tức, lúc `ClipName` còn rỗng. Vì vậy cho `ClipSlot` một hàm tĩnh:

   ```csharp
   public static ClipSlot Attach(GameObject target, string clipName)
   {
       ClipSlot slot = target.GetComponent<ClipSlot>();
       if (slot != null) return slot;
       bool wasActive = target.activeSelf;
       target.SetActive(false);                 // so OnEnable waits until ClipName is set
       slot = target.AddComponent<ClipSlot>();
       slot.ClipName = clipName;
       target.SetActive(wasActive);
       return slot;
   }
   ```

3. **Tạo** `Assets/Scripts/Directors/HomeDirector.cs`. Bảng ghép (cha là `Background Sprites`, tên theo
   `docs/scenes/Home.md`):

   | Object | Clip |
   | --- | --- |
   | `morningstudy0000_0` | `home_morning_study` |
   | `MorningStartResting0000_0` | `home_morning_startrest` |
   | `MorningRest0011_0` | `home_morning_rest` |
   | `NoonStudy0000_0` | `home_noon_study` |
   | `NoonStartResting0000_0` | `home_noon_startrest` |
   | `NoonRest0011_0` | `home_noon_rest` |
   | `NightStudy0000_0` | `home_night_study` |
   | `NightStartResting0000_0` | `home_night_startrest` |
   | `NightRest0011_0` | `home_night_rest` |

   Viết bảng này thành một mảng hai cột trong code và lặp, không chép chín đoạn giống nhau.

4. Thời gian chuyển "bắt đầu nghỉ" sang "nghỉ": `PomodoroDayNight.TransitionToResting` đang chờ cứng 2 giây,
   trong khi clip `startrest` dài 11/12 = 0,92 giây. Đổi `new WaitForSeconds(2f)` thành `new WaitForSeconds(0.92f)`.

**Kiểm trên web:**

```
| 51 | Home lúc mới vào | Pompom nghỉ, hình và chuyển động như bản cũ của buổi hiện tại | home_<buổi>_rest |
| 52 | Bấm Start 1 phút | Pompom học như cũ: tia nắng, lá, khói, tay viết đều chuyển động | home_<buổi>_study |
| 53 | Chờ hết phút | Chuyển sang "bắt đầu nghỉ" rồi "nghỉ", không nháy hình | startrest, rest |
| 54 | F3 ở Home sau các bước trên | clips 3 (không phải 9) | ClipSlot tải lười |
| 55 | F3: FPS thấp nhất khi Pompom đang học | Từ 55 trở lên; ghi số vào dev-log cạnh số đo ở M2.4 | hiệu năng |
```

**Xong khi:** năm dòng đạt.

**Bẫy:**

- `PomodoroDayNight.UpdateBackground` tắt hết rồi bật một object: object được bật lần đầu sẽ tải clip ngay lúc đó
  (một lần khựng nhỏ ở lần đầu đổi trạng thái). Chấp nhận ở bước này. Nếu An thấy khựng rõ: trong `HomeDirector`,
  sau khi gắn slot, tạo sẵn player cho ba object của buổi hiện tại trong lúc màn hình còn đen.
- Object trong `Old BGSprites` (`playerLearning`, ...) đang tắt và không ai bật: không đụng.
- Đừng xoá hay đổi tên chín object cũ: `PomodoroDayNight.dayCycles` trỏ tới chúng.
- Sau bước này bộ nhớ **chưa giảm**: scene vẫn kéo theo 144 ảnh cũ qua các Animator đã tắt. Giảm ở M3.6.

## M3.5 Hoà chéo giữa các trạng thái

**PRD:** A3 ("chuyển tiếp mượt, không đổi ảnh nền đột ngột"). **Cần trước:** M3.4.

Hiện đổi trạng thái là tắt object này, bật object kia: hình nhảy trong một khung. Vì nền của các clip cùng buổi
gần như giống nhau, hoà chéo 250 ms là đủ mềm.

**Sửa:** `Home/PomodoroDayNight.cs`, hàm `UpdateBackground`:

1. Xác định object đang bật (`previous`) và object cần bật (`next`). Nếu trùng nhau: thoát.
2. Bật `next`. Lấy `PatchClipPlayer` của nó (`next.GetComponentInChildren<PatchClipPlayer>()`).
3. Nếu `next` không có player (clip hỏng, đang dùng Animator cũ) hoặc không có `previous`: tắt hết các object
   khác như code cũ, xong.
4. Có player: đặt `sortingOrder` của player mới cao hơn player cũ 2 (nền và ô của nó là +0 và +1), `Alpha = 0`,
   rồi `Tween.Value(player, 100, 0f, 1f, 0.25f, EaseType.OutQuad, v => player.Alpha = v, () => previous.SetActive(false))`.
   Xong thì trả `sortingOrder` về giá trị gốc.
5. Clip cũ giữ `Alpha = 1` suốt lúc hoà (xem animation-assets.md mục 8, dòng cuối).

Thêm vào `PatchClipPlayer` một hàm `SetSortingOrder(int order)` đặt cho nền và mọi vùng.

Làm tương tự cho `Forest/MeditationTimer.UpdateBackground`.

**Kiểm trên web:**

```
| 56 | Home: Start, rồi bấm đặt lại ngay | Hình tan mềm từ học sang nghỉ trong khoảng 1/4 giây, không nháy | hoà chéo |
| 57 | Bấm Start và đặt lại thật nhanh 6 lần | Kết thúc ở đúng trạng thái cuối; chỉ một hình hiện; không lỗi đỏ | tween bị huỷ đúng cách |
| 58 | Bật giảm chuyển động (sau khi có M5.2; trước đó bỏ qua dòng này) | Vẫn hoà chéo (alpha được giữ) | Motion.Reduced |
```

**Xong khi:** hai dòng đầu đạt.

**Bẫy:**

- Bấm nhanh: tween hoà của lần trước bị huỷ và `done` không chạy, nên `previous` của lần trước **không bao giờ
  bị tắt**. Trước khi bắt đầu một lần hoà mới, tắt mọi object nền trừ `previous` và `next` hiện tại, và đặt
  `Alpha = 1` cho player của `previous`.
- Kênh tween phải là số riêng (100 trở lên) và owner là player mới, để không đụng kênh `Alpha` chuẩn.
- Đổi buổi trong ngày (sáng sang trưa) cũng đi qua `UpdateBackground`: được hoà 250 ms. M4.8 kéo dài riêng
  trường hợp đó thành 4 giây.

## M3.6 Dọn asset cũ, đo lại

**PRD:** 5.2, B9. **Cần trước:** M3.2 đến M3.5 đều đạt mức **Web**, và **An đồng ý xoá**.

**Làm:**

1. Tạo `ArtSource/` ở gốc project (ngoài `Assets`). Chuyển bằng `git mv` (giữ lịch sử), chỉ các thư mục khung:

   | Từ | Tới |
   | --- | --- |
   | `Assets/Art/House/{Morning,Noon,Night}{Study,Rest}` | `ArtSource/House/...` |
   | `Assets/Art/Forest/Meditation`, `Meditationrest`, `mainwalk1j` | `ArtSource/Forest/...` |

2. Trong `ArtSource/`, xoá mọi file `.meta`, `.anim`, `.controller` (chúng vô nghĩa ngoài `Assets`):
   ```bash
   find ArtSource -name "*.meta" -o -name "*.anim" -o -name "*.controller"
   ```
   Xem danh sách trước, rồi mới xoá bằng `git rm`.
3. Xoá file `.meta` của các thư mục đã chuyển trong `Assets/Art/...` (ví dụ `Assets/Art/House/MorningStudy.meta`).
4. `python3 Tools/gen_meta.py --check` phải xanh. `python3 Tools/scene_inventory.py` rồi xem
   `docs/scenes/Home.md`: các dòng sprite và controller cũ hiện `?` (tham chiếu đã mất). Đó là kết quả đúng.
5. Sửa lệnh trong tài liệu: các lệnh `extract_patches.py` ở M3.2–M3.4 và trong animation-assets.md trỏ sang
   `ArtSource/...`.
6. Thêm `ArtSource/README.md` hai dòng: đây là ảnh gốc để chạy lại `Tools/art/extract_patches.py`, không xoá.

**Kiểm:**

- CI xanh. Trong log build có thể có cảnh báo "missing sprite"/"missing controller": chấp nhận.
- Web: chạy lại các dòng 45–57. Thêm:

```
| 59 | Mở trang với DevTools > Network, tắt cache | Tổng MB tải về; so với số ghi ở dòng 15 | M3.6 |
| 60 | F3 ở Home và ForestSitting | FPS thấp nhất; so với số ở M2.4 | M3.6 |
```

**Xong khi:** game trông như trước, và dev-log có bảng trước/sau của dung lượng tải và FPS.

**Bẫy:**

- Sau bước này **không còn đường lui về Animator cũ**: clip mới hỏng thì object đó trống. Vì thế chỉ làm khi
  M3.2–M3.5 đã ở mức Web.
- Không chuyển `Assets/Art/Forest/forestwalk` (các lớp nền cuộn, vẫn dùng trực tiếp) hay bất kỳ thư mục nào
  ngoài bảng.
- Không chuyển file ảnh đơn lẻ đang là sprite ban đầu của object cũ nếu nó nằm ngoài các thư mục trên.
- CI lấy cache `Library` theo hash của `Assets/**`: lượt đầu sau khi xoá sẽ import lại nhiều, lâu hơn thường lệ.
