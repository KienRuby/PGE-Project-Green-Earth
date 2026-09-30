# 100 test case kiểm thử game — PGE Project Green Earth

Tài liệu này là **kế hoạch kiểm thử trực tiếp trong game**, chưa phải kết quả kiểm thử. Cài sẵn một bản game có thể chạy trên điện thoại Android hoặc mở game trong Unity Editor, sau đó thực hiện thao tác như người chơi. Không có ca kiểm thử tải mã nguồn, cấu hình Unity hoặc tạo APK/AAB. Mỗi ca cần ghi thêm người chạy, ngày chạy, thiết bị, bản game, trạng thái Pass/Fail/Blocked và bằng chứng.

## Quy ước và chuẩn bị

- **P0**: luồng khởi động, chơi hoặc dữ liệu có thể bị chặn/mất. **P1**: chức năng chính. **P2**: giao diện và trường hợp phụ.
- Ưu tiên chạy trên điện thoại Android API 24 trở lên; có thể chạy các ca không phụ thuộc cảm ứng/dịch vụ di động trong Unity Editor. Ghi phiên bản game và thiết bị vào biên bản.
- Chuẩn bị ba hồ sơ thử: **Mới** (chưa có tiến trình), **Đã mở khóa** (có Chapter/Gem Mine/Tower Def), **Giàu tài nguyên** (có đủ Energy, Data Chips, Red Gems, Gold trong trận). Tạo hồ sơ bằng công cụ kiểm thử hoặc bản sao dữ liệu, không sửa dữ liệu thật của người chơi.
- Các ca cần quảng cáo, Google Play Games, Unity Authentication hoặc Cloud Save chỉ chạy khi dịch vụ và tài khoản thử đã cấu hình; nếu chưa, ghi **Blocked**, không đánh dấu Pass.
- Nhóm A là một luồng chơi nhanh liên tục trên hồ sơ Mới. Các nhóm còn lại chạy độc lập với hồ sơ ghi trong từng ca; đặt lại dữ liệu thử trước các ca liên quan đến tiền, lượt chơi hoặc phần thưởng. Nếu game không khởi động được, ghi **Blocked** và lỗi mở game, không suy đoán kết quả.

## A. Luồng chơi đầu tiên: kiểm thử nhanh (TC-001–010)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-001 | P0 | Mở game lần đầu bằng hồ sơ Mới. | Vào màn chính, không đứng ở màn loading hoặc thoát đột ngột. |
| TC-002 | P0 | Quan sát màn chính sau TC-001. | Thanh tài nguyên, điều hướng dưới và nút vào trận hiển thị, có thể chạm được. |
| TC-003 | P1 | Mở tab Chapter bằng hồ sơ Mới. | Chapter đầu được chọn và có thể chơi; chapter chưa mở hiển thị trạng thái khóa. |
| TC-004 | P0 | Chọn Chapter đầu và bấm Start một lần. | Energy giảm đúng chi phí hiển thị; game chuyển sang trận. |
| TC-005 | P0 | Quan sát HUD ngay khi trận bắt đầu. | Nhân vật, thanh máu, thanh EXP, thông tin wave và nút Pause hiển thị. |
| TC-006 | P0 | Di chuyển nhân vật theo joystick hoặc WASD, sau đó thả điều khiển. | Nhân vật di chuyển và dừng đúng thao tác, không bị kẹt. |
| TC-007 | P0 | Tiếp cận quái trong tầm bắn nhưng không nhấn nút bắn. | Nhân vật tự bắn; quái nhận sát thương. |
| TC-008 | P0 | Nhặt EXP đến khi lên cấp, chọn một thẻ chipset. | Bảng chọn mở, kỹ năng được áp dụng và trận tiếp tục. |
| TC-009 | P0 | Bấm Pause, mở Stats, rồi Resume. | Trận dừng trong Pause, Stats mở đúng; Resume tiếp tục đúng lượt chơi. |
| TC-010 | P0 | Để nhân vật thua, bỏ qua hồi sinh, nhận thưởng và về màn chính. | Màn kết quả xuất hiện; thưởng cộng đúng một lần và màn chính phản ánh số dư mới. |

