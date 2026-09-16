---
name: ui
description: Unity UI expert for menus, HUDs, screens, panels, buttons, labels, and all visual interface elements. Routes to UI Toolkit, uGUI, or IMGUI based on project context. Also acts as Design Fidelity gatekeeper: whenever the user provides a visual UI design (screenshot, mockup, image, or screen reference), enforces pixel-faithful implementation (zero deviation, no unsolicited redesign), measurement discipline against project tokens, asset/token reuse, missing-state handling, and Definition-of-Done self-verification.
---

# Unity UI & Design Fidelity Master Skill

Điều phối, xây dựng, chỉnh sửa và chuẩn hóa toàn bộ giao diện người dùng (UI) trong Unity (uGUI, UI Toolkit, IMGUI), đồng thời đảm bảo **kỷ luật thi công thiết kế chuẩn xác tuyệt đối (Pixel-Faithful / Zero Deviation)** khi có ảnh mẫu, mockup hoặc tài liệu tham khảo trực quan.

---

## PHẦN 1: QUY CHUẨN THI CÔNG THEO THIẾT KẾ GỐC (DESIGN FIDELITY)

Khi người dùng cung cấp một giao diện trực quan (ảnh chụp màn hình, mockup, link Figma, hoặc màn hình có sẵn cần dựng lại) — mục tiêu là **độ lệch bằng 0** so với bản gốc, không phải "gần giống". Đây là việc tái tạo chính xác một đặc tả (spec) kỹ thuật, **tuyệt đối không tự ý diễn giải lại theo gu cá nhân hay "sáng tạo thêm"**.

Tuân thủ quy trình 5 bước nghiêm ngặt sau:

### Bước 1 — Phân tích thiết kế trước khi code
- **Xác định lưới & tỉ lệ (Grid & Scale)**: Nhận diện hệ thống spacing (lưới 4pt/8pt, type scale theo tỉ lệ). Nếu đo được các khoảng cách xấp xỉ 4/8/16/24/32px, giả định đó là chủ ý theo grid, không làm tròn tùy tiện theo số đo lệch do nén ảnh.
- **Bóc tách danh sách phần tử**: Component, layout, spacing, typography, màu sắc và giá trị cụ thể của từng phần tử.
- **Chỉ ra điểm chưa rõ**: Phần nào bị cắt, mờ, thiếu số đo hoặc thiếu trạng thái — liệt kê rõ thay vì tự đoán mò.
- **Tái sử dụng component**: Nếu có nhiều màn hình được đưa cùng lúc, xác định phần tử lặp lại (button, header, card, item slot) để dùng chung Prefab/Template thay vì tạo nhiều bản rời rạc.

### Bước 2 — Kiểm kê Asset & Design Token có sẵn trong Project
Với mỗi icon, hình ảnh, font chữ hoặc giá trị style (màu sắc, bo góc, padding):
1. **Tìm trong project trước**: Quét thư mục asset (`Assets/`, sprite atlases, font assets, theme/palette ScriptableObjects, TextMeshPro styles).
2. **Dùng token/asset sẵn có**: Nếu project đã có preset màu, style font, hoặc prefab nút tương ứng — **bắt buộc dùng cái có sẵn**, không hardcode màu hex hay thông số px thô trừ khi project chưa có hệ thống style.
3. **Nếu asset gần khớp**: Tái sử dụng hoặc điều chỉnh (resize, tint màu), không tạo file rác mới.
4. **Nếu chưa chắc chắn**: Liệt kê các asset tìm được và xác nhận với người dùng, không tự chọn bừa.

### Bước 3 — Xử lý các trạng thái không thấy trong thiết kế tĩnh
Thiết kế tĩnh thường chỉ thể hiện 1 trạng thái. Trước khi hoàn thành, tự rà soát và xử lý theo pattern đã có của project:
- **Trạng thái tương tác (Interaction States)**: Normal, Hover, Pressed/Selected, Disabled.
- **Trạng thái dữ liệu (Data States)**: Loading, Empty, Error, Dữ liệu dài/tràn text.
- **Responsive / Co giãn màn hình**: Neo góc (Anchors), Pivot, Content Size Fitter, tỉ lệ khung hình (16:9, 19.5:9 tai thỏ/notch), vùng an toàn (Safe Area).

