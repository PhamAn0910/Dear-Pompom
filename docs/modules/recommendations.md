# Module: bộ gợi ý (thư của Pompom)

- **Vị trí:** `Assets/Scripts/Core/Recommendations/`
- **Test:** `RecommendationEngineTests.cs` (cơ chế), `RecommendationRuleTests.cs` (từng luật, có test B1 và B2)
- **PRD:** 7.3 (R1 một phần, R2, R4, R5, R6), lỗi B1, B2

## Cách hoạt động

1. Sau mỗi hoạt động, lớp Unity dựng `RuleContext` (số liệu `ProgressStats`, giờ hiện tại, bản ghi mới nhất, tiến độ chương).
2. `RecommendationEngine.Evaluate(context)` hỏi từng luật `Strength(context)` (0 đến 1).
3. Điểm của luật = `Weight` x `Strength` x các hệ số phạt. Luật điểm cao nhất thắng. Hoà thì luật đứng trước trong danh sách thắng.
4. Khi thư thật sự được hiện, gọi `MarkShown`. `Evaluate` không đổi trạng thái gì.

| Ràng buộc (R2, R4) | Giá trị | Ở đâu |
| --- | --- | --- |
| Thời gian nghỉ của mỗi luật | mặc định 24 giờ, đặt riêng từng luật | `RecommendationRule.CooldownHours` |
| Không gửi cùng một thư 2 lần liền | luôn bật | `PickMessage` |
| Số thư tối đa mỗi ngày | 3 | `RecommendationEngine.MaxPerDay` |
| Cùng nhóm với thư vừa gửi | điểm x 0,6 | `SameGroupPenalty` |
| Bị bỏ qua 3 lần liền | điểm x 0,5, bấm "Đi ngay" thì hết phạt | `DismissedPenalty` |

Trạng thái (cooldown, thư vừa gửi, số lần bỏ qua) nằm trong `SaveData.recommendations` nên không mất khi tắt game.

## Bộ luật khởi đầu (`DefaultRules.cs`)

| Id | Khi nào | Trọng số | Nghỉ (giờ) | Dẫn tới |
| --- | --- | --- | --- | --- |
| `mood.persistent` | Tâm trạng thấp 3 ngày trong 5 ngày gần nhất | 4 | 48 | không (chỉ khuyên nghỉ và nói chuyện với người thân) |
| `break.pomodoros` | Vừa xong Pomodoro thứ 3 trở lên trong ngày | 3 | 4 | Thiền |
| `welcome.back` | Hoạt động đầu tiên sau 3 ngày vắng trở lên | 2,8 | 72 | không |
| `focus.long` | Từ 90 phút Pomodoro hôm nay, chưa đi bộ hay thiền | 2,6 | 6 | Đi bộ |
| `mood.low` | Tâm trạng gần nhất (trong 24 giờ) là Tired hoặc Sad | 2,5 | 12 | Nhà hát |
| `night.late` | Sau 23 giờ hoặc trước 5 giờ, vừa hoạt động trong 30 phút | 2,2 | 20 | không |
| `balance.braingame` | Vừa chơi lật thẻ hoặc toán trong 60 phút | 2 | 6 | Đi bộ |
| `chapter.near` | Tiến độ chương kế tiếp từ 75% | 1,5 | 24 | không |
| `timer.interrupted` | Vừa dừng sớm Pomodoro hoặc thiền (từ 1 phút) | 1,4 | 12 | không |
| `journal.evening` | 19 đến 23 giờ, hôm nay có hoạt động, chưa viết nhật ký | 1,3 | 20 | Nhật ký |
| `lapsed.<loại>` | Đã từng làm, nay từ 3 ngày chưa làm lại (4 luật) | 1,2 | 72 | hoạt động đó |
| `pomodoro.done` | Vừa xong một Pomodoro trong 10 phút | 1 | 6 | Nhật ký |
| `untried.<loại>` | Chưa từng làm hoạt động đó (7 luật) | 1 | 48 | hoạt động đó |

Luật "gần đạt mục tiêu tuần" trong PRD R5 chưa có, vì mục tiêu tuần (C7) thuộc Giai đoạn 3.

## Hai lỗi của 1.0 và test giữ chúng không quay lại

| Lỗi | Nguyên nhân gốc | Sửa tận gốc bằng | Test |
| --- | --- | --- | --- |
| B1: luôn hiện "You haven't tried VirtualWalk yet!" | Luật tìm chuỗi `VirtualWalk`, `Orchestra`; game ghi `ForestWalk`, `TheatreVisit`. Luật đầu tiên khớp thì `return` luôn | Loại hoạt động là enum; luật chấm điểm thay vì `return` sớm; cooldown và phạt cùng nhóm | `B1_*` (3 test) |
| B2: mọi bài nhật ký bị coi là buồn | Luật đọc `score`, nhật ký luôn ghi `score = 0` | Trường `mood` riêng; `Mood.Productive` có chỉ số 0 nên có test riêng | `B2_*` (4 test) |

## Thêm một luật mới

1. Viết class kế thừa `RecommendationRule` trong `Rules/`. Đặt `Id` duy nhất, `Group` nếu nó nói điều giống luật khác.
2. `Strength` trả 0 khi không áp dụng. Không đọc `DateTime.Now`; dùng `context.Now`.
3. Thêm vào `DefaultRules.Create()`.
4. Thêm test trong `RecommendationRuleTests` với `World` và `FakeClock`.
5. Giọng văn: ấm, ngắn, cụ thể, không trách. Test `DefaultRules_NeverBlameThePlayer` chặn một số cụm từ.

## Lệch so với PRD

- **R1 (luật là ScriptableObject):** điều kiện của luật là code C# trong lõi (để test được không cần Unity);
  trọng số, thời gian nghỉ và lời thư là dữ liệu trong `DefaultRules.cs`. Chưa có asset ScriptableObject vì máy
  không có Unity Editor để tạo và gán. Khi có editor, bọc `Weight`/`CooldownHours`/`Messages` vào một
  ScriptableObject là thay đổi nhỏ, không đụng engine.
- **R3 (giao qua hòm thư):** Giai đoạn 2. Hiện thư vẫn hiện bằng hộp gợi ý cũ.
