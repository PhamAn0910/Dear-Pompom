# Tạo và dùng asset animation

Trang này trả lời: **có hình gì, biến nó thành chuyển động thế nào, tạo hình mới ra sao**, trong điều kiện
không có Unity Editor và người làm là một AI không vẽ tay được. Đọc [no-editor-unity.md](no-editor-unity.md) và
[game-feel.md](game-feel.md) trước.

## 1. Hiện trạng đã đo (2026-11-16)

Toàn bộ art là tranh vẽ tay kiểu bút sáp: nét viền nâu đen, mảng màu có hạt. `Assets/Art` nặng 184 MB.

| Nhóm | Ở đâu | Dạng | Vấn đề |
| --- | --- | --- | --- |
| Pompom ở Nhà | `Art/House/{Morning,Noon,Night}{Study,Rest}/` | 6 thư mục × 24 ảnh JPG 1920×1080, 12 fps | Mỗi khung là cả bức tranh, nhân vật dính liền nền |
| Pompom thiền | `Art/Forest/Meditation`, `Meditationrest` | 2 × 15 ảnh JPG 1920×1080 | Như trên |
| Pompom đi bộ | `Art/Forest/mainwalk1j/` | 8 ảnh PNG 1920×1080 nền trong | Nhân vật chỉ chiếm 6% khung, phần còn lại là pixel rỗng |
| Bản đồ | `Art/Map/` | `map.png` 2000×1160 và 5 lớp toà nhà PNG riêng | Đứng yên hoàn toàn |
| Nhà hát | `Art/Theatre/` | Mèo piano, chó violin: mỗi con một PNG | Không có khung chuyển động |
| Rừng đi bộ | `Art/Forest/forestwalk/` | 13 lớp PNG (cây, cỏ, đất, nắng) | Đã cuộn, chưa có thị sai mượt |
| UI | rải rác | PNG | Dùng được |

Scene Home tham chiếu cả 9 Animator cùng lúc, tức 144 ảnh toàn màn hình nằm trong bộ nhớ: khoảng 150 MB texture
nén (144 × 1,04 MB), bằng đúng trần ngân sách của PRD cho cả một scene, và cũng là 150 MB phải tải về.

Đo bằng `Tools/art/extract_patches.py --verify` trên asset thật:

| Chuỗi ảnh | Khung | Ô đổi nhiều nhất trong một khung | Bộ nhớ chưa nén trước → sau | Sai khác |
| --- | --- | --- | --- | --- |
| House/MorningStudy | 24 | 202 trên 510 ô | 189,8 → 23,9 MB (giảm 87%) | 0 (giống từng pixel) |
| House/MorningRest | 24 | 216 | 189,8 → 23,9 MB (87%) | 0 |
| House/NoonStudy, NoonRest | 24 | 202, 216 | 189,8 → 23,9 MB (87%) | 0 |
| House/NightStudy | 24 | 89 | 189,8 → 15,9 MB (92%) | 0 |
| House/NightRest | 24 | 113 | 189,8 → 15,9 MB (92%) | 0 |
| Forest/Meditation, Meditationrest | 15 | 152, 144 | 118,7 → 23,9 MB (80%) | 0 |
| Forest/mainwalk1j | 8 | 1 (chế độ trim) | 63,3 → 8,0 MB (87%) | 0 |

Thứ thật sự chuyển động trong tranh Nhà buổi sáng: tia nắng, lá cây, khói cốc trà, cánh tay viết. Thân Pompom
gần như đứng yên.

## 2. Bậc thang kỹ thuật: chọn cách rẻ nhất làm được việc

Đi từ trên xuống. Chỉ xuống bậc dưới khi bậc trên không làm được.

| Bậc | Kỹ thuật | Cần vẽ mới? | Dùng cho |
| --- | --- | --- | --- |
| A | **Tween bằng code** trên hình có sẵn (scale, vị trí, alpha, màu) | Không | Nút, panel, thẻ, chuyển cảnh, phản hồi đúng sai |
| B | **Chuyển động trang trí bằng hàm sin** trên hình có sẵn | Không | Lá lay, mây trôi, toà nhà thở, nhạc công nhún |
| C | **Clip ô vá** (`PatchClipPlayer`) từ chuỗi ảnh có sẵn | Không | Toàn bộ animation vẽ tay hiện có của Pompom |
| D | **Hạt từ sprite sinh bằng script** | Không (script vẽ) | Lấp lánh, tim, cánh hoa, vòng thở |
| E | **Cắt lớp từ tranh có sẵn** bằng script | Không | Cánh cối xay, chân dung Pompom cho hòm thư |
| F | **Hình mới từ công cụ sinh ảnh**, có người duyệt | Có | Nhân vật mới, trạng thái mới của Pompom, vật trang trí |