## B. Màn chính, điều hướng và cài đặt (TC-011–020)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-011 | P0 | Ở màn chính, lần lượt chạm Shop, Lab, Chapter, Chipset, Buddy. | Mỗi tab mở đúng panel; tab được chọn có trạng thái hiển thị tương ứng. |
| TC-012 | P1 | Chạm nhanh qua lại các tab nhiều lần. | Chỉ một panel chính đang hoạt động; giao diện không chồng lên nhau hoặc nhận nhiều lần chạm. |
| TC-013 | P1 | Mở Settings rồi đóng bằng nút đóng. | Settings xuất hiện rồi biến mất; trở lại panel đang xem. |
| TC-014 | P1 | Đổi lần lượt English, Vietnamese, Chinese, Russian trong Settings. | Nhãn hỗ trợ đổi theo ngôn ngữ đã chọn; lựa chọn hiện tại được hiển thị đúng. |
| TC-015 | P1 | Chọn Vietnamese, thoát ứng dụng và mở lại. | Ngôn ngữ đã chọn vẫn được giữ. |
| TC-016 | P1 | Trong Settings, chuyển lần lượt ba chế độ joystick: động, cố định, tắt. | Nút cài đặt phản ánh đúng chế độ; trong trận joystick hoạt động tương ứng. |
| TC-017 | P2 | Bật/tắt hiển thị số sát thương, vào một trận và bắn trúng quái. | Số sát thương chỉ xuất hiện khi tùy chọn được bật. |
| TC-018 | P2 | Bật/tắt rung màn hình, kích hoạt một sự kiện có hiệu ứng rung. | Hiệu ứng rung tuân theo tùy chọn, không thay đổi logic sát thương. |
| TC-019 | P2 | Bật/tắt nhạc và hiệu ứng âm thanh riêng trong Settings. | Nhạc và SFX thay đổi độc lập theo từng tùy chọn. |
| TC-020 | P1 | Thay đổi vài tùy chọn Settings, thoát hẳn rồi mở lại. | Các tùy chọn đã lưu được khôi phục đúng. |

## C. Chọn Chapter và Energy (TC-021–030)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-021 | P0 | Dùng hồ sơ Mới, mở tab Chapter. | Chapter đầu tiên có thể chọn; chapter chưa mở khóa có dấu hiệu khóa. |
| TC-022 | P1 | Chạm mũi tên sang chapter kế tiếp rồi quay lại. | Tên, ảnh, số chapter và trạng thái khóa cập nhật đúng theo chapter đang chọn. |
| TC-023 | P2 | Ở chapter đầu tiên chạm mũi tên lùi; ở chapter cuối chạm mũi tên tiến. | Chỉ số chapter không ra ngoài phạm vi dữ liệu. |
| TC-024 | P0 | Chọn chapter còn khóa và chạm Start. | Không vào GamePlay và không trừ Energy. |
| TC-025 | P1 | Chọn chapter đã mở; so chi phí hiển thị trên Start với cấu hình chapter. | Chi phí Energy trên nút khớp chapter đang chọn. |
| TC-026 | P0 | Đặt Energy đúng bằng chi phí, nhấn Start một lần. | Vào GamePlay; Energy giảm đúng một lần bằng chi phí. |
| TC-027 | P0 | Đặt Energy thấp hơn chi phí một đơn vị, nhấn Start. | Không vào trận; Energy không đổi. |
| TC-028 | P0 | Nhấn Start liên tiếp rất nhanh khi đủ Energy cho đúng một lượt. | Chỉ tạo một lượt chơi và chỉ trừ Energy một lần. |
| TC-029 | P1 | Chọn một chapter đã mở, rời tab rồi trở lại và khởi động lại game. | Chapter đã chọn vẫn được khôi phục từ dữ liệu đã lưu. |
| TC-030 | P0 | Hoàn thành chapter cuối đang mở, trở về màn chính. | Chapter kế tiếp được mở theo tiến trình; các chapter xa hơn vẫn khóa. |

