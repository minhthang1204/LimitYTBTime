using System.IO.Pipes;
using System.Text;
using LimitYT.Core.Ipc;

Console.OutputEncoding = Encoding.UTF8;

const string Help = """
    LimitYT — công cụ quản trị (chạy bằng quyền Administrator)

      limityt status                               Đã dùng hôm nay / tuần này, trạng thái chặn
      limityt history [số tuần]                    Lịch sử theo ngày
      limityt set weekly 7h                        Quota tuần
      limityt set daily 90m                        Quota ngày
      limityt set holiday-daily 3h|off             Quota riêng cho ngày nghỉ
      limityt window list
      limityt window add Mon-Fri 07:00-11:00 --school
      limityt window remove <số>
      limityt holiday list
      limityt holiday add 2027-06-01..2027-08-31 Nghỉ hè
      limityt holiday remove <số>
      limityt holiday today on|off|auto            Hôm nay nghỉ học / đi học / theo lịch
      limityt add 30m [lý do]                      Cộng thêm giờ cho hôm nay (-15m để trừ)
      limityt unlock 1h | unlock off               Tạm bỏ mọi chặn (vẫn tính giờ)
      limityt config                               Đường dẫn file cấu hình

    Thời lượng: 7h, 90m, 1h30m. Ngày: Mon Tue Wed Thu Fri Sat Sun, "Mon-Fri", "Sat,Sun", "all".
    """;

if (args.Length > 0 && args[0] is "help" or "-h" or "--help" or "/?")
{
    Console.WriteLine(Help);
    return 0;
}

try
{
    using var pipe = new NamedPipeClientStream(".", PipeNames.Admin, PipeDirection.InOut);
    pipe.Connect(3000);
    using var reader = new StreamReader(pipe, Encoding.UTF8);
    using var writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };

    writer.WriteLine(PipeJson.Serialize(new AdminRequest([.. args])));
    var reply = reader.ReadLine() is { } line ? PipeJson.Deserialize<AdminReply>(line) : null;
    if (reply is null)
    {
        Console.Error.WriteLine("Không nhận được trả lời từ dịch vụ LimitYT.");
        return 1;
    }

    (reply.Ok ? Console.Out : Console.Error).WriteLine(reply.Message);
    if (!reply.Ok && reply.Message.StartsWith("Lệnh không hợp lệ")) Console.WriteLine(Help);
    return reply.Ok ? 0 : 1;
}
catch (UnauthorizedAccessException)
{
    Console.Error.WriteLine("Cần chạy bằng quyền Administrator (chuột phải Terminal → Run as administrator).");
    return 2;
}
catch (TimeoutException)
{
    Console.Error.WriteLine("Không kết nối được dịch vụ LimitYT. Kiểm tra: sc query LimitYT");
    return 3;
}