Không dùng: rig xương 2D (`com.unity.2d.animation` cần editor để gắn xương), Animator Controller mới, Timeline,
Sprite Atlas viết tay (tham chiếu GUID trong YAML rất dễ hỏng). `PatchClipPlayer` và tween thay cho tất cả.

Vì sao A và B đứng đầu: PRD mục 9.3 ghi rủi ro cao nhất là khối lượng vẽ. 80% cảm giác "sống" đến từ chuyển
động của thứ đã có, không phải từ hình mới.

## 3. Bậc C: clip ô vá

### 3.1 Ý tưởng

Cắt bức tranh thành lưới ô 64×64. Với mỗi ô, nội dung xuất hiện ở nhiều khung nhất trở thành **nền**. Khung nào
có ô khác nền thì chỉ lưu ô đó. Ô giống hệt nhau chỉ lưu một lần. Kết quả: một ảnh nền, một tấm sheet chứa các ô,
một file JSON nói khung nào vẽ ô nào ở đâu. Vì ảnh gốc là JPG (nén theo khối 8×8 độc lập), vùng không đổi giống
nhau từng bit giữa các khung, nên kết quả **giống ảnh gốc từng pixel**.

Chuỗi ảnh có nền trong (Pompom đi bộ) được xử lý ở chế độ `trim`: không có nền, mỗi khung cắt về khung bao
chung của mọi khung.

### 3.2 Chạy công cụ

```bash
python3 Tools/art/extract_patches.py "ArtSource/House/NightStudy" Assets/Resources/Clips/home_night_study --name home_night_study --fps 12 --verify
```

| Tham số | Ý nghĩa | Mặc định |
| --- | --- | --- |
| `<frames_dir>` | Thư mục chứa các khung, đọc theo thứ tự tên file | bắt buộc |
| `<out_dir>` | Luôn là `Assets/Resources/Clips/<name>/` | bắt buộc |
| `--name` | Tên clip, `chữ_thường_gạch_dưới` | bắt buộc |
| `--fps` | Lấy từ `m_SampleRate` của file `.anim` cũ (mọi clip hiện tại là 12) | 12 |
| `--no-loop` | Clip chạy một lần rồi giữ khung cuối | lặp |
| `--tile` | Cạnh ô, bội số của 8 | 64 |
| `--threshold` | 0 là không mất gì. Chỉ tăng khi ảnh gốc là PNG bị nhiễu | 0 |
| `--verify` | Dựng lại từng khung từ kết quả, in sai khác | tắt |

Phải thấy `verify: worst frame PSNR 99.0 dB, largest single-pixel error 0/255`. Nếu PSNR dưới 45 thì dừng, không
commit, ghi vào bug-log.

Tên clip chuẩn (dùng đúng các tên này, code tìm theo tên):

| Tên clip | Nguồn | Khung dùng | Lặp |
| --- | --- | --- | --- |
| `home_morning_study`, `home_noon_study`, `home_night_study` | `House/<Buổi>Study` | tất cả 24 | có |
| `home_morning_startrest`, `home_noon_startrest`, `home_night_startrest` | `House/<Buổi>Rest` khung 0000–0010 | 11 | không |
| `home_morning_rest`, `home_noon_rest`, `home_night_rest` | `House/<Buổi>Rest` khung 0011–0023 | 13 | có |
| `forest_meditate_start` | `Forest/Meditation` khung 0000–0002 | 3 | không |
| `forest_meditate` | `Forest/Meditation` khung 0003–0014 | 12 | có |
| `forest_meditate_rest` | `Forest/Meditationrest` | 15 | có |
| `forest_walk` | `Forest/mainwalk1j` | 8 | có |

Số khung của từng clip con suy từ `m_StopTime × 12` trong các file `.anim` cũ (ví dụ `morningStartResting.anim`
dừng ở 0,9167 giây, tức 11 khung). **Trước khi tách, mở file `.anim` tương ứng và đếm số dòng `- time:` để xác
nhận**; nếu khác bảng thì tin file `.anim`. Thư mục dùng chung cho hai clip thì chép các khung của từng clip ra
thư mục tạm rồi chạy công cụ trên thư mục tạm.

### 3.3 Định dạng JSON