## D. Di chuyển, chiến đấu và vật phẩm rơi (TC-031–040)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-031 | P0 | Trong Editor/Windows, giữ lần lượt W, A, S, D. | Nhân vật di chuyển theo hướng tương ứng, không tự đi khi thả phím. |
| TC-032 | P1 | Trong Editor/Windows, dùng bốn phím mũi tên. | Hướng di chuyển tương đương WASD. |
| TC-033 | P0 | Trên Android, kéo joystick ảo theo bốn hướng rồi thả. | Nhân vật đi theo thao tác và dừng khi thả. |
| TC-034 | P1 | Đổi joystick động/cố định trong Settings, vào trận và kéo ở vùng điều khiển. | Vị trí xuất hiện của joystick đúng chế độ, vẫn điều khiển được. |
| TC-035 | P1 | Điều khiển nhân vật đến sát giới hạn bản đồ. | Nhân vật không đi xuyên ra ngoài vùng chơi. |
| TC-036 | P0 | Đưa nhân vật vào tầm một quái, không nhấn nút bắn. | Vũ khí tự tìm mục tiêu và bắn khi có mục tiêu hợp lệ. |
| TC-037 | P1 | Di chuyển ra khỏi tầm mục tiêu rồi vào lại. | Vũ khí không tiếp tục bắn vào mục tiêu không hợp lệ; trở lại bắn khi đủ điều kiện. |
| TC-038 | P0 | Để quái chạm/tấn công nhân vật. | Máu giảm theo đòn đánh; thanh máu cập nhật, không xuống dưới mức hiển thị hợp lệ. |
| TC-039 | P0 | Đánh bại quái và nhặt vật phẩm EXP rơi ra. | Vật phẩm biến mất khi nhặt; thanh EXP tăng đúng một lần. |
| TC-040 | P1 | Khi nhân vật chưa đầy máu, nhặt hộp hồi máu nếu màn có vật phẩm này. | Máu tăng nhưng không vượt quá máu tối đa. |

## E. Lên cấp, tạm dừng, chết và hồi sinh (TC-041–050)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-041 | P0 | Nhặt đủ EXP để lên một cấp. | Bảng chọn chipset hiện; mô phỏng trận tạm dừng trong lúc chọn. |
| TC-042 | P1 | Mở bảng lên cấp ở hồ sơ thường; lặp lại với hồ sơ có nâng cấp CHIPSET SELECTION kích hoạt thẻ cộng thêm. | Có 3 lựa chọn mặc định, 4 khi hiệu ứng cộng thêm kích hoạt; không có hai thẻ trùng trong cùng lượt. |
| TC-043 | P0 | Chạm một thẻ chipset ở bảng lên cấp. | Bảng đóng, kỹ năng/cấp chipset được áp dụng một lần, trận tiếp tục. |
| TC-044 | P1 | Có ít nhất 20 Red Gems; bấm Draw again một lần. | Trừ 20 Red Gems, tạo bộ lựa chọn mới và đếm một lần đổi. |
| TC-045 | P1 | Bấm Draw again hai lần trong cùng một cấp, thử bấm lần thứ ba. | Lần thứ ba bị chặn; không trừ thêm Red Gems. |
| TC-046 | P1 | Có dưới 20 Red Gems; thử Draw again. | Không đổi thẻ và không làm số Red Gems âm. |
| TC-047 | P0 | Trong trận bấm Pause, đợi vài giây, rồi bấm Resume. | Quái và thời gian trận dừng khi Pause, tiếp tục sau Resume. |
| TC-048 | P1 | Trong Pause, mở lần lượt Stats, Chipset, Artifact. | Đúng nội dung từng tab; đóng Pause không làm mất trạng thái trận. |
| TC-049 | P0 | Để máu về 0; có ít nhất 200 Red Gems; chọn hồi sinh bằng gem. | Trừ đúng 200 Red Gems, nhân vật hồi sinh một lần; lần chết tiếp theo không được hồi sinh lại trong lượt đó. |
| TC-050 | P0 | Để máu về 0 và chọn bỏ qua hồi sinh hoặc chờ hết thời gian. | Vào màn kết quả; chỉ khi nhận thưởng hợp lệ mới cộng phần thưởng, không nhân đôi khi bấm liên tục. |

