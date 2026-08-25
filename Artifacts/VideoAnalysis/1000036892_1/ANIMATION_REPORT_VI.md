# Báo cáo phân tích UI Level Up / Select Chipset

## 1. Nguồn và dữ liệu trích xuất

- Video: `1000036892(1).mp4`
- Thời lượng: 3,65 giây
- Video gốc: 2712×1220, metadata xoay -90°, khung hiển thị thực tế 1220×2712 (dọc)
- Tốc độ video: khoảng 24,05 fps
- Đã trích 41 frame ở 12 fps vào `frames_12fps/`
- Contact sheet toàn video: `contact_sheet_12fps.jpg`
- Contact sheet riêng vùng particle, có timestamp: `particle_motion_keyframes.jpg`

## 2. Những gì thực sự quan sát được trong video

### Bố cục

1. Gameplay vẫn nhìn thấy phía sau qua một lớp phủ xanh-đen rất tối.
2. Tiêu đề `LEVEL UP!` màu cam/vàng, viền đen dày, nằm giữa vùng trên.
3. Dòng phụ `Select Chipset` màu trắng, viền đen.
4. Danh sách dọc gồm 4 lựa chọn chipset; mỗi thẻ có icon bên trái, tên + cấp độ màu cam, mô tả màu trắng.
5. Ba thẻ đầu dùng nền xanh đậm/viền cyan; thẻ Shotgun dùng nền tím đậm/viền tím. Màu này biểu diễn tier/rarity, không có bằng chứng là trạng thái đang chọn.
6. Có scrollbar mảnh bên phải danh sách.
7. Nút dưới cùng hiển thị biểu tượng tiền đỏ, `x20` và `Draw again`.

### Nội dung 4 chipset trong clip

| Thứ tự | Chipset | Cấp | Mô tả trong video | Asset đã có trong dự án |
|---:|---|---|---|---|
| 1 | Spiky Discus | LV.01 | Spins a spiky Discus to attack an enemy. | `spiky-discus` |
| 2 | Energy Jumper Cables | LV.01 | Stealing life from the enemies. | `energy-jumper-cables` |
| 3 | Big Battery | LV.01 | A part that increases Max HP. | `big-battery` |
| 4 | Shotgun | LV.01 | Deals significant damage to nearby enemies with many shells. | `shotgun` |

Toàn bộ bốn icon và các frame rarity đã có trong `Assets/UI/Chipset/Generated/chipset-atlas.png`, đúng nguồn đang được MainMenu sử dụng.

### Chuyển động đã ghi nhận

- Chuyển động rõ nhất là một lớp particle liên tục gồm bánh răng, đai ốc, ốc vít và chi tiết máy màu vàng/cam/xanh lá.
- Particle tập trung dày quanh/sau tiêu đề, thay đổi vị trí, góc xoay, kích thước và độ trong suốt theo thời gian. Cảm giác tổng thể là bung ra quanh tiêu đề rồi trôi/rơi ra ngoài, đồng thời có hạt mới sinh liên tục.
- Tiêu đề, dòng phụ, bốn card và nút reroll giữ nguyên vị trí/kích thước trong toàn bộ đoạn quay.
- Sai khác pixel trung bình giữa hai frame liên tiếp: vùng particle `15,84`, vùng card `1,37`, vùng button `0,29`, vùng background dưới `0,25`. Vì vậy không nên gán animation pop/slide cho card như một hiệu ứng “đã thấy trong clip”.
- Clip bắt đầu khi popup đã mở hoàn toàn và kết thúc trước thao tác chọn. Video không ghi lại animation mở popup, đóng popup, card được chọn hay reroll.

## 3. Thông số hình ảnh tham chiếu

Các giá trị dưới đây đo xấp xỉ trên khung 1220×2712 và nên triển khai bằng anchor/layout thay vì hardcode pixel:

- Vùng particle/header: từ đỉnh màn hình đến khoảng y=620.
- Card list: x≈105..1115 (rộng khoảng 83% màn hình), bắt đầu y≈615.
- Mỗi card cao khoảng 215–220 px; khoảng cách dọc khoảng 30–35 px.
- Icon chiếm khoảng 20% chiều rộng card.
- Nút reroll: rộng khoảng 50% màn hình, cao khoảng 125 px, nằm giữa phía dưới card list.
- Màu tham chiếu từ video (bị ảnh hưởng bởi nén): title cam `#FFAD1D`, common panel xanh đậm gần `#0B3C56`, viền cyan gần `#73F2EE`, rare panel tím đậm gần `#15005A`, viền tím gần `#8C00FF`.

## 4. Đối chiếu với dự án hiện tại

Luồng level hiện có đã đúng:

`EnemyHealth.Die` → `PlayerLevelController.AddEXP` → `OnLevelUp(newLevel)` → `WaveHUDController.HandleLevelUp`.

Hiện tại `HandleLevelUp` chỉ cập nhật text level và gọi banner `LEVEL UP!`; chưa có màn chọn chipset.

Nguồn chipset MainMenu hiện có:

- `ChipsetController.InitializeDatabase()` tạo catalog 24 chipset.
- `ChipsetController.AllChips` cung cấp danh sách đọc.
- `chipset-atlas.png` chứa icon và frame rarity.
- `ChipsetCardUI` đã render được icon, frame, level, progress và trạng thái tier.
- `ChipManager.TrySpendRedGems(20)` phù hợp với nút `x20 Draw again` trong video.