```json
{"name":"forest_walk","mode":"trim","width":1920,"height":1080,"fps":12.0,"loop":true,
 "background":"","sheets":["forest_walk_sheet0"],
 "regions":[{"x":563,"y":458,"w":268,"h":483}],
 "patches":[{"sheet":0,"x":2,"y":2,"w":268,"h":483}],
 "frames":[{"draw":[{"region":0,"patch":0}]}]}
```

- `regions[i]`: chỗ vẽ trên bức tranh gốc, pixel, **gốc ở góc trên trái, y hướng xuống**.
- `patches[k]`: chỗ nằm trong sheet, cùng quy ước. Mỗi ô có viền 2 pixel lặp mép để lọc ảnh không lem.
- `frames[f].draw`: khung `f` vẽ những ô nào. Vùng không được nhắc tới thì để lộ nền.
- Class đọc file: `DearPompom.Core.PatchClipData` (đã có, có test). Nó lo cả việc **lật trục y** giữa quy ước
  ảnh và quy ước Unity: dùng `RegionCentre` và `PatchTextureRect`, không tự tính lại.

### 3.4 `PatchClipPlayer` (mã mẫu, `Assets/Scripts/Motion/PatchClipPlayer.cs`)

Hợp đồng:

- `PatchClipPlayer.Create(parent, clipName, sortingLayerId, sortingOrder)` tạo object con, tải clip, trả `null`
  nếu thiếu file (đã log). Object con có `localPosition = 0`, `localScale = 1`: nó thừa hưởng vị trí và scale
  0,926 của object cũ.
- Nền là một `SpriteRenderer`; mỗi vùng là một `SpriteRenderer` con, thứ tự vẽ cao hơn nền 1. Tất cả dùng chung
  material mặc định của sprite nên được gộp lệnh vẽ.
- `OnEnable` phát lại từ khung 0 (giống Animator cũ: bật object là chạy lại). `Finished` gọi một lần khi clip
  không lặp chạy hết.
- `Alpha` đặt độ trong của nền và mọi ô, để hoà chéo hai clip.
- Chỉ đổi sprite của những vùng khác với khung trước.

