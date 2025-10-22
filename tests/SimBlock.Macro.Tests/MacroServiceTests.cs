using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimBlock.Core.Application.Services;
using SimBlock.Core.Domain.Entities;
using SimBlock.Tests.Macros;
using Xunit;

namespace SimBlock.Tests.Macros
{
    public class MacroServiceTests
    {
        private static (MacroService svc, FakeKeyboardHookService k, FakeMouseHookService m) Create()
        {
            var k = new FakeKeyboardHookService();
            var m = new FakeMouseHookService();
            var logger = NullLogger<MacroService>.Instance;
            var svc = new MacroService(logger, k, m);
            return (svc, k, m);
        }

        private static string StorageDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimBlock", "Macros");

        private static string MacroPath(string name) => Path.Combine(StorageDir, name + ".json");

        [Fact]
        public async Task Recording_Captures_Keyboard_And_Mouse_Events_With_Timestamps()
        {
            var (svc, k, m) = Create();
            svc.StartRecording("TestRec", MacroRecordingDevices.Both);

            // simulate keyboard down/up with tiny delays to ensure timestamp deltas > 0
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x41, IsKeyDown = true, Ctrl = true });
            await Task.Delay(2);
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x41, IsKeyUp = true, Ctrl = true });
            await Task.Delay(2);
            // simulate mouse move and click
            m.FireMouse(new MouseHookEventArgs { Message = 0x0200 /* WM_MOUSEMOVE */, X = 100, Y = 200 });
            await Task.Delay(2);
            m.FireMouse(new MouseHookEventArgs { Message = 0x0201 /* WM_LBUTTONDOWN */, LeftButton = true, X = 100, Y = 200 });

            var macro = svc.StopRecording();
            macro.Events.Should().HaveCount(4);
            macro.Events.Should().Contain(e => e.Device == MacroEventDevice.Keyboard && e.Type == MacroEventType.KeyDown && e.VirtualKeyCode == 0x41 && e.Ctrl);
            macro.Events.Should().Contain(e => e.Device == MacroEventDevice.Mouse && e.Type == MacroEventType.MouseMove && e.X == 100 && e.Y == 200);
            macro.Events.Max(e => e.TimestampMs).Should().BeGreaterThan(0);
        }

        [Fact]
        public void Recording_Respects_Device_Filter()
        {
            var (svc, k, m) = Create();
            svc.StartRecording("OnlyKeyboard", MacroRecordingDevices.Keyboard);
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x42, IsKeyDown = true });
            m.FireMouse(new MouseHookEventArgs { Message = 0x0200 /* WM_MOUSEMOVE */, X = 1, Y = 2 });
            var macro = svc.StopRecording();
            macro.Events.All(e => e.Device == MacroEventDevice.Keyboard).Should().BeTrue();
        }

        [Fact]
        public async Task Save_Load_List_Delete_Works()
        {
            var name = $"Test_{Guid.NewGuid():N}";
            var (svc, _, __) = Create();
            var macro = new Macro { Name = name };
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Keyboard, Type = MacroEventType.KeyDown, TimestampMs = 0, VirtualKeyCode = 0x41 });

            await svc.SaveAsync(macro);
            File.Exists(MacroPath(name)).Should().BeTrue();

            var list = await svc.ListAsync();
            list.Should().Contain(name);

            var loaded = await svc.LoadAsync(name);
            loaded.Should().NotBeNull();
            loaded!.Name.Should().Be(name);
            loaded.Events.Should().HaveCount(1);

            var deleted = await svc.DeleteAsync(name);
            deleted.Should().BeTrue();
            File.Exists(MacroPath(name)).Should().BeFalse();
        }

        [Fact]
        public void ValidateName_Works()
        {
            var (svc, _, __) = Create();
            svc.ValidateName("", out var err1).Should().BeFalse();
            err1.Should().NotBeNull();
            svc.ValidateName("Valid_Name", out var err2).Should().BeTrue();
            err2.Should().BeNull();
            svc.ValidateName(new string('a', 200), out var err3).Should().BeFalse();
            err3.Should().NotBeNull();
        }

        [Fact]
        public async Task Import_Export_Rename_Exists_ListInfo_Works()
        {
            var (svc, _, __) = Create();
            var name = $"Imp_{Guid.NewGuid():N}";
            var macro = new Macro { Name = name };
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Mouse, Type = MacroEventType.MouseMove, TimestampMs = 10, X = 1, Y = 2 });
            await svc.SaveAsync(macro);

            var tempPath = Path.Combine(Path.GetTempPath(), name + ".json");
            if (File.Exists(tempPath)) File.Delete(tempPath);
            (await svc.ExportAsync(name, tempPath, overwrite: true)).Should().BeTrue();
            File.Exists(tempPath).Should().BeTrue();

            // Import back under same name should fail without overwrite (file already exists)
            (await svc.ImportAsync(tempPath, overwrite: false)).Should().BeFalse();

            var newName = name + "_ren";
            (await svc.RenameAsync(name, newName)).Should().BeTrue();
            (await svc.ExistsAsync(newName)).Should().BeTrue();

            // After rename, original name no longer exists -> overwrite true should succeed
            (await svc.ImportAsync(tempPath, overwrite: true)).Should().BeTrue();

            var infos = await svc.ListInfoAsync();
            infos.Should().NotBeNull();
            infos.Any(i => i.Name == newName || i.Name == name).Should().BeTrue();

            // cleanup
            await svc.DeleteAsync(newName);
            await svc.DeleteAsync(name);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        [Fact]
        public async Task PlayAsync_With_No_Events_Does_Not_Toggle_Blocking()
        {
            var (svc, k, m) = Create();
            var macro = new Macro { Name = "Empty" };
            await svc.PlayAsync(macro);
            k.SetBlockingCalls.Should().Be(0);
            m.SetBlockingCalls.Should().Be(0);
        }
    }
}