## F. Daily Gem Mine (TC-051–060)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-051 | P1 | Dùng hồ sơ chưa đạt mốc mở Gem Mine, xem tab Chapter. | Lối vào Gem Mine vẫn khóa/không cho bắt đầu. |
| TC-052 | P1 | Dùng hồ sơ đã mở Gem Mine, mở modal. | Modal hiển thị 5 cấp, lượt vào còn lại và cấp đang chọn. |
| TC-053 | P1 | Dùng tiến trình Gem Mine mới, xem 5 cấp. | Cấp 1 mở; cấp 2–5 khóa. |
| TC-054 | P1 | Chạm cấp đang khóa. | Không thể bắt đầu cấp đó; lựa chọn hợp lệ trước đó không bị thay bằng cấp khóa. |
| TC-055 | P0 | Có 5 lượt vào, chọn cấp 1 và bấm Start một lần. | Vào scene GenMine; lượt vào giảm đúng 1. |
| TC-056 | P0 | Dùng hết 5 lượt vào trong ngày, thử Start lần thứ sáu. | Không vào màn mới và lượt không xuống âm. |
| TC-057 | P1 | Hoàn thành cấp Gem Mine đang mở cao nhất rồi quay lại modal. | Cấp kế tiếp mở; các cấp xa hơn vẫn khóa. |
| TC-058 | P1 | Chọn một cấp đã mở, đóng rồi mở lại modal. | Cấp đã chọn và trạng thái khóa được khôi phục. |
| TC-059 | P1 | Sau khi đã dùng lượt, đóng hẳn game và mở lại cùng ngày. | Số lượt còn lại không tự đặt lại. |
| TC-060 | P1 | Dùng đồng hồ/môi trường thử qua ngày reset hợp lệ, mở lại Gem Mine. | Lượt vào được làm mới một lần cho ngày mới; tiến trình cấp đã mở vẫn còn. |

## G. Tower Def (TC-061–070)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-061 | P1 | Dùng hồ sơ chưa đạt mốc mở Tower Def. | Không thể bắt đầu Tower Def khi còn khóa. |
| TC-062 | P1 | Dùng hồ sơ đã mở Tower Def, xem danh sách màn. | Màn được phép chơi chọn được; màn chưa mở không bắt đầu được. |
| TC-063 | P0 | Bắt đầu màn Tower Def mới. | Scene TowerDef mở, Gold đầu trận là 100 theo cấu hình mặc định. |
| TC-064 | P0 | Chạm ô trống và xây turret khi có 100 Gold. | Turret xuất hiện ở đúng ô; Gold giảm 50. |
| TC-065 | P0 | Chạm ô trống và xây generator khi có ít nhất 60 Gold. | Generator xuất hiện; Gold giảm 60. |
| TC-066 | P1 | Có ít Gold hơn giá công trình, thử xây. | Không tạo công trình; Gold không âm và không bị trừ. |
| TC-067 | P1 | Chạm ô đã có công trình. | Mở thao tác của công trình đã đặt; không xây chồng thêm công trình khác. |
| TC-068 | P1 | Có đủ Gold và nâng cấp một turret/generator. | Cấp hoặc chỉ số công trình tăng; Gold giảm đúng chi phí hiển thị. |
| TC-069 | P1 | Để cổng bị thương, mở bảng cổng và chọn sửa/nâng cấp khi đủ điều kiện. | Chỉ số cổng thay đổi theo thao tác; tài nguyên bị trừ đúng một lần. |
| TC-070 | P0 | Chạy hai lượt: một lượt cổng bị phá, một lượt vượt hết wave. | Lượt đầu báo thua; lượt sau báo thắng và cập nhật tiến trình, không báo cả hai kết quả. |