```csharp
using System;
using DearPompom.Core;
using UnityEngine;

namespace DearPompom
{
    /// <summary>Plays a clip made by Tools/art/extract_patches.py. See docs/guides/animation-assets.md.</summary>
    public sealed class PatchClipPlayer : MonoBehaviour
    {
        public const float PixelsPerUnit = 100f;

        public event Action Finished;
        public PatchClipData Data { get; private set; }

        private Texture2D[] _sheets;
        private Sprite[] _sprites;            // one per patch, created the first time it is shown
        private SpriteRenderer _background;
        private SpriteRenderer[] _regions;
        private int[][] _table;
        private int[] _shown;                 // patch index currently shown in each region, -1 = none
        private double _elapsed;
        private int _frame = -1;
        private bool _finished;
        private float _alpha = 1f;

        public static PatchClipPlayer Create(Transform parent, string clipName, int sortingLayerId, int sortingOrder)
        {
            string folder = "Clips/" + clipName + "/";
            TextAsset json = Resources.Load<TextAsset>(folder + clipName);
            if (json == null) { Debug.LogError("[DearPompom] clip json missing: " + folder + clipName); return null; }
            PatchClipData data = JsonUtility.FromJson<PatchClipData>(json.text);
            string problem = data == null ? "not valid JSON" : data.Validate();
            if (problem != null) { Debug.LogError("[DearPompom] clip '" + clipName + "': " + problem); return null; }

            var sheets = new Texture2D[data.sheets.Count];
            for (int i = 0; i < sheets.Length; i++)
            {
                sheets[i] = Resources.Load<Texture2D>(folder + data.sheets[i]);
                if (sheets[i] == null) { Debug.LogError("[DearPompom] sheet missing: " + folder + data.sheets[i]); return null; }
            }
            Sprite background = null;
            if (data.mode == "patch")
            {
                background = Resources.Load<Sprite>(folder + data.background);
                if (background == null) { Debug.LogError("[DearPompom] background missing: " + folder + data.background); return null; }
            }

            var go = new GameObject("Clip " + clipName);
            go.transform.SetParent(parent, false);
            var player = go.AddComponent<PatchClipPlayer>();
            player.Build(data, sheets, background, sortingLayerId, sortingOrder);
            return player;
        }

        private void Build(PatchClipData data, Texture2D[] sheets, Sprite background, int layer, int order)
        {
            Data = data;
            _sheets = sheets;
            _sprites = new Sprite[data.patches.Count];
            _table = data.BuildFrameTable();
            _shown = new int[data.RegionCount];
            _regions = new SpriteRenderer[data.RegionCount];

            if (background != null)
            {
                _background = gameObject.AddComponent<SpriteRenderer>();
                _background.sprite = background;
                _background.sortingLayerID = layer;
                _background.sortingOrder = order;
            }
            for (int r = 0; r < _regions.Length; r++)
            {
                var child = new GameObject("r" + r);
                child.transform.SetParent(transform, false);
                float x, y;
                data.RegionCentre(r, PixelsPerUnit, out x, out y);
                child.transform.localPosition = new Vector3(x, y, 0f);
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sortingLayerID = layer;
                renderer.sortingOrder = order + 1;
                renderer.enabled = false;
                _regions[r] = renderer;
                _shown[r] = -1;
            }
            Restart();
        }

        public float Alpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                var colour = new Color(1f, 1f, 1f, value);
                if (_background != null) _background.color = colour;
                if (_regions != null) for (int r = 0; r < _regions.Length; r++) _regions[r].color = colour;
            }
        }

        public void Restart()
        {
            _elapsed = 0;
            _finished = false;
            Show(0);
        }

        private void OnEnable()
        {
            if (Data != null) Restart(); // same behaviour as the old Animator: enabling replays the clip
        }

        private void Update()
        {
            if (Data == null) return;
            _elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Show(Flipbook.FrameAt(_elapsed, Data.fps, Data.FrameCount, Data.loop));
            if (!_finished && Flipbook.IsFinished(_elapsed, Data.fps, Data.FrameCount, Data.loop))
            {
                _finished = true;
                if (Finished != null) Finished();
            }
        }

        private void Show(int frame)
        {
            if (frame == _frame && frame != 0) return;
            _frame = frame;
            int[] row = _table[frame];
            for (int r = 0; r < row.Length; r++)
            {
                int patch = row[r];
                if (patch == _shown[r]) continue;
                _shown[r] = patch;
                SpriteRenderer renderer = _regions[r];
                if (patch < 0) { renderer.enabled = false; continue; }
                renderer.sprite = SpriteFor(patch);
                renderer.enabled = true;
            }
        }

        private Sprite SpriteFor(int patch)
        {
            if (_sprites[patch] != null) return _sprites[patch];
            Texture2D sheet = _sheets[Data.patches[patch].sheet];
            float x, y, w, h;
            Data.PatchTextureRect(patch, sheet.height, out x, out y, out w, out h);
            // FullRect: without it Unity traces the outline of every patch, which is slow and needs a readable texture.
            Sprite sprite = Sprite.Create(sheet, new Rect(x, y, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            _sprites[patch] = sprite;
            return sprite;
        }

        private void OnDestroy()
        {
            if (_sprites == null) return;
            for (int i = 0; i < _sprites.Length; i++) if (_sprites[i] != null) Destroy(_sprites[i]);
        }
    }
}
```

Điều phải biết khi dùng:

- **`sheet.height` phải là chiều cao thật của file PNG.** Nếu cài đặt import co ảnh lại (`maxTextureSize` nhỏ
  hơn sheet), mọi ô lệch chỗ. `TextureImportRules` (mục 4) đặt `maxTextureSize = 2048`, và công cụ không bao giờ
  tạo sheet lớn hơn 2048.
- Lần đầu mỗi ô được hiện, `Sprite.Create` chạy một lần. Clip Nhà có tới 600 ô: chạy rải trong vòng lặp đầu là
  chấp nhận được. Nếu F3 cho thấy vòng lặp đầu bị giật, thêm hàm `Warm()` tạo sẵn 50 sprite mỗi khung bằng
  coroutine trong lúc màn hình còn đen.
- **Khe hở giữa các ô.** Ô nằm sát nhau là các hình vuông riêng; khi scale 0,926 có thể thấy đường kẻ một pixel
  lộ nền. Nền gần như giống ô nên thường không thấy. Nếu thấy: tăng rect của mỗi sprite thêm 0,5 pixel mỗi phía
  (lấn vào viền lặp mép 2 pixel đã có sẵn), tức `new Rect(x - 0.5f, y - 0.5f, w + 1f, h + 1f)`.
- Không đổi `transform.localScale` của từng ô. Muốn co giãn cả clip thì scale object cha.

### 3.5 Thay Animator cũ mà không sửa scene

`PomodoroDayNight` và `MeditationTimer` bật tắt các object nền bằng `SetActive`. Giữ nguyên logic đó:

```csharp
// trong HomeDirector.Attach: với mỗi object con của "Background Sprites"
Transform old = SceneFind.ByPath(scene, "Background Sprites/NightStudy0000_0");
var oldSprite = old.GetComponent<SpriteRenderer>();
PatchClipPlayer player = PatchClipPlayer.Create(old, "home_night_study", oldSprite.sortingLayerID, oldSprite.sortingOrder);
if (player != null)
{
    old.GetComponent<Animator>().enabled = false;   // only when the new clip really loaded
    oldSprite.enabled = false;
}
```

Clip mới là **con** của object cũ nên được bật tắt cùng nó. Nếu clip tải hỏng, Animator cũ vẫn chạy: không bao
giờ có màn hình trống. Bảng ghép tên object với tên clip nằm trong bước tương ứng của plan.

Hai giai đoạn, tách bằng một lượt chơi thử trên web:

1. **Song song.** Thêm clip mới, tắt component cũ bằng code. Asset cũ vẫn trong build (bộ nhớ chưa giảm).
2. **Dọn.** Sau khi An xác nhận hình giống hệt: chuyển các thư mục khung cũ từ `Assets/Art/...` sang
   `ArtSource/...` ở gốc project (ngoài `Assets`, Unity không import), xoá file `.anim`, `.controller` và `.meta`
   của chúng. Scene còn tham chiếu GUID đã mất: Unity coi là `null`, không lỗi build. Bộ nhớ và dung lượng tải
   chỉ giảm sau giai đoạn này.

`ArtSource/` là nơi giữ ảnh gốc để chạy lại công cụ. Không xoá.

## 4. Cài đặt import: `Assets/Editor/TextureImportRules.cs`

Unity trên CI chạy script này khi import. Đặt trong thư mục `Editor` nên không vào bản build. Không kiểm được
bằng stub: **chỉ CI mới xác nhận nó biên dịch**, và lỗi trong file này làm hỏng cả build.

```csharp
using UnityEditor;
using UnityEngine;

namespace DearPompom.EditorTools
{
    /// <summary>Import settings by folder, because nobody can set them in an Inspector.</summary>
    public sealed class TextureImportRules : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/Resources/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            // Big hand-drawn pictures compress well enough; small white effect sprites band badly, keep them raw.
            importer.textureCompression = path.Contains("/Clips/")
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
```

| Thư mục | Chứa | Nén |
| --- | --- | --- |
| `Assets/Resources/Clips/<clip>/` | Kết quả của `extract_patches.py` | Có |
| `Assets/Resources/Fx/` | Sprite hạt từ `make_fx_sprites.py` | Không |
| `Assets/Resources/Ui/` | Hình UI mới (khung thư, nền panel) | Không |
| `Assets/Resources/Portraits/` | Chân dung nhân vật cho hòm thư | Không |
| `Assets/Resources/Sfx/`, `Music/` | Âm thanh mới | theo [audio.md](../modules/audio.md) |

## 5. Bậc B: chuyển động trang trí

Một component `Ambient` cho mọi chuyển động lặp vô hạn. Tính từ thời gian tuyệt đối (không cộng dồn), nên không
trôi và không phụ thuộc tốc độ khung hình. `Motion.Reduced` thì đứng ở tư thế nghỉ.

| Kiểu | Công thức (`t` = giây, `p` = pha riêng của object) | Số chuẩn |
| --- | --- | --- |
| `Sway` (lá, cây, cờ) | xoay Z = `amount × sin(2π × (t / period + p))` quanh điểm gốc | amount 1,5–3°, period 3–5 s |
| `Bob` (mây, thuyền, thư) | y = nghỉ + `amount × sin(...)` | amount 0,05–0,15 đơn vị, period 4–7 s |
| `Breathe` (Pompom, toà nhà, nút gợi ý) | scale = nghỉ × `(1 + amount × sin(...))` | amount 0,015–0,03, period 4 s |
| `Spin` (cánh cối xay) | xoay Z = `speed × t` | 20–90°/giây theo tiến độ mục tiêu tuần |
| `Drift` (mây) | x = `min + ((t × speed + p × range) mod range)` | 0,1–0,3 đơn vị/giây |
| `Pulse` (ánh đèn, vòng sáng) | alpha = `base + amount × sin(...)` | amount 0,1–0,2, period 2–3 s |

Luật làm cho chuyển động trang trí trông tự nhiên:

- **Mỗi object một pha khác nhau.** `p` lấy từ vị trí: `p = Mathf.Repeat(x × 0.37f + y × 0.73f, 1f)`. Mười cái
  cây lay đồng loạt trông như máy.
- **Chu kỳ lệch nhau**: nhân `period` với `0,85 + 0,3 × p`.
- **Biên độ nhỏ.** Nếu nhìn là thấy ngay nó đang lắc thì đã quá tay. Bắt đầu từ số nhỏ nhất trong bảng.
- **Xoay quanh gốc, không quanh tâm.** Sprite có pivot ở tâm và ta không sửa meta, nên cây xoay quanh tâm sẽ
  trượt chân. Tính bù trong code: với `pivot` là vector từ tâm object tới điểm gốc (ví dụ đáy cây =
  `(0, -cao/2)` theo đơn vị thế giới đã nhân scale),
  `vị trí = nghỉ + pivot − Xoay(góc) × pivot`.

```csharp
// lõi của Ambient.Update cho kiểu Sway
float angle = amount * Mathf.Sin(2f * Mathf.PI * (Time.time / period + phase));
Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
transform.localRotation = _restRotation * rotation;
transform.localPosition = _restPosition + _pivot - rotation * _pivot;
```

Object là UI (`Image` trong canvas, như các toà nhà trên bản đồ) thì cùng công thức, nhưng `_pivot` tính bằng
đơn vị của canvas đó. Canvas của World là World Space nên đơn vị trùng đơn vị thế giới.

Toà nhà sáng lên khi rê chuột (PRD A5): các toà nhà là `Button` nên đã có `ButtonFeel` (phóng ×1,04). Thêm
`Image.color` từ `(0.92, 0.92, 0.92)` lên trắng bằng `Tween.Value` 150 ms, tức bình thường hơi tối, rê vào thì
sáng. Không cần hình mới.

## 6. Bậc D: hạt và hiệu ứng

### 6.1 Sprite hạt

```bash
python3 Tools/art/make_fx_sprites.py Assets/Resources/Fx
```

Sinh `dot`, `sparkle`, `star`, `ring`, `heart`, `petal` (128×128, trắng, viền có hạt bút sáp). Màu đặt lúc chạy
bằng `Image.color`. Kết quả cố định (seed cố định), chạy lại không đổi file. Cần hình mới thì thêm một hàm
`shape_<tên>` vào script, không vẽ tay.

Bảng màu hạt, lấy từ tranh hiện có:

| Dùng cho | Màu |
| --- | --- |
| Lấp lánh, hoàn thành | `#FFE278`, `#FFF4C8` |
| Ấm, Pompom vui | `#FF96AA`, `#FFC85A` |
| Rừng, thiền | `#96D282`, `#BEE6FF` |
| Nhà hát | `#C48AD6`, `#FFE278` |

### 6.2 `FxBurst`: hạt là `Image` trên canvas phủ

Vì mọi canvas của scene là Screen Space Overlay, hạt vẽ bằng `SpriteRenderer` sẽ **nằm sau toàn bộ UI**. Do đó
hạt là `Image` trong canvas `[DearPompom UI]` (thứ tự 500). Không dùng `ParticleSystem`: cấu hình nó bằng code
dài, khó đúng khi không thấy kết quả, và vẫn nằm sau UI.

Hợp đồng `Assets/Scripts/Motion/FxBurst.cs`:

- `Fx.Burst(Vector2 screenPoint, string sprite, int count, Color color, FxStyle style)`.
- `Fx.BurstAt(Transform worldOrUiTarget, ...)` đổi vị trí sang điểm màn hình:
  `RectTransformUtility.WorldToScreenPoint(camera, position)` với object thế giới; với UI trong canvas Overlay,
  `position` của `RectTransform` đã là toạ độ màn hình.
- Pool cố định 40 `Image`, tạo một lần, tắt sẵn. Hết pool thì bỏ qua yêu cầu mới (không tạo thêm).
- Mỗi hạt là một struct: vị trí, vận tốc, trọng lực, xoay, tuổi, tuổi thọ, scale đầu. Một vòng `Update` duy nhất
  cập nhật mảng.
- `Motion.Reduced` thì `Burst` không làm gì.
- `raycastTarget = false` trên mọi `Image` hạt (không chặn bấm).

