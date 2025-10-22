using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimBlock.Core.Application.Services;
using SimBlock.Core.Domain.Entities;
using SimBlock.Tests.Macros;
using Xunit;

namespace SimBlock.Tests.Macros
{
    public class MacroMappingServiceTests
    {
        private static (MacroMappingService svc, FakeMacroServiceForMapping macros, FakeKeyboardHookService k, FakeMouseHookService m) Create()
        {
            var macros = new FakeMacroServiceForMapping();
            var k = new FakeKeyboardHookService();
            var m = new FakeMouseHookService();
            var svc = new MacroMappingService(NullLogger<MacroMappingService>.Instance, macros, k, m);
            return (svc, macros, k, m);
        }

        [Fact]
        public async Task Add_Update_List_Remove_Enable_Clear_Works()
        {
            var (svc, macros, k, m) = Create();
            await svc.ClearAllBindingsAsync();

            var b = new MacroBinding
            {
                MacroName = "A",
                Enabled = true,
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Keyboard, VirtualKeyCode = 0x41, Ctrl = false, Alt = false, Shift = false, OnKeyDown = true }
            };

            (await svc.AddOrUpdateBindingAsync(b)).Should().BeTrue();
            var list = await svc.ListBindingsAsync();
            list.Should().HaveCount(1);

            // Update with same trigger but different macro name
            b.MacroName = "B";
            (await svc.AddOrUpdateBindingAsync(b)).Should().BeTrue();
            (await svc.ListBindingsAsync()).Single().MacroName.Should().Be("B");

            // Toggle enable
            var id = (await svc.ListBindingsAsync()).Single().Id;
            (await svc.EnableBindingAsync(id, false)).Should().BeTrue();
            (await svc.ListBindingsAsync()).Single().Enabled.Should().BeFalse();

            // Remove by id
            (await svc.RemoveBindingAsync(id)).Should().BeTrue();
            (await svc.ListBindingsAsync()).Should().BeEmpty();

            await svc.ClearAllBindingsAsync();
            (await svc.ListBindingsAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task Keyboard_Trigger_Fires_Play_And_Is_Debounced()
        {
            var (svc, macros, k, m) = Create();
            await svc.ClearAllBindingsAsync();

            var macro = new Macro { Name = "KeyMacro" };
            await macros.SaveAsync(macro);

            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = macro.Name,
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Keyboard, VirtualKeyCode = 0x41, OnKeyDown = true },
                Enabled = true
            });

            // Fire first time
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x41, IsKeyDown = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().Be(1);

            // Fire again quickly -> debounced
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x41, IsKeyDown = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().Be(1);

            // After debounce window
            await Task.Delay(220);
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x41, IsKeyDown = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().Be(2);
        }

        [Fact]
        public async Task Mouse_Trigger_Fires_Play()
        {
            var (svc, macros, k, m) = Create();
            await svc.ClearAllBindingsAsync();

            var macro = new Macro { Name = "MouseMacro" };
            await macros.SaveAsync(macro);

            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = macro.Name,
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Mouse, Button = 0, OnButtonDown = true },
                Enabled = true
            });

            m.FireMouse(new MouseHookEventArgs { Message = 0x0201 /* WM_LBUTTONDOWN */, LeftButton = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Disabled_Or_Busy_Mapping_Does_Not_Fire()
        {
            var (svc, macros, k, m) = Create();
            await svc.ClearAllBindingsAsync();
            await macros.SaveAsync(new Macro { Name = "BusyMacro" });

            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = "BusyMacro",
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Keyboard, VirtualKeyCode = 0x42, OnKeyDown = true },
                Enabled = false
            });

            // Disabled -> should not fire
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x42, IsKeyDown = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().Be(0);

            // Enable but mark macro service as busy (playing)
            var id = (await svc.ListBindingsAsync()).Single().Id;
            await svc.EnableBindingAsync(id, true);
            macros.IsPlaying = true;
            k.FireKey(new KeyboardHookEventArgs { VkCode = 0x42, IsKeyDown = true });
            await Task.Delay(60);
            macros.PlayCalls.Should().Be(0);
        }

        [Fact]
        public async Task RemoveBindingsForMacro_Removes_All()
        {
            var (svc, macros, k, m) = Create();
            await svc.ClearAllBindingsAsync();
            await macros.SaveAsync(new Macro { Name = "M1" });
            await macros.SaveAsync(new Macro { Name = "M2" });

            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = "M1",
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Keyboard, VirtualKeyCode = 0x41, OnKeyDown = true },
                Enabled = true
            });
            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = "M1",
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Mouse, Button = 0, OnButtonDown = true },
                Enabled = true
            });
            await svc.AddOrUpdateBindingAsync(new MacroBinding
            {
                MacroName = "M2",
                Trigger = new MacroTrigger { Device = MacroTriggerDevice.Keyboard, VirtualKeyCode = 0x42, OnKeyDown = true },
                Enabled = true
            });

            (await svc.ListBindingsAsync()).Should().HaveCount(3);
            (await svc.RemoveBindingsForMacroAsync("M1")).Should().BeTrue();
            var list = await svc.ListBindingsAsync();
            list.Should().HaveCount(1);
            list.Single().MacroName.Should().Be("M2");
        }
    }
}