## H. Lab, Chipset, Buddy và Pet (TC-071–080)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-071 | P1 | Mở Lab, chọn một ô nâng cấp. | Hiển thị chỉ số/cấp hiện tại và chi phí tương ứng. |
| TC-072 | P0 | Có đủ Data Chips, mua một cấp Lab. | Chỉ số/cấp tăng một lần; Data Chips giảm đúng giá. |
| TC-073 | P1 | Có ít Data Chips hơn chi phí Lab, thử mua. | Không nâng cấp và không trừ tài nguyên. |
| TC-074 | P1 | Mở Chipset và chọn một chipset đã sở hữu. | Thẻ và chi tiết chipset khớp vật phẩm được chọn. |
| TC-075 | P0 | Trang bị một chipset vào ô hợp lệ rồi vào trận. | Trạng thái trang bị được lưu; hiệu ứng/chỉ số tương ứng xuất hiện trong trận. |
| TC-076 | P1 | Có đủ nguyên liệu, nâng cấp một chipset. | Cấp chipset tăng; tài nguyên giảm đúng chi phí. |
| TC-077 | P1 | Thiếu nguyên liệu hoặc đã ở giới hạn nâng cấp, thử nâng chipset. | Giao dịch bị chặn; cấp và tài nguyên giữ nguyên. |
| TC-078 | P1 | Mở Buddy, chọn một drone đã sở hữu và trang bị vào ô hợp lệ. | Đội hình cập nhật; drone tương ứng xuất hiện khi vào trận. |
| TC-079 | P1 | Có đủ tài nguyên, nâng cấp một buddy; sau đó thử lại khi thiếu. | Lần đủ tài nguyên thành công; lần thiếu bị chặn và không trừ thêm. |
| TC-080 | P2 | Mở giao diện Pet Craft, chọn pet đủ nguyên liệu rồi thử một pet thiếu nguyên liệu; bấm Craft ở cả hai trường hợp. | Chỉ trường hợp đủ nguyên liệu tạo pet và trừ nguyên liệu; trường hợp thiếu giữ nguyên dữ liệu. |

## I. Shop, đăng nhập hằng ngày và thành tựu (TC-081–090)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-081 | P1 | Mở Shop và chuyển qua các mục hàng/hộp có sẵn. | Tên, ảnh, giá và nút thao tác của mặt hàng đang chọn khớp dữ liệu. |
| TC-082 | P0 | Có đủ tiền hợp lệ, mua một hộp vật phẩm thử. | Tiền trừ một lần; phần thưởng xuất hiện và được ghi vào bộ sưu tập tương ứng. |
| TC-083 | P0 | Có ít tiền hơn giá hộp, thử mua. | Không trừ tiền, không cấp hộp hoặc vật phẩm. |
| TC-084 | P1 | Bấm nút mua/mở hộp liên tiếp thật nhanh. | Chỉ xử lý số giao dịch hợp lệ; không cấp trùng phần thưởng cho cùng một giao dịch. |
| TC-085 | P1 | Mở bảng phần thưởng đăng nhập bằng hồ sơ Mới. | Ngày hiện tại có trạng thái có thể nhận; ngày tương lai bị khóa. |
| TC-086 | P0 | Nhận phần thưởng đăng nhập ngày hiện tại một lần. | Phần thưởng được cộng đúng một lần; ngày chuyển sang đã nhận/chờ. |
| TC-087 | P0 | Bấm nhận lại trong cùng ngày, kể cả sau khi khởi động lại. | Không cấp thêm phần thưởng thường lần thứ hai. |
| TC-088 | P1 | Khi quảng cáo thưởng khả dụng, dùng chức năng nhận lại phần thưởng ngày qua ad. | Chỉ cấp thưởng khi ad hoàn tất hợp lệ; tối đa một lần ad trong ngày. |
| TC-089 | P1 | Thực hiện hành động làm tăng tiến độ một thành tựu. | Thanh/số tiến độ thành tựu cập nhật đúng; chưa đạt mốc thì không thể nhận. |
| TC-090 | P0 | Đạt mốc thành tựu, nhận thưởng rồi bấm nhận lại. | Thưởng cộng một lần; trạng thái thành tựu chuyển sang đã nhận. |