| `FxStyle` | Số hạt | Vận tốc đầu | Trọng lực | Tuổi thọ | Scale theo tuổi | Alpha theo tuổi |
| --- | --- | --- | --- | --- | --- | --- |
| `Pop` (bấm đúng, ghép thẻ) | 8 | 250–450 px/s, toả tròn | 600 px/s² | 0,5 s | `OutBack` 0 → 1 trong 20% đầu | 1 rồi mờ ở 40% cuối |
| `Rise` (hoàn thành, thư đến) | 14 | 80–160 px/s, hướng lên ±30° | −40 (bay lên) | 1,2 s | 0,6 → 1 | hiện trong 15% đầu, mờ ở 50% cuối |
| `Fall` (cánh hoa, lá) | 10 | 30–80 px/s xuống, lắc ngang theo sin | 60 px/s² | 2 s | giữ nguyên | mờ ở 30% cuối |

Vị trí và vận tốc tính theo pixel của canvas chuẩn 1920×1080. Điểm màn hình đổi sang điểm canvas bằng
`RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out local)`.

### 6.3 Màn mừng hoàn thành (PRD A2)

Một hàm `Celebrate.Play(ActivityType type)` nghe sự kiện `ActivityRecorded` có `completed = true`:

1. Lớp tối mờ 35% hiện trong 200 ms (chặn bấm bên dưới).
2. Pompom xuất hiện giữa màn hình bằng `OutBack` 300 ms. Hình: khung `walking0002` cắt từ clip `forest_walk`
   (PNG nền trong, đã có). Nhún hai lần bằng `Tween.Punch`.
3. `Fx.Burst` kiểu `Rise`, 14 hạt `sparkle` và `star`, màu theo bảng.
4. Một dòng chữ ("25 minutes of focus. Nice work.") hiện bằng fade 200 ms.
5. Sau 1,6 giây hoặc khi bấm bất kỳ đâu: tất cả mờ đi trong 200 ms.

Tổng tối đa 2 giây, bỏ qua được bằng một cú bấm. `Motion.Reduced`: chỉ bước 1, 4, 5.

## 7. Bậc E: cắt lớp từ tranh có sẵn

Khi cần một phần của tranh thành sprite riêng (cánh cối xay để quay, mặt Pompom làm chân dung):

1. Xem ảnh bằng công cụ đọc ảnh để lấy toạ độ khung bao (pixel, gốc trên trái).
2. Cắt bằng Pillow, lưu PNG vào `Assets/Resources/...`, tên chữ thường.
3. Phần bị cắt khỏi ảnh nền cần được lấp: chép vùng lân cận hoặc để nguyên nếu vật mới che đúng chỗ cũ.
4. Xem lại ảnh kết quả **bằng mắt** (đọc file ảnh) trước khi commit.

Việc đã biết trước:

| Cần | Nguồn | Cách |
| --- | --- | --- |
| Chân dung Pompom cho thư | `Art/House/MorningStudy/morningstudy0000.jpg`, vùng mặt khoảng (740, 390)–(1210, 720) | Cắt hình vuông, bo góc bằng mặt nạ alpha mềm, đặt trên nền giấy |
| Pompom đứng (mừng hoàn thành, onboarding) | khung của `forest_walk` | Dùng thẳng sprite từ sheet |
| Cánh cối xay quay | `Art/Map/map_0000_windmill.png` | Cánh và thân dính nhau, nét vẽ tay: tách bằng tay không sạch. Chuyển sang bậc F hoặc cho cả cối xay `Sway` nhẹ và để cánh đứng yên tới khi có hình mới |
| Mây, khói bếp cho bản đồ | chưa có | Vẽ bằng script như sprite hạt (mây = vài hình tròn mềm chồng nhau), tô `#FFFFFF` alpha 70% |

## 8. Bậc F: hình mới

AI viết code không vẽ được tranh bút sáp đồng nhất với tranh có sẵn. Hình mới chỉ đến từ **công cụ sinh ảnh có
ảnh tham chiếu**, và **An duyệt bằng mắt** trước khi vào game. Không tự ý đưa hình sinh ra vào `Assets/`.

Quy trình:

1. **Viết yêu cầu** vào `docs/art-requests.md`: cần hình gì, dùng ở đâu, kích thước, mấy khung, nền trong hay
   không. Mỗi yêu cầu kèm một hoặc hai ảnh tham chiếu từ `Assets/Art` (cùng nhân vật, cùng phong cách).
2. **Sinh ảnh** bằng công cụ đang có trong phiên làm việc (ví dụ công cụ sinh ảnh của Canva nếu được kết nối).
   Mô tả phong cách cố định, dùng nguyên văn:
   *"hand-drawn children's book illustration, wax crayon and coloured pencil texture, dark brown slightly wobbly
   outlines, soft muted pastel palette, flat shading with visible paper grain, no gradients, no 3D, plain white
   background"*, cộng mô tả nhân vật (Pompom: sinh vật lông xù trắng kem, hai búi lông trên đầu, má hồng, mắt
   đen tròn, áo len xanh nhạt).