### Bước 4 — Kỷ luật đo lường khi triển khai
- Dùng đúng đơn vị hệ thống của Unity (RectTransform anchors/offsets, layout groups, hoặc USS flex/px/%).
- **Tuyệt đối KHÔNG tự ý**:
  - Đổi bố cục, khoảng cách, màu sắc, kích thước, vị trí phần tử so với thiết kế gốc.
  - Thêm/bớt hiệu ứng, animation, bo góc, đổ bóng (shadow) nếu bản vẽ không có.
  - Tự áp đặt "best practice cá nhân" làm lệch thiết kế — nếu nghi ngờ bản gốc có điểm bất hợp lý, hãy nêu ra để hỏi, không tự sửa.

### Bước 5 — Tự kiểm tra (Definition of Done)
Trước khi bàn giao, đối chiếu checklist:
- [ ] Layout/spacing/alignment khớp thiết kế gốc ở từng phần tử.
- [ ] Màu sắc và Typography (font, size, weight) dùng đúng token/font asset của dự án, không lệch tông.
- [ ] Asset đặt đúng vị trí, đúng tỉ lệ (Aspect Ratio), đúng pivot.
- [ ] Các trạng thái thiếu (hover/disabled/empty/responsive) được xử lý đồng bộ với toàn app.
- [ ] Không có phần tử nào bị thêm/bớt ngoài ý thiết kế; không có "cải tiến tự phát".

---

## PHẦN 2: ĐIỀU PHỐI HỆ THỐNG UI UNITY (ROUTING LOGIC)

Sau khi nắm rõ yêu cầu thiết kế, xác định hệ thống UI phù hợp trong Unity để triển khai:

### 1. Phân loại theo từ khóa / file trực tiếp:

| Người dùng nhắc đến | Điều phối tới |
|---|---|
| File `.uxml` hoặc `.uss` (kể cả trong `/Editor/`) | `ui-uitk` (UI Toolkit) |
| "UI Toolkit", "UITK", "UIElements", "CreateGUI" | `ui-uitk` |
| Prefab Canvas, `.prefab` UI, `Canvas`, `RectTransform` | `ui-ugui` (uGUI) |
| "IMGUI", "OnGUI", "OnInspectorGUI", "immediate mode" | `ui-imgui` |
| Link Figma (`figma.com/...`), "Figma" | Yêu cầu screenshot/mô tả cụ thể và áp dụng Quy trình Phần 1 |

### 2. Tự động nhận diện từ Project nếu chưa rõ:
- Có file `.uxml`, `.uss` hoặc component `UIDocument` trong scene → **UI Toolkit**.
- Có `Canvas`, `RectTransform`, `CanvasScaler`, `HorizontalLayoutGroup` trong scene/prefab → **uGUI**.
- Script editor có hàm `CreateGUI()` → **UI Toolkit (Editor)**.
- Script editor có hàm `OnGUI()` hoặc `OnInspectorGUI()` → **IMGUI (Legacy Editor)**.

### 3. Nguyên tắc mặc định:
- Dự án đã có sẵn framework nào: Đi theo framework đó để đồng bộ kiến trúc.
- UI Game/Runtime mới tinh: Ưu tiên **uGUI (`ui-ugui`)** do tính phổ biến, tương thích mobile và tài nguyên có sẵn trong project.
- Công cụ Editor mới tinh: Ưu tiên **UI Toolkit (`ui-uitk`)** trừ khi dự án đang chạy thuần IMGUI.

---

## PHẦN 3: KỶ LUẬT PHẠM VI (SCOPE DISCIPLINE) & BÁO CÁO

### Giữ đúng phạm vi yêu cầu:
- Chỉ tạo/sửa giao diện visual. Không tự ý viết thêm gameplay scripts phức tạp gắn vào button nếu người dùng chưa yêu cầu logic cụ thể.
- Đặt tên GameObject / Component / UXML Element rõ ràng, chuẩn theo quy ước dự án hoặc `PascalCase` / `camelCase`.

### Định dạng báo cáo khi hoàn thành:
1. **Những gì đã tái sử dụng**: Liệt kê các Sprite, Font, Color Token, Prefab đã dùng lại từ project.
2. **Những gì mới tạo**: Nêu rõ file/prefab nào vừa tạo mới và lý do.
3. **Các điểm giả định (nếu có)**: Nêu rõ các trạng thái bổ sung (hover, disabled, responsive) để người dùng xác nhận.
4. **Xác nhận checklist Definition of Done** đã đạt yêu cầu.