## J. Lưu dữ liệu, dịch vụ ngoài và tương thích (TC-091–100)

| ID | Mức | Điều kiện và thao tác | Kết quả mong đợi |
| --- | --- | --- | --- |
| TC-091 | P0 | Thay đổi Energy/tài nguyên hoặc nâng cấp hợp lệ, đóng hẳn game rồi mở lại. | Tiến trình cục bộ được khôi phục, không mất hoặc nhân đôi tài nguyên. |
| TC-092 | P0 | Chơi khi không có Internet, hoàn thành một thay đổi có lưu dữ liệu. | Vẫn lưu được cục bộ; mất mạng không làm game crash. |
| TC-093 | P1 | Sau TC-092, bật mạng và đăng nhập tài khoản thử đã cấu hình. | Trạng thái đồng bộ cập nhật; tiến trình mới không bị ghi đè âm thầm. |
| TC-094 | P0 | Tạo local save và cloud save khác nhau trên tài khoản thử, kích hoạt đồng bộ. | Luồng xung đột xử lý theo revision; nếu cùng revision nhưng khác nội dung, không ghi đè cả hai âm thầm. |
| TC-095 | P1 | Dùng tài khoản thử đăng nhập Google Play Games khi dịch vụ đã cấu hình. | Đăng nhập thành công hoặc báo lỗi rõ ràng; game vẫn điều khiển được sau lỗi. |
| TC-096 | P1 | Tắt mạng trước khi bấm nút quảng cáo thưởng. | Không cấp thưởng khi chưa xem ad thành công; nút/trạng thái phản ánh quảng cáo không khả dụng. |
| TC-097 | P1 | Trong trận, tạm dừng rồi dùng Home và xác nhận rời trận. | Trở về MainMenu; không còn quái/âm thanh/UI của trận cũ chồng lên màn chính. |
| TC-098 | P1 | Trên Android, bấm Back khi mở Reward popup; sau đó bấm Back một lần ở màn chính. | Back đóng popup trước; lần bấm đầu ở màn chính chưa thoát game và hiển thị hướng dẫn bấm lại để thoát. |
| TC-099 | P2 | Chạy game trên hai màn hình Android có kích thước/tai thỏ khác nhau. | Nút chính, thanh tài nguyên, joystick và modal nằm trong vùng an toàn, có thể chạm được. |
| TC-100 | P0 | Chơi liên tục Chapter → màn kết quả → MainMenu → Gem Mine/Tower Def → MainMenu nhiều lượt. | Không kẹt scene, không nhân đôi HUD/sự kiện, tài nguyên và tiến trình vẫn đúng sau mỗi lượt. |

## Ghi kết quả chạy

Sao chép dòng sau cho mỗi ca: **ID | Phiên bản game | Thiết bị/OS | Ngày | Pass/Fail/Blocked | Kết quả thực tế | Ảnh/log | Bug ID**. Đánh dấu **Blocked** nếu game hoặc dịch vụ cần cho ca đó chưa sẵn sàng; không suy đoán kết quả.