3. **Xoá nền** bằng công cụ xoá nền, xuất PNG.
4. **Kiểm bằng script**: đúng kích thước, có kênh alpha, viền không còn quầng trắng (đo alpha ở mép), tên file
   đúng quy ước.
5. **Gửi An xem** cạnh ảnh tham chiếu. Chỉ khi An đồng ý mới chép vào `Assets/Resources/` và commit.
6. **Ghi nguồn** vào `Assets/Art/LICENSES.md`: tên file, công cụ, ngày, câu mô tả đã dùng.

Giảm khối lượng vẽ (PRD 9.3):

- Nhân vật mới (thủ thư, người gác rừng, thợ xay) chỉ cần **một chân dung** 512×512 cho thư và hội thoại. Trên
  bản đồ và trong scene, họ không cần xuất hiện toàn thân.
- Trạng thái mới của Pompom ("vui", "vẫy chào", "buồn ngủ") làm bằng **một tư thế tĩnh + chuyển động bậc A, B**
  (nhún, lắc, hạt), không vẽ từng khung. Sáu clip mà A3 yêu cầu được đáp ứng như sau:

| Trạng thái A3 | Nguồn |
| --- | --- |
| Đứng thở | clip `home_*_rest` (có sẵn) |
| Học | clip `home_*_study` (có sẵn) |
| Nghỉ | clip `home_*_startrest` rồi `home_*_rest` (có sẵn) |
| Vui | sprite đứng từ `forest_walk` + `Punch` + hạt `Rise` |
| Buồn ngủ | clip `home_night_rest` (có sẵn) với tốc độ 8 fps |
| Vẫy chào | bậc F: một tư thế giơ tay, xoay ±8° quanh vai bằng `Sway`; trước khi có hình thì dùng "Vui" |

- Chuyển tiếp giữa hai clip (A3 "không đổi ảnh nền đột ngột"): hoà chéo 250 ms bằng `PatchClipPlayer.Alpha`.
  Clip mới nằm trên, alpha 0 → 1; clip cũ giữ alpha 1 bên dưới và tắt khi xong. Không làm mờ clip cũ cùng lúc,
  nếu không giữa chừng sẽ lộ màu nền camera.

## 9. Ngân sách và quy ước

| Hạng mục | Trần |
| --- | --- |
| Texture mỗi scene (nén) | 150 MB theo PRD; mục tiêu thực tế dưới 40 MB |
| Số clip ô vá tải cùng lúc | 4 |
| Kích thước một sheet | 2048×2048 |
| Sprite hạt | 128×128 |
| Chân dung | 512×512 |
| Hạt sống cùng lúc | 40 |
| Tổng `Assets/Resources` | ghi vào dev-log sau mỗi lần thêm; tăng quá 20 MB một bước thì phải giải thích |

Tên file: `chữ_thường_gạch_dưới`, không dấu, không cách. Clip: `<khu>_<buổi nếu có>_<hành động>`. Mỗi clip một
thư mục riêng trong `Resources/Clips/`.

Bản build hiện **không nén** (`webGLCompressionFormat: 2` trong `ProjectSettings/ProjectSettings.asset`), nên
dung lượng tải bằng dung lượng texture. Bước M0.2 của plan bật gzip có kèm bộ giải nén dự phòng, chạy được
trên GitHub Pages.

## 10. Danh sách kiểm khi thêm một asset animation

- [ ] Công cụ in `PSNR 99.0` (clip) hoặc đã xem ảnh bằng mắt (hình cắt, hình sinh).
- [ ] File nằm đúng thư mục trong `Assets/Resources/`, tên chữ thường.
- [ ] Đã chạy `python3 Tools/gen_meta.py`.
- [ ] Code tải bằng `Resources.Load`, có kiểm `null` và log đường dẫn.
- [ ] Có đường lui: thiếu asset thì game vẫn chạy (hình cũ, hoặc bỏ qua hiệu ứng).
- [ ] `Motion.Reduced` được tôn trọng.
- [ ] Đã thêm dòng kiểm vào [web-test.md](../web-test.md) và ghi dung lượng thêm vào [dev-log.md](../dev-log.md).
- [ ] Hình sinh mới: An đã duyệt, đã ghi `Assets/Art/LICENSES.md`.