Khoảng trống chức năng: dự án mới có dữ liệu/menu chipset; chưa thấy hệ thống áp hiệu ứng gameplay tương ứng cho Spiky Discus, Energy Jumper Cables, Big Battery, Shotgun khi người chơi chọn trong run.

## 5. Thiết kế tích hợp đề xuất

### Luồng runtime

1. Khi `OnLevelUp` phát ra, cập nhật HUD level và mở `ChipsetLevelUpPopup`.
2. Tạm dừng simulation bằng `Time.timeScale = 0`, nhưng popup/particle/tween chạy bằng unscaled time.
3. Lấy 4 chipset khác nhau từ catalog dùng chung với MainMenu; không copy lại tên/icon bằng tay.
4. Hiển thị icon/tier frame từ cùng `chipset-atlas.png`.
5. Khi bấm card, áp/chồng cấp chipset cho run hiện tại, đóng popup rồi khôi phục time scale trước đó.
6. `Draw again` trừ 20 Red Gems rồi tạo lại 4 lựa chọn; disable nút khi không đủ tiền.
7. Nếu một lần cộng EXP làm tăng nhiều cấp, xếp các lần chọn vào hàng đợi và mở lần lượt, không mở chồng nhiều popup.

### Phân tách dữ liệu quan trọng

Nên tái sử dụng **catalog, icon và tier** từ MainMenu nhưng tạo bản sao runtime cho mỗi run. Không nên sửa trực tiếp level/count persistent của `ChipItemData` trong MainMenu khi người chơi chọn level-up, trừ khi thiết kế chủ đích biến lựa chọn trong trận thành nâng cấp tài khoản vĩnh viễn.

Catalog 24 chip hiện đang được khởi tạo bên trong `ChipsetController`; để dùng sạch ở cả hai scene, nên tách nó thành `ChipsetCatalog` dùng chung (ScriptableObject hoặc service/factory thuần dữ liệu). MainMenu và Gameplay cùng đọc một nguồn này.

### Hierarchy uGUI đề xuất

```text
GameplayCanvas
└── ChipsetLevelUpPopup (stretch full screen, mặc định inactive)
    ├── Dimmer
    ├── ParticleLayer
    ├── SafeArea
    │   ├── Header
    │   │   ├── LevelUpTitle
    │   │   └── SelectChipsetLabel
    │   ├── ChoicesScrollRect
    │   │   └── Viewport/Content (VerticalLayoutGroup)
    │   │       └── 4 × ChipsetChoiceCard
    │   └── RerollButton
    └── InputBlocker
```

- CanvasScaler giữ chuẩn hiện tại của gameplay; dùng anchor và layout group để chạy được nhiều tỉ lệ màn hình.
- ParticleLayer đặt sau chữ nhưng trước dim gameplay/card.
- Các image trang trí để `raycastTarget = false`; card và reroll là phần tử tương tác.

## 6. Timing đề xuất để hoàn thiện phần video không ghi lại

Đây là timing **đề xuất tích hợp**, không phải chuyển động đã quan sát trong clip:

- 0–100 ms: dimmer alpha 0 → khoảng 0,70.
- 0–220 ms: title scale 0,78 → 1,08 → 1,00, easing Back/Out.
- 80–280 ms: card fade 0 → 1 và trượt lên 20–28 px; stagger 45–60 ms/card.
- Particle: chạy liên tục bằng unscaled time; lifetime khoảng 0,8–1,8 s; random rotation; vàng/cam/xanh lá; phát dày quanh header, không che nội dung card.
- Chọn card: scale 1 → 1,04 → 1 trong 100–140 ms, sau đó fade popup 120–180 ms.
- Không dùng `WaitForSeconds` khi time scale bằng 0; dùng `WaitForSecondsRealtime` hoặc tween unscaled.

## 7. Các file nên thêm/sửa khi triển khai

- Thêm `Assets/Scripts/UI/ChipsetLevelUpPopup.cs`: subscribe event, mở/đóng, reroll và hàng đợi nhiều level.
- Thêm `Assets/Scripts/UI/ChipsetChoiceCardUI.cs`: bản card ngang giống video; không ép `ChipsetCardUI` dạng card dọc của MainMenu làm layout khác nhiệm vụ.
- Thêm nguồn catalog dùng chung, ví dụ `Assets/Scripts/Data/ChipsetCatalog.cs` + asset tương ứng; di chuyển dữ liệu 24 chip ra khỏi UI controller.
- Thêm runtime loadout/effect service để áp lựa chọn vào player trong trận.
- Sửa `WaveHUDController.HandleLevelUp`: chỉ cập nhật level; popup tự xử lý lựa chọn, tránh banner cũ hiển thị chồng.
- Sửa `GamePlayHUDSceneBuilder.cs` để dựng và wire popup bằng đúng atlas MainMenu.
- Thêm test cho: mở popup khi level-up, 4 lựa chọn không trùng, reroll trừ đúng 20 Red Gems, thiếu tiền không reroll, nhiều level-up được queue, time scale được khôi phục.

## 8. Kết luận

Có thể tái tạo gần như toàn bộ visual của clip bằng asset hiện có. Phần thiếu lớn nhất không phải UI mà là catalog dùng chung đúng kiến trúc và logic áp hiệu ứng chipset vào gameplay. Để khớp video, ưu tiên lớp particle cơ khí ở header, overlay tối, 4 card ngang và nút reroll; không cần animation liên tục trên card vì clip không cho thấy điều đó.
